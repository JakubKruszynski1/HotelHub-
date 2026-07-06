using HotelHub.Domain;

namespace HotelHub.Structural.Decorators;

/// <summary>
/// Usługa dodatkowa możliwa do doliczenia do pokoju.
/// </summary>
public enum RoomExtra
{
    Breakfast,
    Parking,
    Spa
}

/// <summary>
/// Wzorzec: Decorator (Decorator — klasa bazowa). Opakowuje <see cref="IRoom"/>
/// i dolicza usługę dodatkową do ceny oraz opisu pokoju.
/// Dekoratory można łączyć łańcuchowo (np. śniadanie + parking + SPA).
/// </summary>
public abstract class RoomExtraDecorator : IRoom
{
    /// <summary>Opakowany komponent — pokój lub kolejny dekorator.</summary>
    public IRoom Inner { get; }

    /// <summary>Rodzaj usługi dodatkowej doliczanej przez ten dekorator.</summary>
    public abstract RoomExtra Extra { get; }

    /// <summary>Polska nazwa usługi wyświetlana w opisie.</summary>
    public abstract string ExtraName { get; }

    /// <summary>Dopłata za usługę za jedną noc.</summary>
    public abstract Money ExtraPricePerNight { get; }

    protected RoomExtraDecorator(IRoom inner)
    {
        Inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    public Money GetPrice() => Inner.GetPrice() + ExtraPricePerNight;

    public string GetDescription() => $"{Inner.GetDescription()} + {ExtraName}";

    /// <summary>Zdejmuje wszystkie dekoratory i zwraca bazowy pokój.</summary>
    public static Room Unwrap(IRoom room) => room switch
    {
        Room baseRoom => baseRoom,
        RoomExtraDecorator decorator => Unwrap(decorator.Inner),
        _ => throw new NotSupportedException($"Nieznana implementacja pokoju: {room.GetType().Name}.")
    };

    /// <summary>Zwraca listę usług dodatkowych doliczonych do pokoju.</summary>
    public static IReadOnlyList<RoomExtra> GetExtras(IRoom room)
    {
        var extras = new List<RoomExtra>();

        while (room is RoomExtraDecorator decorator)
        {
            extras.Add(decorator.Extra);
            room = decorator.Inner;
        }

        extras.Reverse();
        return extras;
    }

    public static bool HasExtra(IRoom room, RoomExtra extra) => GetExtras(room).Contains(extra);

    /// <summary>Opakowuje pokój dekoratorem odpowiadającym wskazanej usłudze.</summary>
    public static IRoom Apply(IRoom room, RoomExtra extra) => extra switch
    {
        RoomExtra.Breakfast => new BreakfastDecorator(room),
        RoomExtra.Parking => new ParkingDecorator(room),
        RoomExtra.Spa => new SpaDecorator(room),
        _ => throw new NotSupportedException($"Nieobsługiwana usługa dodatkowa: {extra}.")
    };
}
