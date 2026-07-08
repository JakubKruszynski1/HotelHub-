using HotelHub.Behavioral.States;
using HotelHub.Domain;

namespace HotelHub.Behavioral.Observers;

/// <summary>
/// Wzorzec: Observer (ConcreteObserver). Przy każdej zmianie stanu rezerwacji
/// publikuje powiadomienia do <see cref="NotificationCenter"/>: gość-właściciel
/// dostaje informację o losie swojej rezerwacji, kanał recepcji — o zdarzeniach
/// wymagających jej uwagi (nowa rezerwacja, płatność, anulowanie).
/// </summary>
public sealed class NotificationPublisher : IReservationObserver
{
    public void OnReservationChanged(Reservation reservation, string eventDescription)
    {
        var center = NotificationCenter.Instance;
        var guestId = reservation.Guest.Id;
        var number = reservation.ReservationNumber;

        if (eventDescription.StartsWith("Rezerwacja utworzona", StringComparison.Ordinal))
        {
            center.PublishToGuest(guestId,
                $"Przyjęliśmy Twoją rezerwację {number} — oczekuje na potwierdzenie recepcji.");
            center.PublishToReception($"Nowa rezerwacja {number} oczekuje na akceptację.");
            return;
        }

        switch (reservation.State)
        {
            case ConfirmedState:
                center.PublishToGuest(guestId,
                    $"Rezerwacja {number} została potwierdzona — możesz ją opłacić.");
                break;

            case RejectedState:
                center.PublishToGuest(guestId,
                    $"Rezerwacja {number} została odrzucona. Powód: {reservation.RejectionReason}");
                break;

            case PaidState:
                center.PublishToGuest(guestId,
                    $"Dziękujemy! Rezerwacja {number} została opłacona.");
                center.PublishToReception($"Rezerwacja {number} została opłacona.");
                break;

            case CheckedInState:
                center.PublishToGuest(guestId,
                    $"Witamy w hotelu! Zameldowano Cię w ramach rezerwacji {number}.");
                break;

            case CompletedState:
                center.PublishToGuest(guestId,
                    $"Pobyt w ramach rezerwacji {number} został zakończony. Do zobaczenia!");
                break;

            case CancelledState:
                center.PublishToGuest(guestId, $"Rezerwacja {number} została anulowana.");
                center.PublishToReception($"Rezerwacja {number} została anulowana.");
                break;
        }
    }
}
