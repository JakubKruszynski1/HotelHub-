using HotelHub.Domain;

namespace HotelHub.Behavioral.States;

/// <summary>
/// Wzorzec: State (ConcreteState). Rezerwacja anulowana —
/// stan końcowy, brak dalszych przejść; nie blokuje pokoju.
/// </summary>
public sealed class CancelledState : IReservationState
{
    public string Name => "Anulowana";
    public bool BlocksRoom => false;
    public bool CountsTowardRevenue => false;

    public void Confirm(Reservation reservation) =>
        Console.WriteLine("Nie można potwierdzić anulowanej rezerwacji.");

    public void Pay(Reservation reservation) =>
        Console.WriteLine("Nie można opłacić anulowanej rezerwacji.");

    public void Cancel(Reservation reservation) =>
        Console.WriteLine("Rezerwacja jest już anulowana.");

    public void CheckIn(Reservation reservation) =>
        Console.WriteLine("Nie można zameldować gościa — rezerwacja została anulowana.");

    public void CheckOut(Reservation reservation) =>
        Console.WriteLine("Nie można wymeldować gościa — rezerwacja została anulowana.");
}
