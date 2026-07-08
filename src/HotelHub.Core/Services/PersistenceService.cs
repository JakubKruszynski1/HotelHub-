using System.Globalization;
using System.Text.Json;
using HotelHub.Behavioral.Observers;
using HotelHub.Behavioral.Pricing;
using HotelHub.Behavioral.States;
using HotelHub.Creational;
using HotelHub.Domain;
using HotelHub.Structural.Composite;
using HotelHub.Structural.Decorators;

namespace HotelHub.Services;

/// <summary>
/// Zapis i odczyt pełnego stanu aplikacji do pliku JSON (<see cref="System.Text.Json"/>):
/// pokoje (z parametrami i wyłączeniami), goście, konta użytkowników (z hashami haseł),
/// rezerwacje (ze stanem, numerem i historią) oraz powiadomienia.
/// Ścieżka pliku jest stała (katalog aplikacji) — nigdy nie pochodzi od użytkownika.
/// Wczytane dane są walidowane przed użyciem (plik mógł zostać ręcznie zmodyfikowany).
/// </summary>
public sealed class PersistenceService
{
    public const string DateFormat = "yyyy-MM-dd";
    private const string TimestampFormat = "yyyy-MM-dd HH:mm:ss";

    private static readonly string DataFilePath =
        Path.Combine(AppContext.BaseDirectory, "hotelhub-data.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly HotelRegistry _registry = HotelRegistry.Instance;

    /// <summary>Zapisuje pełny stan aplikacji do pliku JSON.</summary>
    public void Save()
    {
        var snapshot = new SnapshotDto
        {
            HotelName = _registry.HotelStructure?.Name ?? "HotelHub",
            Rooms = _registry.Rooms.Select(room => new RoomDto
            {
                Number = room.Number,
                Type = room.Type.ToString(),
                PricePerNight = room.BasePricePerNight.Amount,
                Capacity = room.Capacity,
                Description = room.Description,
                Amenities = room.Amenities.ToList(),
                ImagePath = room.ImagePath,
                IsOutOfService = room.IsOutOfService,
                OutOfServiceReason = room.OutOfServiceReason
            }).ToList(),
            Guests = _registry.Guests.Select(guest => new GuestDto
            {
                Id = guest.Id,
                FirstName = guest.FirstName,
                LastName = guest.LastName,
                Email = guest.Email
            }).ToList(),
            Accounts = _registry.Accounts.Select(account => new AccountDto
            {
                Id = account.Id,
                Login = account.Login,
                PasswordHash = account.PasswordHash,
                Role = account.Role.ToString(),
                GuestId = account.GuestId,
                CreatedAt = Stamp(account.CreatedAt)
            }).ToList(),
            Reservations = _registry.Reservations.Select(reservation => new ReservationDto
            {
                Id = reservation.Id,
                GuestId = reservation.Guest.Id,
                RoomNumber = reservation.BaseRoom.Number,
                From = reservation.Stay.From.ToString(DateFormat, CultureInfo.InvariantCulture),
                To = reservation.Stay.To.ToString(DateFormat, CultureInfo.InvariantCulture),
                Extras = RoomExtraDecorator.GetExtras(reservation.Room)
                    .Select(extra => extra.ToString())
                    .ToList(),
                Status = StateToCode(reservation.State),
                ReservationNumber = reservation.ReservationNumber,
                RejectionReason = reservation.RejectionReason,
                CreatedAt = Stamp(reservation.CreatedAt),
                Pricing = reservation.Pricing is null ? null : PricingToCode(reservation.Pricing),
                History = reservation.History.Select(change => new HistoryDto
                {
                    At = Stamp(change.At),
                    StateName = change.StateName,
                    Description = change.Description,
                    ActorLogin = change.ActorLogin
                }).ToList()
            }).ToList(),
            Notifications = NotificationCenter.Instance.All.Select(notification => new NotificationDto
            {
                Id = notification.Id,
                RecipientGuestId = notification.RecipientGuestId,
                Message = notification.Message,
                CreatedAt = Stamp(notification.CreatedAt),
                IsRead = notification.IsRead
            }).ToList()
        };

        File.WriteAllText(DataFilePath, JsonSerializer.Serialize(snapshot, JsonOptions));
        Console.WriteLine($"Dane zapisano do pliku: {DataFilePath}");
    }

    /// <summary>
    /// Wczytuje dane z pliku JSON, waliduje je i zastępuje zawartość rejestru
    /// oraz centrum powiadomień. Nieprawidłowe wpisy są pomijane z ostrzeżeniem.
    /// </summary>
    public bool Load()
    {
        if (!File.Exists(DataFilePath))
        {
            Console.WriteLine($"Brak pliku danych: {DataFilePath}");
            return false;
        }

        SnapshotDto? snapshot;

        try
        {
            snapshot = JsonSerializer.Deserialize<SnapshotDto>(File.ReadAllText(DataFilePath), JsonOptions);
        }
        catch (JsonException exception)
        {
            Console.WriteLine($"Nie udało się odczytać pliku JSON (uszkodzony format): {exception.Message}");
            return false;
        }
        catch (IOException exception)
        {
            Console.WriteLine($"Błąd odczytu pliku danych: {exception.Message}");
            return false;
        }

        if (snapshot is null)
        {
            Console.WriteLine("Plik danych jest pusty lub nieprawidłowy.");
            return false;
        }

        var rooms = LoadRooms(snapshot);
        var guests = LoadGuests(snapshot);
        var accounts = LoadAccounts(snapshot, guests);
        var reservations = LoadReservations(snapshot, rooms, guests);

        _registry.ReplaceAll(rooms, guests, reservations, accounts);
        _registry.SetHotelStructure(HotelBranch.BuildHotel(snapshot.HotelName ?? "HotelHub", rooms));
        RestoreReservationNumbering(reservations);
        NotificationCenter.Instance.ReplaceAll(LoadNotifications(snapshot));

        Console.WriteLine(
            $"Wczytano dane: pokoje: {rooms.Count}, goście: {guests.Count}, " +
            $"konta: {accounts.Count}, rezerwacje: {reservations.Count}.");
        return true;
    }

    private static List<Room> LoadRooms(SnapshotDto snapshot)
    {
        var rooms = new List<Room>();

        foreach (var dto in snapshot.Rooms ?? [])
        {
            if (!Enum.TryParse<RoomType>(dto.Type, ignoreCase: true, out var type))
            {
                Console.WriteLine($"Pominięto pokój {dto.Number}: nieznany typ '{dto.Type}'.");
                continue;
            }

            if (rooms.Any(r => r.Number == dto.Number))
            {
                Console.WriteLine($"Pominięto zduplikowany pokój {dto.Number}.");
                continue;
            }

            try
            {
                var room = RoomFactory.CreateRoom(type, dto.Number);

                if (dto.PricePerNight > 0 && dto.Capacity > 0)
                {
                    room.UpdateDetails(new Money(dto.PricePerNight), dto.Capacity,
                        dto.Description ?? room.Description, dto.Amenities ?? room.Amenities);
                }

                if (dto.IsOutOfService && !string.IsNullOrWhiteSpace(dto.OutOfServiceReason))
                {
                    room.SetOutOfService(dto.OutOfServiceReason);
                }

                rooms.Add(room);
            }
            catch (Exception exception) when (exception is ArgumentException or ArgumentOutOfRangeException)
            {
                Console.WriteLine($"Pominięto nieprawidłowy pokój {dto.Number}: {exception.Message}");
            }
        }

        return rooms;
    }

    private static List<Guest> LoadGuests(SnapshotDto snapshot)
    {
        var guests = new List<Guest>();

        foreach (var dto in snapshot.Guests ?? [])
        {
            try
            {
                guests.Add(new Guest(dto.Id, dto.FirstName ?? string.Empty, dto.LastName ?? string.Empty,
                    dto.Email ?? string.Empty));
            }
            catch (ArgumentException exception)
            {
                Console.WriteLine($"Pominięto nieprawidłowego gościa: {exception.Message}");
            }
        }

        return guests;
    }

    private static List<UserAccount> LoadAccounts(SnapshotDto snapshot, List<Guest> guests)
    {
        var accounts = new List<UserAccount>();

        foreach (var dto in snapshot.Accounts ?? [])
        {
            if (!Enum.TryParse<UserRole>(dto.Role, ignoreCase: true, out var role))
            {
                Console.WriteLine($"Pominięto konto '{dto.Login}': nieznana rola '{dto.Role}'.");
                continue;
            }

            if (role == UserRole.Guest && guests.All(g => g.Id != dto.GuestId))
            {
                Console.WriteLine($"Pominięto konto '{dto.Login}': brak powiązanego gościa.");
                continue;
            }

            if (accounts.Any(a => string.Equals(a.Login, dto.Login, StringComparison.OrdinalIgnoreCase)))
            {
                Console.WriteLine($"Pominięto zduplikowane konto '{dto.Login}'.");
                continue;
            }

            try
            {
                accounts.Add(new UserAccount(dto.Id, dto.Login ?? string.Empty, dto.PasswordHash ?? string.Empty,
                    role, dto.GuestId, ParseStamp(dto.CreatedAt)));
            }
            catch (ArgumentException exception)
            {
                Console.WriteLine($"Pominięto nieprawidłowe konto '{dto.Login}': {exception.Message}");
            }
        }

        return accounts;
    }

    private static List<Reservation> LoadReservations(
        SnapshotDto snapshot, List<Room> rooms, List<Guest> guests)
    {
        var reservations = new List<Reservation>();

        foreach (var dto in snapshot.Reservations ?? [])
        {
            var guest = guests.FirstOrDefault(g => g.Id == dto.GuestId);
            var room = rooms.FirstOrDefault(r => r.Number == dto.RoomNumber);

            if (guest is null || room is null)
            {
                Console.WriteLine($"Pominięto rezerwację {dto.Id}: brak powiązanego gościa lub pokoju.");
                continue;
            }

            if (!DateTime.TryParseExact(dto.From, DateFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var from) ||
                !DateTime.TryParseExact(dto.To, DateFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var to))
            {
                Console.WriteLine($"Pominięto rezerwację {dto.Id}: nieprawidłowy format daty.");
                continue;
            }

            try
            {
                IRoom decoratedRoom = room;

                foreach (var extraName in dto.Extras ?? [])
                {
                    if (Enum.TryParse<RoomExtra>(extraName, ignoreCase: true, out var extra))
                    {
                        decoratedRoom = RoomExtraDecorator.Apply(decoratedRoom, extra);
                    }
                    else
                    {
                        Console.WriteLine($"Pominięto nieznaną usługę '{extraName}' w rezerwacji {dto.Id}.");
                    }
                }

                var reservation = new Reservation(
                    dto.Id, guest, decoratedRoom, DateRange.Restore(from, to), CodeToPricing(dto.Pricing));

                var state = CodeToState(dto.Status);

                if (state is null)
                {
                    Console.WriteLine(
                        $"Rezerwacja {dto.Id}: nieznany status '{dto.Status}' — przywrócono stan Oczekująca.");
                    state = new PendingState();
                }

                reservation.RestoreState(state);
                reservation.AssignNumber(dto.ReservationNumber ?? string.Empty);
                reservation.RestoreMetadata(
                    ParseStamp(dto.CreatedAt),
                    dto.RejectionReason,
                    (dto.History ?? []).Select(h => new StateChange(
                        ParseStamp(h.At), h.StateName ?? string.Empty,
                        h.Description ?? string.Empty, h.ActorLogin ?? "system")));

                reservations.Add(reservation);
            }
            catch (ArgumentException exception)
            {
                Console.WriteLine($"Pominięto rezerwację {dto.Id}: {exception.Message}");
            }
        }

        return reservations;
    }

    private static List<Notification> LoadNotifications(SnapshotDto snapshot)
    {
        var notifications = new List<Notification>();

        foreach (var dto in snapshot.Notifications ?? [])
        {
            try
            {
                notifications.Add(new Notification(dto.Id, dto.RecipientGuestId,
                    dto.Message ?? string.Empty, ParseStamp(dto.CreatedAt), dto.IsRead));
            }
            catch (ArgumentException)
            {
                Console.WriteLine("Pominięto nieprawidłowe powiadomienie z pliku danych.");
            }
        }

        return notifications;
    }

    /// <summary>
    /// Synchronizuje licznik numerów rezerwacji z najwyższym wczytanym numerem
    /// i nadaje numery rezerwacjom z plików w starszym formacie.
    /// </summary>
    private void RestoreReservationNumbering(List<Reservation> reservations)
    {
        var highest = reservations
            .Select(r => r.ReservationNumber.Split('-'))
            .Where(parts => parts.Length == 3 && int.TryParse(parts[2], NumberStyles.Integer,
                CultureInfo.InvariantCulture, out _))
            .Select(parts => int.Parse(parts[2], CultureInfo.InvariantCulture))
            .DefaultIfEmpty(0)
            .Max();

        _registry.SyncReservationCounter(highest);

        foreach (var reservation in reservations.Where(r => r.ReservationNumber.Length == 0))
        {
            reservation.AssignNumber(_registry.NextReservationNumber());
        }
    }

    private static string Stamp(DateTime value) =>
        value.ToString(TimestampFormat, CultureInfo.InvariantCulture);

    private static DateTime ParseStamp(string? value) =>
        DateTime.TryParseExact(value, TimestampFormat, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var parsed)
            ? parsed
            : DateTime.Now;

    /// <summary>Mapuje strategię cenową na kod zapisywany w JSON (np. PromoPricing → "Promo").</summary>
    private static string PricingToCode(IPricingStrategy pricing) =>
        pricing.GetType().Name.Replace("Pricing", string.Empty);

    /// <summary>Mapuje kod z JSON na strategię cenową; null oznacza cenę bazową × liczba nocy.</summary>
    private static IPricingStrategy? CodeToPricing(string? code) => code switch
    {
        "Standard" => new StandardPricing(),
        "HighSeason" => new HighSeasonPricing(),
        "Weekend" => new WeekendPricing(),
        "Promo" => new PromoPricing(),
        _ => null
    };

    /// <summary>Mapuje stan rezerwacji na kod zapisywany w JSON (np. PaidState → "Paid").</summary>
    private static string StateToCode(IReservationState state) =>
        state.GetType().Name.Replace("State", string.Empty);

    /// <summary>Mapuje kod z JSON na stan rezerwacji; null dla nieznanego kodu.</summary>
    private static IReservationState? CodeToState(string? code) => code switch
    {
        "Pending" => new PendingState(),
        "Confirmed" => new ConfirmedState(),
        "Paid" => new PaidState(),
        "CheckedIn" => new CheckedInState(),
        "Completed" => new CompletedState(),
        "Cancelled" => new CancelledState(),
        "Rejected" => new RejectedState(),
        _ => null
    };

    private sealed class SnapshotDto
    {
        public string? HotelName { get; set; }
        public List<RoomDto>? Rooms { get; set; }
        public List<GuestDto>? Guests { get; set; }
        public List<AccountDto>? Accounts { get; set; }
        public List<ReservationDto>? Reservations { get; set; }
        public List<NotificationDto>? Notifications { get; set; }
    }

    private sealed class RoomDto
    {
        public int Number { get; set; }
        public string? Type { get; set; }
        public decimal PricePerNight { get; set; }
        public int Capacity { get; set; }
        public string? Description { get; set; }
        public List<string>? Amenities { get; set; }
        public string? ImagePath { get; set; }
        public bool IsOutOfService { get; set; }
        public string? OutOfServiceReason { get; set; }
    }

    private sealed class GuestDto
    {
        public Guid Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }

    private sealed class AccountDto
    {
        public Guid Id { get; set; }
        public string? Login { get; set; }
        public string? PasswordHash { get; set; }
        public string? Role { get; set; }
        public Guid? GuestId { get; set; }
        public string? CreatedAt { get; set; }
    }

    private sealed class ReservationDto
    {
        public Guid Id { get; set; }
        public Guid GuestId { get; set; }
        public int RoomNumber { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public List<string>? Extras { get; set; }
        public string? Status { get; set; }
        public string? ReservationNumber { get; set; }
        public string? RejectionReason { get; set; }
        public string? CreatedAt { get; set; }
        public string? Pricing { get; set; }
        public List<HistoryDto>? History { get; set; }
    }

    private sealed class HistoryDto
    {
        public string? At { get; set; }
        public string? StateName { get; set; }
        public string? Description { get; set; }
        public string? ActorLogin { get; set; }
    }

    private sealed class NotificationDto
    {
        public Guid Id { get; set; }
        public Guid? RecipientGuestId { get; set; }
        public string? Message { get; set; }
        public string? CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
