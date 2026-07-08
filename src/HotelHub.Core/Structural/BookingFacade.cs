using HotelHub.Behavioral.Observers;
using HotelHub.Behavioral.Pricing;
using HotelHub.Behavioral.States;
using HotelHub.Creational;
using HotelHub.Domain;
using HotelHub.Services;
using HotelHub.Structural.Composite;
using HotelHub.Structural.Decorators;

namespace HotelHub.Structural;

/// <summary>
/// Wzorzec: Facade. Udostępnia warstwie UI wysokopoziomowe operacje rezerwacyjne
/// i ukrywa orkiestrację podsystemów: <see cref="AvailabilityService"/> →
/// <see cref="ReservationBuilder"/> → <see cref="PaymentService"/> → powiadomienia (Observer).
/// UI korzysta wyłącznie z fasady, nigdy z podsystemów bezpośrednio.
/// </summary>
public sealed class BookingFacade : IBookingFacade
{
    /// <summary>
    /// Blokada operacji mutujących — UI webowe (Blazor Server) może wywoływać
    /// modyfikacje współbieżnie z wielu połączeń, a stan jest współdzielony
    /// przez Singleton <see cref="HotelRegistry"/>.
    /// </summary>
    private static readonly object SyncRoot = new();

    private readonly ICurrentUserContext _userContext;
    private readonly HotelRegistry _registry = HotelRegistry.Instance;

    public BookingFacade()
        : this(FixedCurrentUser.SystemContext)
    {
    }

    public BookingFacade(ICurrentUserContext userContext)
    {
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    private ActorContext Actor => _userContext.Actor;
    private readonly AccountService _accountService = new();
    private readonly AvailabilityService _availability = new();
    private readonly PaymentService _payment = new();
    private readonly InvoiceService _invoice = new();
    private readonly PersistenceService _persistence = new();

    private readonly List<IReservationObserver> _observers =
    [
        new GuestNotifier(),
        new ReceptionNotifier(),
        new AuditLogger(),
        new NotificationPublisher()
    ];

    /// <summary>
    /// Rejestruje dodatkowego obserwatora (Observer) obok istniejących
    /// i podpina go do wszystkich obecnych rezerwacji.
    /// </summary>
    public void RegisterObserver(IReservationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        lock (SyncRoot)
        {
            _observers.Add(observer);

            foreach (var reservation in _registry.Reservations)
            {
                reservation.Attach(observer);
            }
        }
    }

    /// <summary>Wszystkie pokoje hotelu.</summary>
    public IReadOnlyCollection<Room> Rooms => _registry.Rooms;

    /// <summary>Wszyscy zarejestrowani goście.</summary>
    public IReadOnlyCollection<Guest> Guests => _registry.Guests;

    /// <summary>Wszystkie rezerwacje w systemie.</summary>
    public IReadOnlyCollection<Reservation> Reservations => _registry.Reservations;

    /// <summary>Korzeń drzewa struktury hotelu (Composite).</summary>
    public HotelBranch? GetHotelStructure() => _registry.HotelStructure;

    /// <summary>
    /// Konfiguruje hotel: tworzy pokoje przez <see cref="RoomFactory"/> (Factory Method),
    /// rejestruje je i buduje drzewo struktury (Composite). Używane przy seedowaniu danych.
    /// </summary>
    public void SetupHotel(string hotelName, params (RoomType Type, int Number)[] rooms)
    {
        foreach (var (type, number) in rooms)
        {
            _registry.AddRoom(RoomFactory.CreateRoom(type, number));
        }

        _registry.SetHotelStructure(HotelBranch.BuildHotel(hotelName, _registry.Rooms));
    }

    /// <summary>
    /// Seeduje przykładowe dane, o ile rejestr jest pusty: 12 pokoi na 3 piętrach
    /// (zróżnicowane typy, ceny i udogodnienia, jeden w remoncie), konta demonstracyjne
    /// (admin + 2 gości) oraz 5 rezerwacji w różnych stanach — każdy ekran aplikacji
    /// ma sensowną treść od pierwszego uruchomienia.
    /// </summary>
    public void SeedSampleData()
    {
        lock (SyncRoot)
        {
            if (_registry.Rooms.Count > 0)
            {
                return;
            }

            SetupHotel("Hotel Pod Różą",
                (RoomType.Standard, 101),
                (RoomType.Standard, 102),
                (RoomType.Deluxe, 103),
                (RoomType.Standard, 104),
                (RoomType.Deluxe, 201),
                (RoomType.Apartment, 202),
                (RoomType.Standard, 203),
                (RoomType.Deluxe, 204),
                (RoomType.Apartment, 301),
                (RoomType.Apartment, 302),
                (RoomType.Deluxe, 303),
                (RoomType.Standard, 304));

            // Zróżnicowanie oferty: apartament premium i pokój ekonomiczny.
            UpdateRoom(301, 750m, 6,
                "Apartament premium na najwyższym piętrze z tarasem i panoramicznym widokiem na miasto.",
                ["Wi-Fi", "Telewizor", "Prywatna łazienka", "Klimatyzacja", "Minibar", "Sejf",
                 "Aneks kuchenny", "Salon", "Taras widokowy", "Ekspres do kawy"]);
            UpdateRoom(304, 180m, 2,
                "Kompaktowy pokój w atrakcyjnej cenie — świetny wybór na krótki pobyt.",
                ["Wi-Fi", "Telewizor", "Prywatna łazienka"]);

            // Pokój w remoncie — niewidoczny w portalu i w dostępności.
            SetRoomOutOfService(104, "Remont łazienki — planowe zakończenie prac za 3 tygodnie");

            var jan = RegisterGuest("Jan", "Kowalski", "jan.kowalski@example.com");
            var anna = RegisterGuest("Anna", "Nowak", "anna.nowak@example.com");

            // Konta demonstracyjne (dane logowania w README i na ekranie logowania).
            _accountService.CreateGuestAccount("jan.kowalski", "Gosc1234!", jan);
            _accountService.CreateGuestAccount("anna.nowak", "Gosc1234!", anna);
            _accountService.CreateReceptionAccount("admin", "admin123");

            var admin = new ActorContext("admin", UserRole.Reception, null);
            var janActor = new ActorContext("jan.kowalski", UserRole.Guest, jan.Id);
            var annaActor = new ActorContext("anna.nowak", UserRole.Guest, anna.Id);
            var today = DateTime.Today;

            Room RoomNo(int number) => _registry.FindRoomByNumber(number)!;

            // 1) Oczekująca — widoczna w kolejce akceptacji recepcji.
            MakeReservation(jan, RoomNo(103),
                new DateRange(today.AddDays(14), today.AddDays(17)), actor: janActor);

            // 2) Potwierdzona — gość może ją opłacić.
            var confirmed = MakeReservation(anna, RoomNo(201),
                new DateRange(today.AddDays(7), today.AddDays(10)),
                extras: [RoomExtra.Breakfast], actor: annaActor);
            confirmed.Confirm(admin);

            // 3) Opłacona z przyjazdem dzisiaj — na pulpicie dnia recepcji.
            var arrivingToday = MakeReservation(jan, RoomNo(102),
                new DateRange(today, today.AddDays(3)),
                extras: [RoomExtra.Breakfast, RoomExtra.Parking], actor: janActor);
            arrivingToday.Confirm(admin);
            arrivingToday.Pay(janActor);

            // 4) Odrzucona z powodem — widocznym u gościa.
            var rejected = MakeReservation(anna, RoomNo(301),
                new DateRange(today.AddDays(20), today.AddDays(23)),
                promoCode: "PROMO20", actor: annaActor);
            rejected.Reject("W tym terminie apartament jest niedostępny z powodu rezerwacji grupowej.", admin);

            // 5) Zakończona — zasila raport przychodów.
            var completed = MakeReservation(anna, RoomNo(204),
                new DateRange(today, today.AddDays(1)), actor: annaActor);
            completed.Confirm(admin);
            completed.Pay(annaActor);
            completed.CheckIn(admin);
            completed.CheckOut(admin);
        }
    }

    public Guest? FindGuestByEmail(string email) => _registry.FindGuestByEmail(email);

    public Guest? FindGuestById(Guid guestId) => _registry.FindGuestById(guestId);

    public UserAccount? FindAccountByLogin(string login) => _registry.FindAccountByLogin(login);

    /// <summary>
    /// Samodzielna rejestracja konta gościa: tworzy jednocześnie encję
    /// <see cref="Guest"/> i powiązane <see cref="UserAccount"/> (rola Guest).
    /// Konta recepcji powstają wyłącznie z seeda — nigdy z UI.
    /// </summary>
    public OperationResult RegisterGuestAccount(
        string login, string password, string firstName, string lastName, string email)
    {
        lock (SyncRoot)
        {
            try
            {
                UserAccount.ValidateLogin(login);
                UserAccount.ValidatePassword(password);

                if (_registry.FindAccountByLogin(login) is not null)
                {
                    return OperationResult.Fail($"Login '{login.Trim()}' jest już zajęty.");
                }

                var guest = new Guest(firstName, lastName, email);
                _registry.AddGuest(guest);
                _accountService.CreateGuestAccount(login, password, guest);
                return OperationResult.Ok($"Utworzono konto '{login.Trim()}'.");
            }
            catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
            {
                return OperationResult.Fail(exception.Message);
            }
        }
    }

    /// <summary>Weryfikuje login i hasło; zwraca konto przy powodzeniu, inaczej null.</summary>
    public UserAccount? VerifyCredentials(string login, string password)
    {
        lock (SyncRoot)
        {
            return _accountService.VerifyCredentials(login, password);
        }
    }

    /// <summary>Zmienia hasło konta po weryfikacji dotychczasowego.</summary>
    public OperationResult ChangePassword(string login, string currentPassword, string newPassword)
    {
        lock (SyncRoot)
        {
            return _accountService.ChangePassword(login, currentPassword, newPassword);
        }
    }

    /// <summary>Aktualizuje dane profilu gościa (portal gościa i panel recepcji).</summary>
    public OperationResult UpdateGuestProfile(Guid guestId, string firstName, string lastName, string email)
    {
        lock (SyncRoot)
        {
            var guest = _registry.FindGuestById(guestId);

            if (guest is null)
            {
                return OperationResult.Fail("Nie znaleziono gościa o podanym identyfikatorze.");
            }

            var other = _registry.FindGuestByEmail(email);

            if (other is not null && other.Id != guestId)
            {
                return OperationResult.Fail($"Adres e-mail {email?.Trim()} jest już używany przez innego gościa.");
            }

            try
            {
                guest.UpdateProfile(firstName, lastName, email);
                return OperationResult.Ok("Zapisano zmiany profilu.");
            }
            catch (ArgumentException exception)
            {
                return OperationResult.Fail(exception.Message);
            }
        }
    }

    public Reservation? FindReservationByShortId(string shortId) =>
        _registry.FindReservationByShortId(shortId);

    public Room? FindRoomByNumber(int number) => _registry.FindRoomByNumber(number);

    public Reservation? FindReservationByNumber(string reservationNumber) =>
        _registry.FindReservationByNumber(reservationNumber);

    /// <summary>Rezerwacje wskazanego gościa, od najnowszych.</summary>
    public IReadOnlyList<Reservation> GetReservationsForGuest(Guid guestId) =>
        _registry.Reservations
            .Where(r => r.Guest.Id == guestId)
            .OrderByDescending(r => r.CreatedAt)
            .ToList();

    /// <summary>Rejestruje nowego gościa w systemie.</summary>
    public Guest RegisterGuest(string firstName, string lastName, string email)
    {
        lock (SyncRoot)
        {
            var guest = new Guest(firstName, lastName, email);
            _registry.AddGuest(guest);
            return guest;
        }
    }

    /// <summary>Zwraca pokoje wolne w zadanym terminie.</summary>
    public IReadOnlyList<Room> GetAvailableRooms(DateRange stay) =>
        _availability.GetAvailableRooms(stay);

    /// <summary>Dni zajęte pokoju we wskazanym miesiącu (kalendarz zajętości).</summary>
    public IReadOnlyCollection<DateTime> GetOccupiedDays(int roomNumber, int year, int month) =>
        _availability.GetOccupiedDays(roomNumber, year, month);

    // --- Powiadomienia (Observer → NotificationCenter) ---

    /// <summary>Powiadomienia wykonującego: gość — własne, recepcja — kanał recepcji.</summary>
    public IReadOnlyList<Notification> GetNotifications(ActorContext? actor = null)
    {
        var context = actor ?? ActorContext.System;

        if (context.CanActAsReception)
        {
            return NotificationCenter.Instance.GetForReception();
        }

        return context.GuestId is { } guestId
            ? NotificationCenter.Instance.GetForGuest(guestId)
            : [];
    }

    /// <summary>Liczba nieprzeczytanych powiadomień wykonującego (dzwoneczek).</summary>
    public int GetUnreadNotificationCount(ActorContext? actor = null)
    {
        var context = actor ?? ActorContext.System;

        if (context.CanActAsReception)
        {
            return NotificationCenter.Instance.UnreadCountForReception();
        }

        return context.GuestId is { } guestId
            ? NotificationCenter.Instance.UnreadCountForGuest(guestId)
            : 0;
    }

    /// <summary>Oznacza wszystkie powiadomienia wykonującego jako przeczytane.</summary>
    public void MarkNotificationsRead(ActorContext? actor = null)
    {
        var context = actor ?? ActorContext.System;

        if (context.CanActAsReception)
        {
            NotificationCenter.Instance.MarkAllReadForReception();
        }
        else if (context.GuestId is { } guestId)
        {
            NotificationCenter.Instance.MarkAllReadForGuest(guestId);
        }
    }

    // --- Zarządzanie pokojami (panel recepcji) ---

    /// <summary>Dodaje pokój wskazanego typu (Factory Method) i przebudowuje drzewo hotelu.</summary>
    public OperationResult AddRoom(RoomType type, int number)
    {
        lock (SyncRoot)
        {
            try
            {
                _registry.AddRoom(RoomFactory.CreateRoom(type, number));
                RebuildHotelStructure();
                return OperationResult.Ok($"Dodano pokój {number}.");
            }
            catch (Exception exception) when (
                exception is ArgumentException or ArgumentOutOfRangeException
                    or InvalidOperationException or NotSupportedException)
            {
                return OperationResult.Fail(exception.Message);
            }
        }
    }

    /// <summary>Aktualizuje parametry pokoju (cena bazowa, pojemność, opis, udogodnienia).</summary>
    public OperationResult UpdateRoom(
        int number, decimal pricePerNight, int capacity, string description, IEnumerable<string> amenities)
    {
        lock (SyncRoot)
        {
            var room = _registry.FindRoomByNumber(number);

            if (room is null)
            {
                return OperationResult.Fail($"Nie znaleziono pokoju o numerze {number}.");
            }

            try
            {
                room.UpdateDetails(new Money(pricePerNight), capacity, description, amenities);
                return OperationResult.Ok($"Zapisano zmiany pokoju {number}.");
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                return OperationResult.Fail(exception.Message);
            }
        }
    }

    /// <summary>
    /// Wyłącza pokój z użytku (remont) — pokój znika z portalu i z dostępności.
    /// Nie można wyłączyć pokoju z aktywnymi rezerwacjami (Opłacona/Zameldowana).
    /// </summary>
    public OperationResult SetRoomOutOfService(int number, string reason)
    {
        lock (SyncRoot)
        {
            var room = _registry.FindRoomByNumber(number);

            if (room is null)
            {
                return OperationResult.Fail($"Nie znaleziono pokoju o numerze {number}.");
            }

            var activeReservation = _registry.Reservations.FirstOrDefault(r =>
                r.BaseRoom.Number == number &&
                r.State is Behavioral.States.PaidState or Behavioral.States.CheckedInState);

            if (activeReservation is not null)
            {
                return OperationResult.Fail(
                    $"Nie można wyłączyć pokoju {number} — ma aktywną rezerwację " +
                    $"{activeReservation.ReservationNumber} ({activeReservation.State.Name}).");
            }

            try
            {
                room.SetOutOfService(reason);
                return OperationResult.Ok($"Pokój {number} został wyłączony z użytku.");
            }
            catch (ArgumentException exception)
            {
                return OperationResult.Fail(exception.Message);
            }
        }
    }

    /// <summary>Przywraca pokój do użytku.</summary>
    public OperationResult ReturnRoomToService(int number)
    {
        lock (SyncRoot)
        {
            var room = _registry.FindRoomByNumber(number);

            if (room is null)
            {
                return OperationResult.Fail($"Nie znaleziono pokoju o numerze {number}.");
            }

            room.ReturnToService();
            return OperationResult.Ok($"Pokój {number} został przywrócony do użytku.");
        }
    }

    private void RebuildHotelStructure() =>
        _registry.SetHotelStructure(HotelBranch.BuildHotel(
            _registry.HotelStructure?.Name ?? "HotelHub", _registry.Rooms));

    /// <summary>
    /// Wylicza wycenę pobytu bez tworzenia rezerwacji — na potrzeby podglądu
    /// „na żywo" w kreatorze: pokój opakowany dekoratorami usług (Decorator)
    /// i taryfa dobrana strategią cenową (Strategy, kod PROMO20 nadpisuje).
    /// </summary>
    public ReservationQuote CalculateQuote(
        Room room, DateRange stay, IEnumerable<RoomExtra>? extras = null, string? promoCode = null)
    {
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(stay);

        var decoratedRoom = ApplyExtras(room, extras);
        var pricing = PricingSelector.Select(stay, promoCode);

        return new ReservationQuote(
            decoratedRoom.GetDescription(),
            decoratedRoom.GetPrice(),
            pricing.Name,
            pricing.Calculate(decoratedRoom.GetPrice(), stay));
    }

    /// <summary>
    /// Tworzy rezerwację: sprawdza dostępność pokoju, dobiera strategię cenową
    /// (Strategy — automatycznie z dat, kod PROMO20 nadpisuje), opakowuje pokój
    /// usługami dodatkowymi (Decorator), buduje rezerwację przez
    /// <see cref="ReservationBuilder"/> i podpina obserwatorów.
    /// Przy <paramref name="autoConfirm"/> od razu ją potwierdza.
    /// </summary>
    public Reservation MakeReservation(
        Guest guest, Room room, DateRange stay, string? promoCode = null,
        IEnumerable<RoomExtra>? extras = null, ActorContext? actor = null)
    {
        ArgumentNullException.ThrowIfNull(guest);
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(stay);

        lock (SyncRoot)
        {
            if (!_availability.IsRoomAvailable(room.Number, stay))
            {
                throw new InvalidOperationException(
                    $"Pokój {room.Number} jest zajęty w terminie {stay}.");
            }

            var pricing = PricingSelector.Select(stay, promoCode);

            var reservation = new ReservationBuilder()
                .ForGuest(guest)
                .WithRoom(ApplyExtras(room, extras))
                .Between(stay)
                .WithPricing(pricing)
                .Build();

            _registry.AddReservation(reservation);
            AttachObservers(reservation);
            reservation.AnnounceCreation(actor ?? ActorContext.System);
            return reservation;
        }
    }

    /// <summary>Opakowuje pokój dekoratorami wskazanych usług dodatkowych.</summary>
    private static IRoom ApplyExtras(Room room, IEnumerable<RoomExtra>? extras)
    {
        IRoom decorated = room;

        foreach (var extra in (extras ?? []).Distinct())
        {
            decorated = RoomExtraDecorator.Apply(decorated, extra);
        }

        return decorated;
    }

    /// <summary>Dodaje usługę dodatkową (Decorator) do rezerwacji i przelicza cenę.</summary>
    public OperationResult AddExtraToReservation(Reservation reservation, RoomExtra extra, ActorContext? actor = null)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        lock (SyncRoot)
        {
            if (!reservation.CanModifyExtras)
            {
                return OperationResult.Fail(
                    $"Nie można dodać usług do rezerwacji w stanie: {reservation.State.Name}.");
            }

            try
            {
                reservation.AddExtra(extra);
                return OperationResult.Ok($"Dodano usługę. Nowa cena rezerwacji: {reservation.TotalPrice}.");
            }
            catch (InvalidOperationException exception)
            {
                return OperationResult.Fail(exception.Message);
            }
        }
    }

    /// <summary>
    /// Opłaca rezerwację: pobiera płatność (symulacja) i przechodzi do stanu Opłacona.
    /// Operację niedozwoloną (stan/rola/właściciel) odrzuca wzorzec State z komunikatem.
    /// </summary>
    public OperationResult PayReservation(Reservation reservation, ActorContext? actor = null)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        lock (SyncRoot)
        {
            var context = actor ?? ActorContext.System;

            if (!reservation.CanPay(context))
            {
                return reservation.Pay(context);
            }

            if (!_payment.ProcessPayment(reservation))
            {
                return OperationResult.Fail("Płatność została odrzucona.");
            }

            return reservation.Pay(context);
        }
    }

    /// <summary>Potwierdza rezerwację (tylko recepcja, ze stanu Oczekująca).</summary>
    public OperationResult ConfirmReservation(Reservation reservation, ActorContext? actor = null)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        lock (SyncRoot)
        {
            return reservation.Confirm(actor ?? ActorContext.System);
        }
    }

    /// <summary>Odrzuca rezerwację z obowiązkowym powodem (tylko recepcja, ze stanu Oczekująca).</summary>
    public OperationResult RejectReservation(Reservation reservation, string reason, ActorContext? actor = null)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        lock (SyncRoot)
        {
            return reservation.Reject(reason, actor ?? ActorContext.System);
        }
    }

    /// <summary>Anuluje rezerwację (gość-właściciel w Oczekująca/Potwierdzona; recepcja także w Opłacona).</summary>
    public OperationResult CancelReservation(Reservation reservation, ActorContext? actor = null)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        lock (SyncRoot)
        {
            return reservation.Cancel(actor ?? ActorContext.System);
        }
    }

    /// <summary>Melduje gościa (tylko recepcja, wymaga opłaconej rezerwacji).</summary>
    public OperationResult CheckIn(Reservation reservation, ActorContext? actor = null)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        lock (SyncRoot)
        {
            return reservation.CheckIn(actor ?? ActorContext.System);
        }
    }

    /// <summary>Wymeldowuje gościa i kończy rezerwację (tylko recepcja).</summary>
    public OperationResult CheckOut(Reservation reservation, ActorContext? actor = null)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        lock (SyncRoot)
        {
            return reservation.CheckOut(actor ?? ActorContext.System);
        }
    }

    /// <summary>Generuje tekstowe potwierdzenie rezerwacji.</summary>
    public string GetInvoice(Reservation reservation) => _invoice.GenerateInvoice(reservation);

    /// <summary>Zapisuje stan aplikacji do pliku JSON.</summary>
    public void SaveData()
    {
        lock (SyncRoot)
        {
            _persistence.Save();
        }
    }

    /// <summary>Wczytuje stan aplikacji z pliku JSON i ponownie podpina obserwatorów.</summary>
    public bool LoadData()
    {
        lock (SyncRoot)
        {
            if (!_persistence.Load())
            {
                return false;
            }

            foreach (var reservation in _registry.Reservations)
            {
                AttachObservers(reservation);
            }

            return true;
        }
    }

    private void AttachObservers(Reservation reservation)
    {
        foreach (var observer in _observers)
        {
            reservation.Attach(observer);
        }
    }

    // --- Jawna implementacja IBookingFacade — kontekst wykonującego z ICurrentUserContext ---

    BookingResult IBookingFacade.MakeReservation(
        Room room, DateRange stay, string? promoCode, IEnumerable<RoomExtra>? extras)
    {
        if (Actor.GuestId is not { } guestId || _registry.FindGuestById(guestId) is not { } guest)
        {
            return BookingResult.Fail("Rezerwację może utworzyć wyłącznie zalogowany gość.");
        }

        try
        {
            return BookingResult.Ok(MakeReservation(guest, room, stay, promoCode, extras, Actor));
        }
        catch (InvalidOperationException exception)
        {
            return BookingResult.Fail(exception.Message);
        }
    }

    OperationResult IBookingFacade.AddExtraToReservation(Reservation reservation, RoomExtra extra) =>
        AddExtraToReservation(reservation, extra, Actor);

    OperationResult IBookingFacade.ConfirmReservation(Reservation reservation) =>
        ConfirmReservation(reservation, Actor);

    OperationResult IBookingFacade.RejectReservation(Reservation reservation, string reason) =>
        RejectReservation(reservation, reason, Actor);

    OperationResult IBookingFacade.PayReservation(Reservation reservation) =>
        PayReservation(reservation, Actor);

    OperationResult IBookingFacade.CancelReservation(Reservation reservation) =>
        CancelReservation(reservation, Actor);

    OperationResult IBookingFacade.CheckIn(Reservation reservation) => CheckIn(reservation, Actor);

    OperationResult IBookingFacade.CheckOut(Reservation reservation) => CheckOut(reservation, Actor);

    OperationResult IBookingFacade.SaveData()
    {
        try
        {
            SaveData();
            return OperationResult.Ok("Dane zostały zapisane do pliku JSON.");
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return OperationResult.Fail($"Nie udało się zapisać danych: {exception.Message}");
        }
    }

    OperationResult IBookingFacade.LoadData() =>
        LoadData()
            ? OperationResult.Ok("Dane zostały wczytane z pliku JSON.")
            : OperationResult.Fail("Nie udało się wczytać danych — brak pliku lub plik jest uszkodzony.");

    IReadOnlyList<Notification> IBookingFacade.GetMyNotifications() => GetNotifications(Actor);

    int IBookingFacade.GetUnreadNotificationCount() => GetUnreadNotificationCount(Actor);

    void IBookingFacade.MarkNotificationsRead() => MarkNotificationsRead(Actor);
}
