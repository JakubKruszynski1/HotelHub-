using HotelHub.Behavioral.Observers;
using HotelHub.Behavioral.States;
using HotelHub.Domain;
using HotelHub.Domain.RoomTypes;

namespace HotelHub.Tests;

/// <summary>
/// Testy cyklu życia rezerwacji (State): poprawne przejścia stanów
/// i odrzucanie operacji nielegalnych (bez wyjątków), plus powiadamianie
/// obserwatorów (Observer) przy zmianach stanu.
/// </summary>
public class ReservationStateTests
{
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

        reservation.Confirm();
        Assert.IsType<ConfirmedState>(reservation.State);

        reservation.Pay();
        Assert.IsType<PaidState>(reservation.State);

        reservation.CheckIn();
        Assert.True(reservation.IsCheckedIn);
        Assert.IsType<PaidState>(reservation.State);

        reservation.CheckOut();
        Assert.IsType<CompletedState>(reservation.State);
    }

    [Fact]
    public void Pay_FromPending_IsRejected()
    {
        var reservation = CreateReservation();

        reservation.Pay();

        Assert.IsType<PendingState>(reservation.State);
    }

    [Fact]
    public void Cancel_FromPendingAndConfirmed_Succeeds()
    {
        var pending = CreateReservation();
        pending.Cancel();
        Assert.IsType<CancelledState>(pending.State);

        var confirmed = CreateReservation();
        confirmed.Confirm();
        confirmed.Cancel();
        Assert.IsType<CancelledState>(confirmed.State);
    }

    [Fact]
    public void Cancel_FromPaid_IsRejected()
    {
        var reservation = CreatePaidReservation();

        reservation.Cancel();

        Assert.IsType<PaidState>(reservation.State);
    }

    [Fact]
    public void CheckIn_WithoutPayment_IsRejected()
    {
        var reservation = CreateReservation();
        reservation.Confirm();

        reservation.CheckIn();

        Assert.False(reservation.IsCheckedIn);
        Assert.IsType<ConfirmedState>(reservation.State);
    }

    [Fact]
    public void CheckOut_WithoutCheckIn_IsRejected()
    {
        var reservation = CreatePaidReservation();

        reservation.CheckOut();

        Assert.IsType<PaidState>(reservation.State);
    }

    [Fact]
    public void CancelledReservation_HasNoTransitionsAndDoesNotBlockRoom()
    {
        var reservation = CreateReservation();
        reservation.Cancel();

        reservation.Confirm();
        reservation.Pay();
        reservation.CheckIn();
        reservation.CheckOut();

        Assert.IsType<CancelledState>(reservation.State);
        Assert.False(reservation.BlocksRoom);
        Assert.False(reservation.CountsTowardRevenue);
    }

    [Fact]
    public void CompletedReservation_HasNoTransitionsAndCountsTowardRevenue()
    {
        var reservation = CreatePaidReservation();
        reservation.CheckIn();
        reservation.CheckOut();

        reservation.Confirm();
        reservation.Pay();
        reservation.Cancel();

        Assert.IsType<CompletedState>(reservation.State);
        Assert.True(reservation.CountsTowardRevenue);
    }

    [Fact]
    public void StateChanges_NotifyAttachedObservers()
    {
        var reservation = CreateReservation();
        var observer = new RecordingObserver();
        reservation.Attach(observer);

        reservation.Confirm();
        reservation.Pay();

        Assert.Equal(2, observer.Events.Count);
        Assert.Contains("Rezerwacja potwierdzona", observer.Events);
        Assert.Contains("Rezerwacja opłacona", observer.Events);
    }

    [Fact]
    public void DetachedObserver_IsNotNotified()
    {
        var reservation = CreateReservation();
        var observer = new RecordingObserver();
        reservation.Attach(observer);
        reservation.Detach(observer);

        reservation.Confirm();

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
        reservation.Confirm();
        reservation.Pay();
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
