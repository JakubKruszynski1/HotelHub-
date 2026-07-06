using HotelHub.Domain;

namespace HotelHub.Services;

/// <summary>
/// Symulacja płatności — wypisuje przebieg transakcji w konsoli.
/// W prawdziwym systemie w tym miejscu byłaby integracja z bramką płatności.
/// </summary>
public sealed class PaymentService
{
    /// <summary>Symuluje pobranie płatności za rezerwację. Zwraca true przy powodzeniu.</summary>
    public bool ProcessPayment(Reservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        Console.WriteLine($"[PŁATNOŚĆ] Przetwarzanie płatności {reservation.TotalPrice} za rezerwację {reservation.ShortId}...");
        Console.WriteLine("[PŁATNOŚĆ] Płatność zaakceptowana.");
        return true;
    }
}
