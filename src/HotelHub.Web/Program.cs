using HotelHub.Structural;
using HotelHub.Web.Auth;
using HotelHub.Web.Components;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Uwierzytelnianie cookie: logowanie/wylogowanie przez klasyczne endpointy HTTP.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name = "HotelHub.Auth";
        options.LoginPath = "/login";
        options.AccessDeniedPath = "/access-denied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
    });

builder.Services.AddAuthorization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddHttpContextAccessor();

// Kontekst zalogowanego użytkownika (scoped) dla komponentów i proxy fasady.
builder.Services.AddScoped<CurrentUserContext>();
builder.Services.AddScoped<ICurrentUserContext>(sp => sp.GetRequiredService<CurrentUserContext>());

// UI otrzymuje WYŁĄCZNIE IBookingFacade — proxy autoryzujące (Proxy),
// które samo tworzy i chroni fasadę właściwą.
builder.Services.AddScoped<IBookingFacade>(sp =>
    new AuthorizedBookingFacade(sp.GetRequiredService<ICurrentUserContext>()));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.MapAuthEndpoints();

// Seedowanie danych demonstracyjnych przy starcie (idempotentne).
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<IBookingFacade>().SeedSampleData();
}

app.Run();
