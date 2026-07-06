using HotelHub.Creational;
using HotelHub.Structural.Composite;
using HotelHub.Structural.Decorators;

namespace HotelHub.Domain;

/// <summary>
/// Wzorzec: Decorator (ConcreteComponent), Factory Method (Product)
/// oraz Composite (Leaf — liść drzewa struktury hotelu).
/// Abstrakcyjna klasa bazowa pokoju hotelowego; konkretne typy pokoi dziedziczą po niej.
/// </summary>
public abstract class Room : IRoom, IHotelComponent
{
    public int Number { get; }
    public int Capacity { get; }
    public Money BasePricePerNight { get; }
    public abstract RoomType Type { get; }

    /// <summary>Polska nazwa typu pokoju wyświetlana w konsoli.</summary>
    public abstract string TypeName { get; }

    public string Name => $"Pokój {Number}";

    protected Room(int number, int capacity, Money basePricePerNight)
    {
        if (number <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(number), "Numer pokoju musi być dodatni.");
        }

        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity), "Pojemność pokoju musi być dodatnia.");
        }

        Number = number;
        Capacity = capacity;
        BasePricePerNight = basePricePerNight ?? throw new ArgumentNullException(nameof(basePricePerNight));
    }

    public virtual Money GetPrice() => BasePricePerNight;

    public virtual string GetDescription() =>
        $"Pokój {Number} ({TypeName}, {Capacity} os.)";

    /// <summary>
    /// Przychód pokoju (liść kompozytu) — suma cen rezerwacji tego pokoju
    /// zarejestrowanych w <see cref="HotelRegistry"/>.
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
