namespace HotelHub.Domain;

/// <summary>
/// Rola konta użytkownika: gość hotelowy (portal publiczny)
/// lub pracownik recepcji (panel administracyjny).
/// </summary>
public enum UserRole
{
    Guest,
    Reception
}
