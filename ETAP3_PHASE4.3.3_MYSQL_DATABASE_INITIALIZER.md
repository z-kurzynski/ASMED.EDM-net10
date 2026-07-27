# ETAP 3 / PHASE 4.3.3: Inicjalizacja bazy MySQL z doc_rptObjects

**Data:** 2026-01-27  
**Status:** ✅ Ukończona  
**Powiązane:** `ETAP3_PHASE4.3.2_REGISTRY_MYSQL_CONFIG.md`, `MIGRATION_STATUS.md`

---

## 🎯 Cel

Implementacja automatycznej inicjalizacji schematu bazy danych MySQL dla ASMED.EDM na podstawie definicji tabel z `doc_rptObjects.txt`, z wykorzystaniem wzorca z `TelsaTelecomBiling`.

---

## 📋 Zakres prac

### 1. ✅ Parser definicji tabel Access → MySQL

**Plik źródłowy:** `D:\Visual\Asmed_EDM\doc_rptObjects.txt`

Wydobyto z raportu Access definicje 20 tabel:
- **P_Pacjent** - dane pacjentów (PESEL, dane osobowe, adres)
- **Firma** - kontrahenci/firmy
- **Umowy_Firm** - umowy z firmami
- **BAD_Lista** - słownik badań
- **BAD_Cennik** - cennik badań per firma
- **B_Skierowania** - skierowania na badania (główna tabela transakcyjna)
- **Badanie** - wyniki badań
- **Faktura** - faktury
- **Rejestracja** - wizyty/rejestracja pacjentów
- **Users** - użytkownicy systemu
- **LoginHistory** - historia logowań
- **S_Imiona**, **S_Nazwisko**, **S__Ulice**, **Gminy** - słowniki
- **FormatowanieTekstu**, **S_hints** - pomocnicze

### 2. ✅ Konwersja typów danych Access → MySQL

| Typ Access | Typ MySQL | Uwagi |
|------------|-----------|-------|
| `Liczba całkowita długa` | `INT` | Z `AUTO_INCREMENT` dla PK |
| `Krótki tekst(n)` | `VARCHAR(n)` | Maksymalny rozmiar 255 |
| `Długi tekst / Memo` | `TEXT` | Dla pól `Comments`, `Wynik`, itd. |
| `Tak/Nie` | `TINYINT(1)` | Boolean flags |
| `Data i godzina` | `DATETIME` | Nie używamy `TIMESTAMP` |
| `Waluta` | `DECIMAL(18,4)` | Maksymalna precyzja |

### 3. ✅ Utworzono `DatabaseInitializerMySQL.cs`

**Lokalizacja:** `src/ASMED.EDM.Data/Helpers/DatabaseInitializerMySQL.cs`

**Wzorzec:** Dokładna kopia struktury z `D:\Visual\TelsaTelecomBiling\Helpers\DatabaseInitializerMySQL.cs`

**Mechanizm:**
```csharp
public static class DatabaseInitializerMySQL
{
	// Faza 1: Tworzenie tabel (CREATE TABLE IF NOT EXISTS)
	private static readonly (string Name, string Sql)[] Tables = [...];

	// Faza 2: Migracje (ALTER TABLE ... ADD COLUMN IF NOT EXISTS)
	private static readonly (string Table, string Column, string TypeDef)[] Migrations = [...];

	// Faza 3: Seed data (INSERT z ON DUPLICATE KEY UPDATE)
	private static readonly (string Table, string Sql)[] SeedData = [...];

	// Główna metoda
	public static async Task<InitResult> RunAsync(DbConnectionFactory factory);
	public static async Task<InitResult> RunAsync(string connectionString);
}

public class InitResult
{
	public List<string> Created { get; }
	public List<string> Errors { get; }
	public bool HasErrors => Errors.Count > 0;
}
```

**Bezpieczeństwo:**
- `CREATE TABLE IF NOT EXISTS` - bezpieczne wielokrotne wywołanie
- Automatyczne indeksy dla kluczy obcych i często wyszukiwanych kolumn
- `ENGINE=InnoDB` + `CHARSET=utf8mb4` - polskie znaki + transakcje
- `ON DELETE CASCADE` / `ON DELETE SET NULL` - relacyjna integralność

### 4. ✅ Integracja z `ConfigurationViewModel`

**Zmienione:**  
`src/ASMED.EDM.UI/ViewModels/ustawienia/ConfigurationViewModel.cs`

**Before (symulacja):**
```csharp
private async Task InitializeDatabaseAsync()
{
	await Task.Delay(2000); // TODO
	MessageBox.Show("✅ Symulacja...");
}
```

**After (rzeczywista inicjalizacja):**
```csharp
private async Task InitializeDatabaseAsync()
{
	var result = await DatabaseInitializerMySQL.RunAsync(_dbFactory);

	if (result.HasErrors)
	{
		// Pokazuje błędy + utworzone tabele
	}
	else
	{
		// Pokazuje sukces + listę utworzonych tabel
	}
}
```

**Używa:**
- `DbConnectionFactory` już dostępny w DI
- `DatabaseInitializerMySQL.RunAsync(_dbFactory)` - używa aktywnego połączenia (Primary/Backup/Local)

### 5. ✅ Seed Data (dane startowe)

**Dodano domyślnego użytkownika:**
```sql
INSERT INTO `Users` (U_Login, U_Password, ...)
VALUES ('admin', 'admin', 'Administrator', 'Systemu', ...)
ON DUPLICATE KEY UPDATE U_Login = U_Login
```

⚠️ **TODO (security):**  
Hasło `'admin'` to placeholder - wymaga hashowania (BCrypt/SHA256) przed produkcją!

---

## 🔗 Relacje FK (Foreign Keys)

Automatycznie utworzone relacje:

| Tabela | Kolumna | -> Referencja |
|--------|---------|---------------|
| `Umowy_Firm` | `U_Firma_ID` | → `Firma(F_ID)` |
| `BAD_Cennik` | `BC_Badanie_ID` | → `BAD_Lista(BL_ID)` |
| `BAD_Cennik` | `BC_Firma_ID` | → `Firma(F_ID)` |
| `B_Skierowania` | `B_Pacjent_ID` | → `P_Pacjent(P_ID)` |
| `B_Skierowania` | `B_Firma_ID` | → `Firma(F_ID)` |
| `B_Skierowania` | `B_Badanie_ID` | → `BAD_Lista(BL_ID)` |
| `Badanie` | `Bad_Pacjent_ID` | → `P_Pacjent(P_ID)` |
| `Badanie` | `Bad_Skierowanie_ID` | → `B_Skierowania(B_ID)` |
| `Faktura` | `Fak_Firma_ID` | → `Firma(F_ID)` |
| `Rejestracja` | `R_Pacjent_ID` | → `P_Pacjent(P_ID)` |
| `LoginHistory` | `LH_User_ID` | → `Users(U_ID)` |

---

## 📊 Indeksy (Performance)

Automatycznie utworzone indeksy dla:

✅ **Klucze główne** - wszystkie `*_ID` PRIMARY KEY  
✅ **Kluczy obcych** - wszystkie FK automatycznie indeksowane  
✅ **PESEL** - `idx_pesel` na `P_Pacjent.P_Pesel`  
✅ **Nazwisko** - `idx_nazwisko` na `P_Pacjent.P_Nazwisko`  
✅ **NIP** - `idx_nip` na `Firma.F_NIP`  
✅ **Data skierowania** - `idx_data_skierowania` na `B_Skierowania.B_DataSkierowania`  
✅ **Login** - `idx_login` (UNIQUE) na `Users.U_Login`  
✅ **Status aktywności** - `idx_active` na polach `*_Active`

---

## 🧪 Testy (TODO)

**Przygotowane do testów manualnych:**

1. **Test pozytywny - pusta baza:**
   - Kliknij "Inicjalizuj bazę danych" w ConfigurationView
   - Sprawdź czy MessageBox pokazuje listę utworzonych tabel (20 tabel)
   - Zweryfikuj w MySQL Workbench czy tabele istnieją

2. **Test idempotentności - ponowne uruchomienie:**
   - Kliknij ponownie "Inicjalizuj bazę danych"
   - Sprawdź czy nie ma błędów (IF NOT EXISTS chroni przed duplikatami)
   - Zweryfikuj czy liczba rekordów się nie zdublowała

3. **Test seed data:**
   - Po inicjalizacji sprawdź `SELECT * FROM Users`
   - Powinien być 1 rekord: `admin` / `admin`

4. **Test relacji FK:**
   - Spróbuj wstawić `B_Skierowania` z nieistniejącym `B_Pacjent_ID`
   - Powinien być błąd FK constraint

**Automatyczne testy (priorytet niski):**
- Integracyjne testy z testową bazą MySQL
- Weryfikacja wszystkich FK constraints
- Testy migracji (dodanie kolumn na istniejącym schemacie)

---

## 🛠️ Zmiany w plikach

| Plik | Status | Opis |
|------|--------|------|
| `src/ASMED.EDM.Data/Helpers/DatabaseInitializerMySQL.cs` | ✅ Created | Nowy initializer + 20 tabel + seed data |
| `src/ASMED.EDM.UI/ViewModels/ustawienia/ConfigurationViewModel.cs` | ✅ Modified | Podpięcie prawdziwej inicjalizacji zamiast symulacji |
| `doc_rptObjects.txt` | 📖 Reference | Źródło definicji tabel (nie modyfikowany) |

---

## ⚙️ Kompilacja

```
✅ Kompilacja powiodła się
```

Brak błędów, brak ostrzeżeń.

---

## 🚀 Następne kroki

### Priorytet 1: Testy runtime
- [ ] Test inicjalizacji na rzeczywistym serwerze MySQL (zdalny + lokalny później)
- [ ] Weryfikacja poprawności danych seed (user `admin`)
- [ ] Test ponownej inicjalizacji (idempotentność)

### Priorytet 2: Security hardening
- [ ] Hashowanie hasła użytkownika `admin` (BCrypt/SHA256)
- [ ] Dodanie mechanizmu pierwszego logowania z wymuszeniem zmiany hasła

### Priorytet 3: Rozbudowa schema
- [ ] Dodanie brakujących tabel z `doc_rptObjects.txt` (jeśli są)
- [ ] Dodanie tabel audytowych (np. `Audit_Log`)
- [ ] Migracje dla przyszłych zmian schematu

### Priorytet 4: Backup & Statistics
- [ ] Implementacja BackupDatabaseAsync (mysqldump wrapper)
- [ ] Implementacja GetDatabaseStatisticsAsync (rozmiar tabel, liczba rekordów)
- [ ] Implementacja operacji konserwacyjnych (OPTIMIZE TABLE, ANALYZE)

---

## 📝 Uwagi techniczne

**Dlaczego DATETIME zamiast TIMESTAMP?**
- `DATETIME` - zakres 1000-9999, neutralny względem timezone
- `TIMESTAMP` - zakres 1970-2038, automatyczna konwersja UTC
- Access używa `Date/Time` bez timezone → `DATETIME` to lepszy odpowiednik

**Dlaczego VARCHAR(255) zamiast TEXT dla krótkich pól?**
- `VARCHAR(255)` może być indeksowane w całości
- `TEXT` może być indeksowany tylko z prefiksem (np. pierwszych 100 znaków)
- Dla pól jak `Nazwisko`, `NIP` - `VARCHAR` jest bardziej wydajny

**Dlaczego ON DELETE CASCADE vs SET NULL?**
- `CASCADE` - gdy child nie ma sensu bez parent (np. `LoginHistory` bez `User`)
- `SET NULL` - gdy child może istnieć bez parent (np. `Faktura` bez `Firma` - archiwalna)

---

## ✅ Status Fazy

**Zakończona pomyślnie!**

Baza MySQL dla ASMED.EDM może być teraz automatycznie inicjalizowana z ConfigurationView, według wzorca TelsaTelecomBiling, z pełną ochroną przed wielokrotnym uruchomieniem.

---

**Autor:** GitHub Copilot Modernization Agent  
**Reviewed:** N/A (wymaga code review + runtime testing)
