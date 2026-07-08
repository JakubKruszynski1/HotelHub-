using HotelHub.Creational;
using HotelHub.Domain;

namespace HotelHub.Services;

/// <summary>
/// Serwis dostępności pokoi — wykrywa kolizje terminów (overlap):
/// pokój nie może mieć dwóch rezerwacji w nakładających się zakresach dat.
/// Pokoje wyłączone z użytku (remont) traktowane są jako niedostępne.
/// </summary>
public sealed class AvailabilityService
{
    private readonly HotelRegistry _registry = HotelRegistry.Instance;

    /// <summary>Sprawdza, czy pokój jest wolny w zadanym terminie (i nie jest wyłączony z użytku).</summary>
    public bool IsRoomAvailable(int roomNumber, DateRange stay)
    {
        ArgumentNullException.ThrowIfNull(stay);

        var room = _registry.FindRoomByNumber(roomNumber);

        if (room is null || room.IsOutOfService)
        {
            return false;
        }

        return !_registry.Reservations.Any(reservation =>
            reservation.BaseRoom.Number == roomNumber &&
            reservation.BlocksRoom &&
            reservation.Stay.Overlaps(stay));
    }

    /// <summary>Zwraca pokoje wolne w zadanym terminie, posortowane po numerze.</summary>
    public IReadOnlyList<Room> GetAvailableRooms(DateRange stay)
    {
        ArgumentNullException.ThrowIfNull(stay);

        return _registry.Rooms
            .Where(room => !room.IsOutOfService && IsRoomAvailable(room.Number, stay))
            .OrderBy(room => room.Number)
            .ToList();
    }

    /// <summary>
    /// Zwraca dni zajęte przez blokujące rezerwacje pokoju we wskazanym miesiącu —
    /// dla kalendarza zajętości na stronie szczegółów pokoju.
    /// </summary>
    public IReadOnlyCollection<DateTime> GetOccupiedDays(int roomNumber, int year, int month)
    {
        var monthStart = new DateTime(year, month, 1);
        var monthEnd = monthStart.AddMonths(1);
        var occupied = new HashSet<DateTime>();

        foreach (var reservation in _registry.Reservations)
        {
            if (reservation.BaseRoom.Number != roomNumber || !reservation.BlocksRoom)
            {
                continue;
            }

            foreach (var night in reservation.Stay.EachNight())
            {
                if (night >= monthStart && night < monthEnd)
                {
                    occupied.Add(night);
                }
            }
        }

        return occupied;
    }
}
