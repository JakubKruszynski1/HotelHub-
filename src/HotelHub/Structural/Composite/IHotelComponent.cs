using HotelHub.Domain;

namespace HotelHub.Structural.Composite;

/// <summary>
/// Wzorzec: Composite (Component). Wspólny interfejs elementów struktury hotelu —
/// zarówno gałęzi (hotel, piętro), jak i liści (pokoje).
/// </summary>
public interface IHotelComponent
{
    /// <summary>Nazwa elementu wyświetlana w drzewie struktury.</summary>
    string Name { get; }

    /// <summary>Przychód wygenerowany przez element (dla gałęzi — rekurencyjna suma dzieci).</summary>
    Money GetRevenue();

    /// <summary>Wypisuje element (i rekurencyjnie jego dzieci) z wcięciem.</summary>
    void Display(int indent);
}
