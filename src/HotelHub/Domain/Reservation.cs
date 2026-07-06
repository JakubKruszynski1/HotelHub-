using HotelHub.Behavioral.Pricing;
using HotelHub.Structural.Decorators;

namespace HotelHub.Domain;

/// <summary>
/// Rezerwacja pokoju hotelowego dla gościa w zadanym terminie.
/// Enkapsulacja: settery prywatne, modyfikacje wyłącznie przez metody domenowe.
/// </summary>
public sealed class Reservation
{
    public Guid Id { get; }
    public Guest Guest { get; }

    /// <summary>Pokój — może być opakowany dekoratorami usług dodatkowych.</summary>
    public IRoom Room { get; private set; }

    public DateRange Stay { get; }

    /// <summary>Strategia cenowa użyta do wyliczenia ceny (Strategy); brak = cena bazowa × liczba nocy.</summary>
    public IPricingStrategy? Pricing { get; }

    public Money TotalPrice { get; private set; }

    /// <summary>Bazowy pokój po zdjęciu wszystkich dekoratorów.</summary>
    public Room BaseRoom => RoomExtraDecorator.Unwrap(Room);

    /// <summary>Skrócony identyfikator do wyświetlania i wyszukiwania w konsoli.</summary>
    public string ShortId => Id.ToString()[..8];

    /// <summary>Czy rezerwacja blokuje pokój w swoim terminie.</summary>
    public bool BlocksRoom => true;

    public Reservation(Guest guest, IRoom room, DateRange stay, IPricingStrategy? pricing = null)
        : this(Guid.NewGuid(), guest, room, stay, pricing)
    {
    }

    public Reservation(Guid id, Guest guest, IRoom room, DateRange stay, IPricingStrategy? pricing = null)
    {
        Id = id;
        Guest = guest ?? throw new ArgumentNullException(nameof(guest));
        Room = room ?? throw new ArgumentNullException(nameof(room));
        Stay = stay ?? throw new ArgumentNullException(nameof(stay));
        Pricing = pricing;
        TotalPrice = CalculateTotal();
    }

    /// <summary>
    /// Dodaje usługę dodatkową (Decorator) do pokoju i przelicza cenę całkowitą.
    /// </summary>
    public void AddExtra(RoomExtra extra)
    {
        if (RoomExtraDecorator.HasExtra(Room, extra))
        {
            throw new InvalidOperationException("Ta usługa jest już dodana do rezerwacji.");
        }

        Room = RoomExtraDecorator.Apply(Room, extra);
        TotalPrice = CalculateTotal();
    }

    private Money CalculateTotal() =>
        Pricing is not null
            ? Pricing.Calculate(Room.GetPrice(), Stay)
            : Room.GetPrice() * Stay.Nights;

    public override string ToString() =>
        $"Rezerwacja {ShortId} | {Guest.FullName} | {Room.GetDescription()} | {Stay} | {TotalPrice}";
}
