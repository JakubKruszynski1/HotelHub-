using HotelHub.Behavioral.States;
using HotelHub.Domain;

namespace HotelHub.UI;

/// <summary>
/// Renderowanie tabel i komunikatów w konsoli, z kolorami statusów rezerwacji.
/// </summary>
public static class ConsoleRenderer
{
    public static void Header(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"=== {title} ===");
    }

    public static void Success(string message) => WriteColored(message, ConsoleColor.Green);

    public static void Error(string message) => WriteColored($"BŁĄD: {message}", ConsoleColor.Red);

    public static void Info(string message) => Console.WriteLine(message);

    /// <summary>Tabela pokoi: numer, typ, pojemność, cena za noc.</summary>
    public static void RenderRooms(IEnumerable<Room> rooms)
    {
        Console.WriteLine($"{"Nr",-6}{"Typ",-12}{"Osoby",-7}{"Cena/noc",-14}");
        Console.WriteLine(new string('-', 39));

        foreach (var room in rooms)
        {
            Console.WriteLine($"{room.Number,-6}{room.TypeName,-12}{room.Capacity,-7}{room.GetPrice(),-14}");
        }
    }

    /// <summary>Tabela rezerwacji: identyfikator, gość, pokój, termin, cena, status (kolorowany).</summary>
    public static void RenderReservations(IEnumerable<Reservation> reservations)
    {
        Console.WriteLine(
            $"{"Id",-10}{"Gość",-22}{"Pokój",-8}{"Termin",-35}{"Cena",-14}{"Status",-14}");
        Console.WriteLine(new string('-', 103));

        foreach (var reservation in reservations)
        {
            Console.Write($"{reservation.ShortId,-10}");
            Console.Write($"{Truncate(reservation.Guest.FullName, 20),-22}");
            Console.Write($"{reservation.BaseRoom.Number,-8}");
            Console.Write($"{reservation.Stay,-35}");
            Console.Write($"{reservation.TotalPrice,-14}");
            WriteColored(GetStatusLabel(reservation), StatusColor(reservation.State));
        }
    }

    private static string GetStatusLabel(Reservation reservation) =>
        reservation.IsCheckedIn && reservation.State is PaidState
            ? $"{reservation.State.Name} (zameldowany)"
            : reservation.State.Name;

    private static ConsoleColor StatusColor(IReservationState state) => state switch
    {
        PendingState => ConsoleColor.Yellow,
        ConfirmedState => ConsoleColor.Cyan,
        PaidState => ConsoleColor.Green,
        CompletedState => ConsoleColor.DarkGray,
        CancelledState => ConsoleColor.Red,
        _ => ConsoleColor.White
    };

    private static string Truncate(string text, int maxLength) =>
        text.Length <= maxLength ? text : text[..(maxLength - 1)] + "…";

    private static void WriteColored(string message, ConsoleColor color)
    {
        var previous = Console.ForegroundColor;
        Console.ForegroundColor = color;
        Console.WriteLine(message);
        Console.ForegroundColor = previous;
    }
}
