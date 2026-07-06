using HotelHub.Domain;

namespace HotelHub.Behavioral.States;

/// <summary>
/// Wzorzec: State (ConcreteState). Rezerwacja opłacona — można zameldować gościa,
/// a po zameldowaniu wymeldować (co kończy rezerwację). Anulowanie nie jest już możliwe.
/// </summary>
public sealed class PaidState : IReservationState
{
    public string Name => "Opłacona";
    public bool BlocksRoom => true;
    public bool CountsTowardRevenue => true;

    public void Confirm(Reservation reservation) =>
        Console.WriteLine("Rezerwacja jest już potwierdzona i opłacona.");

    public void Pay(Reservation reservation) =>
        Console.WriteLine("Rezerwacja jest już opłacona.");

    public void Cancel(Reservation reservation) =>
        Console.WriteLine("Nie można anulować opłaconej rezerwacji.");

    public void CheckIn(Reservation reservation)
    {
        if (reservation.IsCheckedIn)
        {
            Console.WriteLine("Gość jest już zameldowany.");
            return;
        }

        reservation.MarkCheckedIn();
    }

    public void CheckOut(Reservation reservation)
    {
        if (!reservation.IsCheckedIn)
        {
            Console.WriteLine("Nie można wymeldować gościa, który nie został zameldowany.");
            return;
        }

        reservation.TransitionTo(new CompletedState(), "Pobyt zakończony (check-out)");
    }
}
