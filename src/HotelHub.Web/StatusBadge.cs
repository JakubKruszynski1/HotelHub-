using HotelHub.Behavioral.States;
using HotelHub.Domain;

namespace HotelHub.Web;

/// <summary>
/// Jedna wspólna mapa kolorów badge stanów rezerwacji w całej aplikacji:
/// Oczekująca — szary, Potwierdzona — niebieski, Opłacona — zielony,
/// Zameldowana — turkus, Zakończona — granat, Anulowana — czerwony,
/// Odrzucona — pomarańcz.
/// </summary>
public static class StatusBadge
{
    public static string CssClass(Reservation reservation) => reservation.State switch
    {
        PendingState => "badge bg-secondary",
        ConfirmedState => "badge bg-primary",
        PaidState => "badge bg-success",
        CheckedInState => "badge bg-info text-dark",
        CompletedState => "badge bg-dark",
        CancelledState => "badge bg-danger",
        RejectedState => "badge bg-warning text-dark",
        _ => "badge bg-light text-dark"
    };

    public static string Label(Reservation reservation) => reservation.State.Name;
}
