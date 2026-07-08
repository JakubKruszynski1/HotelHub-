using HotelHub.Domain;
using HotelHub.Structural;
using HotelHub.Structural.Composite;
using HotelHub.Structural.Decorators;

namespace HotelHub.UI;

/// <summary>
/// Interaktywne menu tekstowe aplikacji. Korzysta wyłącznie z fasady
/// (<see cref="BookingFacade"/>) — nigdy z podsystemów bezpośrednio.
/// </summary>
public sealed class ConsoleMenu
{
    private readonly BookingFacade _facade;

    public ConsoleMenu(BookingFacade facade)
    {
        _facade = facade ?? throw new ArgumentNullException(nameof(facade));
    }

    /// <summary>Wyświetla menu, obsługuje jedną opcję. Zwraca false, gdy użytkownik kończy pracę.</summary>
    public bool ShowAndHandle()
    {
        Console.WriteLine();
        Console.WriteLine("=== HOTELHUB - SYSTEM REZERWACJI ===");
        Console.WriteLine("1. Wyświetl strukturę hotelu (drzewo: hotel → piętra → pokoje)");
        Console.WriteLine("2. Wyświetl dostępne pokoje w terminie");
        Console.WriteLine("3. Zarejestruj gościa");
        Console.WriteLine("4. Utwórz rezerwację (kreator krok po kroku)");
        Console.WriteLine("5. Dodaj usługi do rezerwacji (śniadanie / parking / SPA)");
        Console.WriteLine("6. Opłać rezerwację");
        Console.WriteLine("7. Anuluj rezerwację");
        Console.WriteLine("8. Zamelduj / wymelduj gościa");
        Console.WriteLine("9. Wyświetl wszystkie rezerwacje");
        Console.WriteLine("10. Raport przychodów hotelu");
        Console.WriteLine("11. Zapisz / wczytaj dane (JSON)");
        Console.WriteLine("0. Wyjście");

        var choice = InputReader.ReadInt("Wybierz opcję: ", 0, 11);

        switch (choice)
        {
            case 1: ShowHotelStructure(); break;
            case 2: ShowAvailableRooms(); break;
            case 3: RegisterGuest(); break;
            case 4: CreateReservation(); break;
            case 5: AddExtras(); break;
            case 6: PayReservation(); break;
            case 7: CancelReservation(); break;
            case 8: CheckInOrOut(); break;
            case 9: ShowAllReservations(); break;
            case 10: ShowRevenueReport(); break;
            case 11: SaveOrLoad(); break;
            case 0: return false;
        }

        return true;
    }

    private void ShowHotelStructure()
    {
        var hotel = _facade.GetHotelStructure();

        if (hotel is null)
        {
            ConsoleRenderer.Info("Struktura hotelu nie została jeszcze skonfigurowana.");
            return;
        }

        ConsoleRenderer.Header("STRUKTURA HOTELU");
        hotel.Display(0);
    }

    private void ShowAvailableRooms()
    {
        var stay = AskDateRange();

        if (stay is null)
        {
            return;
        }

        var rooms = _facade.GetAvailableRooms(stay);

        if (rooms.Count == 0)
        {
            ConsoleRenderer.Info($"Brak wolnych pokoi w terminie {stay}.");
            return;
        }

        ConsoleRenderer.Header($"POKOJE DOSTĘPNE W TERMINIE {stay}");
        ConsoleRenderer.RenderRooms(rooms);
    }

    private void RegisterGuest()
    {
        ConsoleRenderer.Header("REJESTRACJA GOŚCIA");
        var firstName = InputReader.ReadText("Imię: ");
        var lastName = InputReader.ReadText("Nazwisko: ");
        var email = InputReader.ReadEmail("E-mail: ");

        try
        {
            var guest = _facade.RegisterGuest(firstName, lastName, email);
            ConsoleRenderer.Success($"Zarejestrowano gościa: {guest}");
        }
        catch (Exception exception) when (exception is ArgumentException or InvalidOperationException)
        {
            ConsoleRenderer.Error(exception.Message);
        }
    }

    private void CreateReservation()
    {
        ConsoleRenderer.Header("KREATOR REZERWACJI");

        if (_facade.Guests.Count == 0)
        {
            ConsoleRenderer.Info("Brak zarejestrowanych gości — najpierw użyj opcji 3.");
            return;
        }

        ConsoleRenderer.Info("Krok 1/4 — wybór gościa. Zarejestrowani goście:");

        foreach (var registeredGuest in _facade.Guests)
        {
            ConsoleRenderer.Info($"  - {registeredGuest}");
        }

        var email = InputReader.ReadEmail("E-mail gościa: ");
        var guest = _facade.FindGuestByEmail(email);

        if (guest is null)
        {
            ConsoleRenderer.Error("Nie znaleziono gościa o podanym adresie e-mail. Zarejestruj go opcją 3.");
            return;
        }

        ConsoleRenderer.Info("Krok 2/4 — termin pobytu.");
        var stay = AskDateRange();

        if (stay is null)
        {
            return;
        }

        var rooms = _facade.GetAvailableRooms(stay);

        if (rooms.Count == 0)
        {
            ConsoleRenderer.Info($"Brak wolnych pokoi w terminie {stay}.");
            return;
        }

        ConsoleRenderer.Info("Krok 3/4 — wybór pokoju. Dostępne pokoje:");
        ConsoleRenderer.RenderRooms(rooms);
        var roomNumber = InputReader.ReadInt("Numer pokoju: ", 1, 9999);
        var room = rooms.FirstOrDefault(r => r.Number == roomNumber);

        if (room is null)
        {
            ConsoleRenderer.Error("Wybrany pokój nie znajduje się na liście dostępnych.");
            return;
        }

        ConsoleRenderer.Info("Krok 4/4 — kod promocyjny.");
        var promoCode = InputReader.ReadOptional("Kod promocyjny (Enter, aby pominąć): ");

        try
        {
            var reservation = _facade.MakeReservation(guest, room, stay, promoCode);
            ConsoleRenderer.Success($"Utworzono rezerwację {reservation.ShortId}.");
            Console.Write(_facade.GetInvoice(reservation));
        }
        catch (InvalidOperationException exception)
        {
            ConsoleRenderer.Error(exception.Message);
        }
    }

    private void AddExtras()
    {
        var reservation = AskReservation();

        if (reservation is null)
        {
            return;
        }

        Console.WriteLine("Dostępne usługi:");
        Console.WriteLine($"1. Śniadanie (+{BreakfastDecorator.PricePerNight} zł/noc)");
        Console.WriteLine($"2. Parking (+{ParkingDecorator.PricePerNight} zł/noc)");
        Console.WriteLine($"3. SPA (+{SpaDecorator.PricePerNight} zł/noc)");
        var choice = InputReader.ReadInt("Wybierz usługę: ", 1, 3);

        var extra = choice switch
        {
            1 => RoomExtra.Breakfast,
            2 => RoomExtra.Parking,
            _ => RoomExtra.Spa
        };

        var result = _facade.AddExtraToReservation(reservation, extra);

        if (result.Success)
        {
            ConsoleRenderer.Success(result.Message);
        }
        else
        {
            ConsoleRenderer.Info(result.Message);
        }
    }

    private void PayReservation()
    {
        var reservation = AskReservation();

        if (reservation is null)
        {
            return;
        }

        var result = _facade.PayReservation(reservation);

        if (result.Success)
        {
            ConsoleRenderer.Success(result.Message);
            Console.Write(_facade.GetInvoice(reservation));
        }
        else
        {
            ConsoleRenderer.Info(result.Message);
        }
    }

    private void CancelReservation()
    {
        var reservation = AskReservation();

        if (reservation is not null)
        {
            ConsoleRenderer.Info(_facade.CancelReservation(reservation).Message);
        }
    }

    private void CheckInOrOut()
    {
        var reservation = AskReservation();

        if (reservation is null)
        {
            return;
        }

        Console.WriteLine("1. Zamelduj (check-in)");
        Console.WriteLine("2. Wymelduj (check-out)");
        var choice = InputReader.ReadInt("Wybierz operację: ", 1, 2);

        var result = choice == 1
            ? _facade.CheckIn(reservation)
            : _facade.CheckOut(reservation);

        ConsoleRenderer.Info(result.Message);
    }

    private void ShowAllReservations()
    {
        if (_facade.Reservations.Count == 0)
        {
            ConsoleRenderer.Info("Brak rezerwacji w systemie.");
            return;
        }

        ConsoleRenderer.Header("WSZYSTKIE REZERWACJE");
        ConsoleRenderer.RenderReservations(_facade.Reservations);
    }

    private void ShowRevenueReport()
    {
        var hotel = _facade.GetHotelStructure();

        if (hotel is null)
        {
            ConsoleRenderer.Info("Struktura hotelu nie została jeszcze skonfigurowana.");
            return;
        }

        ConsoleRenderer.Header("RAPORT PRZYCHODÓW HOTELU");
        ConsoleRenderer.Info("(uwzględnia rezerwacje opłacone i zakończone)");
        DisplayRevenue(hotel, 0);
    }

    /// <summary>Rekurencyjna agregacja przychodów po drzewie Composite.</summary>
    private static void DisplayRevenue(IHotelComponent component, int indent)
    {
        Console.WriteLine($"{new string(' ', indent * 2)}{component.Name}: {component.GetRevenue()}");

        if (component is HotelBranch branch)
        {
            foreach (var child in branch.Children)
            {
                DisplayRevenue(child, indent + 1);
            }
        }
    }

    private void SaveOrLoad()
    {
        Console.WriteLine("1. Zapisz dane do pliku JSON");
        Console.WriteLine("2. Wczytaj dane z pliku JSON");
        var choice = InputReader.ReadInt("Wybierz operację: ", 1, 2);

        if (choice == 1)
        {
            _facade.SaveData();
        }
        else if (_facade.LoadData())
        {
            ConsoleRenderer.Success("Dane zostały wczytane.");
        }
    }

    /// <summary>Wczytuje termin pobytu; błędny zakres dat kończy operację komunikatem.</summary>
    private static DateRange? AskDateRange()
    {
        var from = InputReader.ReadDate($"Data przyjazdu ({InputReader.DateFormat}): ");
        var to = InputReader.ReadDate($"Data wyjazdu ({InputReader.DateFormat}): ");

        try
        {
            return new DateRange(from, to);
        }
        catch (ArgumentException exception)
        {
            ConsoleRenderer.Error(exception.Message);
            return null;
        }
    }

    /// <summary>Wyświetla rezerwacje i prosi o wybór jednej po identyfikatorze.</summary>
    private Reservation? AskReservation()
    {
        if (_facade.Reservations.Count == 0)
        {
            ConsoleRenderer.Info("Brak rezerwacji w systemie.");
            return null;
        }

        ConsoleRenderer.RenderReservations(_facade.Reservations);
        var shortId = InputReader.ReadText("Podaj identyfikator rezerwacji (pierwsze znaki): ", 36);
        var reservation = _facade.FindReservationByShortId(shortId);

        if (reservation is null)
        {
            ConsoleRenderer.Error("Nie znaleziono rezerwacji o podanym identyfikatorze.");
        }

        return reservation;
    }
}
