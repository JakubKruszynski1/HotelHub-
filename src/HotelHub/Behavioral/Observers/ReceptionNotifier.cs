using HotelHub.Domain;

namespace HotelHub.Behavioral.Observers;

/// <summary>
/// Wzorzec: Observer (ConcreteObserver). Powiadamia recepcję o zmianie stanu
/// rezerwacji — wypisuje komunikat w konsoli z prefiksem [RECEPCJA].
/// </summary>
public sealed class ReceptionNotifier : IReservationObserver
{
    public void OnReservationChanged(Reservation reservation, string eventDescription) =>
        Console.WriteLine(
            $"[RECEPCJA] {eventDescription} | rezerwacja {reservation.ShortId} | " +
            $"gość: {reservation.Guest.FullName} | status: {reservation.State.Name}");
}
