using HotelHub.Domain;

namespace HotelHub.Behavioral.States;

/// <summary>
/// Wzorzec: State (ConcreteState). Rezerwacja oczekująca na decyzję recepcji —
/// recepcja może ją potwierdzić lub odrzucić (z powodem); anulować może
/// gość-właściciel lub recepcja.
/// </summary>
public sealed class PendingState : IReservationState
{
    public string Name => "Oczekująca";
    public bool BlocksRoom => true;
    public bool CountsTowardRevenue => false;

    public OperationResult Confirm(Reservation reservation, ActorContext actor)
    {
        if (!actor.CanActAsReception)
        {
            return OperationResult.Fail("Rezerwację może potwierdzić wyłącznie recepcja.");
        }

        reservation.TransitionTo(new ConfirmedState(), "Rezerwacja potwierdzona przez recepcję", actor);
        return OperationResult.Ok($"Rezerwacja {reservation.ReservationNumber} została potwierdzona.");
    }

    public OperationResult Reject(Reservation reservation, ActorContext actor, string reason)
    {
        if (!actor.CanActAsReception)
        {
            return OperationResult.Fail("Rezerwację może odrzucić wyłącznie recepcja.");
        }

        reason = reason?.Trim() ?? string.Empty;

        if (reason.Length == 0)
        {
            return OperationResult.Fail("Podanie powodu odrzucenia jest obowiązkowe.");
        }

        reservation.SetRejectionReason(reason);
        reservation.TransitionTo(new RejectedState(), $"Rezerwacja odrzucona: {reason}", actor);
        return OperationResult.Ok($"Rezerwacja {reservation.ReservationNumber} została odrzucona.");
    }

    public OperationResult Pay(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Rezerwacja oczekuje na potwierdzenie recepcji — nie można jej jeszcze opłacić.");

    public OperationResult Cancel(Reservation reservation, ActorContext actor)
    {
        if (!actor.CanActAsReception && !actor.IsOwnerOf(reservation))
        {
            return OperationResult.Fail("Możesz anulować wyłącznie własną rezerwację.");
        }

        reservation.TransitionTo(new CancelledState(), "Rezerwacja anulowana", actor);
        return OperationResult.Ok($"Rezerwacja {reservation.ReservationNumber} została anulowana.");
    }

    public OperationResult CheckIn(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można zameldować gościa — rezerwacja nie została opłacona.");

    public OperationResult CheckOut(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można wymeldować gościa — rezerwacja nie została opłacona.");
}
