using System.Globalization;

namespace HotelHub.UI;

/// <summary>
/// Bezpieczne wczytywanie danych od użytkownika: liczby przez <c>TryParse</c>,
/// daty przez <c>TryParseExact</c> (format yyyy-MM-dd, <see cref="CultureInfo.InvariantCulture"/>),
/// teksty z przycięciem, odrzuceniem pustych i limitem długości.
/// Błędne wejście nie przerywa aplikacji — pętla ponawia pytanie.
/// </summary>
public static class InputReader
{
    public const string DateFormat = "yyyy-MM-dd";

    /// <summary>Wczytuje liczbę całkowitą z zadanego zakresu (int.TryParse w pętli).</summary>
    public static int ReadInt(string prompt, int min, int max)
    {
        while (true)
        {
            Console.Write(prompt);

            if (int.TryParse(ReadLine(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var value)
                && value >= min && value <= max)
            {
                return value;
            }

            Console.WriteLine($"Nieprawidłowa wartość. Podaj liczbę całkowitą z zakresu {min}-{max}.");
        }
    }

    /// <summary>Wczytuje niepusty tekst (trim) o długości nieprzekraczającej limitu.</summary>
    public static string ReadText(string prompt, int maxLength = 50)
    {
        while (true)
        {
            Console.Write(prompt);
            var text = ReadLine().Trim();

            if (text.Length == 0)
            {
                Console.WriteLine("Wartość nie może być pusta.");
                continue;
            }

            if (text.Length > maxLength)
            {
                Console.WriteLine($"Wartość nie może przekraczać {maxLength} znaków.");
                continue;
            }

            return text;
        }
    }

    /// <summary>Wczytuje adres e-mail (proste sprawdzenie: musi zawierać '@' i '.').</summary>
    public static string ReadEmail(string prompt)
    {
        while (true)
        {
            var email = ReadText(prompt, maxLength: 100);

            if (email.Contains('@') && email.Contains('.'))
            {
                return email;
            }

            Console.WriteLine("Adres e-mail musi zawierać znaki '@' oraz '.'.");
        }
    }

    /// <summary>Wczytuje datę w formacie yyyy-MM-dd (DateTime.TryParseExact w pętli).</summary>
    public static DateTime ReadDate(string prompt)
    {
        while (true)
        {
            Console.Write(prompt);

            if (DateTime.TryParseExact(ReadLine().Trim(), DateFormat, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var date))
            {
                return date;
            }

            Console.WriteLine($"Nieprawidłowa data. Użyj formatu {DateFormat}, np. 2026-08-15.");
        }
    }

    /// <summary>Wczytuje opcjonalny tekst — Enter bez wartości zwraca null.</summary>
    public static string? ReadOptional(string prompt)
    {
        Console.Write(prompt);
        var text = ReadLine().Trim();
        return text.Length == 0 ? null : text;
    }

    /// <summary>
    /// Wczytuje linię z konsoli; koniec strumienia wejściowego zgłasza
    /// <see cref="EndOfStreamException"/>, co pozwala zakończyć aplikację zamiast zapętlić się.
    /// </summary>
    private static string ReadLine() =>
        Console.ReadLine() ?? throw new EndOfStreamException("Koniec strumienia wejściowego.");
}
