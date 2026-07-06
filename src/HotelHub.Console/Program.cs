using System.Text;
using HotelHub.Structural;
using HotelHub.UI;

namespace HotelHub;

/// <summary>
/// Punkt wejścia aplikacji konsolowej: seedowanie danych, pętla menu
/// i globalny try-catch — aplikacja nigdy nie kończy się nieobsłużonym wyjątkiem.
/// </summary>
public static class Program
{
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var facade = new BookingFacade();
        facade.SeedSampleData();

        Console.WriteLine("Witaj w systemie HotelHub!");
        Console.WriteLine("Załadowano przykładowe dane: hotel Pod Różą (2 piętra, 8 pokoi) i 2 gości.");

        var menu = new ConsoleMenu(facade);
        var running = true;

        while (running)
        {
            try
            {
                running = menu.ShowAndHandle();
            }
            catch (EndOfStreamException)
            {
                // Koniec strumienia wejściowego (np. przekierowane wejście) — kończymy pracę.
                running = false;
            }
            catch (Exception exception)
            {
                ConsoleRenderer.Error($"Wystąpił nieoczekiwany błąd: {exception.Message}");
                ConsoleRenderer.Info("Powrót do menu głównego.");
            }
        }

        Console.WriteLine("Do zobaczenia!");
    }
}
