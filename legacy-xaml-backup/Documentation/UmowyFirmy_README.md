# Funkcjonalność Umów z Firmami - Instrukcja Implementacji

## Przegląd
Nowa funkcjonalność pozwala na zarządzanie umowami z firmami, śledzenie budżetu oraz wartości wykonanych badań.

## Struktura Bazy Danych

### Tabela: Umowy_Firm
```sql
CREATE TABLE Umowy_Firm (
    Id COUNTER PRIMARY KEY,
    Firma_ID LONG NOT NULL,
    Data_Umowy DATETIME NOT NULL,
    Ilosc_Miesiecy LONG DEFAULT 0,
    Czy_Terminowa YESNO DEFAULT Yes,
    Status TEXT(50) DEFAULT 'Aktywna',
    Budzet CURRENCY DEFAULT 0,
    Data_Koncowa DATETIME
);
```

### Pole w tabeli Lista_badania_do_faktury
Jeśli chcesz powiązać badania z konkretnymi umowami, dodaj pole:
```sql
ALTER TABLE Lista_badania_do_faktury ADD COLUMN Umowa_ID LONG;
```

## Kroki Implementacji

### 1. Utworzenie Tabeli w Bazie Danych
Wykonaj skrypt SQL znajdujący się w pliku `Database/CreateUmowyFirmTable.sql`:
- Otwórz bazę danych MS Access
- Przejdź do zakładki "Tworzenie" → "Projekt zapytania" → "Widok SQL"
- Wklej kod SQL z pliku i wykonaj

### 2. Dodane Pliki

#### Modele:
- `Models/Umowa.cs` - Model danych umowy z automatycznym wyliczaniem dat i kwot

#### ViewModele:
- `ViewModels/firma/UmowyFirmyViewModel.cs` - Logika zarządzania umowami

#### Widoki:
- `Views/firma/UmowyFirmyWindow.xaml` - Okno z listą umów firmy
- `Views/firma/UmowyFirmyWindow.xaml.cs` - Code-behind dla okna umów
- `Views/firma/UmowaEditDialog.xaml` - Dialog edycji/dodawania umowy
- `Views/firma/UmowaEditDialog.xaml.cs` - Code-behind dla dialogu

#### Konwertery:
- `Converters/NegativeValueToBoolConverter.cs` - Konwerter do oznaczania ujemnych wartości

#### Dokumentacja:
- `Database/CreateUmowyFirmTable.sql` - Skrypt SQL tworzący tabelę

### 3. Modyfikacje Istniejących Plików

#### ViewModels/firma/FirmaViewModel.cs
Dodano:
- `public ICommand PokazUmowyCommand { get; }`
- Metodę `PokazUmowy(Firma firma)`

#### Views/firma/FirmaView.xaml
Dodano:
- Nową kolumnę "Umowy" z przyciskiem 📄 otwierającym okno zarządzania umowami

## Funkcjonalność

### Okno Zarządzania Umowami
Po kliknięciu przycisku "Umowy" w liście firm otwiera się okno pokazujące:
- Listę wszystkich umów danej firmy
- Datę umowy
- Ilość miesięcy trwania
- Czy umowa jest terminowa
- Datę końcową (wyliczana automatycznie lub "Bezterminowa")
- Status umowy (Aktywna, Zakończona, Anulowana)
- Budżet umowy
- Wartość wykonanych badań (pobierana z bazy)
- Pozostałą kwotę (wyliczana automatycznie)

### Funkcje:
1. **Dodawanie umowy** - Przycisk "➕ Dodaj umowę"
2. **Edycja umowy** - Przycisk ✏️ w rekordzie
3. **Usuwanie umowy** - Przycisk 🗑️ w rekordzie (z potwierdzeniem)

### Automatyczne Wyliczenia:
- **Data końcowa**: Automatycznie wyliczana jako Data_Umowy + Ilosc_Miesiecy (jeśli terminowa)
- **Pozostała kwota**: Automatycznie wyliczana jako Budzet - WartoscWykonanychBadan
- **Wartość wykonanych badań**: Sumowana z tabeli Lista_badania_do_faktury (jeśli pole Umowa_ID zostanie dodane)

## Wizualne Oznaczenia
- Kolumna "Pozostało (PLN)" wyświetla wartości ujemne (przekroczenie budżetu) w kolorze czerwonym i pogrubioną czcionką

## Uwagi Techniczne
1. Okno umów używa tego samego motywu Syncfusion (Windows11Light) co reszta aplikacji
2. Wszystkie okna są modalne i wyśrodkowane względem głównego okna
3. Walidacja danych odbywa się w dialogu edycji
4. Po każdej operacji (dodanie, edycja, usunięcie) lista jest automatycznie odświeżana

## Rozszerzenia (Opcjonalne)

### Powiązanie Badań z Umowami
Jeśli chcesz śledzić, które badania należą do jakiej umowy:

1. Dodaj pole `Umowa_ID` do tabeli `Lista_badania_do_faktury`
2. Zmodyfikuj UI dodawania/edycji badań, aby pozwalał wybierać umowę
3. Wartość wykonanych badań będzie automatycznie sumowana dla każdej umowy

### Raporty
Możesz dodać raporty pokazujące:
- Umowy wygasające w najbliższym czasie
- Umowy z przekroczonym budżetem
- Statystyki wykorzystania budżetu

## Testowanie
1. Otwórz widok firm (FirmaView)
2. Kliknij przycisk 📄 "Umowy" przy wybranej firmie
3. Dodaj nową umowę używając przycisku "➕ Dodaj umowę"
4. Sprawdź automatyczne wyliczenia dat i kwot
5. Przetestuj edycję i usuwanie umów
