# HotelHub — dwustronny system rezerwacji hotelowej

Projekt zaliczeniowy z przedmiotu „Wzorce projektowe i architektura aplikacji"
(studia magisterskie). Realistyczny system rezerwacji hotelowej (C# 12 / .NET 8,
Blazor Server) z **portalem gościa** i **panelem recepcji**, uwierzytelnianiem
cookie z rolami oraz **10 wzorcami projektowymi** — w tym Proxy autoryzującym
dostęp do fasady.

Zero paczek NuGet w projektach produkcyjnych — wyłącznie standardowa biblioteka
.NET i shared framework ASP.NET Core (`PasswordHasher<T>`, cookie auth);
xUnit tylko w projekcie testowym. Zero zasobów z internetu/CDN — własny CSS,
ikony inline SVG i lokalnie wygenerowane grafiki pokoi.

## Uruchomienie

Wymagany [.NET SDK 8.0](https://dotnet.microsoft.com/download/dotnet/8.0). Jedna komenda:

```bash
dotnet run --project src/HotelHub.Web
```

Aplikacja wstaje pod adresem **http://localhost:5000** z pełnym seedem:
12 pokoi na 3 piętrach (jeden w remoncie), 3 konta i 5 rezerwacji w różnych
stanach — każdy ekran ma treść od pierwszego uruchomienia.

Testy (97 testów xUnit):

```bash
dotnet test
```

## Konta demonstracyjne

| Login | Hasło | Rola |
|---|---|---|
| `admin` | `admin123` | Recepcja — panel `/admin` |
| `jan.kowalski` | `Gosc1234!` | Gość |
| `anna.nowak` | `Gosc1234!` | Gość |

Dane logowania są też wyświetlane w ramce „Konta demonstracyjne" na ekranie
logowania (projekt edukacyjny). Rejestracja samodzielna tworzy wyłącznie konta
gości; konta recepcji pochodzą tylko z seeda.

## Role i światy UI

**Portal gościa** (kremowo-granatowy z złotem): strona główna z wyszukiwarką,
katalog pokoi `/rooms` z filtrami dat/typu/osób, szczegóły pokoju z cennikiem
taryf i kalendarzem zajętości, kreator rezerwacji `/book/{nr}` (walidacja
konfliktu terminów na żywo, usługi z przeliczaniem ceny, kod `PROMO20`
z przekreśleniem starej ceny), `/my-reservations` z akcjami zależnymi od stanu,
profil, dzwoneczek powiadomień.

**Panel recepcji** (`/admin`, ciemny sidebar): pulpit dnia (kolejka akceptacji,
przyjazdy/wyjazdy dziś, obłożenie), rezerwacje z filtrami i historią operacji,
goście, zarządzanie pokojami (dodawanie, edycja, wyłączanie z użytku z powodem),
raporty przychodów po drzewie hotelu, dziennik zdarzeń z wykonawcami,
kopia zapasowa JSON.

Cykl życia rezerwacji: **Oczekująca** → (recepcja potwierdza / odrzuca z powodem)
→ **Potwierdzona** → (gość-właściciel opłaca) → **Opłacona** → (recepcja melduje)
→ **Zameldowana** → (recepcja wymeldowuje) → **Zakończona**. Anulowanie:
gość-właściciel w Oczekującej/Potwierdzonej, recepcja także w Opłaconej.

## 10 wzorców projektowych

### Kreacyjne

| Wzorzec | Klasy | Rola |
|---|---|---|
| Singleton | `Creational/HotelRegistry` | thread-safe (`Lazy<T>`) rejestr pokoi, gości, kont i rezerwacji z atomowym licznikiem numerów `RES-RRRR-NNNN` |
| Factory Method | `Creational/RoomFactory` + `Domain/RoomTypes/*` | tworzenie pokoi bez znajomości klas konkretnych |
| Builder | `Creational/ReservationBuilder` | budowa rezerwacji płynnym API z walidacją kompletności |

### Strukturalne

| Wzorzec | Klasy | Rola |
|---|---|---|
| Decorator | `Structural/Decorators/*` | usługi dodatkowe doliczane do pokoju; cena w kreatorze przelicza się na żywo |
| Composite | `Structural/Composite/*` (`Room` = liść) | drzewo hotel → piętra → pokoje; raport przychodów per gałąź |
| Facade | `Structural/BookingFacade` | orkiestracja podsystemów; jedyna droga UI do logiki |
| **Proxy** | `Structural/AuthorizedBookingFacade` (+ `IBookingFacade`) | **Protection Proxy**: kontroluje dostęp do fasady na podstawie roli i własności zasobu; UI otrzymuje wyłącznie `IBookingFacade` |

### Behawioralne

| Wzorzec | Klasy | Rola |
|---|---|---|
| State | `Behavioral/States/*` | cykl życia rezerwacji z autoryzacją ról w przejściach; nielegalna operacja = komunikat, nie wyjątek |
| Observer | `Behavioral/Observers/*` | powiadomienia per użytkownik (`NotificationCenter` + dzwoneczek), `audit.log` z loginem wykonawcy |
| Strategy | `Behavioral/Pricing/*` | taryfy: standard / wysoki sezon ×1,5 / weekend ×1,2 / `PROMO20` ×0,8 — dobierane automatycznie z dat |

Każda klasa uczestnicząca we wzorcu ma komentarz XML ze wskazaniem wzorca i roli.
W widocznym UI nie występują nazwy wzorców — wyłącznie język domeny hotelowej.

## Bezpieczeństwo i odporność

- hasła wyłącznie hashowane (`PasswordHasher<UserAccount>`), nigdzie plain text,
- cookie auth przez klasyczne endpointy HTTP (poza obwodem Blazor), role w claimach,
- autoryzacja twarda: trasy `/admin/*` niedostępne dla gościa (strona „Brak dostępu"),
  a Proxy odmawia operacji nawet przy bezpośrednim wywołaniu (np. opłacenie cudzej
  rezerwacji, płatność przez recepcję),
- walidacje domenowe: daty (`yyyy-MM-dd`, InvariantCulture, zakaz przeszłości,
  maks. 30 dni), kwoty `decimal` bez ujemnych, overlap-check dostępności,
  login 3–30 znaków, hasło min. 8,
- persystencja JSON obejmuje pokoje, gości, **konta (z hashami)**, rezerwacje
  z historią i powiadomienia; plik walidowany przy odczycie, wpisy nieprawidłowe
  pomijane; stałe nazwy plików (`hotelhub-data.json`, `audit.log`) w katalogu aplikacji,
- `HotelRegistry` i operacje mutujące fasady pod `lock` (współbieżność Blazor Server),
- formaty UI: kwoty `1 575,00 zł`, daty `dd.MM.yyyy`; storage w InvariantCulture.

## Struktura projektu

```
HotelHub/
├── HotelHub.sln
├── src/
│   ├── HotelHub.Core/           # domena, 10 wzorców, serwisy (class library)
│   │   ├── Domain/              # encje, value objecty, konta, wyniki operacji
│   │   ├── Creational/          # Singleton, Factory Method, Builder
│   │   ├── Structural/          # Decorator, Composite, Facade, Proxy
│   │   ├── Behavioral/          # State, Observer (+NotificationCenter), Strategy
│   │   └── Services/            # dostępność, płatności, faktury, konta, persystencja
│   └── HotelHub.Web/            # Blazor Server: portal gościa + panel recepcji
│       ├── Auth/                # endpointy logowania, claimy, kontekst użytkownika
│       ├── Components/          # layouty ról, strony, komponenty współdzielone
│       └── wwwroot/             # design system CSS, grafiki pokoi SVG
└── tests/HotelHub.Tests/        # 97 testów xUnit
```

## Testy

- macierz autoryzacji przejść stanów (operacja × rola × właściciel),
- Proxy: odmowy dostępu i poprawna delegacja,
- hashowanie/weryfikacja/zmiana hasła, rejestracja kont (unikalność loginu, walidacje),
- NotificationCenter: adresowanie per gość, licznik nieprzeczytanych,
- blokada wyłączenia pokoju z aktywnymi rezerwacjami, numeracja rezerwacji,
- strategie cenowe, dekoratory, zakresy dat (testy z wcześniejszych etapów).
