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
        : base(number, capacity: 5, new Money(DefaultPricePerNight))
    {
    }
}
