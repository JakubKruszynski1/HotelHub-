using System.Security.Claims;
using HotelHub.Domain;

namespace HotelHub.Web.Auth;

/// <summary>
/// Mapowanie claimów zalogowanego użytkownika na domenowy kontekst wykonującego
/// (<see cref="ActorContext"/>) oraz budowa claimów przy logowaniu.
/// </summary>
public static class ClaimsPrincipalExtensions
{
    public const string GuestIdClaim = "hotelhub:guestid";
    public const string DisplayNameClaim = "hotelhub:displayname";

    public static ActorContext ToActorContext(this ClaimsPrincipal? principal)
    {
        if (principal?.Identity?.IsAuthenticated != true)
        {
            return ActorContext.Anonymous;
        }

        var login = principal.Identity!.Name ?? "anonim";
        UserRole? role = principal.IsInRole(nameof(UserRole.Reception))
            ? UserRole.Reception
            : principal.IsInRole(nameof(UserRole.Guest)) ? UserRole.Guest : null;

        Guid? guestId = Guid.TryParse(principal.FindFirstValue(GuestIdClaim), out var parsed)
            ? parsed
            : null;

        return new ActorContext(login, role, guestId);
    }

    /// <summary>Nazwa wyświetlana w pasku użytkownika (imię i nazwisko gościa lub login).</summary>
    public static string DisplayName(this ClaimsPrincipal principal) =>
        principal.FindFirstValue(DisplayNameClaim) ?? principal.Identity?.Name ?? "—";

    /// <summary>Inicjały do kółka awatara, wyliczane z nazwy wyświetlanej.</summary>
    public static string Initials(this ClaimsPrincipal principal)
    {
        var parts = principal.DisplayName()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length switch
        {
            0 => "?",
            1 => parts[0][..Math.Min(2, parts[0].Length)].ToUpperInvariant(),
            _ => $"{char.ToUpperInvariant(parts[0][0])}{char.ToUpperInvariant(parts[^1][0])}"
        };
    }

    /// <summary>Polska etykieta roli do paska użytkownika.</summary>
    public static string RoleLabel(this ClaimsPrincipal principal) =>
        principal.IsInRole(nameof(UserRole.Reception)) ? "Recepcja" : "Gość";

    /// <summary>Buduje pryncypała cookie z kompletem claimów (login, rola, gość, nazwa).</summary>
    public static ClaimsPrincipal BuildPrincipal(UserAccount account, string displayName)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new(ClaimTypes.Name, account.Login),
            new(ClaimTypes.Role, account.Role.ToString()),
            new(DisplayNameClaim, displayName)
        };

        if (account.GuestId is { } guestId)
        {
            claims.Add(new Claim(GuestIdClaim, guestId.ToString()));
        }

        var identity = new ClaimsIdentity(claims, "HotelHubCookie");
        return new ClaimsPrincipal(identity);
    }
}
