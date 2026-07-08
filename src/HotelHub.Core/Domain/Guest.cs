namespace HotelHub.Domain;

/// <summary>
/// Gość hotelowy. Walidacja danych osobowych w konstruktorze
/// (niepuste pola, limit 50 znaków, proste sprawdzenie e-maila).
/// </summary>
public sealed class Guest
{
    public const int MaxNameLength = 50;

    public Guid Id { get; }
    public string FirstName { get; private set; }
    public string LastName { get; private set; }
    public string Email { get; private set; }

    public string FullName => $"{FirstName} {LastName}";

    /// <summary>Inicjały gościa do wyświetlania w pasku użytkownika.</summary>
    public string Initials =>
        $"{char.ToUpperInvariant(FirstName[0])}{char.ToUpperInvariant(LastName[0])}";

    public Guest(string firstName, string lastName, string email)
        : this(Guid.NewGuid(), firstName, lastName, email)
    {
    }

    public Guest(Guid id, string firstName, string lastName, string email)
    {
        Id = id;
        FirstName = ValidateName(firstName, "Imię");
        LastName = ValidateName(lastName, "Nazwisko");
        Email = ValidateEmail(email);
    }

    /// <summary>Aktualizuje dane profilu z pełną walidacją (edycja profilu / panel recepcji).</summary>
    public void UpdateProfile(string firstName, string lastName, string email)
    {
        FirstName = ValidateName(firstName, "Imię");
        LastName = ValidateName(lastName, "Nazwisko");
        Email = ValidateEmail(email);
    }

    private static string ValidateName(string value, string fieldName)
    {
        value = value?.Trim() ?? string.Empty;

        if (value.Length == 0)
        {
            throw new ArgumentException($"{fieldName} nie może być puste.");
        }

        if (value.Length > MaxNameLength)
        {
            throw new ArgumentException($"{fieldName} nie może przekraczać {MaxNameLength} znaków.");
        }

        return value;
    }

    private static string ValidateEmail(string email)
    {
        email = email?.Trim() ?? string.Empty;

        if (email.Length == 0 || !email.Contains('@') || !email.Contains('.'))
        {
            throw new ArgumentException("Adres e-mail musi zawierać znaki '@' oraz '.'.");
        }

        return email;
    }

    public override string ToString() => $"{FullName} <{Email}>";
}
