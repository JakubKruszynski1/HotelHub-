using HotelHub.Domain;

namespace HotelHub.Behavioral.Pricing;

/// <summary>
/// Wzorzec: Strategy (ConcreteStrategy). Taryfa wysokiego sezonu —
/// noce przypadające na lipiec i sierpień liczone z mnożnikiem ×1.5.
/// </summary>
public sealed class HighSeasonPricing : IPricingStrategy
{
    public const decimal HighSeasonMultiplier = 1.5m;

    public string Name => "Wysoki sezon (lipiec-sierpień, x1.5)";

    public Money Calculate(Money basePricePerNight, DateRange stay)
    {
        ArgumentNullException.ThrowIfNull(basePricePerNight);
        ArgumentNullException.ThrowIfNull(stay);

        var total = Money.Zero(basePricePerNight.Currency);

        foreach (var night in stay.EachNight())
        {
            total += IsHighSeason(night)
                ? basePricePerNight * HighSeasonMultiplier
                : basePricePerNight;
        }

        return total;
    }

    /// <summary>Wysoki sezon obejmuje lipiec i sierpień.</summary>
    public static bool IsHighSeason(DateTime night) => night.Month is 7 or 8;
}
