using HotelHub.Domain;

namespace HotelHub.Behavioral.States;

/// <summary>
/// Wzorzec: State (State). Definiuje zachowanie rezerwacji w danym stanie cyklu życia.
/// Przejścia: Oczekująca → Potwierdzona → Opłacona → Zameldowana → Zakończona;
/// odrzucenie (z powodem) z Oczekującej; anulowanie z Oczekującej/Potwierdzonej
/// (gość-właściciel lub recepcja) oraz z Opłaconej (tylko recepcja).
/// Każda operacja przyjmuje kontekst wykonującego (rola + gość) i zwraca
/// <see cref="OperationResult"/> — operacja nielegalna lub wykonana przez
/// niewłaściwą rolę jest odrzucana czytelnym komunikatem, bez wyjątków.
/// </summary>
public interface IReservationState
{
    /// <summary>Polska nazwa stanu wyświetlana w UI.</summary>
    string Name { get; }

    /// <summary>Czy rezerwacja w tym stanie blokuje pokój w swoim terminie.</summary>
    bool BlocksRoom { get; }

    /// <summary>Czy rezerwacja w tym stanie liczy się do raportu przychodów.</summary>
    bool CountsTowardRevenue { get; }

    OperationResult Confirm(Reservation reservation, ActorContext actor);
    OperationResult Reject(Reservation reservation, ActorContext actor, string reason);
    OperationResult Pay(Reservation reservation, ActorContext actor);
    OperationResult Cancel(Reservation reservation, ActorContext actor);
    OperationResult CheckIn(Reservation reservation, ActorContext actor);
    OperationResult CheckOut(Reservation reservation, ActorContext actor);
}
