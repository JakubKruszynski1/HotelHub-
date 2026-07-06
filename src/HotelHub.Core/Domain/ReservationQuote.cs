namespace HotelHub.Domain;

/// <summary>
/// Wycena pobytu przygotowana przez fasadę na potrzeby kreatora rezerwacji:
/// opis pokoju z usługami (Decorator), cena za noc, nazwa taryfy (Strategy)
/// i łączna cena pobytu.
/// </summary>
public sealed record ReservationQuote(
    string RoomDescription,
    Money PricePerNight,
    string TariffName,
    Money TotalPrice);
