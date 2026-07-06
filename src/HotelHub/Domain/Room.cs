namespace HotelHub.Domain;

/// <summary>
/// Wzorzec: Decorator (ConcreteComponent) oraz Factory Method (Product).
/// Abstrakcyjna klasa bazowa pokoju hotelowego; konkretne typy pokoi dziedziczą po niej.
/// </summary>
public abstract class Room : IRoom
{
    public int Number { get; }
    public int Capacity { get; }
    public Money BasePricePerNight { get; }
    public abstract RoomType Type { get; }

    /// <summary>Polska nazwa typu pokoju wyświetlana w konsoli.</summary>
    public abstract string TypeName { get; }

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

    public override string ToString() => $"{GetDescription()} - {GetPrice()}/noc";
}
