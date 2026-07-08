namespace HotelHub.Domain;

/// <summary>
/// Wpis historii rezerwacji: znacznik czasu przejścia, nazwa stanu docelowego,
/// opis zdarzenia i login wykonującego operację (do dziennika zdarzeń).
/// </summary>
public sealed record StateChange(
    DateTime At,
    string StateName,
    string Description,
    string ActorLogin);
