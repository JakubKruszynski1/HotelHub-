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

    public OperationResult Confirm(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można potwierdzić anulowanej rezerwacji.");

    public OperationResult Reject(Reservation reservation, ActorContext actor, string reason) =>
        OperationResult.Fail("Nie można odrzucić anulowanej rezerwacji.");

    public OperationResult Pay(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można opłacić anulowanej rezerwacji.");

    public OperationResult Cancel(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Rezerwacja jest już anulowana.");

    public OperationResult CheckIn(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można zameldować gościa — rezerwacja została anulowana.");

    public OperationResult CheckOut(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można wymeldować gościa — rezerwacja została anulowana.");
}
