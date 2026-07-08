using HotelHub.Creational;
using HotelHub.Domain;
using Microsoft.AspNetCore.Identity;

namespace HotelHub.Services;

/// <summary>
/// Serwis kont użytkowników: tworzenie kont, weryfikacja poświadczeń
/// i zmiana hasła. Hasła wyłącznie hashowane przez
/// <see cref="PasswordHasher{TUser}"/> — nigdzie w postaci jawnej.
/// </summary>
public sealed class AccountService
{
    private readonly PasswordHasher<UserAccount> _hasher = new();
    private readonly HotelRegistry _registry = HotelRegistry.Instance;

    /// <summary>Tworzy konto gościa powiązane z istniejącą encją gościa.</summary>
    public UserAccount CreateGuestAccount(string login, string password, Guest guest)
    {
        ArgumentNullException.ThrowIfNull(guest);
        return CreateAccount(login, password, UserRole.Guest, guest.Id);
    }

    /// <summary>Tworzy konto recepcji — wyłącznie z seeda, nigdy z UI.</summary>
    public UserAccount CreateReceptionAccount(string login, string password) =>
        CreateAccount(login, password, UserRole.Reception, guestId: null);

    private UserAccount CreateAccount(string login, string password, UserRole role, Guid? guestId)
    {
        UserAccount.ValidateLogin(login);
        UserAccount.ValidatePassword(password);

        var account = new UserAccount(login, passwordHash: "-", role, guestId);
        account.UpdatePasswordHash(_hasher.HashPassword(account, password));
        _registry.AddAccount(account);
        return account;
    }

    /// <summary>
    /// Weryfikuje poświadczenia; zwraca konto przy poprawnym haśle, inaczej null.
    /// Przy zmianie parametrów hashowania hash jest przeliczany na nowo.
    /// </summary>
    public UserAccount? VerifyCredentials(string login, string password)
    {
        if (string.IsNullOrWhiteSpace(login) || string.IsNullOrEmpty(password))
        {
            return null;
        }

        var account = _registry.FindAccountByLogin(login);

        if (account is null)
        {
            return null;
        }

        var result = _hasher.VerifyHashedPassword(account, account.PasswordHash, password);

        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        if (result == PasswordVerificationResult.SuccessRehashNeeded)
        {
            account.UpdatePasswordHash(_hasher.HashPassword(account, password));
        }

        return account;
    }

    /// <summary>Zmienia hasło po weryfikacji dotychczasowego.</summary>
    public OperationResult ChangePassword(string login, string currentPassword, string newPassword)
    {
        var account = VerifyCredentials(login, currentPassword);

        if (account is null)
        {
            return OperationResult.Fail("Dotychczasowe hasło jest nieprawidłowe.");
        }

        try
        {
            UserAccount.ValidatePassword(newPassword);
        }
        catch (ArgumentException exception)
        {
            return OperationResult.Fail(exception.Message);
        }

        account.UpdatePasswordHash(_hasher.HashPassword(account, newPassword));
        return OperationResult.Ok("Hasło zostało zmienione.");
    }
}
