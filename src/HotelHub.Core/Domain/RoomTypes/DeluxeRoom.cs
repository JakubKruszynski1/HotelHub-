namespace HotelHub.Domain.RoomTypes;

/// <summary>
/// Wzorzec: Factory Method (ConcreteProduct). Pokój deluxe — 3 osoby, podwyższony standard.
/// </summary>
public sealed class DeluxeRoom : Room
{
    public const decimal DefaultPricePerNight = 350m;

    public override RoomType Type => RoomType.Deluxe;
    public override string TypeName => "Deluxe";

    public DeluxeRoom(int number)
        : base(number, capacity: 3, new Money(DefaultPricePerNight),
            description: "Przestronny pokój o podwyższonym standardzie z widokiem na miasto, " +
                         "strefą wypoczynkową i dbałością o każdy detal.",
            amenities: ["Wi-Fi", "Telewizor", "Prywatna łazienka", "Klimatyzacja",
                        "Minibar", "Sejf", "Widok na miasto"],
            imagePath: "img/rooms/deluxe.svg")
    {
    }
}
