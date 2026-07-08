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
    public static string CssClass(Reservation reservation) => "state-badge " + reservation.State switch
    {
        PendingState => "st-pending",
        ConfirmedState => "st-confirmed",
        PaidState => "st-paid",
        CheckedInState => "st-checkedin",
        CompletedState => "st-completed",
        CancelledState => "st-cancelled",
        RejectedState => "st-rejected",
        _ => "st-pending"
    };

    public static string Label(Reservation reservation) => reservation.State.Name;
}
