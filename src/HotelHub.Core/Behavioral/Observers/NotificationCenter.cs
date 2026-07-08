namespace HotelHub.Behavioral.Observers;

/// <summary>
/// Centrum powiadomień per odbiorca (gość lub kanał recepcji) — thread-safe,
/// jedna instancja w aplikacji. Zasila dzwoneczek z licznikiem nieprzeczytanych
/// w obu światach UI.
/// </summary>
public sealed class NotificationCenter
{
    private static readonly Lazy<NotificationCenter> LazyInstance = new(() => new NotificationCenter());

    public static NotificationCenter Instance => LazyInstance.Value;

    private readonly object _syncRoot = new();
    private readonly List<Notification> _notifications = [];

    private NotificationCenter()
    {
    }

    /// <summary>Wszystkie powiadomienia (persystencja).</summary>
    public IReadOnlyList<Notification> All
    {
        get { lock (_syncRoot) { return _notifications.ToList(); } }
    }

    public void PublishToGuest(Guid guestId, string message)
    {
        lock (_syncRoot) { _notifications.Add(new Notification(guestId, message)); }
    }

    public void PublishToReception(string message)
    {
        lock (_syncRoot) { _notifications.Add(new Notification(null, message)); }
    }

    public IReadOnlyList<Notification> GetForGuest(Guid guestId)
    {
        lock (_syncRoot)
        {
            return _notifications
                .Where(n => n.RecipientGuestId == guestId)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }
    }

    public IReadOnlyList<Notification> GetForReception()
    {
        lock (_syncRoot)
        {
            return _notifications
                .Where(n => n.RecipientGuestId is null)
                .OrderByDescending(n => n.CreatedAt)
                .ToList();
        }
    }

    public int UnreadCountForGuest(Guid guestId)
    {
        lock (_syncRoot) { return _notifications.Count(n => n.RecipientGuestId == guestId && !n.IsRead); }
    }

    public int UnreadCountForReception()
    {
        lock (_syncRoot) { return _notifications.Count(n => n.RecipientGuestId is null && !n.IsRead); }
    }

    public void MarkAllReadForGuest(Guid guestId)
    {
        lock (_syncRoot)
        {
            foreach (var notification in _notifications.Where(n => n.RecipientGuestId == guestId))
            {
                notification.MarkRead();
            }
        }
    }

    public void MarkAllReadForReception()
    {
        lock (_syncRoot)
        {
            foreach (var notification in _notifications.Where(n => n.RecipientGuestId is null))
            {
                notification.MarkRead();
            }
        }
    }

    /// <summary>Zastępuje zawartość powiadomieniami wczytanymi z pliku JSON.</summary>
    public void ReplaceAll(IEnumerable<Notification> notifications)
    {
        ArgumentNullException.ThrowIfNull(notifications);

        lock (_syncRoot)
        {
            _notifications.Clear();
            _notifications.AddRange(notifications);
        }
    }
}
