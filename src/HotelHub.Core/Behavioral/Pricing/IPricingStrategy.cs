using HotelHub.Domain;

namespace HotelHub.Behavioral.Pricing;

/// <summary>
/// Wzorzec: Strategy (Strategy). Definiuje wymienną politykę cenową —
/// sposób wyliczania łącznej ceny pobytu z ceny bazowej za noc.
/// </summary>
public interface IPricingStrategy
{
    /// <summary>Czytelna nazwa strategii wyświetlana w konsoli i na fakturze.</summary>
    string Name { get; }

    /// <summary>Wylicza łączną cenę pobytu na podstawie ceny bazowej za noc i zakresu dat.</summary>
    Money Calculate(Money basePricePerNight, DateRange stay);
}
