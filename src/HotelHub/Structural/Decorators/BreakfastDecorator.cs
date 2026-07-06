using HotelHub.Domain;

namespace HotelHub.Structural.Decorators;

/// <summary>
/// Wzorzec: Decorator (ConcreteDecorator). Dolicza śniadanie do ceny pokoju (+40 zł/noc).
/// </summary>
public sealed class BreakfastDecorator : RoomExtraDecorator
{
    public const decimal PricePerNight = 40m;

    public override RoomExtra Extra => RoomExtra.Breakfast;
    public override string ExtraName => "śniadanie";
    public override Money ExtraPricePerNight => new(PricePerNight);

    public BreakfastDecorator(IRoom inner)
        : base(inner)
    {
    }
}
