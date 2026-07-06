using HotelHub.Domain;

namespace HotelHub.Creational;

/// <summary>
/// Wzorzec: Singleton. Centralny rejestr hotelu — jedyna instancja w aplikacji,
/// tworzona leniwie i w sposób bezpieczny wątkowo przez <see cref="Lazy{T}"/>.
/// Przechowuje kolekcje pokoi, gości i rezerwacji; na zewnątrz wystawia je
/// wyłącznie jako <see cref="IReadOnlyCollection{T}"/>.
/// </summary>
public sealed class HotelRegistry
{
    private static readonly Lazy<HotelRegistry> LazyInstance = new(() => new HotelRegistry());

    /// <summary>Jedyna instancja rejestru (Singleton).</summary>
    public static HotelRegistry Instance => LazyInstance.Value;

    private readonly object _syncRoot = new();
    private readonly List<Room> _rooms = [];
    private readonly List<Guest> _guests = [];
    private readonly List<Reservation> _reservations = [];

    private HotelRegistry()
    {
    }

    public IReadOnlyCollection<Room> Rooms
    {
        get { lock (_syncRoot) { return _rooms.AsReadOnly(); } }
    }

    public IReadOnlyCollection<Guest> Guests
    {
        get { lock (_syncRoot) { return _guests.AsReadOnly(); } }
    }

    public IReadOnlyCollection<Reservation> Reservations
    {
        get { lock (_syncRoot) { return _reservations.AsReadOnly(); } }
    }

    public void AddRoom(Room room)
    {
        ArgumentNullException.ThrowIfNull(room);

        lock (_syncRoot)
        {
            if (_rooms.Any(r => r.Number == room.Number))
            {
                throw new InvalidOperationException($"Pokój o numerze {room.Number} już istnieje w rejestrze.");
            }

            _rooms.Add(room);
        }
    }

    public void AddGuest(Guest guest)
    {
        ArgumentNullException.ThrowIfNull(guest);

        lock (_syncRoot)
        {
            if (_guests.Any(g => string.Equals(g.Email, guest.Email, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Gość z adresem e-mail {guest.Email} jest już zarejestrowany.");
            }

            _guests.Add(guest);
        }
    }

    public void AddReservation(Reservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        lock (_syncRoot)
        {
            if (_reservations.Any(r => r.Id == reservation.Id))
            {
                throw new InvalidOperationException($"Rezerwacja {reservation.Id} już istnieje w rejestrze.");
            }

            _reservations.Add(reservation);
        }
    }

    public Room? FindRoomByNumber(int number)
    {
        lock (_syncRoot) { return _rooms.FirstOrDefault(r => r.Number == number); }
    }

    public Guest? FindGuestByEmail(string email)
    {
        lock (_syncRoot)
        {
            return _guests.FirstOrDefault(
                g => string.Equals(g.Email, email?.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public Reservation? FindReservationByShortId(string shortId)
    {
        lock (_syncRoot)
        {
            return _reservations.FirstOrDefault(
                r => r.Id.ToString().StartsWith(shortId?.Trim() ?? string.Empty, StringComparison.OrdinalIgnoreCase));
        }
    }

    /// <summary>
    /// Zastępuje zawartość rejestru danymi wczytanymi z pliku JSON.
    /// Wykorzystywane wyłącznie przez serwis persystencji.
    /// </summary>
    public void ReplaceAll(IEnumerable<Room> rooms, IEnumerable<Guest> guests, IEnumerable<Reservation> reservations)
    {
        ArgumentNullException.ThrowIfNull(rooms);
        ArgumentNullException.ThrowIfNull(guests);
        ArgumentNullException.ThrowIfNull(reservations);

        lock (_syncRoot)
        {
            _rooms.Clear();
            _guests.Clear();
            _reservations.Clear();
            _rooms.AddRange(rooms);
            _guests.AddRange(guests);
            _reservations.AddRange(reservations);
        }
    }
}
