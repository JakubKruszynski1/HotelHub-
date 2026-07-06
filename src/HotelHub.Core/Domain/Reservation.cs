using HotelHub.Behavioral.Observers;
using HotelHub.Behavioral.Pricing;
using HotelHub.Behavioral.States;
using HotelHub.Structural.Decorators;

namespace HotelHub.Domain;

/// <summary>
/// Rezerwacja pokoju hotelowego dla gościa w zadanym terminie.
/// Wzorzec: State (Context) — trzyma <see cref="IReservationState"/> i deleguje do niego operacje.
/// Wzorzec: Observer (Subject) — powiadamia subskrybentów przy każdej zmianie stanu.
/// Enkapsulacja: settery prywatne, modyfikacje wyłącznie przez metody domenowe.
/// </summary>
public sealed class Reservation
{
    private readonly List<IReservationObserver> _observers = [];

    public Guid Id { get; }
    public Guest Guest { get; }

    /// <summary>Pokój — może być opakowany dekoratorami usług dodatkowych.</summary>
    public IRoom Room { get; private set; }

    public DateRange Stay { get; }

    /// <summary>Strategia cenowa użyta do wyliczenia ceny (Strategy); brak = cena bazowa × liczba nocy.</summary>
    public IPricingStrategy? Pricing { get; }

    public Money TotalPrice { get; private set; }

    /// <summary>Bieżący stan cyklu życia rezerwacji (State).</summary>
    public IReservationState State { get; private set; }

    /// <summary>Czy gość jest aktualnie zameldowany.</summary>
    public bool IsCheckedIn { get; private set; }

    /// <summary>Bazowy pokój po zdjęciu wszystkich dekoratorów.</summary>
    public Room BaseRoom => RoomExtraDecorator.Unwrap(Room);

    /// <summary>Skrócony identyfikator do wyświetlania i wyszukiwania w konsoli.</summary>
    public string ShortId => Id.ToString()[..8];

    /// <summary>Czy rezerwacja blokuje pokój w swoim terminie (deleguje do stanu).</summary>
    public bool BlocksRoom => State.BlocksRoom;

    /// <summary>Czy rezerwacja liczy się do raportu przychodów (deleguje do stanu).</summary>
    public bool CountsTowardRevenue => State.CountsTowardRevenue;

    /// <summary>Usługi dodatkowe można modyfikować tylko przed opłaceniem.</summary>
    public bool CanModifyExtras => State is PendingState or ConfirmedState;

    public Reservation(Guest guest, IRoom room, DateRange stay, IPricingStrategy? pricing = null)
        : this(Guid.NewGuid(), guest, room, stay, pricing)
    {
    }

    public Reservation(Guid id, Guest guest, IRoom room, DateRange stay, IPricingStrategy? pricing = null)
    {
        Id = id;
        Guest = guest ?? throw new ArgumentNullException(nameof(guest));
        Room = room ?? throw new ArgumentNullException(nameof(room));
        Stay = stay ?? throw new ArgumentNullException(nameof(stay));
        Pricing = pricing;
        TotalPrice = CalculateTotal();
        State = new PendingState();
    }

    // --- Operacje cyklu życia — delegowane do bieżącego stanu (State) ---

    public void Confirm() => State.Confirm(this);
    public void Pay() => State.Pay(this);
    public void Cancel() => State.Cancel(this);
    public void CheckIn() => State.CheckIn(this);
    public void CheckOut() => State.CheckOut(this);

    /// <summary>Przejście do nowego stanu z powiadomieniem obserwatorów.</summary>
    public void TransitionTo(IReservationState newState, string eventDescription)
    {
        State = newState ?? throw new ArgumentNullException(nameof(newState));
        Notify(eventDescription);
    }

    /// <summary>Melduje gościa (wywoływane przez stan Opłacona) i powiadamia obserwatorów.</summary>
    public void MarkCheckedIn()
    {
        IsCheckedIn = true;
        Notify("Gość zameldowany (check-in)");
    }

    /// <summary>Przywraca stan po wczytaniu danych z pliku JSON — bez powiadomień.</summary>
    public void RestoreState(IReservationState state, bool isCheckedIn)
    {
        State = state ?? throw new ArgumentNullException(nameof(state));
        IsCheckedIn = isCheckedIn;
    }

    // --- Observer (Subject) ---

    public void Attach(IReservationObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        if (!_observers.Contains(observer))
        {
            _observers.Add(observer);
        }
    }

    public void Detach(IReservationObserver observer) => _observers.Remove(observer);

    private void Notify(string eventDescription)
    {
        foreach (var observer in _observers)
        {
            observer.OnReservationChanged(this, eventDescription);
        }
    }

    // --- Usługi dodatkowe (Decorator) ---

    /// <summary>
    /// Dodaje usługę dodatkową (Decorator) do pokoju, przelicza cenę całkowitą
    /// i powiadamia obserwatorów.
    /// </summary>
    public void AddExtra(RoomExtra extra)
    {
        if (RoomExtraDecorator.HasExtra(Room, extra))
        {
            throw new InvalidOperationException("Ta usługa jest już dodana do rezerwacji.");
        }

        Room = RoomExtraDecorator.Apply(Room, extra);
        TotalPrice = CalculateTotal();
        Notify($"Dodano usługę do rezerwacji (nowa cena: {TotalPrice})");
    }

    private Money CalculateTotal() =>
        Pricing is not null
            ? Pricing.Calculate(Room.GetPrice(), Stay)
            : Room.GetPrice() * Stay.Nights;

    public override string ToString() =>
        $"Rezerwacja {ShortId} | {Guest.FullName} | {Room.GetDescription()} | {Stay} | {TotalPrice} | {State.Name}";
}
