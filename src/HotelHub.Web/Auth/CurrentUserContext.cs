using HotelHub.Domain;
using HotelHub.Structural;
using Microsoft.AspNetCore.Components.Authorization;

namespace HotelHub.Web.Auth;

/// <summary>
/// Serwis scoped dostarczający kontekst zalogowanego użytkownika komponentom
/// i proxy fasady: w obwodzie Blazor czyta stan z <see cref="AuthenticationStateProvider"/>,
/// w klasycznych żądaniach HTTP (SSR, endpointy logowania) — z <c>HttpContext.User</c>.
/// </summary>
public sealed class CurrentUserContext : ICurrentUserContext
{
    private readonly AuthenticationStateProvider? _authenticationState;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserContext(
        IHttpContextAccessor httpContextAccessor,
        AuthenticationStateProvider? authenticationState = null)
    {
        _httpContextAccessor = httpContextAccessor;
        _authenticationState = authenticationState;
    }

    public ActorContext Actor
    {
        get
        {
            var fromCircuit = TryGetFromAuthenticationState();

            if (fromCircuit is not null)
            {
                return fromCircuit;
            }

            return _httpContextAccessor.HttpContext?.User.ToActorContext() ?? ActorContext.Anonymous;
        }
    }

    private ActorContext? TryGetFromAuthenticationState()
    {
        if (_authenticationState is null)
        {
            return null;
        }

        try
        {
            var task = _authenticationState.GetAuthenticationStateAsync();
            return task.IsCompletedSuccessfully ? task.Result.User.ToActorContext() : null;
        }
        catch (InvalidOperationException)
        {
            // Stan uwierzytelnienia niedostępny poza obwodem Blazor — użyjemy HttpContext.
            return null;
        }
    }
}
