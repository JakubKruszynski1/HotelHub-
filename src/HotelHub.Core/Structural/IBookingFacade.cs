using HotelHub.Domain;
using HotelHub.Structural.Composite;
using HotelHub.Structural.Decorators;

namespace HotelHub.Structural;

/// <summary>
/// Wzorzec: Proxy (Subject) i Facade. Kontrakt fasady rezerwacyjnej dla warstwy UI —
/// komponenty otrzymują wyłącznie ten interfejs (przez proxy autoryzujące),
/// nigdy fasadę właściwą ani serwisy bezpośrednio. Operacje nie przyjmują
/// kontekstu wykonującego — dostarcza go <see cref="ICurrentUserContext"/>.
/// </summary>
public interface IBookingFacade
{
    // --- Zapytania ---

    /// <summary>Wszystkie pokoje hotelu.</summary>
    IReadOnlyCollection<Room> Rooms { get; }

    /// <summary>Wszyscy goście (wyłącznie recepcja).</summary>
    IReadOnlyCollection<Guest> Guests { get; }

    /// <summary>Wszystkie rezerwacje (wyłącznie recepcja).</summary>
    IReadOnlyCollection<Reservation> Reservations { get; }

    /// <summary>Rezerwacje wskazanego gościa (recepcja lub sam gość).</summary>
    IReadOnlyList<Reservation> GetReservationsForGuest(Guid guestId);

    /// <summary>Drzewo struktury hotelu (Composite) — raporty recepcji.</summary>
    HotelBranch? GetHotelStructure();

    Guest? FindGuestById(Guid guestId);
    Room? FindRoomByNumber(int number);
    Reservation? FindReservationByNumber(string reservationNumber);

    /// <summary>Pokoje wolne w zadanym terminie.</summary>
    IReadOnlyList<Room> GetAvailableRooms(DateRange stay);

    /// <summary>Wycena pobytu „na żywo" (Decorator + Strategy) bez tworzenia rezerwacji.</summary>
    ReservationQuote CalculateQuote(
        Room room, DateRange stay, IEnumerable<RoomExtra>? extras = null, string? promoCode = null);

    /// <summary>Tekstowe potwierdzenie rezerwacji (recepcja lub właściciel).</summary>
    string GetInvoice(Reservation reservation);

    // --- Konta ---

    /// <summary>Samodzielna rejestracja konta gościa (tworzy gościa i konto).</summary>
    OperationResult RegisterGuestAccount(string login, string password, string firstName, string lastName, string email);

    /// <summary>Weryfikacja poświadczeń przy logowaniu.</summary>
    UserAccount? VerifyCredentials(string login, string password);

    /// <summary>Zmiana hasła — wyłącznie własnego konta.</summary>
    OperationResult ChangePassword(string login, string currentPassword, string newPassword);

    /// <summary>Edycja profilu gościa (sam gość lub recepcja).</summary>
    OperationResult UpdateGuestProfile(Guid guestId, string firstName, string lastName, string email);

    // --- Cykl życia rezerwacji ---

    /// <summary>Tworzy rezerwację dla aktualnie zalogowanego gościa (stan Oczekująca).</summary>
    BookingResult MakeReservation(
        Room room, DateRange stay, string? promoCode = null, IEnumerable<RoomExtra>? extras = null);

    /// <summary>Dodaje usługę dodatkową do rezerwacji (właściciel, przed opłaceniem).</summary>
    OperationResult AddExtraToReservation(Reservation reservation, RoomExtra extra);

    /// <summary>Potwierdza rezerwację (wyłącznie recepcja).</summary>
    OperationResult ConfirmReservation(Reservation reservation);

    /// <summary>Odrzuca rezerwację z obowiązkowym powodem (wyłącznie recepcja).</summary>
    OperationResult RejectReservation(Reservation reservation, string reason);

    /// <summary>Opłaca rezerwację (wyłącznie gość-właściciel).</summary>
    OperationResult PayReservation(Reservation reservation);

    /// <summary>Anuluje rezerwację (właściciel w Oczekująca/Potwierdzona; recepcja także w Opłacona).</summary>
    OperationResult CancelReservation(Reservation reservation);

    /// <summary>Melduje gościa (wyłącznie recepcja).</summary>
    OperationResult CheckIn(Reservation reservation);

    /// <summary>Wymeldowuje gościa (wyłącznie recepcja).</summary>
    OperationResult CheckOut(Reservation reservation);

    // --- Persystencja (wyłącznie recepcja) ---

    OperationResult SaveData();
    OperationResult LoadData();
}
