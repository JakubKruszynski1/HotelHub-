using HotelHub.Domain;

namespace HotelHub.Structural.Composite;

/// <summary>
/// Wzorzec: Composite (Composite). Gałąź struktury hotelu (hotel lub piętro) —
/// zawiera listę dzieci i deleguje do nich rekurencyjnie wyliczanie przychodu
/// oraz wyświetlanie drzewa. Liściem drzewa jest <see cref="Room"/>.
/// </summary>
public sealed class HotelBranch : IHotelComponent
{
    private readonly List<IHotelComponent> _children = [];

    public string Name { get; }

    public IReadOnlyCollection<IHotelComponent> Children => _children.AsReadOnly();

    public HotelBranch(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Nazwa elementu struktury hotelu nie może być pusta.", nameof(name));
        }

        Name = name.Trim();
    }

    public void Add(IHotelComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);
        _children.Add(component);
    }

    public void Remove(IHotelComponent component)
    {
        ArgumentNullException.ThrowIfNull(component);
        _children.Remove(component);
    }

    /// <summary>Przychód gałęzi = rekurencyjna suma przychodów wszystkich dzieci.</summary>
    public Money GetRevenue()
    {
        var total = Money.Zero();

        foreach (var child in _children)
        {
            total += child.GetRevenue();
        }

        return total;
    }

    public void Display(int indent)
    {
        Console.WriteLine($"{new string(' ', indent * 2)}[{Name}]");

        foreach (var child in _children)
        {
            child.Display(indent + 1);
        }
    }

    /// <summary>
    /// Buduje drzewo hotelu z listy pokoi: piętro wyznaczane jest z numeru pokoju
    /// (np. pokój 203 leży na piętrze 2). Wykorzystywane przy seedowaniu
    /// i po wczytaniu danych z pliku JSON.
    /// </summary>
    public static HotelBranch BuildHotel(string hotelName, IEnumerable<Room> rooms)
    {
        var hotel = new HotelBranch(hotelName);

        foreach (var floorGroup in rooms.GroupBy(r => r.Number / 100).OrderBy(g => g.Key))
        {
            var floor = new HotelBranch($"Piętro {floorGroup.Key}");

            foreach (var room in floorGroup.OrderBy(r => r.Number))
            {
                floor.Add(room);
            }

            hotel.Add(floor);
        }

        return hotel;
    }

    public override string ToString() => Name;
}
