using HotelHub.Structural;
using HotelHub.Web;
using HotelHub.Web.Components;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Fasada (Facade) — jedyny punkt dostępu UI do logiki domenowej.
// Singleton: cała aplikacja webowa współdzieli stan przez HotelRegistry (Singleton).
builder.Services.AddSingleton<BookingFacade>();

// Webowy obserwator zdarzeń (Observer) — zasila stronę /events.
builder.Services.AddSingleton<WebNotifier>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Rejestracja webowego obserwatora obok istniejących (E-MAIL, RECEPCJA, AUDYT)
// i seedowanie przykładowych danych — jak w aplikacji konsolowej.
var facade = app.Services.GetRequiredService<BookingFacade>();
facade.RegisterObserver(app.Services.GetRequiredService<WebNotifier>());
facade.SeedSampleData();

app.Run();
