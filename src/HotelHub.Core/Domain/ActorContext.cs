namespace HotelHub.Domain;

/// <summary>
/// Kontekst wykonującego operację: login, rola i powiązany gość.
/// Maszyna stanów rezerwacji (State) oraz proxy fasady (Proxy) weryfikują
/// na jego podstawie, czy operacja jest dozwolona dla danej roli/właściciela.
/// </summary>
public sealed record ActorContext(string Login, UserRole? Role, Guid? GuestId, bool IsSystem = false)
{
    /// <summary>Kontekst systemowy (seed danych, procesy wewnętrzne) — pomija ograniczenia ról.</summary>
    public static readonly ActorContext System = new("system", null, null, IsSystem: true);

    /// <summary>Użytkownik niezalogowany.</summary>
    public static readonly ActorContext Anonymous = new("anonim", null, null);

    public static ActorContext ForAccount(UserAccount account)
    {
        ArgumentNullException.ThrowIfNull(account);
        return new ActorContext(account.Login, account.Role, account.GuestId);
    }

    /// <summary>Czy wykonujący ma uprawnienia recepcji.</summary>
    public bool CanActAsReception => IsSystem || Role == UserRole.Reception;

    /// <summary>Czy wykonujący jest zalogowanym gościem.</summary>
    public bool IsGuest => Role == UserRole.Guest;

    /// <summary>Czy wykonujący jest właścicielem wskazanej rezerwacji.</summary>
    public bool IsOwnerOf(Reservation reservation)
    {
        ArgumentNullException.ThrowIfNull(reservation);
        return IsSystem || (GuestId is not null && reservation.Guest.Id == GuestId);
    }
}
