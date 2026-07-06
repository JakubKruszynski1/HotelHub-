using HotelHub.Domain;

namespace HotelHub.Behavioral.Observers;

/// <summary>
/// Wzorzec: Observer (ConcreteObserver). Symuluje wysyłkę e-maila do gościa —
/// wypisuje powiadomienie w konsoli z prefiksem [E-MAIL].
/// </summary>
public sealed class GuestNotifier : IReservationObserver
{
    public void OnReservationChanged(Reservation reservation, string eventDescription) =>
        Console.WriteLine(
            $"[E-MAIL] Do: {reservation.Guest.Email} | {eventDescription} | " +
            $"rezerwacja {reservation.ShortId}, {reservation.Room.GetDescription()}, {reservation.Stay}");
}
