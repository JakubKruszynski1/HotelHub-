using System.Globalization;

namespace HotelHub.Domain;

/// <summary>
/// Value object reprezentujący kwotę pieniężną (wyłącznie <see cref="decimal"/>).
/// Niemutowalny, waliduje brak wartości ujemnych i zgodność walut przy operacjach.
/// </summary>
public sealed class Money : IEquatable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency = "PLN")
    {
        if (amount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(amount), "Kwota nie może być ujemna.");
        }

        if (string.IsNullOrWhiteSpace(currency))
        {
            throw new ArgumentException("Waluta nie może być pusta.", nameof(currency));
        }

        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency.Trim().ToUpperInvariant();
    }

    public static Money Zero(string currency = "PLN") => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator *(Money money, decimal multiplier)
    {
        if (multiplier < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(multiplier), "Mnożnik nie może być ujemny.");
        }

        return new Money(money.Amount * multiplier, money.Currency);
    }

    public static Money operator *(Money money, int multiplier) => money * (decimal)multiplier;

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
        {
            throw new InvalidOperationException(
                $"Nie można wykonać operacji na różnych walutach: {left.Currency} i {right.Currency}.");
        }
    }

    public bool Equals(Money? other) =>
        other is not null && Amount == other.Amount && Currency == other.Currency;

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    public override string ToString() =>
        string.Format(CultureInfo.InvariantCulture, "{0:0.00} {1}", Amount, Currency);
}
