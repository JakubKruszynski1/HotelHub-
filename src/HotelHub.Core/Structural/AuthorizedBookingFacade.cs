using HotelHub.Behavioral.Observers;
using HotelHub.Domain;
using HotelHub.Structural.Composite;
using HotelHub.Structural.Decorators;

namespace HotelHub.Structural;

/// <summary>
/// Wzorzec: Proxy (Protection Proxy). Kontroluje dostęp do <see cref="BookingFacade"/>
/// na podstawie roli i własności zasobu: przed delegacją do fasady właściwej
/// weryfikuje regułę dostępu operacji (np. potwierdzanie — tylko recepcja,
/// opłacenie — tylko gość-właściciel, odczyty gościa filtrowane po jego GuestId).
/// Naruszenie kończy się <see cref="AuthorizationResult"/> z komunikatem — nigdy ominięciem.
/// </summary>
public sealed class AuthorizedBookingFacade : IBookingFacade
{
    private readonly BookingFacade _inner;
    private readonly ICurrentUserContext _userContext;

    public AuthorizedBookingFacade(BookingFacade inner, ICurrentUserContext userContext)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
    }

    /// <summary>Tworzy proxy wraz z własną fasadą właściwą — UI nie potrzebuje do niej referencji.</summary>
    public AuthorizedBookingFacade(ICurrentUserContext userContext)
        : this(new BookingFacade(userContext), userContext)
    {
    }

    private ActorContext Actor => _userContext.Actor;

    // --- Reguły dostępu ---

    private AuthorizationResult RequireReception() =>
        Actor.CanActAsReception
            ? AuthorizationResult.Allowed
            : AuthorizationResult.Denied("Ta operacja jest dostępna wyłącznie dla recepcji.");

    private AuthorizationResult RequireOwnerGuest(Reservation reservation) =>
        Actor.IsOwnerOf(reservation) && (Actor.IsGuest || Actor.IsSystem)
            ? AuthorizationResult.Allowed
            : AuthorizationResult.Denied("Ta operacja jest dostępna wyłącznie dla gościa będącego właścicielem rezerwacji.");

    private AuthorizationResult RequireOwnerOrReception(Reservation reservation) =>
        Actor.CanActAsReception || Actor.IsOwnerOf(reservation)
            ? AuthorizationResult.Allowed
            : AuthorizationResult.Denied("Brak dostępu do cudzej rezerwacji.");

    private AuthorizationResult RequireSelfOrReception(Guid guestId) =>
        Actor.CanActAsReception || Actor.GuestId == guestId
            ? AuthorizationResult.Allowed
            : AuthorizationResult.Denied("Brak dostępu do danych innego gościa.");

    private static OperationResult Refused(AuthorizationResult authorization) =>
        OperationResult.Fail(authorization.Message);

    // --- Zapytania ---

    public IReadOnlyCollection<Room> Rooms => _inner.Rooms;

    public IReadOnlyCollection<Guest> Guests =>
        Actor.CanActAsReception ? _inner.Guests : [];

    public IReadOnlyCollection<Reservation> Reservations =>
        Actor.CanActAsReception ? _inner.Reservations : [];

    public IReadOnlyList<Reservation> GetReservationsForGuest(Guid guestId) =>
        RequireSelfOrReception(guestId).Granted
            ? _inner.GetReservationsForGuest(guestId)
            : [];

    public HotelBranch? GetHotelStructure() =>
        Actor.CanActAsReception ? _inner.GetHotelStructure() : null;

    public Guest? FindGuestById(Guid guestId) =>
        RequireSelfOrReception(guestId).Granted ? _inner.FindGuestById(guestId) : null;

    public Room? FindRoomByNumber(int number) => _inner.FindRoomByNumber(number);

    public Reservation? FindReservationByNumber(string reservationNumber)
    {
        var reservation = _inner.FindReservationByNumber(reservationNumber);

        return reservation is not null && RequireOwnerOrReception(reservation).Granted
            ? reservation
            : null;
    }

    public IReadOnlyList<Room> GetAvailableRooms(DateRange stay) => _inner.GetAvailableRooms(stay);

    public IReadOnlyCollection<DateTime> GetOccupiedDays(int roomNumber, int year, int month) =>
        _inner.GetOccupiedDays(roomNumber, year, month);

    public ReservationQuote CalculateQuote(
        Room room, DateRange stay, IEnumerable<RoomExtra>? extras = null, string? promoCode = null) =>
        _inner.CalculateQuote(room, stay, extras, promoCode);

    public string GetInvoice(Reservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        var authorization = RequireOwnerOrReception(reservation);
        return authorization.Granted ? _inner.GetInvoice(reservation) : authorization.Message;
    }

    // --- Konta ---

    public OperationResult RegisterGuestAccount(
        string login, string password, string firstName, string lastName, string email) =>
        _inner.RegisterGuestAccount(login, password, firstName, lastName, email);

    public UserAccount? VerifyCredentials(string login, string password) =>
        _inner.VerifyCredentials(login, password);

    public string GetAccountDisplayName(UserAccount account) => _inner.GetAccountDisplayName(account);

    public void SeedSampleData() => _inner.SeedSampleData();

    public OperationResult ChangePassword(string login, string currentPassword, string newPassword) =>
        string.Equals(Actor.Login, login?.Trim(), StringComparison.OrdinalIgnoreCase) || Actor.IsSystem
            ? _inner.ChangePassword(login!, currentPassword, newPassword)
            : OperationResult.Fail("Możesz zmienić wyłącznie własne hasło.");

    public OperationResult UpdateGuestProfile(Guid guestId, string firstName, string lastName, string email)
    {
        var authorization = RequireSelfOrReception(guestId);

        return authorization.Granted
            ? _inner.UpdateGuestProfile(guestId, firstName, lastName, email)
            : Refused(authorization);
    }

    // --- Cykl życia rezerwacji ---

    public BookingResult MakeReservation(
        Room room, DateRange stay, string? promoCode = null, IEnumerable<RoomExtra>? extras = null)
    {
        if (Actor.GuestId is not { } guestId || _inner.FindGuestById(guestId) is not { } guest)
        {
            return BookingResult.Fail("Rezerwację może utworzyć wyłącznie zalogowany gość.");
        }

        try
        {
            return BookingResult.Ok(_inner.MakeReservation(guest, room, stay, promoCode, extras, Actor));
        }
        catch (InvalidOperationException exception)
        {
            return BookingResult.Fail(exception.Message);
        }
    }

    public OperationResult AddExtraToReservation(Reservation reservation, RoomExtra extra)
    {
        var authorization = RequireOwnerGuest(reservation);

        return authorization.Granted
            ? _inner.AddExtraToReservation(reservation, extra, Actor)
            : Refused(authorization);
    }

    public OperationResult ConfirmReservation(Reservation reservation)
    {
        var authorization = RequireReception();
        return authorization.Granted ? _inner.ConfirmReservation(reservation, Actor) : Refused(authorization);
    }

    public OperationResult RejectReservation(Reservation reservation, string reason)
    {
        var authorization = RequireReception();
        return authorization.Granted ? _inner.RejectReservation(reservation, reason, Actor) : Refused(authorization);
    }

    public OperationResult PayReservation(Reservation reservation)
    {
        var authorization = RequireOwnerGuest(reservation);
        return authorization.Granted ? _inner.PayReservation(reservation, Actor) : Refused(authorization);
    }

    public OperationResult CancelReservation(Reservation reservation)
    {
        var authorization = RequireOwnerOrReception(reservation);
        return authorization.Granted ? _inner.CancelReservation(reservation, Actor) : Refused(authorization);
    }

    public OperationResult CheckIn(Reservation reservation)
    {
        var authorization = RequireReception();
        return authorization.Granted ? _inner.CheckIn(reservation, Actor) : Refused(authorization);
    }

    public OperationResult CheckOut(Reservation reservation)
    {
        var authorization = RequireReception();
        return authorization.Granted ? _inner.CheckOut(reservation, Actor) : Refused(authorization);
    }

    // --- Powiadomienia ---

    public IReadOnlyList<Notification> GetMyNotifications() => _inner.GetNotifications(Actor);

    public int GetUnreadNotificationCount() => _inner.GetUnreadNotificationCount(Actor);

    public void MarkNotificationsRead() => _inner.MarkNotificationsRead(Actor);

    // --- Zarządzanie pokojami ---

    public OperationResult AddRoom(RoomType type, int number)
    {
        var authorization = RequireReception();
        return authorization.Granted ? _inner.AddRoom(type, number) : Refused(authorization);
    }

    public OperationResult UpdateRoom(
        int number, decimal pricePerNight, int capacity, string description, IEnumerable<string> amenities)
    {
        var authorization = RequireReception();

        return authorization.Granted
            ? _inner.UpdateRoom(number, pricePerNight, capacity, description, amenities)
            : Refused(authorization);
    }

    public OperationResult SetRoomOutOfService(int number, string reason)
    {
        var authorization = RequireReception();
        return authorization.Granted ? _inner.SetRoomOutOfService(number, reason) : Refused(authorization);
    }

    public OperationResult ReturnRoomToService(int number)
    {
        var authorization = RequireReception();
        return authorization.Granted ? _inner.ReturnRoomToService(number) : Refused(authorization);
    }

    // --- Persystencja ---

    public OperationResult SaveData()
    {
        var authorization = RequireReception();
        return authorization.Granted ? ((IBookingFacade)_inner).SaveData() : Refused(authorization);
    }

    public OperationResult LoadData()
    {
        var authorization = RequireReception();
        return authorization.Granted ? ((IBookingFacade)_inner).LoadData() : Refused(authorization);
    }
}
