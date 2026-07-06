using HotelHub.Structural;
using HotelHub.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Fasada (Facade) — jedyny punkt dostępu UI do logiki domenowej.
// Singleton: cała aplikacja webowa współdzieli stan przez HotelRegistry (Singleton).
builder.Services.AddSingleton<BookingFacade>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Seedowanie przykładowych danych — jak w aplikacji konsolowej.
app.Services.GetRequiredService<BookingFacade>().SeedSampleData();

app.Run();
