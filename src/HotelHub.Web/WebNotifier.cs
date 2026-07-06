using HotelHub.Behavioral.Observers;
using HotelHub.Domain;

namespace HotelHub.Web;

/// <summary>
/// Wzorzec: Observer (ConcreteObserver). Webowy odpowiednik powiadomień konsolowych —
/// przy każdej zmianie stanu rezerwacji dopisuje zdarzenia do listy w pamięci,
/// wyświetlanej chronologicznie na stronie /events (kanały E-MAIL, RECEPCJA, AUDYT).
/// </summary>
public sealed class WebNotifier : IReservationObserver
{
    /// <summary>Pojedynczy wpis panelu zdarzeń.</summary>
    public sealed record WebEvent(DateTime Timestamp, string Channel, string Message);

    private readonly object _sync = new();
    private readonly List<WebEvent> _events = [];

    /// <summary>Migawka zdarzeń (kopiowana pod blokadą — Blazor może czytać współbieżnie).</summary>
    public IReadOnlyList<WebEvent> Events
    {
        get { lock (_sync) { return _events.ToList(); } }
    }

    public void OnReservationChanged(Reservation reservation, string eventDescription)
    {
        var now = DateTime.Now;

        lock (_sync)
        {
            _events.Add(new WebEvent(now, "E-MAIL",
                $"Do: {reservation.Guest.Email} | {eventDescription} | rezerwacja {reservation.ShortId}, " +
                $"{reservation.Room.GetDescription()}, {reservation.Stay}"));
            _events.Add(new WebEvent(now, "RECEPCJA",
                $"{eventDescription} | rezerwacja {reservation.ShortId} | gość: {reservation.Guest.FullName} | " +
                $"status: {reservation.State.Name}"));
            _events.Add(new WebEvent(now, "AUDYT",
                $"{eventDescription} | rezerwacja {reservation.ShortId} | status: {reservation.State.Name} " +
                "(wpis dopisany także do audit.log)"));
        }
    }
}
