using System.Globalization;
using HotelHub.Domain;

namespace HotelHub.Web;

/// <summary>
/// Polskie formatowanie kwot i dat w UI: <c>1 575,00 zł</c>, <c>dd.MM.yyyy</c>.
/// Przechowywanie i serializacja pozostają w InvariantCulture.
/// </summary>
public static class Fmt
{
    private static readonly CultureInfo Polish = CultureInfo.GetCultureInfo("pl-PL");

    public static string Money(Money money) =>
        string.Format(Polish, "{0:#,0.00} zł", money.Amount);

    public static string Money(decimal amount) =>
        string.Format(Polish, "{0:#,0.00} zł", amount);

    public static string Date(DateTime date) => date.ToString("dd.MM.yyyy", Polish);

    public static string DateTimeText(DateTime date) => date.ToString("dd.MM.yyyy HH:mm", Polish);

    public static string Range(DateRange range) => $"{Date(range.From)} – {Date(range.To)}";

    public static string Nights(int nights) => nights switch
    {
        1 => "1 noc",
        2 or 3 or 4 => $"{nights} noce",
        _ => $"{nights} nocy"
    };
}
