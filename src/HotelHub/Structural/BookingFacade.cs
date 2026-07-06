using HotelHub.Behavioral.Observers;
using HotelHub.Behavioral.Pricing;
using HotelHub.Behavioral.States;
using HotelHub.Creational;
using HotelHub.Domain;
using HotelHub.Services;
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
    private readonly HotelRegistry _registry = HotelRegistry.Instance;
    private readonly AvailabilityService _availability = new();
    private readonly PaymentService _payment = new();
    private readonly InvoiceService _invoice = new();
    private readonly PersistenceService _persistence = new();

    private readonly IReservationObserver[] _observers =
    [
        new GuestNotifier(),
        new ReceptionNotifier(),
        new AuditLogger()
    ];

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
    /// Tworzy rezerwację: sprawdza dostępność pokoju, dobiera strategię cenową
    /// (Strategy — automatycznie z dat, kod PROMO20 nadpisuje), buduje rezerwację
    /// przez <see cref="ReservationBuilder"/>, podpina obserwatorów i potwierdza ją.
    /// </summary>
    public Reservation MakeReservation(Guest guest, Room room, DateRange stay, string? promoCode = null)
    {
        ArgumentNullException.ThrowIfNull(guest);
        ArgumentNullException.ThrowIfNull(room);
        ArgumentNullException.ThrowIfNull(stay);

        if (!_availability.IsRoomAvailable(room.Number, stay))
        {
            throw new InvalidOperationException(
                $"Pokój {room.Number} jest zajęty w terminie {stay}.");
        }

        var pricing = PricingSelector.Select(stay, promoCode);

        var reservation = new ReservationBuilder()
            .ForGuest(guest)
            .WithRoom(room)
            .Between(stay)
            .WithPricing(pricing)
            .Build();

        _registry.AddReservation(reservation);
        AttachObservers(reservation);
        reservation.Confirm();
        return reservation;
    }

    /// <summary>Dodaje usługę dodatkową (Decorator) do rezerwacji i przelicza cenę.</summary>
    public bool AddExtraToReservation(Reservation reservation, RoomExtra extra)
    {
        ArgumentNullException.ThrowIfNull(reservation);

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

    /// <summary>
    /// Opłaca rezerwację: pobiera płatność (symulacja) i przechodzi do stanu Opłacona.
    /// Nielegalną operację odrzuca wzorzec State z komunikatem.
    /// </summary>
    public bool PayReservation(Reservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        if (reservation.State is not ConfirmedState)
        {
            reservation.Pay();
            return false;
        }

        if (!_payment.ProcessPayment(reservation))
        {
            Console.WriteLine("Płatność została odrzucona.");
            return false;
        }

        reservation.Pay();
        return true;
    }

    /// <summary>Anuluje rezerwację (możliwe tylko ze stanów Oczekująca/Potwierdzona).</summary>
    public void CancelReservation(Reservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        reservation.Cancel();
    }

    /// <summary>Melduje gościa (wymaga opłaconej rezerwacji).</summary>
    public void CheckIn(Reservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        reservation.CheckIn();
    }

    /// <summary>Wymeldowuje gościa i kończy rezerwację.</summary>
    public void CheckOut(Reservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        reservation.CheckOut();
    }

    /// <summary>Generuje tekstowe potwierdzenie rezerwacji.</summary>
    public string GetInvoice(Reservation reservation) => _invoice.GenerateInvoice(reservation);

    /// <summary>Zapisuje stan aplikacji do pliku JSON.</summary>
    public void SaveData() => _persistence.Save();

    /// <summary>Wczytuje stan aplikacji z pliku JSON i ponownie podpina obserwatorów.</summary>
    public bool LoadData()
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

    private void AttachObservers(Reservation reservation)
    {
        foreach (var observer in _observers)
        {
            reservation.Attach(observer);
        }
    }
}
