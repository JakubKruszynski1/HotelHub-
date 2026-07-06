using System.Globalization;

namespace HotelHub.Domain;

/// <summary>
/// Value object reprezentujący zakres dat pobytu (od–do, bez godzin).
/// Walidacja w konstruktorze: koniec po początku, zakaz dat z przeszłości,
/// maksymalna długość pobytu 30 dni.
/// </summary>
public sealed class DateRange : IEquatable<DateRange>
{
    public const int MaxStayNights = 30;

    public DateTime From { get; }
    public DateTime To { get; }

    /// <summary>Liczba nocy pobytu.</summary>
    public int Nights => (To - From).Days;

    public DateRange(DateTime from, DateTime to)
    {
        from = from.Date;
        to = to.Date;

        if (to <= from)
        {
            throw new ArgumentException("Data zakończenia pobytu musi być późniejsza niż data rozpoczęcia.");
        }

        if (from < DateTime.Today)
        {
            throw new ArgumentException("Nie można utworzyć rezerwacji z datą w przeszłości.");
        }

        if ((to - from).Days > MaxStayNights)
        {
            throw new ArgumentException($"Maksymalna długość pobytu to {MaxStayNights} dni.");
        }

        From = from;
        To = to;
    }

    /// <summary>Sprawdza, czy zakresy nakładają się (przedziały półotwarte — dzień wyjazdu nie koliduje z dniem przyjazdu).</summary>
    public bool Overlaps(DateRange other) => From < other.To && other.From < To;

    /// <summary>Zwraca daty kolejnych nocy pobytu (data rozpoczęcia każdej nocy).</summary>
    public IEnumerable<DateTime> EachNight()
    {
        for (var night = From; night < To; night = night.AddDays(1))
        {
            yield return night;
        }
    }

    public bool Equals(DateRange? other) =>
        other is not null && From == other.From && To == other.To;

    public override bool Equals(object? obj) => Equals(obj as DateRange);

    public override int GetHashCode() => HashCode.Combine(From, To);

    public override string ToString() =>
        string.Format(CultureInfo.InvariantCulture, "{0:yyyy-MM-dd} - {1:yyyy-MM-dd} ({2} nocy)", From, To, Nights);
}
