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
    public Money TotalPrice { get; private set; }

    public Reservation(Guest guest, IRoom room, DateRange stay, Money totalPrice)
        : this(Guid.NewGuid(), guest, room, stay, totalPrice)
    {
    }

    public Reservation(Guid id, Guest guest, IRoom room, DateRange stay, Money totalPrice)
    {
        Id = id;
        Guest = guest ?? throw new ArgumentNullException(nameof(guest));
        Room = room ?? throw new ArgumentNullException(nameof(room));
        Stay = stay ?? throw new ArgumentNullException(nameof(stay));
        TotalPrice = totalPrice ?? throw new ArgumentNullException(nameof(totalPrice));
    }

    /// <summary>
    /// Podmienia pokój na wersję opakowaną dekoratorem usług dodatkowych
    /// i aktualizuje cenę całkowitą rezerwacji.
    /// </summary>
    public void UpgradeRoom(IRoom decoratedRoom, Money newTotalPrice)
    {
        Room = decoratedRoom ?? throw new ArgumentNullException(nameof(decoratedRoom));
        TotalPrice = newTotalPrice ?? throw new ArgumentNullException(nameof(newTotalPrice));
    }

    public override string ToString() =>
        $"Rezerwacja {Id.ToString()[..8]} | {Guest.FullName} | {Room.GetDescription()} | {Stay} | {TotalPrice}";
}
