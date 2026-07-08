using HotelHub.Behavioral.Observers;

namespace HotelHub.Tests;

/// <summary>
/// Testy centrum powiadomień: adresowanie per gość / kanał recepcji,
/// licznik nieprzeczytanych i oznaczanie jako przeczytane.
/// </summary>
public class NotificationCenterTests
{
    [Fact]
    public void Notifications_AreAddressedPerGuest()
    {
        var center = NotificationCenter.Instance;
        var guestA = Guid.NewGuid();
        var guestB = Guid.NewGuid();

        center.PublishToGuest(guestA, "Wiadomość dla A");
        center.PublishToGuest(guestB, "Wiadomość dla B");

        Assert.Single(center.GetForGuest(guestA));
        Assert.Equal("Wiadomość dla A", center.GetForGuest(guestA)[0].Message);
        Assert.Single(center.GetForGuest(guestB));
        Assert.DoesNotContain(center.GetForGuest(guestA), n => n.Message.Contains("dla B"));
    }

    [Fact]
    public void UnreadCounter_TracksOnlyOwnNotifications()
    {
        var center = NotificationCenter.Instance;
        var guest = Guid.NewGuid();

        Assert.Equal(0, center.UnreadCountForGuest(guest));

        center.PublishToGuest(guest, "Pierwsza");
        center.PublishToGuest(guest, "Druga");
        Assert.Equal(2, center.UnreadCountForGuest(guest));

        center.MarkAllReadForGuest(guest);
        Assert.Equal(0, center.UnreadCountForGuest(guest));
        Assert.All(center.GetForGuest(guest), n => Assert.True(n.IsRead));
    }

    [Fact]
    public void ReceptionChannel_IsSeparateFromGuests()
    {
        var center = NotificationCenter.Instance;
        var guest = Guid.NewGuid();
        var before = center.UnreadCountForReception();

        center.PublishToReception("Nowa rezerwacja oczekuje na akceptację.");
        center.PublishToGuest(guest, "Twoja rezerwacja została potwierdzona.");

        Assert.Equal(before + 1, center.UnreadCountForReception());
        Assert.DoesNotContain(center.GetForGuest(guest), n => n.RecipientGuestId is null);
        Assert.Contains(center.GetForReception(), n => n.Message.Contains("akceptację"));
    }

    [Fact]
    public void EmptyMessage_IsRejected()
    {
        Assert.Throws<ArgumentException>(() => new Notification(Guid.NewGuid(), "  "));
    }
}
