using HotelHub.Domain;

namespace HotelHub.Behavioral.States;

/// <summary>
/// Wzorzec: State (State). Definiuje zachowanie rezerwacji w danym stanie cyklu życia.
/// Przejścia: Oczekująca → Potwierdzona → Opłacona → Zakończona;
/// anulowanie możliwe z Oczekującej i Potwierdzonej.
/// Nielegalna operacja nie zgłasza wyjątku — wypisuje komunikat w konsoli.
/// </summary>
public interface IReservationState
{
    /// <summary>Polska nazwa stanu wyświetlana w konsoli.</summary>
    string Name { get; }

    /// <summary>Czy rezerwacja w tym stanie blokuje pokój w swoim terminie.</summary>
    bool BlocksRoom { get; }

    /// <summary>Czy rezerwacja w tym stanie liczy się do raportu przychodów.</summary>
    bool CountsTowardRevenue { get; }

    void Confirm(Reservation reservation);
    void Pay(Reservation reservation);
    void Cancel(Reservation reservation);
    void CheckIn(Reservation reservation);
    void CheckOut(Reservation reservation);
}
