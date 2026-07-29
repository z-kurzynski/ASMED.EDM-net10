# Algorytm zapisu "Lista do faktury" — projekt i kroki

Cel:
- Opisać krok po kroku algorytm zapisu listy badań do faktury (nagłówek + pozycje),
- uwzględnić sprawdzenia istnienia ID, różne ścieżki (INSERT / UPDATE), transakcję, walidację i obsługę błędów.

Zakres danych (źródła):
- Rekordy pozycji: `AccessDbContext.AssignedBadanieDto` (pola używane w zapisie)
  - Id/relacje: `Bad_ID`, `Bad_L_ID`, `Bad_F_ID`, `Bad_P_ID`, `Bad_S_ID`
  - Daty/typ: `Bad_Data`, `Bad_Data_Do`, `Bad_Typ`, `Bad_Wynik`, `Bad_Nr_KS`
  - Pacjent: `P_imie`, `P_nazwisko`, `P_zawod`, `PacjentDisplay`
  - Ceny: `Bad_Cena1` .. `Bad_Cena10`, `Bad_Razem`
  - Prezentacyjne: `Lp`, `FirmaNazwa`, `FirmaCennik`
- Pola nagłówka z UI (`Grid_0_Firma_Faktura`):
  - `WybranaFirma` (nazwa)
  - `SelectedFirmaId` (id firmy)
  - `SelectedCennik`
  - `NumerFaktury`
  - `DataWystawienia`
  - `SumaBrutto` (ew. obliczana z `TotalRazem`)
  - `Uwagi`

Założenia:
- Operacje bazy muszą być wykonywane w transakcji (atomicznie).
- Wszystkie zapytania parametrówane, bez string concatenation.
- Przy błędzie rollback i komunikat dla użytkownika (użyj `NotificationHelper.ShowError`).
- Logowanie diagnostyczne do Output / logu.

1. Walidacja wejścia (przed transakcją)
- Sprawdź, że kolekcja pozycji (`SelectedLista.Badania`) nie jest pusta — jeśli pusta, zablokuj zapis lub zaproponuj komunikat.
- Waliduj nagłówek:
  - `WybranaFirma` lub `SelectedFirmaId` (przynajmniej jedno).
  - `NumerFaktury` (format/unikat).
  - `DataWystawienia` (jeśli null ustaw DateTime.Today).
- Sprawdź spójność cen: każda pozycja powinna mieć `Bad_Razem` zgodne z sumą cen (opcjonalnie).

2. Planowanie ścieżek zapisu (per pozycja)
- Dla każdej pozycji:
  - Jeśli `Bad_ID` istnieje w tabeli badań (DB) → aktualizujemy rekord BADANIE (UPDATE) i powiązania (np. nadanie `Bad_L_ID`, `Bad_F_ID`).
  - Jeśli `Bad_ID` == null lub nie istnieje → tworzymy nowy rekord BADANIE (INSERT) i pozyskujemy nowy `Bad_ID`.
- Dla list/faktur:
  - Jeśli istnieje faktura z `NumerFaktury` (wg klucza biznesowego) → zaktualizuj nagłówek (UPDATE).
  - W przeciwnym wypadku → INSERT nagłówka faktury i pobierz `FakturaId`.
- Powiąż pozycje z fakturą (kolumna FK) — UPDATE lub podczas INSERT ustawić FK.

3. Przykładowy przebieg operacji (wysoki poziom)
- Otwórz połączenie i rozpocznij transakcję.
- (Opcjonalnie) Lock tabel/rekordów jeśli wymagane (pessimistic).
- Sprawdzenia przed zapisem:
  - Czy `SelectedFirmaId` istnieje w tabeli `Firma`. Jeśli brak — opcje:
    - utworzyć nową firmę (INSERT) z danymi z `FirmaDto`,
    - lub zgłosić walidację i przerwać.
- Zapis nagłówka faktury:
  - SELECT po `NumerFaktury` (unikat) → jeśli istnieje -> UPDATE nagłówka (`Uwagi`, `SumaBrutto`, `DataWystawienia`, `FirmaId`, `Cennik`).
  - else INSERT nagłówka -> pobierz `FakturaId` (SCOPE_IDENTITY / RETURNING).
- Dla każdej pozycji:
  - Jeśli `Bad_ID` jest nie-null:
    - SELECT czy rekord istnieje -> UPDATE pola cen/typ/pacjent/powiązania (`Bad_L_ID`, `Bad_F_ID`, `Bad_Razem`).
    - Jeśli rekord nie istnieje (np. został usunięty) -> INSERT nowego rekordu i przypisz jego ID.
  - Jeśli `Bad_ID` null -> INSERT pozycję i przypisz `Bad_ID`.
  - W każdej pozycji przypisz `Bad_L_ID` = id listy (jeśli lista ma własne ID) lub `Bad_F_ID` = id faktury.
- Po zapisaniu wszystkich pozycji oblicz i zapisz `SumaBrutto` w nagłówku (jeśli nie była ręcznie wpisana).
- Commit transakcji.
- Powiadom użytkownika `NotificationHelper.PrintSuccess`.

4. Pseudokod C# (szkielet)
- Zawrzeć w metodzie `Save_List_Of_invoice_Comand` w VM.

Przykład (pseudokod, użyj parametryzowanych zapytań):

// open connection and transaction

using var conn = db.GetConnection(); conn.Open(); using var tx = conn.BeginTransaction(); try { // 1. ensure Firma int firmaId = EnsureFirma(conn, tx, SelectedFirmaId, WybranaFirma, ...);
// 2. header
int fakturaId = UpsertFakturaHeader(conn, tx, NumerFaktury, DataWystawienia, firmaId, SelectedCennik, Uwagi);

// 3. pozycje
foreach (var dto in SelectedLista.Badania) {
    if (dto.Bad_ID.HasValue) {
        if (ExistsBadanie(conn, tx, dto.Bad_ID.Value)) {
            UpdateBadanie(conn, tx, dto.Bad_ID.Value, dto, fakturaId, firmaId);
        } else {
            var newBadId = InsertBadanie(conn, tx, dto, fakturaId, firmaId);
            dto.Bad_ID = newBadId;
        }
    } else {
        var newBadId = InsertBadanie(conn, tx, dto, fakturaId, firmaId);
        dto.Bad_ID = newBadId;
    }
}

// 4. update totals
UpdateFakturaSum(conn, tx, fakturaId, CalculateSum(SelectedLista.Badania));

tx.Commit();
NotificationHelper.PrintSuccess("Lista zapisana do faktury.");
} catch (Exception ex) { tx.Rollback(); NotificationHelper.ShowError($"Błąd zapisu: {ex.Message}"); throw; }

// --

5. SQL: wskazówki
- INSERT nagłówka:
  - Access: użyj parametrów; pobranie ostatniego ID w Access może wymagać SELECT @@IDENTITY lub innego mechanizmu.
- UPDATE pozycja:
  - aktualizuj kolumny cen, dat, wyników i FK.
- Zawsze używaj parametrów.

6. Sprawdzanie istnienia ID
- Funkcja `ExistsX(conn, tx, id)` -> SELECT 1 FROM Table WHERE id = @id.
- Dla firm: jeśli SelectedFirmaId==null, spróbuj match by name (`Nazwa`) i ewentualnie `NIP`.
- Dla pacjenta: jeśli `Bad_P_ID` null i chcesz powiązać -> szukaj przez `P_imie` + `P_nazwisko` + data urodzenia (jeśli dostępna).

7. Obsługa konfliktów i reguły biznesowe
- Konflikt numeru faktury (unikat): jeśli numer jest już w DB, zapytaj użytkownika o nadpisanie lub automatyczne dopisanie sufiksu.
- Jeśli pozycja pochodzi z istniejącego `Bad_ID` powiązanego z inną fakturą:
  - reguła: przenieść (update `Bad_F_ID`) lub kopiować (insert nowy), zależnie od wymagań.
- Audyt: zapisuj kto i kiedy wykonał zmianę (kolumny `UpdatedBy`, `UpdatedAt`).

8. Transakcje i retry
- Otocz operacje transakcją.
- W przypadku deadlock / transient error: retry kilka razy z krótkim backoffem.
- Dla długich operacji rozważ blokady tylko tam gdzie niezbędne (np. przy inspekcji istnienia numeru faktury).

9. Walidacja po zapisie i UX
- Po commit:
  - odśwież widoki (`LoadAvailableBadania()`, `LoadFirmyItemsFromDb()`).
  - zaktualizuj `SelectedLista` i `TotalRazem`.
  - powiadom użytkownika: `NotificationHelper.PrintSuccess`.
- W przypadku niepowodzenia: `NotificationHelper.ShowError`, zapisz szczegóły w logu (Output).

10. Testy i scenariusze
- Unit i integracyjne:
  - Zapis nowej faktury (nowa firma, nowe pozycje).
  - Zapis faktury aktualizującej istniejący nagłówek.
  - Przenoszenie/aktualizacja istniejącej pozycji (`Bad_ID` istnieje).
  - Konflikt numeru faktury.
  - Transakcja rollback po symulowanym błędzie.
- Testy UI:
  - poprawność mapowania pól UI -> DB,
  - poprawne ustawienie `SelectedCennik` i wpływ na ceny.

11. Dodatkowe rekomendacje techniczne
- Centralizuj logikę dostępu do DB w `AccessDbHelper` / repository (`FakturaRepository`, `BadanieRepository`) — ułatwia testowanie.
- Przy dużej liczbie pozycji używaj batch/parametrized bulk insert jeśli DB wspiera.
- Dokumentuj wersje migracji schematu DB.

---

Planujesz, że mam przygotować:
- szkic metody `Save_List_Of_invoice_Comand` w `ListaFaktAddViewModel` (C# + parametryzowane zapytania),
- gotowe zapytania SQL (INSERT/UPDATE) dopasowane do Twojej struktury DB (`AccessDbContext`),
- lub implementację repozytorium z testami — daj znać którą opcję wybrać.

Daj znać, jaki format preferujesz następnie: kompletny kod C#, SQL czy diagram sekwencji procesu zapisu.

