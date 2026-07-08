using HotelHub.Domain;

namespace HotelHub.Behavioral.States;

/// <summary>
/// Wzorzec: State (ConcreteState). Rezerwacja opłacona — recepcja może
/// zameldować gościa lub (na jego prośbę) anulować rezerwację;
/// gość nie może już anulować samodzielnie.
/// </summary>
public sealed class PaidState : IReservationState
{
    public string Name => "Opłacona";
    public bool BlocksRoom => true;
    public bool CountsTowardRevenue => true;

    public OperationResult Confirm(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Rezerwacja jest już potwierdzona i opłacona.");

    public OperationResult Reject(Reservation reservation, ActorContext actor, string reason) =>
        OperationResult.Fail("Odrzucić można wyłącznie rezerwację oczekującą na potwierdzenie.");

    public OperationResult Pay(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Rezerwacja jest już opłacona.");

    public OperationResult Cancel(Reservation reservation, ActorContext actor)
    {
        if (!actor.CanActAsReception)
        {
            return OperationResult.Fail(
                "Opłaconą rezerwację może anulować wyłącznie recepcja — skontaktuj się z hotelem.");
        }

        reservation.TransitionTo(new CancelledState(), "Opłacona rezerwacja anulowana przez recepcję", actor);
        return OperationResult.Ok($"Rezerwacja {reservation.ReservationNumber} została anulowana.");
    }

    public OperationResult CheckIn(Reservation reservation, ActorContext actor)
    {
        if (!actor.CanActAsReception)
        {
            return OperationResult.Fail("Zameldowania gościa dokonuje wyłącznie recepcja.");
        }

        reservation.TransitionTo(new CheckedInState(), "Gość zameldowany (check-in)", actor);
        return OperationResult.Ok($"Gość zameldowany — rezerwacja {reservation.ReservationNumber}.");
    }

    public OperationResult CheckOut(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można wymeldować gościa, który nie został zameldowany.");
}
