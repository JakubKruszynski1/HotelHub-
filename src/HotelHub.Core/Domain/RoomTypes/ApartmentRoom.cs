namespace HotelHub.Domain.RoomTypes;

/// <summary>
/// Wzorzec: Factory Method (ConcreteProduct). Apartament — 5 osób, najwyższy standard.
/// </summary>
public sealed class ApartmentRoom : Room
{
    public const decimal DefaultPricePerNight = 600m;

    public override RoomType Type => RoomType.Apartment;
    public override string TypeName => "Apartament";

    public ApartmentRoom(int number)
        : base(number, capacity: 5, new Money(DefaultPricePerNight),
            description: "Elegancki apartament z osobną sypialnią, salonem i aneksem kuchennym — " +
                         "idealny dla rodzin i dłuższych pobytów.",
            amenities: ["Wi-Fi", "Telewizor", "Prywatna łazienka", "Klimatyzacja",
                        "Minibar", "Sejf", "Aneks kuchenny", "Salon", "Balkon"],
            imagePath: "img/rooms/apartment.svg")
    {
    }
}
