using HotelHub.Domain;

namespace HotelHub.Behavioral.States;

/// <summary>
/// Wzorzec: State (ConcreteState). Rezerwacja zakończona (gość wymeldowany) —
/// stan końcowy, brak dalszych przejść.
/// </summary>
public sealed class CompletedState : IReservationState
{
    public string Name => "Zakończona";
    public bool BlocksRoom => false;
    public bool CountsTowardRevenue => true;

    public void Confirm(Reservation reservation) =>
        Console.WriteLine("Nie można potwierdzić zakończonej rezerwacji.");

    public void Pay(Reservation reservation) =>
        Console.WriteLine("Nie można opłacić zakończonej rezerwacji.");

    public void Cancel(Reservation reservation) =>
        Console.WriteLine("Nie można anulować zakończonej rezerwacji.");

    public void CheckIn(Reservation reservation) =>
        Console.WriteLine("Nie można zameldować gościa — rezerwacja została zakończona.");

    public void CheckOut(Reservation reservation) =>
        Console.WriteLine("Gość został już wymeldowany.");
}
