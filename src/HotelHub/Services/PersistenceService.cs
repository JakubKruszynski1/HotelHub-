using System.Globalization;
using System.Text.Json;
using HotelHub.Creational;
using HotelHub.Domain;
using HotelHub.Structural.Composite;
using HotelHub.Structural.Decorators;

namespace HotelHub.Services;

/// <summary>
/// Zapis i odczyt stanu aplikacji do pliku JSON (<see cref="System.Text.Json"/>).
/// Ścieżka pliku jest stała (katalog aplikacji) — nigdy nie pochodzi od użytkownika.
/// Wczytane dane są walidowane przed użyciem (plik mógł zostać ręcznie zmodyfikowany).
/// </summary>
public sealed class PersistenceService
{
    public const string DateFormat = "yyyy-MM-dd";

    private static readonly string DataFilePath =
        Path.Combine(AppContext.BaseDirectory, "hotelhub-data.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly HotelRegistry _registry = HotelRegistry.Instance;

    /// <summary>Zapisuje pokoje, gości i rezerwacje do pliku JSON.</summary>
    public void Save()
    {
        var snapshot = new SnapshotDto
        {
            HotelName = _registry.HotelStructure?.Name ?? "HotelHub",
            Rooms = _registry.Rooms
                .Select(room => new RoomDto { Number = room.Number, Type = room.Type.ToString() })
                .ToList(),
            Guests = _registry.Guests
                .Select(guest => new GuestDto
                {
                    Id = guest.Id,
                    FirstName = guest.FirstName,
                    LastName = guest.LastName,
                    Email = guest.Email
                })
                .ToList(),
            Reservations = _registry.Reservations
                .Select(reservation => new ReservationDto
                {
                    Id = reservation.Id,
                    GuestId = reservation.Guest.Id,
                    RoomNumber = reservation.BaseRoom.Number,
                    From = reservation.Stay.From.ToString(DateFormat, CultureInfo.InvariantCulture),
                    To = reservation.Stay.To.ToString(DateFormat, CultureInfo.InvariantCulture),
                    Extras = RoomExtraDecorator.GetExtras(reservation.Room)
                        .Select(extra => extra.ToString())
                        .ToList()
                })
                .ToList()
        };

        File.WriteAllText(DataFilePath, JsonSerializer.Serialize(snapshot, JsonOptions));
        Console.WriteLine($"Dane zapisano do pliku: {DataFilePath}");
    }

    /// <summary>
    /// Wczytuje dane z pliku JSON, waliduje je i zastępuje zawartość rejestru.
    /// Nieprawidłowe wpisy są pomijane z komunikatem ostrzegawczym.
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
        var reservations = LoadReservations(snapshot, rooms, guests);

        _registry.ReplaceAll(rooms, guests, reservations);
        _registry.SetHotelStructure(HotelBranch.BuildHotel(snapshot.HotelName ?? "HotelHub", rooms));

        Console.WriteLine(
            $"Wczytano dane: pokoje: {rooms.Count}, goście: {guests.Count}, rezerwacje: {reservations.Count}.");
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
                rooms.Add(RoomFactory.CreateRoom(type, dto.Number));
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

                reservations.Add(new Reservation(dto.Id, guest, decoratedRoom, new DateRange(from, to)));
            }
            catch (ArgumentException exception)
            {
                Console.WriteLine($"Pominięto rezerwację {dto.Id}: {exception.Message}");
            }
        }

        return reservations;
    }

    private sealed class SnapshotDto
    {
        public string? HotelName { get; set; }
        public List<RoomDto>? Rooms { get; set; }
        public List<GuestDto>? Guests { get; set; }
        public List<ReservationDto>? Reservations { get; set; }
    }

    private sealed class RoomDto
    {
        public int Number { get; set; }
        public string? Type { get; set; }
    }

    private sealed class GuestDto
    {
        public Guid Id { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
    }

    private sealed class ReservationDto
    {
        public Guid Id { get; set; }
        public Guid GuestId { get; set; }
        public int RoomNumber { get; set; }
        public string? From { get; set; }
        public string? To { get; set; }
        public List<string>? Extras { get; set; }
    }
}
