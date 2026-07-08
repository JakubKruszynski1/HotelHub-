namespace HotelHub.Structural;

/// <summary>
/// Wynik weryfikacji uprawnień w proxy autoryzującym (Proxy):
/// dostęp przyznany lub odmowa z czytelnym komunikatem — nigdy ominięcie reguły.
/// </summary>
public sealed record AuthorizationResult(bool Granted, string Message)
{
    public static readonly AuthorizationResult Allowed = new(true, string.Empty);

    public static AuthorizationResult Denied(string message) => new(false, message);
}
