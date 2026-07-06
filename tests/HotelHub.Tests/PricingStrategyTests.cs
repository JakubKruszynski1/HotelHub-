using HotelHub.Behavioral.Pricing;
using HotelHub.Domain;

namespace HotelHub.Tests;

/// <summary>
/// Testy strategii cenowych (Strategy): wyliczenia każdej strategii
/// na konkretnych datach oraz automatyczny dobór strategii.
/// </summary>
public class PricingStrategyTests
{
    private static readonly Money BasePrice = new(200m);

    /// <summary>10 lipca przyszłego roku — zawsze przyszłość i zawsze wysoki sezon.</summary>
    private static DateTime NextYearJuly(int day) => new(DateTime.Today.Year + 1, 7, day);

    /// <summary>Pierwszy wskazany dzień tygodnia w październiku przyszłego roku (poza sezonem).</summary>
    private static DateTime NextYearOctober(DayOfWeek dayOfWeek)
    {
        var date = new DateTime(DateTime.Today.Year + 1, 10, 1);

        while (date.DayOfWeek != dayOfWeek)
        {
            date = date.AddDays(1);
        }

        return date;
    }

    [Fact]
    public void StandardPricing_MultipliesBasePriceByNights()
    {
        var monday = NextYearOctober(DayOfWeek.Monday);
        var stay = new DateRange(monday, monday.AddDays(3));

        var total = new StandardPricing().Calculate(BasePrice, stay);

        Assert.Equal(new Money(600m), total);
    }

    [Fact]
    public void HighSeasonPricing_JulyNights_Multiplied1_5()
    {
        var stay = new DateRange(NextYearJuly(10), NextYearJuly(13));

        var total = new HighSeasonPricing().Calculate(BasePrice, stay);

        // 3 noce lipcowe: 3 × 200 × 1.5 = 900
        Assert.Equal(new Money(900m), total);
    }

    [Fact]
    public void HighSeasonPricing_StayCrossingSeasonBoundary_OnlySeasonNightsMultiplied()
    {
        var stay = new DateRange(
            new DateTime(DateTime.Today.Year + 1, 6, 29),
            new DateTime(DateTime.Today.Year + 1, 7, 2));

        var total = new HighSeasonPricing().Calculate(BasePrice, stay);

        // Noce: 29.06 (200), 30.06 (200), 01.07 (300) = 700
        Assert.Equal(new Money(700m), total);
    }

    [Fact]
    public void WeekendPricing_FridayAndSaturdayNights_Multiplied1_2()
    {
        var friday = NextYearOctober(DayOfWeek.Friday);
        var stay = new DateRange(friday, friday.AddDays(3));

        var total = new WeekendPricing().Calculate(BasePrice, stay);

        // Noce: pt (240), sob (240), nd (200) = 680
        Assert.Equal(new Money(680m), total);
    }

    [Fact]
    public void PromoPricing_WholeStayMultiplied0_8()
    {
        var monday = NextYearOctober(DayOfWeek.Monday);
        var stay = new DateRange(monday, monday.AddDays(3));

        var total = new PromoPricing().Calculate(BasePrice, stay);

        Assert.Equal(new Money(480m), total);
    }

    [Fact]
    public void Selector_PromoCode_OverridesDates()
    {
        var stay = new DateRange(NextYearJuly(10), NextYearJuly(13));

        Assert.IsType<PromoPricing>(PricingSelector.Select(stay, "promo20"));
    }

    [Fact]
    public void Selector_JulyStay_SelectsHighSeason()
    {
        var stay = new DateRange(NextYearJuly(10), NextYearJuly(13));

        Assert.IsType<HighSeasonPricing>(PricingSelector.Select(stay));
    }

    [Fact]
    public void Selector_WeekendStayOutsideSeason_SelectsWeekend()
    {
        var friday = NextYearOctober(DayOfWeek.Friday);
        var stay = new DateRange(friday, friday.AddDays(2));

        Assert.IsType<WeekendPricing>(PricingSelector.Select(stay));
    }

    [Fact]
    public void Selector_WeekdayStayOutsideSeason_SelectsStandard()
    {
        var monday = NextYearOctober(DayOfWeek.Monday);
        var stay = new DateRange(monday, monday.AddDays(3));

        Assert.IsType<StandardPricing>(PricingSelector.Select(stay));
    }
}
