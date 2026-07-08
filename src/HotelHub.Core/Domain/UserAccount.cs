namespace HotelHub.Domain;

/// <summary>
/// Konto użytkownika systemu. Hasło przechowywane wyłącznie jako hash
/// (<c>PasswordHasher&lt;UserAccount&gt;</c>); konto gościa jest powiązane
/// z encją <see cref="Guest"/> przez <see cref="GuestId"/>.
/// </summary>
public sealed class UserAccount
{
    public const int MinLoginLength = 3;
    public const int MaxLoginLength = 30;
    public const int MinPasswordLength = 8;

    public Guid Id { get; }
    public string Login { get; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; }

    /// <summary>Identyfikator gościa powiązanego z kontem (tylko rola Guest).</summary>
    public Guid? GuestId { get; }

    public DateTime CreatedAt { get; }

    public UserAccount(string login, string passwordHash, UserRole role, Guid? guestId)
        : this(Guid.NewGuid(), login, passwordHash, role, guestId, DateTime.Now)
    {
    }

    public UserAccount(Guid id, string login, string passwordHash, UserRole role, Guid? guestId, DateTime createdAt)
    {
        ValidateLogin(login);

        if (role == UserRole.Guest && guestId is null)
        {
            throw new ArgumentException("Konto gościa musi być powiązane z encją gościa.", nameof(guestId));
        }

        Id = id;
        Login = login.Trim();
        PasswordHash = !string.IsNullOrWhiteSpace(passwordHash)
            ? passwordHash
            : throw new ArgumentException("Hash hasła nie może być pusty.", nameof(passwordHash));
        Role = role;
        GuestId = guestId;
        CreatedAt = createdAt;
    }

    /// <summary>Podmienia hash hasła (zmiana hasła lub ponowne hashowanie po weryfikacji).</summary>
    public void UpdatePasswordHash(string newHash)
    {
        if (string.IsNullOrWhiteSpace(newHash))
        {
            throw new ArgumentException("Hash hasła nie może być pusty.", nameof(newHash));
        }

        PasswordHash = newHash;
    }

    /// <summary>
    /// Walidacja loginu: 3–30 znaków, wyłącznie litery, cyfry, podkreślnik i kropka.
    /// </summary>
    public static void ValidateLogin(string login)
    {
        login = login?.Trim() ?? string.Empty;

        if (login.Length is < MinLoginLength or > MaxLoginLength)
        {
            throw new ArgumentException(
                $"Login musi mieć od {MinLoginLength} do {MaxLoginLength} znaków.");
        }

        if (!login.All(c => char.IsLetterOrDigit(c) || c is '_' or '.'))
        {
            throw new ArgumentException(
                "Login może zawierać wyłącznie litery, cyfry, podkreślnik i kropkę.");
        }
    }

    /// <summary>Walidacja hasła w postaci jawnej — przed zahashowaniem.</summary>
    public static void ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < MinPasswordLength)
        {
            throw new ArgumentException($"Hasło musi mieć co najmniej {MinPasswordLength} znaków.");
        }
    }

    public override string ToString() => $"{Login} ({Role})";
}
