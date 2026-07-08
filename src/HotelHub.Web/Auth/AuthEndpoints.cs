using HotelHub.Domain;
using HotelHub.Structural;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace HotelHub.Web.Auth;

/// <summary>
/// Klasyczne endpointy HTTP logowania, rejestracji i wylogowania —
/// <c>SignInAsync</c>/<c>SignOutAsync</c> wykonywane poza obwodem Blazor.
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this WebApplication app)
    {
        app.MapPost("/auth/login", async (
            HttpContext http,
            IBookingFacade facade,
            [FromForm] string? login,
            [FromForm] string? password,
            [FromForm] string? returnUrl) =>
        {
            var account = facade.VerifyCredentials(login ?? string.Empty, password ?? string.Empty);

            if (account is null)
            {
                return Results.Redirect(
                    $"/login?error={Uri.EscapeDataString("Nieprawidłowy login lub hasło.")}" +
                    $"&login={Uri.EscapeDataString(login ?? string.Empty)}" +
                    ReturnUrlSuffix(returnUrl));
            }

            var principal = ClaimsPrincipalExtensions.BuildPrincipal(
                account, facade.GetAccountDisplayName(account));

            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return Results.Redirect(TargetAfterLogin(account, returnUrl));
        });

        app.MapPost("/auth/register", async (
            HttpContext http,
            IBookingFacade facade,
            [FromForm] string? login,
            [FromForm] string? password,
            [FromForm] string? password2,
            [FromForm] string? firstName,
            [FromForm] string? lastName,
            [FromForm] string? email) =>
        {
            string Back(string error) =>
                $"/register?error={Uri.EscapeDataString(error)}" +
                $"&login={Uri.EscapeDataString(login ?? string.Empty)}" +
                $"&firstName={Uri.EscapeDataString(firstName ?? string.Empty)}" +
                $"&lastName={Uri.EscapeDataString(lastName ?? string.Empty)}" +
                $"&email={Uri.EscapeDataString(email ?? string.Empty)}";

            if (password != password2)
            {
                return Results.Redirect(Back("Hasła nie są identyczne."));
            }

            var result = facade.RegisterGuestAccount(
                login ?? string.Empty, password ?? string.Empty,
                firstName ?? string.Empty, lastName ?? string.Empty, email ?? string.Empty);

            if (!result.Success)
            {
                return Results.Redirect(Back(result.Message));
            }

            // Automatyczne zalogowanie świeżo utworzonego konta.
            var account = facade.VerifyCredentials(login!, password!);

            if (account is null)
            {
                return Results.Redirect("/login");
            }

            var principal = ClaimsPrincipalExtensions.BuildPrincipal(
                account, facade.GetAccountDisplayName(account));

            await http.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
            return Results.Redirect("/");
        });

        app.MapPost("/auth/logout", async (HttpContext http) =>
        {
            await http.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Results.Redirect("/");
        });
    }

    private static string TargetAfterLogin(UserAccount account, string? returnUrl)
    {
        if (!string.IsNullOrWhiteSpace(returnUrl) && returnUrl.StartsWith('/') && !returnUrl.StartsWith("//"))
        {
            return returnUrl;
        }

        return account.Role == UserRole.Reception ? "/admin" : "/";
    }

    private static string ReturnUrlSuffix(string? returnUrl) =>
        string.IsNullOrWhiteSpace(returnUrl) ? string.Empty : $"&returnUrl={Uri.EscapeDataString(returnUrl)}";
}
