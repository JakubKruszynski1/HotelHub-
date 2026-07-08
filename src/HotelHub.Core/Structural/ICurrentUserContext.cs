using HotelHub.Domain;

namespace HotelHub.Structural;

/// <summary>
/// Dostarcza kontekst aktualnie zalogowanego użytkownika (login, rola, gość)
/// fasadzie i jej proxy autoryzującemu. W aplikacji webowej implementowany
/// jako serwis scoped wypełniany z claimów; poza web — kontekst stały.
/// </summary>
public interface ICurrentUserContext
{
    /// <summary>Kontekst wykonującego operacje; dla niezalogowanych <see cref="ActorContext.Anonymous"/>.</summary>
    ActorContext Actor { get; }
}

/// <summary>Stały kontekst użytkownika — dla seeda, testów i aplikacji bez logowania.</summary>
public sealed class FixedCurrentUser : ICurrentUserContext
{
    public ActorContext Actor { get; }

    public FixedCurrentUser(ActorContext actor)
    {
        Actor = actor ?? throw new ArgumentNullException(nameof(actor));
    }

    /// <summary>Kontekst systemowy — pomija ograniczenia ról (seed, konsola).</summary>
    public static readonly FixedCurrentUser SystemContext = new(ActorContext.System);
}
