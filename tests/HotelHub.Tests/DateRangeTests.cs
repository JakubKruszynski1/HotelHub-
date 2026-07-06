using HotelHub.Domain;

namespace HotelHub.Tests;

/// <summary>
/// Testy value objectu DateRange: walidacja dat i wykrywanie nakładania się zakresów.
/// </summary>
public class DateRangeTests
{
    private static readonly DateTime Base = DateTime.Today.AddDays(10);

    [Fact]
    public void Constructor_EndBeforeStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DateRange(Base, Base.AddDays(-1)));
    }

    [Fact]
    public void Constructor_EndEqualToStart_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DateRange(Base, Base));
    }

    [Fact]
    public void Constructor_StartInPast_Throws()
    {
        var yesterday = DateTime.Today.AddDays(-1);

        Assert.Throws<ArgumentException>(() => new DateRange(yesterday, yesterday.AddDays(3)));
    }

    [Fact]
    public void Constructor_StayLongerThan30Nights_Throws()
    {
        Assert.Throws<ArgumentException>(() => new DateRange(Base, Base.AddDays(31)));
    }

    [Fact]
    public void Constructor_MaxAllowedStay_Succeeds()
    {
        var range = new DateRange(Base, Base.AddDays(30));

        Assert.Equal(30, range.Nights);
    }

    [Fact]
    public void Nights_ReturnsNumberOfNights()
    {
        var range = new DateRange(Base, Base.AddDays(3));

        Assert.Equal(3, range.Nights);
    }

    [Fact]
    public void Overlaps_OverlappingRanges_ReturnsTrue()
    {
        var first = new DateRange(Base, Base.AddDays(5));
        var second = new DateRange(Base.AddDays(3), Base.AddDays(8));

        Assert.True(first.Overlaps(second));
        Assert.True(second.Overlaps(first));
    }

    [Fact]
    public void Overlaps_ContainedRange_ReturnsTrue()
    {
        var outer = new DateRange(Base, Base.AddDays(10));
        var inner = new DateRange(Base.AddDays(2), Base.AddDays(4));

        Assert.True(outer.Overlaps(inner));
        Assert.True(inner.Overlaps(outer));
    }

    [Fact]
    public void Overlaps_AdjacentRanges_ReturnsFalse()
    {
        // Dzień wyjazdu jednego gościa może być dniem przyjazdu kolejnego.
        var first = new DateRange(Base, Base.AddDays(3));
        var second = new DateRange(Base.AddDays(3), Base.AddDays(6));

        Assert.False(first.Overlaps(second));
        Assert.False(second.Overlaps(first));
    }

    [Fact]
    public void Overlaps_DisjointRanges_ReturnsFalse()
    {
        var first = new DateRange(Base, Base.AddDays(2));
        var second = new DateRange(Base.AddDays(5), Base.AddDays(7));

        Assert.False(first.Overlaps(second));
    }

    [Fact]
    public void EachNight_ReturnsEveryNightOfStay()
    {
        var range = new DateRange(Base, Base.AddDays(3));

        var nights = range.EachNight().ToList();

        Assert.Equal([Base, Base.AddDays(1), Base.AddDays(2)], nights);
    }
}
