using HotelHub.Domain;

namespace HotelHub.Behavioral.Pricing;

/// <summary>
/// Wzorzec: Strategy (ConcreteStrategy). Taryfa standardowa —
/// cena bazowa × liczba nocy (mnożnik ×1.0).
/// </summary>
public sealed class StandardPricing : IPricingStrategy
{
    public string Name => "Standardowa";

    public Money Calculate(Money basePricePerNight, DateRange stay)
    {
        ArgumentNullException.ThrowIfNull(basePricePerNight);
        ArgumentNullException.ThrowIfNull(stay);

        return basePricePerNight * stay.Nights;
    }
}
