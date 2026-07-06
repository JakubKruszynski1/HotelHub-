using HotelHub.Domain;

namespace HotelHub.Structural.Decorators;

/// <summary>
/// Wzorzec: Decorator (ConcreteDecorator). Dolicza pakiet SPA do ceny pokoju (+80 zł/noc).
/// </summary>
public sealed class SpaDecorator : RoomExtraDecorator
{
    public const decimal PricePerNight = 80m;

    public override RoomExtra Extra => RoomExtra.Spa;
    public override string ExtraName => "SPA";
    public override Money ExtraPricePerNight => new(PricePerNight);

    public SpaDecorator(IRoom inner)
        : base(inner)
    {
    }
}
