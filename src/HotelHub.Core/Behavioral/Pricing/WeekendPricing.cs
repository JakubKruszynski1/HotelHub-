using HotelHub.Domain;

namespace HotelHub.Behavioral.Pricing;

/// <summary>
/// Wzorzec: Strategy (ConcreteStrategy). Taryfa weekendowa —
/// noce z piątku na sobotę i z soboty na niedzielę liczone z mnożnikiem ×1.2.
/// </summary>
public sealed class WeekendPricing : IPricingStrategy
{
    public const decimal WeekendMultiplier = 1.2m;

    public string Name => "Weekendowa (noce pt/sob, x1.2)";

    public Money Calculate(Money basePricePerNight, DateRange stay)
    {
        ArgumentNullException.ThrowIfNull(basePricePerNight);
        ArgumentNullException.ThrowIfNull(stay);

        var total = Money.Zero(basePricePerNight.Currency);

        foreach (var night in stay.EachNight())
        {
            total += IsWeekendNight(night)
                ? basePricePerNight * WeekendMultiplier
                : basePricePerNight;
        }

        return total;
    }

    /// <summary>Noc weekendowa zaczyna się w piątek lub sobotę.</summary>
    public static bool IsWeekendNight(DateTime night) =>
        night.DayOfWeek is DayOfWeek.Friday or DayOfWeek.Saturday;
}
