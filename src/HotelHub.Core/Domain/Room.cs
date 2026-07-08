using HotelHub.Creational;
using HotelHub.Structural.Composite;
using HotelHub.Structural.Decorators;

namespace HotelHub.Domain;

/// <summary>
/// Wzorzec: Decorator (ConcreteComponent), Factory Method (Product)
/// oraz Composite (Leaf — liść drzewa struktury hotelu).
/// Abstrakcyjna klasa bazowa pokoju hotelowego; konkretne typy pokoi dziedziczą po niej.
/// Pokój wyłączony z użytku (remont) znika z portalu gościa i z dostępności.
/// </summary>
public abstract class Room : IRoom, IHotelComponent
{
    private readonly List<string> _amenities = [];

    public int Number { get; }
    public int Capacity { get; private set; }
    public Money BasePricePerNight { get; private set; }

    /// <summary>Opis marketingowy pokoju wyświetlany w portalu.</summary>
    public string Description { get; private set; }

    /// <summary>Udogodnienia pokoju (Wi-Fi, TV, minibar...).</summary>
    public IReadOnlyList<string> Amenities => _amenities.AsReadOnly();

    /// <summary>Ścieżka grafiki pokoju w wwwroot (lokalna kompozycja SVG).</summary>
    public string ImagePath { get; private set; }

    /// <summary>Czy pokój jest wyłączony z użytku (np. remont).</summary>
    public bool IsOutOfService { get; private set; }

    /// <summary>Powód wyłączenia z użytku.</summary>
    public string? OutOfServiceReason { get; private set; }

    public abstract RoomType Type { get; }

    /// <summary>Polska nazwa typu pokoju wyświetlana w UI.</summary>
    public abstract string TypeName { get; }

    /// <summary>Piętro pokoju wyznaczane z numeru (gałąź Composite), np. 203 → piętro 2.</summary>
    public int Floor => Number / 100;

    public string Name => $"Pokój {Number}";

    protected Room(
        int number, int capacity, Money basePricePerNight,
        string description, IEnumerable<string> amenities, string imagePath)
    {
        if (number <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(number), "Numer pokoju musi być dodatni.");
        }

        Number = number;
        BasePricePerNight = basePricePerNight ?? throw new ArgumentNullException(nameof(basePricePerNight));
        Capacity = ValidateCapacity(capacity);
        Description = description?.Trim() ?? string.Empty;
        _amenities.AddRange(amenities ?? []);
        ImagePath = imagePath?.Trim() ?? string.Empty;
    }

    /// <summary>Aktualizuje parametry pokoju (panel recepcji) z pełną walidacją.</summary>
    public void UpdateDetails(Money pricePerNight, int capacity, string description, IEnumerable<string> amenities)
    {
        BasePricePerNight = pricePerNight ?? throw new ArgumentNullException(nameof(pricePerNight));
        Capacity = ValidateCapacity(capacity);
        Description = description?.Trim() ?? string.Empty;
        _amenities.Clear();
        _amenities.AddRange((amenities ?? []).Select(a => a.Trim()).Where(a => a.Length > 0));
    }

    /// <summary>Wyłącza pokój z użytku z obowiązkowym powodem (np. remont).</summary>
    public void SetOutOfService(string reason)
    {
        reason = reason?.Trim() ?? string.Empty;

        if (reason.Length == 0)
        {
            throw new ArgumentException("Podanie powodu wyłączenia pokoju jest obowiązkowe.");
        }

        IsOutOfService = true;
        OutOfServiceReason = reason;
    }

    /// <summary>Przywraca pokój do użytku.</summary>
    public void ReturnToService()
    {
        IsOutOfService = false;
        OutOfServiceReason = null;
    }

    private static int ValidateCapacity(int capacity) =>
        capacity > 0
            ? capacity
            : throw new ArgumentOutOfRangeException(nameof(capacity), "Pojemność pokoju musi być dodatnia.");

    public virtual Money GetPrice() => BasePricePerNight;

    public virtual string GetDescription() =>
        $"Pokój {Number} ({TypeName}, {Capacity} os.)";

    /// <summary>
    /// Przychód pokoju (liść kompozytu) — suma cen opłaconych i zakończonych
    /// rezerwacji tego pokoju zarejestrowanych w <see cref="HotelRegistry"/>.
    /// </summary>
    public Money GetRevenue()
    {
        var total = Money.Zero();

        foreach (var reservation in HotelRegistry.Instance.Reservations)
        {
            if (reservation.CountsTowardRevenue &&
                RoomExtraDecorator.Unwrap(reservation.Room).Number == Number)
            {
                total += reservation.TotalPrice;
            }
        }

        return total;
    }

    public void Display(int indent) =>
        Console.WriteLine($"{new string(' ', indent * 2)}- {GetDescription()}, {GetPrice()}/noc");

    public override string ToString() => $"{GetDescription()} - {GetPrice()}/noc";
}
