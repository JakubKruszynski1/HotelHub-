using HotelHub.Domain;
using HotelHub.Domain.RoomTypes;

namespace HotelHub.Tests;

/// <summary>
/// Macierz autoryzacji przejść stanów: każda operacja × rola wykonującego
/// (recepcja / gość-właściciel / obcy gość) × stan rezerwacji.
/// Operacja niedozwolona jest odrzucana komunikatem — bez wyjątku i bez zmiany stanu.
/// </summary>
public class AuthorizationMatrixTests
{
    private static readonly ActorContext Reception = new("recepcja", UserRole.Reception, null);

    [Theory]
    // Potwierdzanie — wyłącznie recepcja, tylko z Oczekującej.
    [InlineData("Pending", "Confirm", "reception", true)]
    [InlineData("Pending", "Confirm", "owner", false)]
    [InlineData("Pending", "Confirm", "stranger", false)]
    [InlineData("Cancelled", "Confirm", "reception", false)]
    // Odrzucanie — wyłącznie recepcja, tylko z Oczekującej.
    [InlineData("Pending", "Reject", "reception", true)]
    [InlineData("Pending", "Reject", "owner", false)]
    [InlineData("Confirmed", "Reject", "reception", false)]
    // Opłacenie — wyłącznie gość-właściciel, tylko z Potwierdzonej.
    [InlineData("Confirmed", "Pay", "owner", true)]
    [InlineData("Confirmed", "Pay", "reception", false)]
    [InlineData("Confirmed", "Pay", "stranger", false)]
    [InlineData("Pending", "Pay", "owner", false)]
    [InlineData("Rejected", "Pay", "owner", false)]
    // Anulowanie — właściciel lub recepcja z Oczekującej/Potwierdzonej; z Opłaconej tylko recepcja.
    [InlineData("Pending", "Cancel", "owner", true)]
    [InlineData("Pending", "Cancel", "reception", true)]
    [InlineData("Pending", "Cancel", "stranger", false)]
    [InlineData("Confirmed", "Cancel", "owner", true)]
    [InlineData("Paid", "Cancel", "reception", true)]
    [InlineData("Paid", "Cancel", "owner", false)]
    [InlineData("CheckedIn", "Cancel", "reception", false)]
    [InlineData("Completed", "Cancel", "reception", false)]
    // Meldowanie i wymeldowanie — wyłącznie recepcja, we właściwych stanach.
    [InlineData("Paid", "CheckIn", "reception", true)]
    [InlineData("Paid", "CheckIn", "owner", false)]
    [InlineData("Confirmed", "CheckIn", "reception", false)]
    [InlineData("CheckedIn", "CheckOut", "reception", true)]
    [InlineData("CheckedIn", "CheckOut", "owner", false)]
    [InlineData("Paid", "CheckOut", "reception", false)]
    public void OperationAuthorizationMatrix(string state, string operation, string actorKind, bool expected)
    {
        var reservation = BuildInState(state);
        var stateBefore = reservation.State.GetType();
        var actor = ActorOfKind(actorKind, reservation);

        var result = operation switch
        {
            "Confirm" => reservation.Confirm(actor),
            "Reject" => reservation.Reject("Powód testowy", actor),
            "Pay" => reservation.Pay(actor),
            "Cancel" => reservation.Cancel(actor),
            "CheckIn" => reservation.CheckIn(actor),
            _ => reservation.CheckOut(actor)
        };

        Assert.Equal(expected, result.Success);
        Assert.False(string.IsNullOrWhiteSpace(result.Message));

        if (!expected)
        {
            Assert.IsType(stateBefore, reservation.State);
        }
    }

    private static ActorContext ActorOfKind(string kind, Reservation reservation) => kind switch
    {
        "reception" => Reception,
        "owner" => new ActorContext("wlasciciel", UserRole.Guest, reservation.Guest.Id),
        _ => new ActorContext("obcy", UserRole.Guest, Guid.NewGuid())
    };

    private static Reservation BuildInState(string state)
    {
        var guest = new Guest("Test", "Macierzowy", $"matrix.{Guid.NewGuid():N}@test.pl");
        var from = DateTime.Today.AddDays(10);
        var reservation = new Reservation(guest, new StandardRoom(101), new DateRange(from, from.AddDays(3)));
        var owner = new ActorContext("wlasciciel", UserRole.Guest, guest.Id);

        switch (state)
        {
            case "Confirmed":
                reservation.Confirm(Reception);
                break;
            case "Paid":
                reservation.Confirm(Reception);
                reservation.Pay(owner);
                break;
            case "CheckedIn":
                reservation.Confirm(Reception);
                reservation.Pay(owner);
                reservation.CheckIn(Reception);
                break;
            case "Completed":
                reservation.Confirm(Reception);
                reservation.Pay(owner);
                reservation.CheckIn(Reception);
                reservation.CheckOut(Reception);
                break;
            case "Cancelled":
                reservation.Cancel(owner);
                break;
            case "Rejected":
                reservation.Reject("Powód testowy", Reception);
                break;
        }

        return reservation;
    }
}
