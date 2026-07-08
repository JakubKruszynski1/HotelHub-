using HotelHub.Behavioral.States;
using HotelHub.Creational;
using HotelHub.Domain;
using HotelHub.Domain.RoomTypes;
using HotelHub.Structural;

namespace HotelHub.Tests;

/// <summary>
/// Testy wzorca Proxy (Protection Proxy): odmowa dostępu z komunikatem
/// (nigdy ominięcie reguły) oraz poprawna delegacja dozwolonych operacji
/// do fasady właściwej.
/// </summary>
public class ProxyTests
{
    private static IBookingFacade ProxyFor(ActorContext actor) =>
        new AuthorizedBookingFacade(new FixedCurrentUser(actor));

    private static ActorContext OwnerOf(Reservation reservation) =>
        new("wlasciciel", UserRole.Guest, reservation.Guest.Id);

    private static readonly ActorContext Reception = new("recepcja", UserRole.Reception, null);

    [Fact]
    public void Guest_CannotConfirmReservation()
    {
        var reservation = CreateReservation();
        var proxy = ProxyFor(OwnerOf(reservation));

        var result = proxy.ConfirmReservation(reservation);

        Assert.False(result.Success);
        Assert.IsType<PendingState>(reservation.State);
    }

    [Fact]
    public void Reception_CannotPayReservation()
    {
        var reservation = CreateReservation();
        reservation.Confirm(Reception);

        var result = ProxyFor(Reception).PayReservation(reservation);

        Assert.False(result.Success);
        Assert.IsType<ConfirmedState>(reservation.State);
    }

    [Fact]
    public void Guest_CannotPaySomeoneElsesReservation()
    {
        var reservation = CreateReservation();
        reservation.Confirm(Reception);
        var stranger = new ActorContext("obcy", UserRole.Guest, Guid.NewGuid());

        var result = ProxyFor(stranger).PayReservation(reservation);

        Assert.False(result.Success);
        Assert.IsType<ConfirmedState>(reservation.State);
    }

    [Fact]
    public void Owner_CanPay_AndReception_CanConfirm_ThroughProxy()
    {
        var reservation = CreateReservation();

        Assert.True(ProxyFor(Reception).ConfirmReservation(reservation).Success);
        Assert.True(ProxyFor(OwnerOf(reservation)).PayReservation(reservation).Success);
        Assert.IsType<PaidState>(reservation.State);
    }

    [Fact]
    public void Guest_CannotSaveOrLoadData()
    {
        var proxy = ProxyFor(new ActorContext("gosc", UserRole.Guest, Guid.NewGuid()));

        Assert.False(proxy.SaveData().Success);
        Assert.False(proxy.LoadData().Success);
    }

    [Fact]
    public void Guest_CannotManageRooms()
    {
        var proxy = ProxyFor(new ActorContext("gosc", UserRole.Guest, Guid.NewGuid()));

        Assert.False(proxy.AddRoom(RoomType.Standard, 999).Success);
        Assert.False(proxy.SetRoomOutOfService(101, "Remont").Success);
    }

    [Fact]
    public void Guest_SeesOnlyOwnReservations()
    {
        var mine = CreateReservation();
        var someoneElses = CreateReservation();
        HotelRegistry.Instance.AddReservation(mine);
        HotelRegistry.Instance.AddReservation(someoneElses);

        var proxy = ProxyFor(OwnerOf(mine));

        Assert.Contains(proxy.GetReservationsForGuest(mine.Guest.Id), r => r.Id == mine.Id);
        Assert.Empty(proxy.GetReservationsForGuest(someoneElses.Guest.Id));
        Assert.Empty(proxy.Reservations);
        Assert.Empty(proxy.Guests);
        Assert.Empty(proxy.GetEventLog());
    }

    [Fact]
    public void Reception_SeesAllReservations()
    {
        var reservation = CreateReservation();
        HotelRegistry.Instance.AddReservation(reservation);

        var proxy = ProxyFor(Reception);

        Assert.Contains(proxy.Reservations, r => r.Id == reservation.Id);
    }

    [Fact]
    public void Anonymous_CannotMakeReservation()
    {
        var proxy = ProxyFor(ActorContext.Anonymous);
        var from = DateTime.Today.AddDays(50);

        var result = proxy.MakeReservation(new StandardRoom(998), new DateRange(from, from.AddDays(2)));

        Assert.False(result.Success);
        Assert.Null(result.Reservation);
    }

    private static Reservation CreateReservation()
    {
        var guest = new Guest("Proxy", "Testowy", $"proxy.{Guid.NewGuid():N}@test.pl");
        var from = DateTime.Today.AddDays(60);

        return new Reservation(guest, new StandardRoom(101), new DateRange(from, from.AddDays(2)));
    }
}
