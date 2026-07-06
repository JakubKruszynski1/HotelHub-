# HotelHub — System Rezerwacji Hotelowej

Projekt zaliczeniowy z przedmiotu „Wzorce projektowe i architektura aplikacji".
System rezerwacji hotelowych (C# 12 / .NET 8) implementujący 9 wzorców projektowych —
po 3 z każdej grupy (kreacyjne, strukturalne, behawioralne) — z **dwoma niezależnymi
interfejsami użytkownika** (konsolowym i webowym Blazor Server) korzystającymi
z tej samej logiki domenowej wyłącznie przez fasadę (`BookingFacade`).

Projekt korzysta wyłącznie ze standardowej biblioteki .NET (zero paczek NuGet
w projektach produkcyjnych; xUnit tylko w projekcie testowym; Blazor Server
jest częścią SDK .NET 8).

## Architektura

```
HotelHub.Core     ← cała logika: Domain, wzorce, Services (class library)
     ▲       ▲
     │       │            oba UI wołają WYŁĄCZNIE BookingFacade (Facade)
HotelHub.Console   HotelHub.Web (Blazor Server)
```

- **HotelHub.Core** — encje z walidacją, 9 wzorców projektowych i serwisy
  (dostępność, płatności, faktury, persystencja JSON),
- **HotelHub.Console** — interaktywne menu tekstowe (`Program.cs` + `UI/`),
- **HotelHub.Web** — aplikacja Blazor Server (interaktywny tryb Server, bez WebAssembly);
  stan współdzielony przez Singleton `HotelRegistry`, operacje mutujące pod `lock`.

## Uruchomienie

Wymagany [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0).

Aplikacja **konsolowa**:

```bash
dotnet run --project src/HotelHub.Console
```

Aplikacja **webowa** (stały port, bez HTTPS):

```bash
dotnet run --project src/HotelHub.Web
```

a następnie otwórz **http://localhost:5000** w przeglądarce.

Oba interfejsy startują z zeseedowanymi danymi (hotel „Pod Różą": 2 piętra,
8 pokoi mieszanych typów, 2 gości).

Testy jednostkowe (wskazują na `HotelHub.Core`):

```bash
dotnet test
```

## Strony aplikacji webowej

| Adres | Funkcja |
|---|---|
| `/` | dashboard: rozwijane drzewo hotelu (Composite) + rezerwacje wg stanów i łączny przychód |
| `/rooms` | tabela pokoi z filtrem dostępności w zakresie dat |
| `/guests` | lista gości + formularz rejestracji z walidacją |
| `/reservations/new` | kreator rezerwacji krok po kroku (Builder): gość → termin → pokój → usługi (Decorator, wycena na żywo) → podsumowanie (Strategy, kod `PROMO20`) |
| `/reservations` | tabela rezerwacji: stan jako kolorowy badge, akcje widoczne tylko gdy stan na nie pozwala (State) |
| `/events` | panel powiadomień (Observer): wpisy [E-MAIL]/[RECEPCJA]/[AUDYT] z obserwatora `WebNotifier` |
| `/data` | zapis/odczyt stanu do pliku JSON |

Aplikacja konsolowa oferuje ten sam pełny przepływ w menu tekstowym
(struktura hotelu, dostępność, rejestracja gościa, kreator, usługi, płatność,
anulowanie, check-in/out, raport przychodów, zapis/odczyt JSON).

## Zaimplementowane wzorce projektowe

### Kreacyjne

| Wzorzec | Klasy | Rola w aplikacji |
|---|---|---|
| Singleton | `Creational/HotelRegistry` | jedyny, bezpieczny wątkowo (`Lazy<T>`) rejestr pokoi, gości i rezerwacji — wspólny stan także dla całej aplikacji webowej |
| Factory Method | `Creational/RoomFactory` + `Domain/RoomTypes/*` | tworzenie pokoi bez znajomości klas konkretnych |
| Builder | `Creational/ReservationBuilder` | budowa rezerwacji płynnym API `.ForGuest().WithRoom().Between().WithPricing().Build()` z walidacją kompletności |

### Strukturalne

| Wzorzec | Klasy | Rola w aplikacji |
|---|---|---|
| Decorator | `Structural/Decorators/*` | łańcuchowe doliczanie usług dodatkowych; w kreatorze webowym cena przelicza się na żywo przy zaznaczaniu |
| Composite | `Structural/Composite/*` (`Room` = liść) | drzewo hotel → piętra → pokoje; rekurencyjny raport przychodów (konsola i dashboard) |
| Facade | `Structural/BookingFacade` | jedyny punkt wejścia obu UI; orkiestracja dostępności, budowy rezerwacji, płatności i powiadomień |

### Behawioralne

| Wzorzec | Klasy | Rola w aplikacji |
|---|---|---|
| State | `Behavioral/States/*` | cykl życia rezerwacji; w web steruje widocznością przycisków akcji, nielegalne operacje odrzucane komunikatem (bez wyjątków) |
| Observer | `Behavioral/Observers/*` + `HotelHub.Web/WebNotifier` | powiadomienia [E-MAIL] i [RECEPCJA] w konsoli, wpisy w `audit.log` oraz panel `/events` w web |
| Strategy | `Behavioral/Pricing/*` | wymienne polityki cenowe dobierane automatycznie z dat, nadpisywane kodem `PROMO20` |

Każda klasa uczestnicząca we wzorcu ma komentarz XML `/// <summary>`
ze wskazaniem wzorca i pełnionej roli.

## Walidacja i odporność na błędy

- liczby wyłącznie przez `int.TryParse`/`decimal.TryParse`, daty przez
  `DateTime.TryParseExact` (`yyyy-MM-dd`, `CultureInfo.InvariantCulture`),
- `DateRange`: koniec po początku, zakaz dat z przeszłości, maks. 30 dni pobytu,
- `Money`: wyłącznie `decimal`, zakaz wartości ujemnych,
- kolizje terminów wykrywane przez `AvailabilityService` (przedziały półotwarte),
- konsola: globalny `try-catch` w pętli menu; web: akcje niedozwolone w danym
  stanie nie mają przycisków, a wywołane mimo to kończą się komunikatem,
- deserializacja JSON odporna na uszkodzony/zmodyfikowany plik
  (`JsonException` + walidacja i pomijanie nieprawidłowych wpisów),
- zapis wyłącznie do stałych plików w katalogu aplikacji
  (`hotelhub-data.json`, `audit.log`) — nazwy plików nigdy od użytkownika.

## Struktura projektu

```
HotelHub/
├── HotelHub.sln
├── src/
│   ├── HotelHub.Core/          # logika współdzielona (class library)
│   │   ├── Domain/             # encje i value objecty z walidacją
│   │   ├── Creational/         # Singleton, Factory Method, Builder
│   │   ├── Structural/         # Decorator, Composite, Facade
│   │   ├── Behavioral/         # State, Observer, Strategy
│   │   └── Services/           # dostępność, płatności, faktury, persystencja JSON
│   ├── HotelHub.Console/       # UI konsolowe (menu, InputReader, renderer)
│   └── HotelHub.Web/           # UI webowe Blazor Server (Components/, wwwroot/)
├── tests/HotelHub.Tests/       # testy xUnit (referencja do Core)
└── docs/                       # diagramy i zrzuty ekranu
```

## Testy

37 testów xUnit: przejścia stanów rezerwacji (w tym odrzucanie nielegalnych),
wyliczenia wszystkich strategii cenowych na konkretnych datach, sumowanie cen
i opisów łańcucha dekoratorów oraz walidacja i nakładanie się zakresów dat.
