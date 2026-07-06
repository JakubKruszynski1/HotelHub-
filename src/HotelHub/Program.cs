using System.Text;
using HotelHub.Domain;
using HotelHub.Structural;
using HotelHub.UI;

namespace HotelHub;

/// <summary>
/// Punkt wejścia aplikacji: seedowanie danych, pętla menu
/// i globalny try-catch — aplikacja nigdy nie kończy się nieobsłużonym wyjątkiem.
/// </summary>
public static class Program
{
    public static void Main()
    {
        Console.OutputEncoding = Encoding.UTF8;

        var facade = new BookingFacade();
        SeedSampleData(facade);

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

    /// <summary>
    /// Seeduje przykładowe dane: 1 hotel, 2 piętra, 8 pokoi (mieszane typy) i 2 gości —
    /// aplikację można demonstrować od razu po uruchomieniu.
    /// </summary>
    private static void SeedSampleData(BookingFacade facade)
    {
        facade.SetupHotel("Hotel Pod Różą",
            (RoomType.Standard, 101),
            (RoomType.Standard, 102),
            (RoomType.Deluxe, 103),
            (RoomType.Standard, 104),
            (RoomType.Deluxe, 201),
            (RoomType.Apartment, 202),
            (RoomType.Standard, 203),
            (RoomType.Apartment, 204));

        facade.RegisterGuest("Jan", "Kowalski", "jan.kowalski@example.com");
        facade.RegisterGuest("Anna", "Nowak", "anna.nowak@example.com");
    }
}
