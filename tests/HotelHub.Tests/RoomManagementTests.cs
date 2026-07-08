using System.Text.RegularExpressions;
using HotelHub.Creational;
using HotelHub.Domain;
using HotelHub.Domain.RoomTypes;
using HotelHub.Structural;

namespace HotelHub.Tests;

/// <summary>
/// Testy zarządzania pokojami (blokada wyłączenia pokoju z aktywnymi rezerwacjami)
/// oraz numeracji rezerwacji (format RES-RRRR-NNNN, atomowy licznik).
/// </summary>
public class RoomManagementTests
{
    private static readonly ActorContext Reception = new("recepcja", UserRole.Reception, null);

    [Fact]
    public void ReservationNumbers_AreSequentialAndWellFormatted()
    {
        var registry = HotelRegistry.Instance;

        var first = registry.NextReservationNumber();
        var second = registry.NextReservationNumber();

        Assert.Matches(new Regex(@"^RES-\d{4}-\d{4}$"), first);
        Assert.Matches(new Regex(@"^RES-\d{4}-\d{4}$"), second);

        var firstNumber = int.Parse(first.Split('-')[2]);
        var secondNumber = int.Parse(second.Split('-')[2]);
        Assert.Equal(firstNumber + 1, secondNumber);
    }

    [Fact]
    public void AddReservation_AssignsReservationNumber()
    {
        var guest = new Guest("Numer", "Testowy", $"num.{Guid.NewGuid():N}@test.pl");
        var from = DateTime.Today.AddDays(90);
        var reservation = new Reservation(guest, new StandardRoom(101), new DateRange(from, from.AddDays(2)));

        Assert.Equal(string.Empty, reservation.ReservationNumber);

        HotelRegistry.Instance.AddReservation(reservation);

        Assert.StartsWith("RES-", reservation.ReservationNumber);
    }

    [Fact]
    public void CannotDisableRoom_WithActiveReservation()
    {
        var facade = new BookingFacade();
        const int roomNumber = 951;
        Assert.True(facade.AddRoom(RoomType.Standard, roomNumber).Success);

        var registration = facade.RegisterGuestAccount(
            $"aktywny_{Guid.NewGuid():N}"[..20], "HasloGoscia1",
            "Aktywny", "Gość", $"aktywny.{Guid.NewGuid():N}@test.pl");
        Assert.True(registration.Success);

        var guest = facade.Guests.First(g => g.FirstName == "Aktywny");
        var owner = new ActorContext("aktywny", UserRole.Guest, guest.Id);
        var from = DateTime.Today.AddDays(100);

        var reservation = facade.MakeReservation(
            guest, facade.FindRoomByNumber(roomNumber)!, new DateRange(from, from.AddDays(2)), actor: owner);
        reservation.Confirm(Reception);
        reservation.Pay(owner);

        // Opłacona rezerwacja blokuje wyłączenie pokoju.
        var blocked = facade.SetRoomOutOfService(roomNumber, "Remont łazienki");
        Assert.False(blocked.Success);
        Assert.False(facade.FindRoomByNumber(roomNumber)!.IsOutOfService);

        // Po anulowaniu przez recepcję wyłączenie jest możliwe.
        Assert.True(reservation.Cancel(Reception).Success);
        Assert.True(facade.SetRoomOutOfService(roomNumber, "Remont łazienki").Success);
        Assert.True(facade.FindRoomByNumber(roomNumber)!.IsOutOfService);
    }

    [Fact]
    public void DisablingRoom_RequiresReason()
    {
        var facade = new BookingFacade();
        const int roomNumber = 952;
        Assert.True(facade.AddRoom(RoomType.Deluxe, roomNumber).Success);

        Assert.False(facade.SetRoomOutOfService(roomNumber, "  ").Success);
        Assert.False(facade.FindRoomByNumber(roomNumber)!.IsOutOfService);
    }

    [Fact]
    public void DisabledRoom_DisappearsFromAvailability()
    {
        var facade = new BookingFacade();
        const int roomNumber = 953;
        Assert.True(facade.AddRoom(RoomType.Apartment, roomNumber).Success);

        var from = DateTime.Today.AddDays(110);
        var stay = new DateRange(from, from.AddDays(2));

        Assert.Contains(facade.GetAvailableRooms(stay), r => r.Number == roomNumber);

        Assert.True(facade.SetRoomOutOfService(roomNumber, "Modernizacja instalacji").Success);
        Assert.DoesNotContain(facade.GetAvailableRooms(stay), r => r.Number == roomNumber);

        Assert.True(facade.ReturnRoomToService(roomNumber).Success);
        Assert.Contains(facade.GetAvailableRooms(stay), r => r.Number == roomNumber);
    }
}
