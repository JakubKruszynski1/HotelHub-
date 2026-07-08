using HotelHub.Behavioral.Observers;
using HotelHub.Behavioral.Pricing;
using HotelHub.Behavioral.States;
using HotelHub.Structural.Decorators;

namespace HotelHub.Domain;

/// <summary>
/// Rezerwacja pokoju hotelowego dla gościa w zadanym terminie.
/// Wzorzec: State (Context) — trzyma <see cref="IReservationState"/> i deleguje do niego
/// operacje wraz z kontekstem wykonującego (rola + gość).
/// Wzorzec: Observer (Subject) — powiadamia subskrybentów przy każdej zmianie stanu.
/// Enkapsulacja: settery prywatne, modyfikacje wyłącznie przez metody domenowe.
/// </summary>
public sealed class Reservation
{
    private readonly List<IReservationObserver> _observers = [];
    private readonly List<StateChange> _history = [];

    public Guid Id { get; }

    /// <summary>Czytelny numer rezerwacji w formacie RES-2026-0001, nadawany przez rejestr.</summary>
    public string ReservationNumber { get; private set; } = string.Empty;

    public Guest Guest { get; }

    /// <summary>Pokój — może być opakowany dekoratorami usług dodatkowych.</summary>
    public IRoom Room { get; private set; }

    public DateRange Stay { get; }

    /// <summary>Strategia cenowa użyta do wyliczenia ceny (Strategy); brak = cena bazowa × liczba nocy.</summary>
    public IPricingStrategy? Pricing { get; }

    public Money TotalPrice { get; private set; }

    /// <summary>Bieżący stan cyklu życia rezerwacji (State).</summary>
    public IReservationState State { get; private set; }

    /// <summary>Powód odrzucenia — obowiązkowy przy przejściu do stanu Odrzucona.</summary>
    public string? RejectionReason { get; private set; }

    /// <summary>Czas utworzenia rezerwacji.</summary>
    public DateTime CreatedAt { get; private set; } = DateTime.Now;

    /// <summary>Historia przejść stanów ze znacznikami czasu i wykonawcą (dziennik zdarzeń).</summary>
    public IReadOnlyList<StateChange> History => _history.AsReadOnly();

    /// <summary>Czy gość jest aktualnie zameldowany.</summary>
    public bool IsCheckedIn => State is CheckedInState;

    /// <summary>Bazowy pokój po zdjęciu wszystkich dekoratorów.</summary>
    public Room BaseRoom => RoomExtraDecorator.Unwrap(Room);

    /// <summary>Skrócony identyfikator techniczny.</summary>
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

    // --- Operacje cyklu życia — delegowane do bieżącego stanu (State) z kontekstem wykonującego ---

    public OperationResult Confirm(ActorContext? actor = null) => State.Confirm(this, actor ?? ActorContext.System);
    public OperationResult Reject(string reason, ActorContext? actor = null) => State.Reject(this, actor ?? ActorContext.System, reason);
    public OperationResult Pay(ActorContext? actor = null) => State.Pay(this, actor ?? ActorContext.System);
    public OperationResult Cancel(ActorContext? actor = null) => State.Cancel(this, actor ?? ActorContext.System);
    public OperationResult CheckIn(ActorContext? actor = null) => State.CheckIn(this, actor ?? ActorContext.System);
    public OperationResult CheckOut(ActorContext? actor = null) => State.CheckOut(this, actor ?? ActorContext.System);

    // --- Operacje dozwolone w bieżącym stanie dla danego wykonawcy — sterują widocznością akcji w UI ---

    public bool CanConfirm(ActorContext actor) => State is PendingState && actor.CanActAsReception;

    public bool CanReject(ActorContext actor) => State is PendingState && actor.CanActAsReception;

    public bool CanPay(ActorContext actor) =>
        State is ConfirmedState && actor.IsOwnerOf(this) && (actor.IsGuest || actor.IsSystem);

    public bool CanCancel(ActorContext actor) =>
        (State is PendingState or ConfirmedState && (actor.IsOwnerOf(this) || actor.CanActAsReception))
        || (State is PaidState && actor.CanActAsReception);

    public bool CanCheckIn(ActorContext actor) => State is PaidState && actor.CanActAsReception;

    public bool CanCheckOut(ActorContext actor) => State is CheckedInState && actor.CanActAsReception;

    /// <summary>Przejście do nowego stanu: wpis w historii i powiadomienie obserwatorów.</summary>
    public void TransitionTo(IReservationState newState, string eventDescription, ActorContext actor)
    {
        State = newState ?? throw new ArgumentNullException(nameof(newState));
        _history.Add(new StateChange(DateTime.Now, newState.Name, eventDescription, actor.Login));
        Notify(eventDescription);
    }

    /// <summary>Ogłasza utworzenie rezerwacji (wpis w historii + powiadomienia).</summary>
    public void AnnounceCreation(ActorContext actor)
    {
        _history.Add(new StateChange(DateTime.Now, State.Name, "Rezerwacja utworzona", actor.Login));
        Notify("Rezerwacja utworzona — oczekuje na potwierdzenie recepcji");
    }

    /// <summary>Ustawia powód odrzucenia (wywoływane przez stan Oczekująca przy odrzuceniu).</summary>
    public void SetRejectionReason(string reason) => RejectionReason = reason;

    /// <summary>Nadaje numer rezerwacji — jednokrotnie, przy dodaniu do rejestru lub odczycie z pliku.</summary>
    public void AssignNumber(string reservationNumber)
    {
        if (ReservationNumber.Length == 0 && !string.IsNullOrWhiteSpace(reservationNumber))
        {
            ReservationNumber = reservationNumber.Trim();
        }
    }

    /// <summary>Przywraca stan po wczytaniu danych z pliku JSON — bez powiadomień.</summary>
    public void RestoreState(IReservationState state) =>
        State = state ?? throw new ArgumentNullException(nameof(state));

    /// <summary>Przywraca metadane po wczytaniu z pliku JSON — bez powiadomień.</summary>
    public void RestoreMetadata(DateTime createdAt, string? rejectionReason, IEnumerable<StateChange>? history)
    {
        CreatedAt = createdAt;
        RejectionReason = rejectionReason;
        _history.Clear();
        _history.AddRange(history ?? []);
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
        $"{(ReservationNumber.Length > 0 ? ReservationNumber : ShortId)} | {Guest.FullName} | " +
        $"{Room.GetDescription()} | {Stay} | {TotalPrice} | {State.Name}";
}
