using HotelHub.Behavioral.Observers;
using HotelHub.Behavioral.States;
using HotelHub.Domain;
using HotelHub.Domain.RoomTypes;

namespace HotelHub.Tests;

/// <summary>
/// Testy cyklu życia rezerwacji (State): poprawne przejścia stanów z kontekstem
/// wykonującego (rola + właściciel), odrzucanie operacji nielegalnych bez wyjątków
/// oraz powiadamianie obserwatorów (Observer) przy zmianach stanu.
/// </summary>
public class ReservationStateTests
{
    private static readonly ActorContext Reception = new("recepcja", UserRole.Reception, null);
    private static readonly ActorContext StrangerGuest = new("obcy.gosc", UserRole.Guest, Guid.NewGuid());

    private static ActorContext OwnerOf(Reservation reservation) =>
        new("wlasciciel", UserRole.Guest, reservation.Guest.Id);

    [Fact]
    public void NewReservation_StartsInPendingState()
    {
        var reservation = CreateReservation();

        Assert.IsType<PendingState>(reservation.State);
        Assert.False(reservation.IsCheckedIn);
    }

    [Fact]
    public void FullLifecycle_PendingToCompleted()
    {
        var reservation = CreateReservation();

        Assert.True(reservation.Confirm(Reception).Success);
        Assert.IsType<ConfirmedState>(reservation.State);

        Assert.True(reservation.Pay(OwnerOf(reservation)).Success);
        Assert.IsType<PaidState>(reservation.State);

        Assert.True(reservation.CheckIn(Reception).Success);
        Assert.True(reservation.IsCheckedIn);
        Assert.IsType<CheckedInState>(reservation.State);

        Assert.True(reservation.CheckOut(Reception).Success);
        Assert.IsType<CompletedState>(reservation.State);
    }

    [Fact]
    public void Pay_FromPending_IsRejected()
    {
        var reservation = CreateReservation();

        var result = reservation.Pay(OwnerOf(reservation));

        Assert.False(result.Success);
        Assert.IsType<PendingState>(reservation.State);
    }

    [Fact]
    public void Confirm_ByGuest_IsRejected()
    {
        var reservation = CreateReservation();

        var result = reservation.Confirm(OwnerOf(reservation));

        Assert.False(result.Success);
        Assert.IsType<PendingState>(reservation.State);
    }

    [Fact]
    public void Reject_RequiresReceptionAndReason()
    {
        var noReason = CreateReservation();
        Assert.False(noReason.Reject("", Reception).Success);
        Assert.IsType<PendingState>(noReason.State);

        var byGuest = CreateReservation();
        Assert.False(byGuest.Reject("Brak wolnych pokoi", OwnerOf(byGuest)).Success);
        Assert.IsType<PendingState>(byGuest.State);

        var rejected = CreateReservation();
        Assert.True(rejected.Reject("Awaria instalacji w pokoju", Reception).Success);
        Assert.IsType<RejectedState>(rejected.State);
        Assert.Equal("Awaria instalacji w pokoju", rejected.RejectionReason);
    }

    [Fact]
    public void Pay_ByReceptionOrStranger_IsRejected()
    {
        var reservation = CreateReservation();
        reservation.Confirm(Reception);

        Assert.False(reservation.Pay(Reception).Success);
        Assert.False(reservation.Pay(StrangerGuest).Success);
        Assert.IsType<ConfirmedState>(reservation.State);
    }

    [Fact]
    public void Cancel_FromPendingAndConfirmed_ByOwner_Succeeds()
    {
        var pending = CreateReservation();
        Assert.True(pending.Cancel(OwnerOf(pending)).Success);
        Assert.IsType<CancelledState>(pending.State);

        var confirmed = CreateReservation();
        confirmed.Confirm(Reception);
        Assert.True(confirmed.Cancel(OwnerOf(confirmed)).Success);
        Assert.IsType<CancelledState>(confirmed.State);
    }

    [Fact]
    public void Cancel_ByStrangerGuest_IsRejected()
    {
        var reservation = CreateReservation();

        Assert.False(reservation.Cancel(StrangerGuest).Success);
        Assert.IsType<PendingState>(reservation.State);
    }

    [Fact]
    public void Cancel_FromPaid_OnlyByReception()
    {
        var reservation = CreatePaidReservation();

        Assert.False(reservation.Cancel(OwnerOf(reservation)).Success);
        Assert.IsType<PaidState>(reservation.State);

        Assert.True(reservation.Cancel(Reception).Success);
        Assert.IsType<CancelledState>(reservation.State);
    }

    [Fact]
    public void CheckIn_WithoutPayment_IsRejected()
    {
        var reservation = CreateReservation();
        reservation.Confirm(Reception);

        var result = reservation.CheckIn(Reception);

        Assert.False(result.Success);
        Assert.False(reservation.IsCheckedIn);
        Assert.IsType<ConfirmedState>(reservation.State);
    }

    [Fact]
    public void CheckIn_ByGuest_IsRejected()
    {
        var reservation = CreatePaidReservation();

        Assert.False(reservation.CheckIn(OwnerOf(reservation)).Success);
        Assert.IsType<PaidState>(reservation.State);
    }

    [Fact]
    public void CheckOut_WithoutCheckIn_IsRejected()
    {
        var reservation = CreatePaidReservation();

        var result = reservation.CheckOut(Reception);

        Assert.False(result.Success);
        Assert.IsType<PaidState>(reservation.State);
    }

    [Fact]
    public void CancelledReservation_HasNoTransitionsAndDoesNotBlockRoom()
    {
        var reservation = CreateReservation();
        reservation.Cancel(OwnerOf(reservation));

        Assert.False(reservation.Confirm(Reception).Success);
        Assert.False(reservation.Pay(OwnerOf(reservation)).Success);
        Assert.False(reservation.CheckIn(Reception).Success);
        Assert.False(reservation.CheckOut(Reception).Success);

        Assert.IsType<CancelledState>(reservation.State);
        Assert.False(reservation.BlocksRoom);
        Assert.False(reservation.CountsTowardRevenue);
    }

    [Fact]
    public void RejectedReservation_HasNoTransitionsAndDoesNotBlockRoom()
    {
        var reservation = CreateReservation();
        reservation.Reject("Pokój wyłączony z użytku", Reception);

        Assert.False(reservation.Confirm(Reception).Success);
        Assert.False(reservation.Pay(OwnerOf(reservation)).Success);
        Assert.False(reservation.Cancel(Reception).Success);

        Assert.IsType<RejectedState>(reservation.State);
        Assert.False(reservation.BlocksRoom);
        Assert.False(reservation.CountsTowardRevenue);
    }

    [Fact]
    public void CompletedReservation_HasNoTransitionsAndCountsTowardRevenue()
    {
        var reservation = CreatePaidReservation();
        reservation.CheckIn(Reception);
        reservation.CheckOut(Reception);

        Assert.False(reservation.Confirm(Reception).Success);
        Assert.False(reservation.Pay(OwnerOf(reservation)).Success);
        Assert.False(reservation.Cancel(Reception).Success);

        Assert.IsType<CompletedState>(reservation.State);
        Assert.True(reservation.CountsTowardRevenue);
    }

    [Fact]
    public void Transitions_AreRecordedInHistoryWithActor()
    {
        var reservation = CreateReservation();

        reservation.Confirm(Reception);
        reservation.Pay(OwnerOf(reservation));

        Assert.Equal(2, reservation.History.Count);
        Assert.Equal("recepcja", reservation.History[0].ActorLogin);
        Assert.Equal("Potwierdzona", reservation.History[0].StateName);
        Assert.Equal("wlasciciel", reservation.History[1].ActorLogin);
        Assert.Equal("Opłacona", reservation.History[1].StateName);
    }

    [Fact]
    public void StateChanges_NotifyAttachedObservers()
    {
        var reservation = CreateReservation();
        var observer = new RecordingObserver();
        reservation.Attach(observer);

        reservation.Confirm(Reception);
        reservation.Pay(OwnerOf(reservation));

        Assert.Equal(2, observer.Events.Count);
        Assert.Contains("Rezerwacja potwierdzona przez recepcję", observer.Events);
        Assert.Contains("Rezerwacja opłacona", observer.Events);
    }

    [Fact]
    public void DetachedObserver_IsNotNotified()
    {
        var reservation = CreateReservation();
        var observer = new RecordingObserver();
        reservation.Attach(observer);
        reservation.Detach(observer);

        reservation.Confirm(Reception);

        Assert.Empty(observer.Events);
    }

    private static Reservation CreateReservation()
    {
        var guest = new Guest("Anna", "Testowa", "anna@test.pl");
        var from = DateTime.Today.AddDays(10);

        return new Reservation(guest, new StandardRoom(101), new DateRange(from, from.AddDays(3)));
    }

    private static Reservation CreatePaidReservation()
    {
        var reservation = CreateReservation();
        reservation.Confirm(Reception);
        reservation.Pay(OwnerOf(reservation));
        return reservation;
    }

    /// <summary>Obserwator testowy rejestrujący otrzymane zdarzenia.</summary>
    private sealed class RecordingObserver : IReservationObserver
    {
        public List<string> Events { get; } = [];

        public void OnReservationChanged(Reservation reservation, string eventDescription) =>
            Events.Add(eventDescription);
    }
}
