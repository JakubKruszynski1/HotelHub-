namespace HotelHub.Domain;

/// <summary>
/// Wynik operacji domenowej: powodzenie/odmowa z czytelnym komunikatem po polsku.
/// Odmowa (np. operacja niedozwolona w danym stanie lub brak uprawnień)
/// nie jest sygnalizowana wyjątkiem — wyjątki nie sterują przepływem.
/// </summary>
public sealed record OperationResult(bool Success, string Message)
{
    public static OperationResult Ok(string message = "Operacja wykonana pomyślnie.") =>
        new(true, message);

    public static OperationResult Fail(string message) => new(false, message);
}
