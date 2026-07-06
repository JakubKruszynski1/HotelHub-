using HotelHub.Domain;

namespace HotelHub.Behavioral.States;

/// <summary>
/// Wzorzec: State (ConcreteState). Rezerwacja oczekująca na potwierdzenie —
/// można ją potwierdzić lub anulować.
/// </summary>
public sealed class PendingState : IReservationState
{
    public string Name => "Oczekująca";
    public bool BlocksRoom => true;
    public bool CountsTowardRevenue => false;

    public void Confirm(Reservation reservation) =>
        reservation.TransitionTo(new ConfirmedState(), "Rezerwacja potwierdzona");

    public void Pay(Reservation reservation) =>
        Console.WriteLine("Nie można opłacić rezerwacji, która nie została jeszcze potwierdzona.");

    public void Cancel(Reservation reservation) =>
        reservation.TransitionTo(new CancelledState(), "Rezerwacja anulowana");

    public void CheckIn(Reservation reservation) =>
        Console.WriteLine("Nie można zameldować gościa — rezerwacja nie została opłacona.");

    public void CheckOut(Reservation reservation) =>
        Console.WriteLine("Nie można wymeldować gościa — rezerwacja nie została opłacona.");
}
