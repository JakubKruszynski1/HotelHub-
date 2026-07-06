using System.Globalization;
using HotelHub.Domain;

namespace HotelHub.Behavioral.Observers;

/// <summary>
/// Wzorzec: Observer (ConcreteObserver). Dopisuje zdarzenia rezerwacyjne
/// z timestampem do pliku <c>audit.log</c> w katalogu aplikacji.
/// Ścieżka pliku jest stała — nigdy nie pochodzi od użytkownika.
/// </summary>
public sealed class AuditLogger : IReservationObserver
{
    private static readonly string LogFilePath =
        Path.Combine(AppContext.BaseDirectory, "audit.log");

    public void OnReservationChanged(Reservation reservation, string eventDescription)
    {
        var entry = string.Format(
            CultureInfo.InvariantCulture,
            "[{0:yyyy-MM-dd HH:mm:ss}] {1} | rezerwacja {2} | gość: {3} | status: {4}",
            DateTime.Now,
            eventDescription,
            reservation.ShortId,
            reservation.Guest.FullName,
            reservation.State.Name);

        try
        {
            File.AppendAllText(LogFilePath, entry + Environment.NewLine);
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            Console.WriteLine($"Ostrzeżenie: nie udało się dopisać wpisu do audit.log ({exception.Message}).");
        }
    }
}
