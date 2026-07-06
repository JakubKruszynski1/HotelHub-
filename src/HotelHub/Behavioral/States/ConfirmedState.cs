using HotelHub.Domain;

namespace HotelHub.Behavioral.States;

/// <summary>
/// Wzorzec: State (ConcreteState). Rezerwacja potwierdzona —
/// można ją opłacić lub anulować.
/// </summary>
public sealed class ConfirmedState : IReservationState
{
    public string Name => "Potwierdzona";
    public bool BlocksRoom => true;
    public bool CountsTowardRevenue => false;

    public void Confirm(Reservation reservation) =>
        Console.WriteLine("Rezerwacja jest już potwierdzona.");

    public void Pay(Reservation reservation) =>
        reservation.TransitionTo(new PaidState(), "Rezerwacja opłacona");

    public void Cancel(Reservation reservation) =>
        reservation.TransitionTo(new CancelledState(), "Rezerwacja anulowana");

    public void CheckIn(Reservation reservation) =>
        Console.WriteLine("Nie można zameldować gościa — rezerwacja nie została opłacona.");

    public void CheckOut(Reservation reservation) =>
        Console.WriteLine("Nie można wymeldować gościa — rezerwacja nie została opłacona.");
}
