using HotelHub.Domain;
using HotelHub.Services;
using HotelHub.Structural;

namespace HotelHub.Tests;

/// <summary>
/// Testy kont użytkowników: hashowanie i weryfikacja hasła, zmiana hasła,
/// walidacje loginu/hasła oraz rejestracja konta gościa (unikalność loginu).
/// </summary>
public class AccountTests
{
    private static string UniqueLogin() => $"test_{Guid.NewGuid():N}"[..20];

    [Fact]
    public void Password_IsStoredHashed_AndVerifiable()
    {
        var service = new AccountService();
        var login = UniqueLogin();

        var account = service.CreateReceptionAccount(login, "TajneHaslo123");

        Assert.NotEqual("TajneHaslo123", account.PasswordHash);
        Assert.True(account.PasswordHash.Length > 30);

        Assert.NotNull(service.VerifyCredentials(login, "TajneHaslo123"));
        Assert.Null(service.VerifyCredentials(login, "zleHaslo999"));
        Assert.Null(service.VerifyCredentials("nieistniejacy_login", "TajneHaslo123"));
    }

    [Fact]
    public void ChangePassword_RequiresCorrectCurrentPassword()
    {
        var service = new AccountService();
        var login = UniqueLogin();
        service.CreateReceptionAccount(login, "StareHaslo1");

        Assert.False(service.ChangePassword(login, "zleHaslo", "NoweHaslo123").Success);
        Assert.True(service.ChangePassword(login, "StareHaslo1", "NoweHaslo123").Success);

        Assert.Null(service.VerifyCredentials(login, "StareHaslo1"));
        Assert.NotNull(service.VerifyCredentials(login, "NoweHaslo123"));
    }

    [Fact]
    public void ChangePassword_RejectsTooShortNewPassword()
    {
        var service = new AccountService();
        var login = UniqueLogin();
        service.CreateReceptionAccount(login, "StareHaslo1");

        var result = service.ChangePassword(login, "StareHaslo1", "krotkie");

        Assert.False(result.Success);
        Assert.NotNull(service.VerifyCredentials(login, "StareHaslo1"));
    }

    [Theory]
    [InlineData("ab")]                              // za krótki
    [InlineData("bardzo_dlugi_login_ponad_trzydziesci_znakow")] // za długi
    [InlineData("zły login!")]                      // niedozwolone znaki
    public void InvalidLogin_IsRejected(string login)
    {
        var service = new AccountService();

        Assert.Throws<ArgumentException>(() => service.CreateReceptionAccount(login, "PoprawneHaslo1"));
    }

    [Fact]
    public void TooShortPassword_IsRejected()
    {
        var service = new AccountService();

        Assert.Throws<ArgumentException>(() => service.CreateReceptionAccount(UniqueLogin(), "krotkie"));
    }

    [Fact]
    public void RegisterGuestAccount_CreatesGuestAndEnforcesUniqueLogin()
    {
        var facade = new BookingFacade();
        var login = UniqueLogin();
        var email = $"{login}@test.pl";

        var first = facade.RegisterGuestAccount(login, "HasloGoscia1", "Jan", "Testowy", email);
        Assert.True(first.Success);
        Assert.NotNull(facade.FindAccountByLogin(login));
        Assert.NotNull(facade.FindGuestByEmail(email));

        var duplicate = facade.RegisterGuestAccount(login, "InneHaslo123", "Adam", "Inny", $"inny.{email}");
        Assert.False(duplicate.Success);
    }

    [Fact]
    public void RegisterGuestAccount_ValidatesInput()
    {
        var facade = new BookingFacade();

        Assert.False(facade.RegisterGuestAccount(UniqueLogin(), "HasloGoscia1", "", "Testowy", "a@b.pl").Success);
        Assert.False(facade.RegisterGuestAccount(UniqueLogin(), "HasloGoscia1", "Jan", "Testowy", "zly-email").Success);
        Assert.False(facade.RegisterGuestAccount(UniqueLogin(), "krotkie", "Jan", "Testowy", "ok@b.pl").Success);
    }
}
