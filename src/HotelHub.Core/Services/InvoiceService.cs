using System.Text;
using HotelHub.Domain;

namespace HotelHub.Services;

/// <summary>
/// Generuje tekstowe potwierdzenie rezerwacji (fakturę) do wyświetlenia w konsoli.
/// </summary>
public sealed class InvoiceService
{
    public string GenerateInvoice(Reservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);

        var builder = new StringBuilder();
        builder.AppendLine("=========================================");
        builder.AppendLine("        HOTELHUB - POTWIERDZENIE         ");
        builder.AppendLine("=========================================");
        builder.AppendLine($"Rezerwacja: {reservation.ShortId}");
        builder.AppendLine($"Gość:       {reservation.Guest.FullName}");
        builder.AppendLine($"E-mail:     {reservation.Guest.Email}");
        builder.AppendLine($"Pokój:      {reservation.Room.GetDescription()}");
        builder.AppendLine($"Termin:     {reservation.Stay}");
        builder.AppendLine($"Cena/noc:   {reservation.Room.GetPrice()}");

        if (reservation.Pricing is not null)
        {
            builder.AppendLine($"Taryfa:     {reservation.Pricing.Name}");
        }

        builder.AppendLine("-----------------------------------------");
        builder.AppendLine($"RAZEM:      {reservation.TotalPrice}");
        builder.AppendLine("=========================================");
        return builder.ToString();
    }
}
