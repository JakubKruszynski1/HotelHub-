namespace HotelHub.Behavioral.Observers;

/// <summary>
/// Powiadomienie adresowane do konkretnego gościa albo do kanału recepcji
/// (<see cref="RecipientGuestId"/> = null), z flagą przeczytania i czasem utworzenia.
/// </summary>
public sealed class Notification
{
    public Guid Id { get; }

    /// <summary>Adresat-gość; null oznacza kanał recepcji.</summary>
    public Guid? RecipientGuestId { get; }

    public string Message { get; }
    public DateTime CreatedAt { get; }
    public bool IsRead { get; private set; }

    public Notification(Guid? recipientGuestId, string message)
        : this(Guid.NewGuid(), recipientGuestId, message, DateTime.Now, isRead: false)
    {
    }

    public Notification(Guid id, Guid? recipientGuestId, string message, DateTime createdAt, bool isRead)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Treść powiadomienia nie może być pusta.", nameof(message));
        }

        Id = id;
        RecipientGuestId = recipientGuestId;
        Message = message.Trim();
        CreatedAt = createdAt;
        IsRead = isRead;
    }

    public void MarkRead() => IsRead = true;
}
