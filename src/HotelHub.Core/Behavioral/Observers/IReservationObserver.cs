using HotelHub.Domain;

namespace HotelHub.Behavioral.Observers;

/// <summary>
/// Wzorzec: Observer (Observer). Subskrybent powiadamiany przy każdej
/// zmianie stanu rezerwacji (podmiotem jest <see cref="Reservation"/>).
/// </summary>
public interface IReservationObserver
{
    /// <summary>Wywoływane przez rezerwację po każdej zmianie jej stanu.</summary>
    void OnReservationChanged(Reservation reservation, string eventDescription);
}
