# HotelHub — System Rezerwacji Hotelowej

Projekt zaliczeniowy z przedmiotu „Wzorce projektowe i architektura aplikacji".
Konsolowa aplikacja do zarządzania rezerwacjami hotelowymi (C# 12 / .NET 8),
implementująca 9 wzorców projektowych — po 3 z każdej grupy (kreacyjne,
strukturalne, behawioralne). Projekt korzysta wyłącznie ze standardowej
biblioteki .NET (zero paczek NuGet w projekcie głównym; xUnit tylko w projekcie testowym).

## Uruchomienie

Wymagany [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0). Dwie komendy:

```bash
cd src/HotelHub
dotnet run
```

Aplikacja startuje z zeseedowanymi danymi (hotel „Pod Różą": 2 piętra, 8 pokoi
mieszanych typów, 2 gości), więc można ją demonstrować od razu.

Testy jednostkowe (z katalogu głównego repozytorium):

```bash
dotnet test
```

## Funkcje

- struktura hotelu jako drzewo: hotel → piętra → pokoje,
- wyszukiwanie pokoi dostępnych w terminie (wykrywanie kolizji rezerwacji),
- rejestracja gości z walidacją danych,
- kreator rezerwacji krok po kroku z automatycznym doborem taryfy
  (wysoki sezon lipiec–sierpień ×1.5, weekend ×1.2, standard ×1.0)
  i kodem promocyjnym `PROMO20` (×0.8),
- usługi dodatkowe doliczane do rezerwacji: śniadanie (+40 zł/noc),
  parking (+25 zł/noc), SPA (+80 zł/noc),
- pełny cykl życia rezerwacji: oczekująca → potwierdzona → opłacona → zakończona
  (+ anulowanie), z symulacją płatności i potwierdzeniem tekstowym,
- powiadomienia o zmianach stanu: e-mail gościa i recepcja (konsola)
  oraz dziennik zdarzeń `audit.log`,
- raport przychodów agregowany rekurencyjnie po drzewie hotelu,
- zapis i odczyt stanu aplikacji do pliku `hotelhub-data.json`
  (pliki powstają w katalogu aplikacji i nie trafiają do repozytorium).

## Zaimplementowane wzorce projektowe

### Kreacyjne

| Wzorzec | Klasy | Rola w aplikacji |
|---|---|---|
| Singleton | `Creational/HotelRegistry` | jedyny, bezpieczny wątkowo (`Lazy<T>`) rejestr pokoi, gości i rezerwacji |
| Factory Method | `Creational/RoomFactory` + `Domain/RoomTypes/*` | tworzenie pokoi bez znajomości klas konkretnych |
| Builder | `Creational/ReservationBuilder` | budowa rezerwacji płynnym API `.ForGuest().WithRoom().Between().WithPricing().Build()` z walidacją kompletności |

### Strukturalne

| Wzorzec | Klasy | Rola w aplikacji |
|---|---|---|
| Decorator | `Structural/Decorators/*` | łańcuchowe doliczanie usług dodatkowych do ceny i opisu pokoju |
| Composite | `Structural/Composite/*` (`Room` = liść) | drzewo hotel → piętra → pokoje; rekurencyjny raport przychodów |
| Facade | `Structural/BookingFacade` | jedyny punkt wejścia dla UI; orkiestracja dostępności, budowy rezerwacji, płatności i powiadomień |

### Behawioralne

| Wzorzec | Klasy | Rola w aplikacji |
|---|---|---|
| State | `Behavioral/States/*` | cykl życia rezerwacji; nielegalne operacje odrzucane komunikatem (bez wyjątków) |
| Observer | `Behavioral/Observers/*` | powiadomienia [E-MAIL] i [RECEPCJA] w konsoli oraz wpisy w `audit.log` przy każdej zmianie stanu |
| Strategy | `Behavioral/Pricing/*` | wymienne polityki cenowe dobierane automatycznie z dat, nadpisywane kodem `PROMO20` |

Każda klasa uczestnicząca we wzorcu ma komentarz XML `/// <summary>`
ze wskazaniem wzorca i pełnionej roli.

## Walidacja i odporność na błędy

- liczby wyłącznie przez `int.TryParse`/`decimal.TryParse`, daty przez
  `DateTime.TryParseExact` (`yyyy-MM-dd`, `CultureInfo.InvariantCulture`),
  z pętlą ponawiania przy błędnym wejściu,
- `DateRange`: koniec po początku, zakaz dat z przeszłości, maks. 30 dni pobytu,
- `Money`: wyłącznie `decimal`, zakaz wartości ujemnych,
- kolizje terminów wykrywane przez `AvailabilityService` (przedziały półotwarte —
  dzień wyjazdu może być dniem przyjazdu kolejnego gościa),
- globalny `try-catch` w pętli menu — błędne dane nigdy nie kończą aplikacji,
- deserializacja JSON odporna na uszkodzony/zmodyfikowany plik
  (`JsonException` + walidacja i pomijanie nieprawidłowych wpisów),
- zapis wyłącznie do stałych plików w katalogu aplikacji
  (`hotelhub-data.json`, `audit.log`) — nazwy plików nigdy od użytkownika.

## Struktura projektu

```
HotelHub/
├── HotelHub.sln
├── src/HotelHub/
│   ├── Program.cs          # punkt wejścia, pętla menu, globalny try-catch
│   ├── Domain/             # encje i value objecty z walidacją
│   ├── Creational/         # Singleton, Factory Method, Builder
│   ├── Structural/         # Decorator, Composite, Facade
│   ├── Behavioral/         # State, Observer, Strategy
│   ├── Services/           # dostępność, płatności, faktury, persystencja JSON
│   └── UI/                 # menu, bezpieczne wejście, renderowanie tabel
├── tests/HotelHub.Tests/   # testy xUnit (stany, ceny, dekoratory, daty)
└── docs/                   # diagramy i zrzuty ekranu
```

## Testy

37 testów xUnit: przejścia stanów rezerwacji (w tym odrzucanie nielegalnych),
wyliczenia wszystkich strategii cenowych na konkretnych datach, sumowanie cen
i opisów łańcucha dekoratorów oraz walidacja i nakładanie się zakresów dat.
