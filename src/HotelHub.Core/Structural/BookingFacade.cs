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
public sealed class BookingFacade
{
    /// <summary>
    /// Blokada operacji mutujących — UI webowe (Blazor Server) może wywoływać
    /// modyfikacje współbieżnie z wielu połączeń, a stan jest współdzielony
    /// przez Singleton <see cref="HotelRegistry"/>.
    /// </summary>
    private static readonly object SyncRoot = new();

    private readonly HotelRegistry _registry = HotelRegistry.Instance;
    private readonly AccountService _accountService = new();
    private readonly AvailabilityService _availability = new();
    private readonly PaymentService _payment = new();
    private readonly InvoiceService _invoice = new();
    private readonly PersistenceService _persistence = new();

    private readonly List<IReservationObserver> _observers =
    [
        new GuestNotifier(),
        new ReceptionNotifier(),
        new AuditLogger()
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
    /// Seeduje przykładowe dane (1 hotel, 2 piętra, 8 pokoi mieszanych typów, 2 gości),
    /// o ile rejestr jest pusty — aplikację można demonstrować od razu po uruchomieniu.
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
                (RoomType.Apartment, 204));

            var jan = RegisterGuest("Jan", "Kowalski", "jan.kowalski@example.com");
            var anna = RegisterGuest("Anna", "Nowak", "anna.nowak@example.com");

            // Konta demonstracyjne (dane logowania w README i na ekranie logowania).
            _accountService.CreateGuestAccount("jan.kowalski", "Gosc1234!", jan);
            _accountService.CreateGuestAccount("anna.nowak", "Gosc1234!", anna);
            _accountService.CreateReceptionAccount("admin", "admin123");
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
    public bool AddExtraToReservation(Reservation reservation, RoomExtra extra)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        lock (SyncRoot)
        {
            if (!reservation.CanModifyExtras)
            {
                Console.WriteLine($"Nie można dodać usług do rezerwacji w stanie: {reservation.State.Name}.");
                return false;
            }

            try
            {
                reservation.AddExtra(extra);
                return true;
            }
            catch (InvalidOperationException exception)
            {
                Console.WriteLine(exception.Message);
                return false;
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
}
