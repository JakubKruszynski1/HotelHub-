using HotelHub.Domain;

namespace HotelHub.Behavioral.Pricing;

/// <summary>
/// Wzorzec: Strategy (ConcreteStrategy). Taryfa promocyjna (kod PROMO20) —
/// cała cena pobytu z mnożnikiem ×0.8.
/// </summary>
public sealed class PromoPricing : IPricingStrategy
{
    public const decimal PromoMultiplier = 0.8m;

    /// <summary>Kod promocyjny aktywujący tę taryfę.</summary>
    public const string PromoCode = "PROMO20";

    public string Name => "Promocyjna (PROMO20, x0.8)";

    public Money Calculate(Money basePricePerNight, DateRange stay)
    {
        ArgumentNullException.ThrowIfNull(basePricePerNight);
        ArgumentNullException.ThrowIfNull(stay);

        return basePricePerNight * stay.Nights * PromoMultiplier;
    }
}
