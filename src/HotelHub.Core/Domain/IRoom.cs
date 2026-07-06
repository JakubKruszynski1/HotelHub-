namespace HotelHub.Domain;

/// <summary>
/// Wzorzec: Decorator (Component). Wspólny interfejs pokoju i jego dekoratorów —
/// pozwala doliczać usługi dodatkowe bez zmiany klas pokoi.
/// </summary>
public interface IRoom
{
    /// <summary>Cena za jedną noc (wraz z ewentualnymi usługami dodatkowymi).</summary>
    Money GetPrice();

    /// <summary>Opis pokoju (wraz z ewentualnymi usługami dodatkowymi).</summary>
    string GetDescription();
}
