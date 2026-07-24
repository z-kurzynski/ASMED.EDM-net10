# ETAP 3 - PHASE 4.3: Settings - Konfiguracja MySQL ✅

**Status**: Complete  
**Data**: 2025-01-23  
**Mod**: UI Settings - ConfigurationView dla połączeń MySQL w chmurze

---

## ✅ Zrealizowane

### 1. ConfigurationViewModel - MVVM ViewModel

**Lokalizacja**: `src\ASMED.EDM.UI\ViewModels\ustawienia\ConfigurationViewModel.cs`

**Funkcjonalność**:
- ✅ **3 typy połączeń MySQL**:
  - 🟢 **Primary Connection** - Główna baza produkcyjna w chmurze
  - 🟡 **Backup Connection** - Baza zapasowa w chmurze
  - 🔵 **Local Connection** - Baza lokalna (offline)

- ✅ **Pola dla każdego typu połączenia**:
  - Server, Database, User, Password, Port
  - Wszystkie bindowane przez CommunityToolkit.Mvvm `[ObservableProperty]`

- ✅ **Ustawienia ogólne**:
  - Enable Failover (checkbox)
  - Connection Timeout (slider 1-30s)

- ✅ **Commands**:
  ```csharp
  [RelayCommand] TestPrimaryConnectionAsync()
  [RelayCommand] TestBackupConnectionAsync()
  [RelayCommand] TestLocalConnectionAsync()
  [RelayCommand] SaveConfigurationAsync()
  ```

- ✅ **Logika**:
  - `LoadCurrentConfiguration()` - ładuje obecną konfigurację z `appsettings.json`
  - `ParseConnectionString()` - parsuje MySQL connection string na komponenty
  - `BuildConnectionString()` - buduje connection string z pól formularza
  - `TestConnectionAsync()` - testuje połączenie używając `IDatabaseConnectionService`

---

### 2. ConfigurationView.xaml - UI Layout

**Lokalizacja**: `src\ASMED.EDM.UI\Views\Settings\ConfigurationView.xaml`

**Design**:
- ✅ **ScrollViewer** - umożliwia scrollowanie dla długiego formularza
- ✅ **3 sekcje Border** (Primary, Backup, Local):
  - Każda z własnym kolorem (#4CAF50, #FF9800, #2196F3)
  - GridLayout z polami: Server, Database, User, Password, Port
  - Przycisk "🧪 Test Connection" per sekcja

- ✅ **Sekcja Settings**:
  - CheckBox: Enable Automatic Failover
  - Slider: Connection Timeout (1-30s)

- ✅ **Sekcja Status & Save**:
  - TextBlock ze statusem (binding do `StatusMessage`)
  - Przycisk "💾 Save Configuration"

---

### 3. ConfigurationView.xaml.cs - Code-Behind

**Lokalizacja**: `src\ASMED.EDM.UI\Views\Settings\ConfigurationView.xaml.cs`

**Wiring**:
```csharp
public ConfigurationView(ConfigurationViewModel viewModel)
{
	InitializeComponent();
	DataContext = viewModel;

	// Bind PasswordBox values (XAML nie pozwala na binding Password!)
	PrimaryPasswordBox.PasswordChanged += (s, e) => viewModel.PrimaryPassword = PrimaryPasswordBox.Password;
	BackupPasswordBox.PasswordChanged += (s, e) => viewModel.BackupPassword = BackupPasswordBox.Password;
	LocalPasswordBox.PasswordChanged += (s, e) => viewModel.LocalPassword = LocalPasswordBox.Password;

	// Initialize values
	PrimaryPasswordBox.Password = viewModel.PrimaryPassword;
	BackupPasswordBox.Password = viewModel.BackupPassword;
	LocalPasswordBox.Password = viewModel.LocalPassword;
}
```

---

### 4. InverseBooleanConverter - Converter

**Lokalizacja**: `src\ASMED.EDM.UI\Converters\BooleanConverters.cs`

**Dodane**:
```csharp
/// <summary>
/// Konwerter odwracający wartość bool
/// </summary>
public class InverseBooleanConverter : IValueConverter
{
	public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
	{
		if (value is bool boolValue)
			return !boolValue;
		return true;
	}
	// ...
}
```

**Rejestracja w App.xaml**:
```xaml
<converters:InverseBooleanConverter x:Key="InverseBooleanConverter"/>
```

**Użycie**: 
```xaml
IsEnabled="{Binding IsTesting, Converter={StaticResource InverseBooleanConverter}}"
```
→ Przycisk disabled gdy `IsTesting = true`

---

### 5. Dependency Injection - App.xaml.cs

**Zarejestrowane**:
```csharp
// Views
services.AddTransient<Views.Settings.ConfigurationView>();
services.AddTransient<Views.Settings.PriceListsView>();
services.AddTransient<Views.Settings.FacilityDataView>();
services.AddTransient<Views.Settings.UsersView>();
services.AddTransient<Views.Settings.ToolsView>();

// ViewModels
services.AddTransient<ViewModels.ConfigurationViewModel>();
```

---

## 🎯 Działanie

### User Flow:
1. User otwiera aplikację → przechodzi do zakładki **🗄️ Baza Danych** → **⚙️ Ustawienia** → **Konfiguracja**
2. Widzi 3 sekcje:
   - 🟢 Primary (główna baza w chmurze)
   - 🟡 Backup (zapasowa baza gdy primary pada)
   - 🔵 Local (baza lokalna gdy brak internetu)
3. Wypełnia dane połączenia (Server, Database, User, Password, Port)
4. Klika **🧪 Test Connection** dla każdej sekcji → status message pokazuje wynik
5. Ustawia **Enable Failover** (checkbox) i **Timeout** (slider)
6. Klika **💾 Save Configuration**

### Backend:
- `ConfigurationViewModel` używa `IDatabaseConnectionService.TestConnectionAsync()` do sprawdzania połączenia
- Parsowanie i budowanie MySQL connection strings
- Logowanie wyników w konsoli (przez `ILogger`)

---

## 📋 TODO - Następne Kroki

### Kr rótkoterminowe
- [ ] **Implementacja zapisu do appsettings.json**:
  - Na razie `SaveConfigurationAsync()` tylko loguje do konsoli
  - Trzeba dodać logikę zapisu do `appsettings.json`:
	```csharp
	// TODO: Użyć System.Text.Json lub Newtonsoft.Json
	// Wczytaj appsettings.json → zaktualizuj sekcję DatabaseSettings → zapisz
	```

- [ ] **Refresh MainViewModel po zapisie**:
  - Po zapisie konfiguracji → wywołaj `MainViewModel.RefreshDatabaseConnectionAsync()`
  - Wymaga przekazania `MainViewModel` przez DI lub event bus

- [ ] **Runtime test**:
  - Uruchom aplikację
  - Przejdź do Settings → Konfiguracja
  - Sprawdź czy formularze się renderują poprawnie
  - Wypełnij dane testowe i kliknij Test Connection

### Długoterminowe
- [ ] Implementacja pozostałych sub-views w Settings:
  - PriceListsView (cenniki)
  - FacilityDataView (dane placówki)
  - UsersView (użytkownicy)
  - ToolsView (narzędzia)

- [ ] Walidacja pól (np. port musi być liczbą 1-65535)
- [ ] Pokazywanie spinnera podczas testowania połączenia
- [ ] Potwierdzenie dialogi przed zapisem
- [ ] Import/export konfiguracji do pliku

---

## ✅ Build Status
**Kompilacja**: ✅ OK (bez błędów, bez ostrzeżeń)

---

## 📝 Notatki

### Dlaczego PasswordBox nie jest bindowany w XAML?
Z powodów bezpieczeństwa WPF nie pozwala na binding `Password` property w XAML. 
Rozwiązanie: handling `PasswordChanged` event w code-behind.

### Dlaczego Transient dla ConfigurationViewModel?
Każda instancja `ConfigurationView` powinna mieć swoją własną `ConfigurationViewModel`, 
żeby nie dzielić stanu między różnymi wywołaniami widoku.

### MySQL Connection String Format
```
Server={server};Port={port};Database={database};User={user};Password={password};
```

Przykład:
```
Server=my-cloud-mysql.com;Port=3306;Database=asmed_edm;User=admin;Password=secret123;
```

---

**Gotowe do testów!** 🎉
