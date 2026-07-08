using HotelHub.Domain;

namespace HotelHub.Behavioral.States;

/// <summary>
/// Wzorzec: State (ConcreteState). Gość zameldowany — pobyt w toku.
/// Jedyna możliwa operacja to wymeldowanie przez recepcję (kończy rezerwację).
/// </summary>
public sealed class CheckedInState : IReservationState
{
    public string Name => "Zameldowana";
    public bool BlocksRoom => true;
    public bool CountsTowardRevenue => true;

    public OperationResult Confirm(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Gość jest już zameldowany.");

    public OperationResult Reject(Reservation reservation, ActorContext actor, string reason) =>
        OperationResult.Fail("Odrzucić można wyłącznie rezerwację oczekującą na potwierdzenie.");

    public OperationResult Pay(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Rezerwacja jest już opłacona.");

    public OperationResult Cancel(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Nie można anulować rezerwacji w trakcie pobytu.");

    public OperationResult CheckIn(Reservation reservation, ActorContext actor) =>
        OperationResult.Fail("Gość jest już zameldowany.");

    public OperationResult CheckOut(Reservation reservation, ActorContext actor)
    {
        if (!actor.CanActAsReception)
        {
            return OperationResult.Fail("Wymeldowania gościa dokonuje wyłącznie recepcja.");
        }

        reservation.TransitionTo(new CompletedState(), "Pobyt zakończony (check-out)", actor);
        return OperationResult.Ok($"Gość wymeldowany — rezerwacja {reservation.ReservationNumber} zakończona.");
    }
}
