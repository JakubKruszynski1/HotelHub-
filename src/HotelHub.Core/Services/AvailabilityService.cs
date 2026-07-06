using HotelHub.Creational;
using HotelHub.Domain;

namespace HotelHub.Services;

/// <summary>
/// Serwis dostępności pokoi — wykrywa kolizje terminów (overlap):
/// pokój nie może mieć dwóch rezerwacji w nakładających się zakresach dat.
/// </summary>
public sealed class AvailabilityService
{
    private readonly HotelRegistry _registry = HotelRegistry.Instance;

    /// <summary>Sprawdza, czy pokój jest wolny w zadanym terminie.</summary>
    public bool IsRoomAvailable(int roomNumber, DateRange stay)
    {
        ArgumentNullException.ThrowIfNull(stay);

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
            .Where(room => IsRoomAvailable(room.Number, stay))
            .OrderBy(room => room.Number)
            .ToList();
    }
}
