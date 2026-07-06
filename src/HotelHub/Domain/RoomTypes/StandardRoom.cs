namespace HotelHub.Domain.RoomTypes;

/// <summary>
/// Wzorzec: Factory Method (ConcreteProduct). Pokój standardowy — 2 osoby.
/// </summary>
public sealed class StandardRoom : Room
{
    public const decimal DefaultPricePerNight = 200m;

    public override RoomType Type => RoomType.Standard;
    public override string TypeName => "Standard";

    public StandardRoom(int number)
        : base(number, capacity: 2, new Money(DefaultPricePerNight))
    {
    }
}
