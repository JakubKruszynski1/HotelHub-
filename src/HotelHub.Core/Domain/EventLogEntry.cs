namespace HotelHub.Domain;

/// <summary>
/// Wpis dziennika zdarzeń panelu recepcji: czas, wykonawca, operacja
/// i rezerwacja, której dotyczy (agregowany z historii rezerwacji).
/// </summary>
public sealed record EventLogEntry(
    DateTime At,
    string ActorLogin,
    string Description,
    string ReservationNumber,
    string StateName);
