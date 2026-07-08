using HotelHub.Domain;

namespace HotelHub.Behavioral.States;

/// <summary>
/// Wzorzec: State (ConcreteState). Rezerwacja odrzucona przez recepcję
/// z obowiązkowym powodem — stan końcowy, brak dalszych przejść;
/// nie blokuje pokoju.
/// </summary>
public sealed class RejectedState : IReservationState
{
    public string Name => "Odrzucona";
    public bool BlocksRoom => false;
    public bool CountsTowardRevenue => false;

    public OperationResult Confirm(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można potwierdzić odrzuconej rezerwacji.");

    public OperationResult Reject(Reservation reservation, ActorContext actor, string reason) =>
        OperationResult.Fail("Rezerwacja została już odrzucona.");

    public OperationResult Pay(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można opłacić odrzuconej rezerwacji.");

    public OperationResult Cancel(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można anulować odrzuconej rezerwacji.");

    public OperationResult CheckIn(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można zameldować gościa — rezerwacja została odrzucona.");

    public OperationResult CheckOut(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można wymeldować gościa — rezerwacja została odrzucona.");
}
