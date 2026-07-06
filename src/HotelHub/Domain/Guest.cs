namespace HotelHub.Domain;

/// <summary>
/// Gość hotelowy. Walidacja danych osobowych w konstruktorze
/// (niepuste pola, limit 50 znaków, proste sprawdzenie e-maila).
/// </summary>
public sealed class Guest
{
    public const int MaxNameLength = 50;

    public Guid Id { get; }
    public string FirstName { get; }
    public string LastName { get; }
    public string Email { get; }

    public string FullName => $"{FirstName} {LastName}";

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
