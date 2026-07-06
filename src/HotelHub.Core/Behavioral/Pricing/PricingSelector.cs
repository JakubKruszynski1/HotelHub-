using HotelHub.Domain;

namespace HotelHub.Behavioral.Pricing;

/// <summary>
/// Wzorzec: Strategy (Context — dobór strategii). Automatycznie wybiera politykę
/// cenową na podstawie dat pobytu, z możliwością nadpisania kodem promocyjnym PROMO20.
/// </summary>
public static class PricingSelector
{
    /// <summary>
    /// Dobiera strategię: kod PROMO20 → promocyjna; noc w lipcu/sierpniu → wysoki sezon;
    /// noc weekendowa (pt/sob) → weekendowa; w pozostałych przypadkach standardowa.
    /// </summary>
    public static IPricingStrategy Select(DateRange stay, string? promoCode = null)
    {
        ArgumentNullException.ThrowIfNull(stay);

        if (string.Equals(promoCode?.Trim(), PromoPricing.PromoCode, StringComparison.OrdinalIgnoreCase))
        {
            return new PromoPricing();
        }

        if (stay.EachNight().Any(HighSeasonPricing.IsHighSeason))
        {
            return new HighSeasonPricing();
        }

        if (stay.EachNight().Any(WeekendPricing.IsWeekendNight))
        {
            return new WeekendPricing();
        }

        return new StandardPricing();
    }
}
