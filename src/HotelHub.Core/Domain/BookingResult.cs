namespace HotelHub.Domain;

/// <summary>
/// Wynik utworzenia rezerwacji: powodzenie/odmowa z komunikatem
/// oraz utworzona rezerwacja przy powodzeniu.
/// </summary>
public sealed record BookingResult(bool Success, string Message, Reservation? Reservation)
{
    public static BookingResult Ok(Reservation reservation) =>
        new(true, $"Utworzono rezerwację {reservation.ReservationNumber}.", reservation);

    public static BookingResult Fail(string message) => new(false, message, null);
}
