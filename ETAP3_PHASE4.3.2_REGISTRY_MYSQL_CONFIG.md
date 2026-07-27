# ETAP 3 - PHASE 4.3.2: Registry-Based MySQL Configuration (TelsaTelecomBiling Pattern) ✅

**Status**: Complete  
**Data**: 2025-01-23  
**Pattern**: Hybrydowy Registry + appsettings.json (jak w TelsaTelecomBiling)

---

## ✅ Problem

Wcześniejsza implementacja (`Phase 4.3`) używała tylko `appsettings.json`:
- ❌ Zapis konfiguracji był TODO/symulowany
- ❌ Brak persystencji zmian przez UI
- ❌ Nie było prostego sposobu na runtime edit

**User feedback**: "ok tylko MySQL nie linkuje się zobacz jak to zrobiliśmy w projekcie // D:\Visual\TelsaTelecomBiling // to jest ten sam serwer"

---

## ✅ Rozwiązanie: TelsaTelecomBiling Pattern

### **Architektura**

**TelsaTelecomBiling** używa **Windows Registry** jako primary storage:
```
HKEY_CURRENT_USER\Software\TelsaTelecom\Biling
├── MySqlConnectionString          (produkcja)
├── MySqlConnectionStringTest      (test)
├── ActiveDatabase                  ("production" | "test")
├── YearlyDatabases                 (JSON array)
└── RunMigrationsOnStartup         ("0" | "1")
```

**Zalety**:
- ✅ **Natychmiastowa persystencja** (settery zapisują od razu do Registry)
- ✅ **Brak JSON parsing/serializacji** (prosta key-value store)
- ✅ **Nie trzeba restartować aplikacji** po zmianie connection string
- ✅ **Fallback do appsettings.json** jeśli Registry puste

---

## ✅ Implementacja w ASMED.EDM

### 1. **RegistryConfigHelper.cs** (nowy plik)

**📁 Lokalizacja**: `src/ASMED.EDM.Core/Helpers/RegistryConfigHelper.cs`

**Funkcjonalność**:
```csharp
public static class RegistryConfigHelper
{
	private const string RegistryKeyPath = @"Software\ASMED\EDM";

	// Klucze konfiguracji
	public const string KeyMySqlPrimaryConnection = "MySqlPrimaryConnection";
	public const string KeyMySqlBackupConnection = "MySqlBackupConnection";
	public const string KeyMySqlLocalConnection = "MySqlLocalConnection";
	public const string KeyActiveConnection = "ActiveConnection";
	public const string KeyEnableFailover = "EnableFailover";
	public const string KeyConnectionTimeout = "ConnectionTimeout";

	// Metody Read/Write
	public static string? GetValue(string keyName, string? defaultValue = null)
	public static void SetValue(string keyName, string? value)
	public static bool GetBoolValue(string keyName, bool defaultValue)
	public static void SetBoolValue(string keyName, bool value)
	public static int GetIntValue(string keyName, int defaultValue)
	public static void SetIntValue(string keyName, int value)
	public static void ClearAll()
}
```

**Registry path**: `HKEY_CURRENT_USER\Software\ASMED\EDM`

---

### 2. **DbConnectionFactory.cs** (nowy plik)

**📁 Lokalizacja**: `src/ASMED.EDM.Data/Services/DbConnectionFactory.cs`

**Funkcjonalność** (pattern z TelsaTelecomBiling):
```csharp
public class DbConnectionFactory
{
	// Strategie sourcing (Registry → appsettings fallback)
	public string PrimaryConnectionString =>
		EnsureMySqlCharset(
			RegistryConfigHelper.GetValue(
				RegistryConfigHelper.KeyMySqlPrimaryConnection,
				_settings.Value.PrimaryConnection));

	public string BackupConnectionString => /* analogicznie */
	public string LocalConnectionString => /* analogicznie */

	public bool EnableFailover { get; set; }
	public int ConnectionTimeout { get; set; }
	public string ActiveConnectionType { get; set; }

	public event EventHandler? ConnectionTypeChanged;

	// Metody zapisu (do Registry)
	public void SavePrimaryConnection(string connectionString)
	public void SaveBackupConnection(string connectionString)
	public void SaveLocalConnection(string connectionString)

	// Test połączenia (async)
	public async Task<(bool Success, string Message, long Ms)> TestConnectionAsync(string? cs = null)

	// Factory methods
	public DbConnection CreateConnection()
	public DbConnection CreateConnectionFromString(string connectionString)

	// Charset validation
	private static string EnsureMySqlCharset(string? cs) // Dodaje CharSet=utf8mb4 jeśli brak
}
```

**Zarejestrowany w DI** (`DataLayerServiceExtensions.cs`):
```csharp
services.AddSingleton<DbConnectionFactory>();
```

---

### 3. **ConfigurationViewModel.cs** (zaktualizowany)

**Zmiany**:

**A. Konstruktor** — dodano `DbConnectionFactory`:
```csharp
public ConfigurationViewModel(
	IDatabaseConnectionService connectionService,
	ILogger<ConfigurationViewModel> logger,
	IConfiguration configuration,
	IOptions<DatabaseSettings> databaseSettings,
	DbConnectionFactory dbFactory)  // ← NOWY
{
	_dbFactory = dbFactory;
	// ...
}
```

**B. `LoadCurrentConfiguration()`** — teraz używa Registry → appsettings fallback:
```csharp
private void LoadCurrentConfiguration()
{
	// Użyj DbConnectionFactory (Registry → appsettings)
	ParseConnectionString(
		_dbFactory.PrimaryConnectionString,  // ← Z Registry lub fallback
		out var primarySrv, out var primaryDb, ...);

	// Analogicznie dla Backup i Local
}
```

**C. `SaveConfigurationAsync()`** — ✅ **TERAZ DZIAŁA**:
```csharp
[RelayCommand]
private async Task SaveConfigurationAsync()
{
	// Buduj connection stringi
	var primaryConnStr = BuildConnectionString(...);
	var backupConnStr = BuildConnectionString(...);
	var localConnStr = BuildConnectionString(...);

	// ✅ Zapisz do Registry przez DbConnectionFactory
	_dbFactory.SavePrimaryConnection(primaryConnStr);
	_dbFactory.SaveBackupConnection(backupConnStr);
	_dbFactory.SaveLocalConnection(localConnStr);
	_dbFactory.EnableFailover = EnableFailover;
	_dbFactory.ConnectionTimeout = ConnectionTimeout;

	StatusMessage = "✅ Konfiguracja zapisana do Registry!";

	MessageBox.Show(
		"✅ Konfiguracja zapisana!\n\n" +
		"Połączenia MySQL zostały zapisane w Windows Registry:\n" +
		"HKEY_CURRENT_USER\\Software\\ASMED\\EDM\n\n" +
		"Nastepnym razem aplikacja użyje tych ustawień.",
		"Sukces", ...);
}
```

**D. `TestConnectionAsync()`** — teraz zwraca timing:
```csharp
var (success, message, ms) = await _dbFactory.TestConnectionAsync(connectionString);

if (success)
{
	StatusMessage = $"✅ {name}: Połączenie OK! [{ms} ms]";
}
```

---

## 🎯 Efekt (Przed vs Po)

### Przed (Phase 4.3):
```
User edits connection string → Click "Save"
  → TODO log + MessageBox "implementacja będzie w następnym kroku"
  → Refresh aplikacji → stare ustawienia z appsettings.json
```

### Po (Phase 4.3.2):
```
User edits connection string → Click "Save"
  → Registry write (HKEY_CURRENT_USER\Software\ASMED\EDM)
  → MessageBox "✅ Konfiguracja zapisana!"
  → Refresh aplikacji → nowe ustawienia z Registry
```

---

## 📋 Flow Diagram: Registry → appsettings.json Fallback

```
┌─────────────────────────────────────────────────────┐
│ DbConnectionFactory.PrimaryConnectionString         │
└─────────────────────┬───────────────────────────────┘
					  │
	   ┌──────────────┴──────────────┐
	   │ RegistryConfigHelper        │
	   │ GetValue(KeyMySqlPrimary,   │
	   │          appsettings.Value)  │
	   └──────────────┬──────────────┘
					  │
		 ┌────────────┴────────────┐
		 │ Registry exists?        │
		 └────┬─────────────┬──────┘
			  │ YES         │ NO
			  │             │
		┌─────▼──────┐  ┌───▼──────────────┐
		│ Return     │  │ Return fallback  │
		│ Registry   │  │ (appsettings.json│
		│ value      │  │  Primary)        │
		└────────────┘  └──────────────────┘
```

**Rezultat**: Zawsze mamy wartość (Registry lub appsettings), nigdy null/empty.

---

## ✅ Build Status
**Kompilacja**: ✅ OK (bez błędów, bez ostrzeżeń)

---

## 🚀 Jak przetestować

1. **Uruchom aplikację** (F5)
2. Przejdź do: **🗄️ Baza Danych** → **⚙️ Ustawienia** → **Konfiguracja**
3. **Edytuj primary connection**:
   - Server: `localhost` (lub zdalny serwer TelsaTelecomBiling)
   - Database: `asmed_edm`
   - User/Password: według potrzeb
4. **Kliknij "🧪 Testuj Primary"** → Powinno zwrócić `✅ Połączenie OK! [X ms]`
5. **Kliknij "💾 Zapisz konfigurację"** → MessageBox `✅ Konfiguracja zapisana!`
6. **Zweryfikuj Registry**:
   ```powershell
   Get-ItemProperty "HKCU:\Software\ASMED\EDM"
   ```
   Powinno pokazać `MySqlPrimaryConnection`, `MySqlBackupConnection`, etc.
7. **Restart aplikacji** → Connection string powinien być zachowany (z Registry, nie appsettings)

---

## 🔧 Różnice vs TelsaTelecomBiling

| Aspekt | TelsaTelecomBiling | ASMED.EDM |
|--------|-------------------|-----------|
| **Registry path** | `Software\TelsaTelecom\Biling` | `Software\ASMED\EDM` |
| **Static factory** | `DbConnectionFactory` (static) | `DbConnectionFactory` (singleton DI) |
| **Connection types** | Production/Test (2) | Primary/Backup/Local (3) |
| **Fallback** | Hardcoded defaults | `appsettings.json` |
| **Event** | `ActiveDatabaseChanged` | `ConnectionTypeChanged` |
| **Yearly databases** | ✅ (dla baz rocznych) | ❌ (na razie nie trzeba) |

---

## 📦 Pliki Dodane/Zmienione

### ✅ Dodane:
- `src/ASMED.EDM.Core/Helpers/RegistryConfigHelper.cs`
- `src/ASMED.EDM.Data/Services/DbConnectionFactory.cs`

### ✅ Zmienione:
- `src/ASMED.EDM.Data/DataLayerServiceExtensions.cs` (rejestracja DI)
- `src/ASMED.EDM.UI/ViewModels/ustawienia/ConfigurationViewModel.cs` (używa `DbConnectionFactory`)

---

## 📋 TODO - Następne Kroki

### Priorytet 1: Runtime Connection Refresh
- [ ] Po zapisie konfiguracji wyemitować event aby `MainViewModel` odświeżył status połączenia
- [ ] Dodać `MainViewModel.RefreshDatabaseConnectionAsync()` do event listenera

### Priorytet 2: Active Connection Switching
- [ ] UI toggle: Primary/Backup/Local (radio buttons lub combo box)
- [ ] Binding do `DbConnectionFactory.ActiveConnectionType`
- [ ] Event listener `ConnectionTypeChanged` → refresh UI

### Priorytet 3: Migration Tool
- [ ] Opcja "Migruj z appsettings.json do Registry" (one-time import)
- [ ] Status indicator: "Używasz Registry" vs "Używasz appsettings.json"

---

**Gotowe do użycia!** 🎉  
MySQL configuration teraz działa tak samo jak w TelsaTelecomBiling — registry-first z fallback.
