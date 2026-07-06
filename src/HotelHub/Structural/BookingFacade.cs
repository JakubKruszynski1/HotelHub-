using HotelHub.Creational;
using HotelHub.Domain;
using HotelHub.Services;
using HotelHub.Structural.Decorators;

namespace HotelHub.Structural;

/// <summary>
/// Wzorzec: Facade. Udostępnia warstwie UI wysokopoziomowe operacje rezerwacyjne
/// i ukrywa orkiestrację podsystemów: <see cref="AvailabilityService"/> →
/// <see cref="ReservationBuilder"/> → <see cref="PaymentService"/> → powiadomienia.
/// UI korzysta wyłącznie z fasady, nigdy z podsystemów bezpośrednio.
/// </summary>
public sealed class BookingFacade
{
    private readonly HotelRegistry _registry = HotelRegistry.Instance;
    private readonly AvailabilityService _availability = new();
    private readonly PaymentService _payment = new();
    private readonly InvoiceService _invoice = new();
    private readonly PersistenceService _persistence = new();

    /// <summary>Rejestruje nowego gościa w systemie.</summary>
    public Guest RegisterGuest(string firstName, string lastName, string email)
    {
        var guest = new Guest(firstName, lastName, email);
        _registry.AddGuest(guest);
        return guest;
    }

    /// <summary>Zwraca pokoje wolne w zadanym terminie.</summary>
    public IReadOnlyList<Room> GetAvailableRooms(DateRange stay) =>
        _availability.GetAvailableRooms(stay);

    /// <summary>
    /// Tworzy rezerwację: sprawdza dostępność pokoju, buduje rezerwację
    /// przez <see cref="ReservationBuilder"/> i rejestruje ją w systemie.
    /// </summary>
    public Reservation MakeReservation(Guest guest, Room room, DateRange stay)
    {
        ArgumentNullException.ThrowIfNull(guest);
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(stay);

        if (!_availability.IsRoomAvailable(room.Number, stay))
        {
            throw new InvalidOperationException(
                $"Pokój {room.Number} jest zajęty w terminie {stay}.");
        }

        var reservation = new ReservationBuilder()
            .ForGuest(guest)
            .WithRoom(room)
            .Between(stay)
            .Build();

        _registry.AddReservation(reservation);
        return reservation;
    }

    /// <summary>Dodaje usługę dodatkową (Decorator) do rezerwacji i przelicza cenę.</summary>
    public void AddExtraToReservation(Reservation reservation, RoomExtra extra)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        reservation.AddExtra(extra);
    }

    /// <summary>Pobiera płatność za rezerwację (symulacja).</summary>
    public bool PayReservation(Reservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        return _payment.ProcessPayment(reservation);
    }

    /// <summary>Generuje tekstowe potwierdzenie rezerwacji.</summary>
    public string GetInvoice(Reservation reservation) => _invoice.GenerateInvoice(reservation);

    /// <summary>Zapisuje stan aplikacji do pliku JSON.</summary>
    public void SaveData() => _persistence.Save();

    /// <summary>Wczytuje stan aplikacji z pliku JSON.</summary>
    public bool LoadData() => _persistence.Load();
}
