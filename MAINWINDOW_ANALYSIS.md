# MainWindow - Analiza Inicjalizacji i Potencjalnych Problemów
**Data**: 2025-01-22  
**Status**: ✅ Build OK (0 błędów, 0 ostrzeżeń)

---

## 📋 Struktura MainWindow

### XAML (MainWindow.xaml)
```
Window
├── Window.Resources
│   ├── DataTemplate (PatientsViewModel → PatientsView)
│   └── DataTemplate (SettingsViewModel → SettingsView)
├── Grid (3 rows: Header, Main, Footer)
│   ├── Border (Header)
│   ├── Grid (Main) → TabControlExt
│   │   ├── TabPacjenci → {Binding PacjentWidok}
│   │   ├── TabWizyty (placeholder)
│   │   ├── TabKartyBadan (placeholder)
│   │   └── TabBazaDanych → Nested TabControlExt
│   │       ├── TabFaktura (placeholder)
│   │       ├── TabPacjentDB (placeholder)
│   │       ├── TabFirma (placeholder)
│   │       ├── TabRaporty (placeholder)
│   │       └── TabUstawienia → {Binding UstawieniaWidok}
│   └── Border (Footer)
│       ├── ClockText
│       ├── chkTopMost
│       └── {Binding DatabaseInfo}
```

### Code-Behind (MainWindow.xaml.cs)
```csharp
public MainWindow(IServiceProvider serviceProvider, MainViewModel mainViewModel)
{
	// 1. Zapisz serviceProvider
	_serviceProvider = serviceProvider;

	// 2. ✅ PRAWIDŁOWA KOLEJNOŚĆ: DataContext PRZED InitializeComponent
	DataContext = mainViewModel;

	// 3. Parsuj XAML - teraz bindingi działają
	InitializeComponent();

	// 4. Zegar
	_clockTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
	_clockTimer.Tick += ClockTimer_Tick;
	_clockTimer.Start();
	UpdateClock();
}
```

---

## 🔍 Znalezione Problemy i Rozwiązania

### ❌ Problem 1: Brakujące Null Safety w DataTemplates
**Symptom**:  
Jeśli `PacjentWidok` lub `UstawieniaWidok` są `null`, bindingi w XAML mogą wyrzucić `NullReferenceException`.

**Rozwiązanie**:
```csharp
// MainViewModel.cs - upewnij się że ViewModele są ZAWSZE zainicjowane
public MainViewModel(
	IUserService userService,
	IDatabaseConnectionService connectionService,
	PatientsViewModel patientsViewModel,  // ✅ MUSI być nie-null
	SettingsViewModel settingsViewModel,   // ✅ MUSI być nie-null
	ILogger<MainViewModel> logger)
{
	// ✅ Weryfikacja null
	PacjentWidok = patientsViewModel ?? throw new ArgumentNullException(nameof(patientsViewModel));
	UstawieniaWidok = settingsViewModel ?? throw new ArgumentNullException(nameof(settingsViewModel));
}
```

**Status**: ✅ Sprawdzimy to zaraz

---

### ❌ Problem 2: Syncfusion Licensing (potencjalny)
**Symptom**:  
Aplikacja może pokazać banner "This application was built using a trial version of Syncfusion..."

**Diagnoza**:
```xaml
Line 11: syncfusionskin:SfSkinManager.Theme="{syncfusionskin:SkinManager ThemeName=Windows11Light}"
```

**Rozwiązanie**:
Jeśli masz licencję, dodaj w `App.xaml.cs` (przed `InitializeComponent`):
```csharp
protected override void OnStartup(StartupEventArgs e)
{
	// ✅ Zarejestruj licencję Syncfusion
	Syncfusion.Licensing.SyncfusionLicenseProvider.RegisterLicense("YOUR-LICENSE-KEY");

	base.OnStartup(e);
	// ... reszta kodu ...
}
```

**Status**: ⚠️ Do weryfikacji (jeśli widzisz trial banner)

---

### ❌ Problem 3: Nested TabControl z negatywnymi marginami
**Symptom**:  
```xaml
Line 127: Margin="0,-8,-8,35"
Line 75,88,103,118,154,168,182,195: Margin="-27,0,0,0"
```

Negatywne marginesy mogą powodować:
- Overlapping controls
- Błędy renderingu na różnych DPI
- Problemy z hit-testing (klikanie w niewłaściwe elementy)

**Rozwiązanie**:
Usuń negatywne marginesy i użyj poprawnego layoutu:
```xaml
<!-- Zamiast Margin="-27,0,0,0" użyj Padding lub Grid Column spacing -->
<syncfusion:TabItemExt Padding="12,14" Margin="0"/>
```

**Status**: ⚠️ Kosmetyczne, ale może powodować klikalne obszary niezgodne z wizualizacją

---

### ❌ Problem 4: Fixed Width w responsive layout
**Symptom**:
```xaml
Line 138,155,169,183,196: Width="200"
Line 218: Width="150"
Line 18: MaxWidth="1600"
Line 19: MinWidth="1500"
```

**Diagnoza**:
- Window ma `WindowState="Maximized"` ale też `MinWidth="1500"` - co jeśli monitor ma 1366px?
- Fixed width controls mogą nie skalować się poprawnie

**Rozwiązanie**:
```xaml
<!-- Zmień na responsive layout -->
<Window MinWidth="1024" MaxWidth="{x:Static SystemParameters.PrimaryScreenWidth}">
```

**Status**: ⚠️ Może powodować problemy na małych monitorach

---

### ❌ Problem 5: Brak error boundary dla ViewModels
**Symptom**:  
Jeśli `PatientsView` lub `SettingsView` wyrzuci exception w konstruktorze, cała aplikacja crashuje.

**Rozwiązanie**:
Opakowuj content w `ErrorBoundary`:
```xaml
<ContentControl Content="{Binding PacjentWidok}">
	<ContentControl.ContentTemplate>
		<DataTemplate>
			<Border BorderBrush="Red" BorderThickness="2" Padding="10">
				<StackPanel>
					<TextBlock Text="⚠️ Błąd ładowania modułu" Foreground="Red"/>
					<ContentPresenter Content="{Binding}"/>
				</StackPanel>
			</Border>
		</DataTemplate>
	</ContentControl.ContentTemplate>
</ContentControl>
```

**Status**: 🔧 Implementacja opcjonalna, ale zalecana dla produkcji

---

## 🎯 Weryfikacja Inicjalizacji (Checklist)

### ✅ Sprawdź MainViewModel
```bash
# Czy PacjentWidok i UstawieniaWidok są zawsze nie-null?
Get-Content "D:\Visual\Asmed_EDM\src\ASMED.EDM.UI\ViewModels\MainViewModel.cs" | Select-String "PacjentWidok|UstawieniaWidok"
```

### ✅ Sprawdź App.xaml.cs
```bash
# Czy DI jest poprawnie skonfigurowane?
Get-Content "D:\Visual\Asmed_EDM\src\ASMED.EDM.UI\App.xaml.cs" | Select-String "AddTransient|AddSingleton"
```

### ✅ Sprawdź Syncfusion License
```bash
# Czy jest zarejestrowana licencja?
Get-Content "D:\Visual\Asmed_EDM\src\ASMED.EDM.UI\App.xaml.cs" | Select-String "SyncfusionLicenseProvider"
```

---

## 🚨 Krytyczne Fix (Do zrobienia TERAZ)

### 1. Weryfikacja MainViewModel initialization
**Sprawdzić**:
- Czy `PacjentWidok` i `UstawieniaWidok` są inicjowane w konstruktorze?
- Czy nie ma race condition z `InitializeDatabaseInfoAsync`?

### 2. Async void w OnStartup (już naprawione)
**Before**:
```csharp
var connectionString = await connectionService.GetActiveConnectionStringAsync();
```
**After**:
```csharp
_ = Task.Run(async () => {
	var connectionString = await connectionService.GetActiveConnectionStringAsync();
});
```
✅ **Status**: Naprawione (Task.Run eliminuje deadlock)

---

## 📊 Diagnoza Runtime Problems

### Jeśli aplikacja się nie uruchamia:
1. **Sprawdź Output window** w VS (Debug → Windows → Output)
2. **Sprawdź Exception Details** (jeśli debugger łapie exception)
3. **Sprawdź DI registration** - czy wszystkie zależności są zarejestrowane?

### Jeśli aplikacja się uruchamia ale UI nie renderuje:
1. **Sprawdź DataContext** - czy `MainViewModel` jest przypisany?
2. **Sprawdź Bindingi** - włącz diagnostykę:
   ```xml
   xmlns:diagnostics="clr-namespace:System.Diagnostics;assembly=WindowsBase"
   diagnostics:PresentationTraceSources.TraceLevel="High"
   ```
3. **Sprawdź ViewModels** - czy `PacjentWidok`/`UstawieniaWidok` nie są null?

---

## 🎯 Next Steps

### Krótkoterminowe (TERAZ)
1. ✅ Sprawdź `MainViewModel.cs` - weryfikacja inicjalizacji
2. ⚠️ Uruchom aplikację w debugger i sprawdź czy wszystko się ładuje
3. ⚠️ Sprawdź Output window dla exception/binding errors

### Średnioterminowe
1. 🔧 Usuń negatywne marginesy w TabControls
2. 🔧 Zmień fixed width na responsive layout
3. 🔧 Dodaj error boundary dla ViewModels

### Długoterminowe
1. 📦 Zarejestruj Syncfusion license (jeśli masz)
2. 🧪 Dodać unit tests dla MainViewModel initialization
3. 🎨 Refaktoryzuj nested TabControl na clean architecture

---

**Następny krok**: Sprawdzam `MainViewModel.cs` teraz...
