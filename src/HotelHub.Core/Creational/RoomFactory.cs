using HotelHub.Domain;
using HotelHub.Domain.RoomTypes;

namespace HotelHub.Creational;

/// <summary>
/// Wzorzec: Factory Method (Creator). Tworzy pokoje na podstawie typu —
/// klient nie zna konkretnych klas (<see cref="StandardRoom"/>, <see cref="DeluxeRoom"/>,
/// <see cref="ApartmentRoom"/>), operuje wyłącznie na abstrakcji <see cref="Room"/>.
/// </summary>
public static class RoomFactory
{
    /// <summary>Tworzy pokój wskazanego typu. Nieznany typ zgłasza <see cref="NotSupportedException"/>.</summary>
    public static Room CreateRoom(RoomType type, int number) => type switch
    {
        RoomType.Standard => new StandardRoom(number),
        RoomType.Deluxe => new DeluxeRoom(number),
        RoomType.Apartment => new ApartmentRoom(number),
        _ => throw new NotSupportedException($"Nieobsługiwany typ pokoju: {type}.")
    };
}
