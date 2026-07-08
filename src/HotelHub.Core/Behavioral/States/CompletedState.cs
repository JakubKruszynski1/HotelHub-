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

    public OperationResult Confirm(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można potwierdzić zakończonej rezerwacji.");

    public OperationResult Reject(Reservation reservation, ActorContext actor, string reason) =>
        OperationResult.Fail("Nie można odrzucić zakończonej rezerwacji.");

    public OperationResult Pay(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można opłacić zakończonej rezerwacji.");

    public OperationResult Cancel(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można anulować zakończonej rezerwacji.");

    public OperationResult CheckIn(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można zameldować gościa — rezerwacja została zakończona.");

    public OperationResult CheckOut(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Gość został już wymeldowany.");
}
