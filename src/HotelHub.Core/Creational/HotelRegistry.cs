using System.Globalization;
using HotelHub.Domain;
using HotelHub.Structural.Composite;

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
    private readonly List<UserAccount> _accounts = [];
    private int _reservationCounter;

    private HotelRegistry()
    {
    }

    /// <summary>Korzeń drzewa struktury hotelu (Composite): hotel → piętra → pokoje.</summary>
    public HotelBranch? HotelStructure { get; private set; }

    public void SetHotelStructure(HotelBranch structure) =>
        HotelStructure = structure ?? throw new ArgumentNullException(nameof(structure));

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

            if (reservation.ReservationNumber.Length == 0)
            {
                reservation.AssignNumber(NextReservationNumber());
            }

            _reservations.Add(reservation);
        }
    }

    /// <summary>Nadaje kolejny numer rezerwacji (atomowy licznik), np. RES-2026-0001.</summary>
    public string NextReservationNumber()
    {
        lock (_syncRoot)
        {
            _reservationCounter++;
            return string.Format(CultureInfo.InvariantCulture, "RES-{0}-{1:0000}",
                DateTime.Today.Year, _reservationCounter);
        }
    }

    /// <summary>Synchronizuje licznik numerów z najwyższym numerem wczytanym z pliku JSON.</summary>
    public void SyncReservationCounter(int lastUsedNumber)
    {
        lock (_syncRoot)
        {
            _reservationCounter = Math.Max(_reservationCounter, lastUsedNumber);
        }
    }

    public IReadOnlyCollection<UserAccount> Accounts
    {
        get { lock (_syncRoot) { return _accounts.AsReadOnly(); } }
    }

    /// <summary>Dodaje konto użytkownika; login musi być unikalny (bez rozróżniania wielkości liter).</summary>
    public void AddAccount(UserAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);

        lock (_syncRoot)
        {
            if (_accounts.Any(a => string.Equals(a.Login, account.Login, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidOperationException($"Login '{account.Login}' jest już zajęty.");
            }

            _accounts.Add(account);
        }
    }

    public UserAccount? FindAccountByLogin(string login)
    {
        lock (_syncRoot)
        {
            return _accounts.FirstOrDefault(
                a => string.Equals(a.Login, login?.Trim(), StringComparison.OrdinalIgnoreCase));
        }
    }

    public UserAccount? FindAccountByGuestId(Guid guestId)
    {
        lock (_syncRoot) { return _accounts.FirstOrDefault(a => a.GuestId == guestId); }
    }

    public Guest? FindGuestById(Guid guestId)
    {
        lock (_syncRoot) { return _guests.FirstOrDefault(g => g.Id == guestId); }
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

    public Reservation? FindReservationByNumber(string reservationNumber)
    {
        lock (_syncRoot)
        {
            return _reservations.FirstOrDefault(r => string.Equals(
                r.ReservationNumber, reservationNumber?.Trim(), StringComparison.OrdinalIgnoreCase));
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
