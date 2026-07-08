namespace HotelHub.Web;

/// <summary>
/// Proste toasty sukces/błąd wyświetlane po każdej akcji (scoped per obwód).
/// Toast znika automatycznie po kilku sekundach.
/// </summary>
public sealed class ToastService
{
    public sealed record Toast(Guid Id, string Message, bool IsSuccess);

    private readonly List<Toast> _toasts = [];

    public event Action? Changed;

    public IReadOnlyList<Toast> Toasts
    {
        get { lock (_toasts) { return _toasts.ToList(); } }
    }

    public void Success(string message) => Add(message, isSuccess: true);

    public void Error(string message) => Add(message, isSuccess: false);

    /// <summary>Pokazuje toast odpowiedni do wyniku operacji.</summary>
    public void FromResult(HotelHub.Domain.OperationResult result)
    {
        if (result.Success)
        {
            Success(result.Message);
        }
        else
        {
            Error(result.Message);
        }
    }

    private void Add(string message, bool isSuccess)
    {
        var toast = new Toast(Guid.NewGuid(), message, isSuccess);

        lock (_toasts) { _toasts.Add(toast); }

        Changed?.Invoke();
        _ = RemoveLaterAsync(toast.Id);
    }

    private async Task RemoveLaterAsync(Guid id)
    {
        await Task.Delay(TimeSpan.FromSeconds(5));

        lock (_toasts) { _toasts.RemoveAll(t => t.Id == id); }

        Changed?.Invoke();
    }
}
