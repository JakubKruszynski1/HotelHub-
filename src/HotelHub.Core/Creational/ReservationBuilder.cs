using System.Text;
using HotelHub.Behavioral.Pricing;
using HotelHub.Domain;

namespace HotelHub.Creational;

/// <summary>
/// Wzorzec: Builder. Buduje rezerwację krok po kroku płynnym API:
/// <c>.ForGuest(g).WithRoom(r).Between(from, to).WithPricing(s).Build()</c>.
/// <c>Build()</c> waliduje kompletność danych i zgłasza czytelny wyjątek
/// z informacją, których elementów brakuje.
/// </summary>
public sealed class ReservationBuilder
{
    private Guest? _guest;
    private IRoom? _room;
    private DateRange? _stay;
    private IPricingStrategy? _pricing;

    public ReservationBuilder ForGuest(Guest guest)
    {
        _guest = guest ?? throw new ArgumentNullException(nameof(guest));
        return this;
    }

    public ReservationBuilder WithRoom(IRoom room)
    {
        _room = room ?? throw new ArgumentNullException(nameof(room));
        return this;
    }

    public ReservationBuilder Between(DateTime from, DateTime to)
    {
        _stay = new DateRange(from, to);
        return this;
    }

    public ReservationBuilder Between(DateRange stay)
    {
        _stay = stay ?? throw new ArgumentNullException(nameof(stay));
        return this;
    }

    public ReservationBuilder WithPricing(IPricingStrategy pricingStrategy)
    {
        _pricing = pricingStrategy ?? throw new ArgumentNullException(nameof(pricingStrategy));
        return this;
    }

    /// <summary>
    /// Tworzy rezerwację. Gdy dane są niekompletne, zgłasza
    /// <see cref="InvalidOperationException"/> z listą brakujących elementów.
    /// </summary>
    public Reservation Build()
    {
        var missing = new StringBuilder();

        if (_guest is null)
        {
            missing.Append("gość (ForGuest), ");
        }

        if (_room is null)
        {
            missing.Append("pokój (WithRoom), ");
        }

        if (_stay is null)
        {
            missing.Append("termin pobytu (Between), ");
        }

        if (missing.Length > 0)
        {
            throw new InvalidOperationException(
                $"Nie można utworzyć rezerwacji — brakuje danych: {missing.ToString().TrimEnd(',', ' ')}.");
        }

        return new Reservation(_guest!, _room!, _stay!, _pricing);
    }
}
