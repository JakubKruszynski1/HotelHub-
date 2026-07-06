using HotelHub.Domain;

namespace HotelHub.Structural.Decorators;

/// <summary>
/// Wzorzec: Decorator (ConcreteDecorator). Dolicza miejsce parkingowe do ceny pokoju (+25 zł/noc).
/// </summary>
public sealed class ParkingDecorator : RoomExtraDecorator
{
    public const decimal PricePerNight = 25m;

    public override RoomExtra Extra => RoomExtra.Parking;
    public override string ExtraName => "parking";
    public override Money ExtraPricePerNight => new(PricePerNight);

    public ParkingDecorator(IRoom inner)
        : base(inner)
    {
    }
}
