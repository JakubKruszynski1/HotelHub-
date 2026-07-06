using HotelHub.Domain;
using HotelHub.Domain.RoomTypes;
using HotelHub.Structural.Decorators;

namespace HotelHub.Tests;

/// <summary>
/// Testy dekoratorów usług dodatkowych (Decorator): sumowanie cen i opisów
/// przy łańcuchu dekoratorów.
/// </summary>
public class DecoratorTests
{
    private static StandardRoom CreateRoom() => new(101);

    [Fact]
    public void BreakfastDecorator_AddsPriceAndDescription()
    {
        IRoom room = new BreakfastDecorator(CreateRoom());

        Assert.Equal(new Money(240m), room.GetPrice());
        Assert.Contains("śniadanie", room.GetDescription());
    }

    [Fact]
    public void ChainedDecorators_SumAllExtras()
    {
        IRoom room = new SpaDecorator(new ParkingDecorator(new BreakfastDecorator(CreateRoom())));

        // 200 + 40 (śniadanie) + 25 (parking) + 80 (SPA) = 345
        Assert.Equal(new Money(345m), room.GetPrice());

        var description = room.GetDescription();
        Assert.Contains("śniadanie", description);
        Assert.Contains("parking", description);
        Assert.Contains("SPA", description);
    }

    [Fact]
    public void Unwrap_ChainedDecorators_ReturnsBaseRoom()
    {
        var baseRoom = CreateRoom();
        IRoom decorated = new SpaDecorator(new BreakfastDecorator(baseRoom));

        Assert.Same(baseRoom, RoomExtraDecorator.Unwrap(decorated));
    }

    [Fact]
    public void GetExtras_ChainedDecorators_ReturnsAllExtras()
    {
        IRoom decorated = new ParkingDecorator(new BreakfastDecorator(CreateRoom()));

        var extras = RoomExtraDecorator.GetExtras(decorated);

        Assert.Equal([RoomExtra.Breakfast, RoomExtra.Parking], extras);
        Assert.True(RoomExtraDecorator.HasExtra(decorated, RoomExtra.Breakfast));
        Assert.False(RoomExtraDecorator.HasExtra(decorated, RoomExtra.Spa));
    }

    [Fact]
    public void AddExtra_OnReservation_RecalculatesTotalPrice()
    {
        var reservation = CreateReservation();

        Assert.Equal(new Money(600m), reservation.TotalPrice);

        reservation.AddExtra(RoomExtra.Breakfast);

        // (200 + 40) × 3 noce = 720
        Assert.Equal(new Money(720m), reservation.TotalPrice);
    }

    [Fact]
    public void AddExtra_Duplicate_Throws()
    {
        var reservation = CreateReservation();
        reservation.AddExtra(RoomExtra.Parking);

        Assert.Throws<InvalidOperationException>(() => reservation.AddExtra(RoomExtra.Parking));
    }

    private static Reservation CreateReservation()
    {
        var guest = new Guest("Jan", "Testowy", "jan@test.pl");
        var from = DateTime.Today.AddDays(10);

        return new Reservation(guest, CreateRoom(), new DateRange(from, from.AddDays(3)));
    }
}
