using HotelHub.Domain;

namespace HotelHub.Behavioral.States;

/// <summary>
/// Wzorzec: State (ConcreteState). Rezerwacja potwierdzona przez recepcję —
/// opłacić może ją wyłącznie gość będący właścicielem; anulować gość-właściciel
/// lub recepcja.
/// </summary>
public sealed class ConfirmedState : IReservationState
{
    public string Name => "Potwierdzona";
    public bool BlocksRoom => true;
    public bool CountsTowardRevenue => false;

    public OperationResult Confirm(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Rezerwacja jest już potwierdzona.");

    public OperationResult Reject(Reservation reservation, ActorContext actor, string reason) =>
        OperationResult.Fail("Odrzucić można wyłącznie rezerwację oczekującą na potwierdzenie.");

    public OperationResult Pay(Reservation reservation, ActorContext actor)
    {
        if (!actor.IsOwnerOf(reservation) || (!actor.IsGuest && !actor.IsSystem))
        {
            return OperationResult.Fail("Rezerwację może opłacić wyłącznie gość będący jej właścicielem.");
        }

        reservation.TransitionTo(new PaidState(), "Rezerwacja opłacona", actor);
        return OperationResult.Ok($"Rezerwacja {reservation.ReservationNumber} została opłacona.");
    }

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
