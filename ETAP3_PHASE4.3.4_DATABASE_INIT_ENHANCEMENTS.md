# ETAP 3 / PHASE 4.3.4: Rozbudowa inicjalizacji bazy MySQL

**Data:** 2026-01-27  
**Status:** ✅ Ukończona  
**Powiązane:** `ETAP3_PHASE4.3.3_MYSQL_DATABASE_INITIALIZER.md`

---

## 🎯 Rozwiązane problemy

### Problem 1: Brak wyboru bazy do inicjalizacji ❌

**Przed:**
- Inicjalizacja zawsze używała aktywnego połączenia z `DbConnectionFactory`
- Nie było kontroli nad tym, która baza (Primary/Backup/Local) jest inicjalizowana

**Po:** ✅
```xaml
<ComboBox SelectedIndex="{Binding SelectedDatabaseTypeIndex}">
	<ComboBoxItem Content="🟢 Primary (Główna produkcyjna)"/>
	<ComboBoxItem Content="🟡 Backup (Zapasowa)"/>
	<ComboBoxItem Content="🔵 Local (Lokalna)"/>
</ComboBox>
```

**Logika:**
```csharp
switch (SelectedDatabaseTypeIndex)
{
	case 0: connectionString = _dbFactory.PrimaryConnectionString; break;
	case 1: connectionString = _dbFactory.BackupConnectionString; break;
	case 2: connectionString = _dbFactory.LocalConnectionString; break;
}

var result = await DatabaseInitializerMySQL.RunAsync(connectionString);
```

---

### Problem 2: Brak rozróżnienia utworzone vs istniejące tabele ❌

**Przed:**
- `InitResult` miał tylko `Created` - wszystkie tabele trafiały tam
- Użytkownik nie wiedział czy tabela była _właśnie_ utworzona czy już istniała

**Po:** ✅

**Rozszerzone `InitResult`:**
```csharp
public class InitResult
{
	public string DatabaseName { get; set; }
	public string ServerName { get; set; }
	public List<string> Created { get; } = [];         // ✨ Nowo utworzone
	public List<string> AlreadyExisted { get; } = [];  // ℹ️ Już istniały
	public List<string> Errors { get; } = [];
	public int TotalTables => Created.Count + AlreadyExisted.Count;
}
```

**Sprawdzanie w `RunAsync`:**
```csharp
// Sprawdź czy tabela już istnieje PRZED CREATE TABLE IF NOT EXISTS
bool tableExists = false;
using (var checkCmd = new MySqlCommand(
	$"SELECT COUNT(*) FROM information_schema.tables " +
	$"WHERE table_schema = '{conn.Database}' AND table_name = '{name}'", conn))
{
	tableExists = Convert.ToInt32(checkCmd.ExecuteScalar()) > 0;
}

using var cmd = new MySqlCommand(sql, conn);
cmd.ExecuteNonQuery();

if (tableExists)
	result.AlreadyExisted.Add(name);
else
	result.Created.Add(name);
```

**Raportowanie:**
```csharp
var createdList = result.Created.Count > 0 
	? "✅ Utworzone:\n• " + string.Join("\n• ", result.Created) 
	: "";

var existedList = result.AlreadyExisted.Count > 0 
	? "ℹ️ Już istniały:\n• " + string.Join("\n• ", result.AlreadyExisted) 
	: "";
```

---

### Problem 3: Brak informacji która baza jest aktywna ❌

**Przed:**
- Użytkownik nie widział w UI która baza jest aktywna
- Nie było informacji o nazwie bazy/servera

**Po:** ✅

**UI - Status w nagłówku:**
```xaml
<Border Background="#FFF3F4F6" BorderBrush="#FFD1D5DB">
	<StackPanel Orientation="Horizontal">
		<TextBlock Text="🔌 Aktywne połączenie: "/>
		<TextBlock Text="{Binding ActiveConnectionType}" 
				  Foreground="#FF059669"
				  FontWeight="Bold"/>
		<TextBlock Text="📊 Baza: "/>
		<TextBlock Text="{Binding ActiveDatabaseName}" 
				  Foreground="#FF1E40AF"
				  FontWeight="SemiBold"/>
	</StackPanel>
</Border>
```

**ViewModel - Properties:**
```csharp
[ObservableProperty]
private string _activeConnectionType = "Primary";

[ObservableProperty]
private string _activeDatabaseName = "-";

private void UpdateActiveConnectionStatus()
{
	ActiveConnectionType = _dbFactory.ActiveConnectionType.ToString();

	var activeCs = _dbFactory.ActiveConnectionString;
	ParseConnectionString(activeCs, out _, out var dbName, out _, out _, out _);
	ActiveDatabaseName = dbName;
}
```

**Wywoływane:**
- W konstruktorze `ConfigurationViewModel` podczas inicjalizacji
- Potencjalnie po każdej zmianie active connection (TODO: wire `ConnectionTypeChanged` event)

---

## 📊 MessageBox raport - przykład

### Sukces (nowa baza):
```
✅ Baza danych została zainicjalizowana!

🗄️ Baza: asmed_edm @ 192.168.1.100
🔌 Typ: Primary (Główna)

✅ Utworzone:
• P_Pacjent
• Firma
• B_Skierowania
• Badanie
• Faktura
• ... (23 tabel)

📊 Podsumowanie: 23 tabel w bazie
```

### Sukces (ponowna inicjalizacja):
```
✅ Baza danych została zainicjalizowana!

🗄️ Baza: asmed_edm @ 192.168.1.100
🔌 Typ: Primary (Główna)

✅ Utworzone:
(brak - wszystkie już istniały)

ℹ️ Już istniały:
• P_Pacjent
• Firma
• B_Skierowania
• ... (23 tabel)

📊 Podsumowanie: 23 tabel w bazie
```

### Błąd (brak konfiguracji):
```
❌ Brak konfiguracji dla bazy Backup (Zapasowa)!

Skonfiguruj połączenie i zapisz przed inicjalizacją.
```

---

## 🛠️ Zmiany w plikach

| Plik | Zmiany |
|------|--------|
| `DatabaseInitializerMySQL.cs` | ✅ `InitResult` rozszerzone o `AlreadyExisted`, `DatabaseName`, `ServerName`, `TotalTables` |
|  | ✅ Sprawdzanie `information_schema.tables` przed CREATE |
|  | ✅ Ustawienie `result.DatabaseName` i `ServerName` z połączenia |
| `ConfigurationViewModel.cs` | ✅ `SelectedDatabaseTypeIndex` - wybór bazy (0/1/2) |
|  | ✅ `ActiveConnectionType`, `ActiveDatabaseName` - status w UI |
|  | ✅ `UpdateActiveConnectionStatus()` - aktualizacja statusu |
|  | ✅ `InitializeDatabaseAsync` - switch po wybranej bazie |
|  | ✅ Raportowanie: `createdList` + `existedList` |
| `ConfigurationView.xaml` | ✅ ComboBox wyboru bazy (Primary/Backup/Local) |
|  | ✅ Border z statusem aktywnego połączenia w nagłówku |
|  | ✅ Binding `SelectedDatabaseTypeIndex` |
|  | ✅ Binding `ActiveConnectionType`, `ActiveDatabaseName` |

---

## ✅ Kompilacja

```
✅ Kompilacja powiodła się
```

Brak błędów, brak ostrzeżeń.

---

## 🧪 Testy runtime (TODO)

### Test 1: Wybór bazy
- [ ] F5 → Ustawienia → Konfiguracja
- [ ] ComboBox: wybierz Primary → Inicjalizuj
- [ ] Sprawdź MessageBox: pokazuje "Primary (Główna)"
- [ ] ComboBox: wybierz Local → Inicjalizuj
- [ ] Sprawdź MessageBox: pokazuje "Local (Lokalna)"

### Test 2: Utworzone vs istniejące
- [ ] Pierwsza inicjalizacja: sprawdź MessageBox → wszystkie w "✅ Utworzone"
- [ ] Druga inicjalizacja: sprawdź MessageBox → wszystkie w "ℹ️ Już istniały"
- [ ] DROP TABLE P_Pacjent; → trzecia inicjalizacja → tylko P_Pacjent w "✅ Utworzone"

### Test 3: Status aktywnego połączenia
- [ ] Otwórz ConfigurationView
- [ ] Sprawdź nagłówek: "🔌 Aktywne połączenie: Primary 📊 Baza: asmed_edm"
- [ ] TODO: zmień active connection → status powinien się zaktualizować (wymaga wire ConnectionTypeChanged)

### Test 4: Brak konfiguracji
- [ ] Ustaw SelectedDatabaseTypeIndex = 1 (Backup)
- [ ] Nie konfiguruj Backup connection
- [ ] Kliknij Inicjalizuj
- [ ] Sprawdź MessageBox: "❌ Brak konfiguracji dla bazy Backup (Zapasowa)!"

---

## 📝 Następne kroki

### Priorytet 1: Runtime testing
- [ ] Test wyboru bazy (Primary/Backup/Local)
- [ ] Test raportowania utworzonych vs istniejących
- [ ] Test statusu aktywnego połączenia

### Priorytet 2: Event wiring
- [ ] Wire `DbConnectionFactory.ConnectionTypeChanged` event
- [ ] Auto-refresh `ActiveConnectionType` + `ActiveDatabaseName` po zmianie
- [ ] Subscribe w `ConfigurationViewModel` konstruktorze

### Priorytet 3: UX improvements
- [ ] Disable ComboBox podczas inicjalizacji (IsInitializing)
- [ ] Progress bar dla długich operacji
- [ ] Toast notification zamiast MessageBox?

---

## ✅ Status Fazy

**Zakończona pomyślnie!**

Wszystkie 3 zgłoszone problemy rozwiązane:
1. ✅ Wybór bazy (Primary/Backup/Local dropdown)
2. ✅ Rozróżnienie utworzone vs istniejące (Created + AlreadyExisted)
3. ✅ Status aktywnego połączenia (nagłówek UI)

---

**Autor:** GitHub Copilot Agent  
**Reviewed:** N/A (wymaga runtime testing)
