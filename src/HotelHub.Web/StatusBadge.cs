using HotelHub.Behavioral.States;
using HotelHub.Domain;

namespace HotelHub.Web;

/// <summary>
/// Mapowanie stanu rezerwacji (State) na kolorowy badge Bootstrapa:
/// Oczekująca — szary, Potwierdzona — niebieski, Opłacona — zielony,
/// Zakończona — granatowy, Anulowana — czerwony.
/// </summary>
public static class StatusBadge
{
    public static string CssClass(Reservation reservation) => reservation.State switch
    {
        PendingState => "badge bg-secondary",
        ConfirmedState => "badge bg-primary",
        PaidState => "badge bg-success",
        CompletedState => "badge bg-dark",
        CancelledState => "badge bg-danger",
        _ => "badge bg-light text-dark"
    };

    public static string Label(Reservation reservation) =>
        reservation.IsCheckedIn && reservation.State is PaidState
            ? $"{reservation.State.Name} (zameldowany)"
            : reservation.State.Name;
}
