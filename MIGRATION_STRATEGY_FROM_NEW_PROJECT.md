# 🎯 STRATEGIA MIGRACJI UI Z POZIOMU ASMED.EDM

**Data rozpoczęcia**: 2025-01-xx  
**Projekt źródłowy**: A:\source\repos\ASMED-WPF-Application\src\ASMED_5  
**Projekt docelowy**: D:\Visual\Asmed_EDM\src\ASMED.EDM.UI  
**Framework**: .NET 10, WPF, Syncfusion 27.1.58

---

## ✅ DLACZEGO Z POZIOMU NOWEGO PROJEKTU?

### Problemy ze starym podejściem:
- ❌ Problemy z zapisem plików na dysku A:\
- ❌ Building errors w starym projekcie .NET 8
- ❌ Konieczność synchronizacji dwóch projektów
- ❌ Ryzyko konfliktów podczas kopiowania

### Zalety nowego podejścia:
- ✅ Pełna kontrola nad strukturą .NET 10
- ✅ Stabilny build environment
- ✅ DI już skonfigurowane
- ✅ Entity Framework już migrowany
- ✅ Git flow kontrolowany
- ✅ Testowanie on-the-fly

---

## 🔄 PROCES MIGRACJI POJEDYNCZEGO MODUŁU

### Krok 1: ANALIZA (w starym projekcie - tylko czytanie)
```powershell
# Otwórz w VSCode (read-only)
code "A:\source\repos\ASMED-WPF-Application\src\ASMED_5\Views\{moduł}"
```

**Co sprawdzić:**
- [ ] Struktura XAML (kontrolki, layouty, resources)
- [ ] Używane kontrolki Syncfusion
- [ ] Bindingi do ViewModel
- [ ] Event handlery w code-behind
- [ ] Zależności (services, repositories)
- [ ] Resources (styles, converters, data templates)

### Krok 2: KOPIOWANIE (do nowego projektu)
```powershell
# W terminalu PowerShell w D:\Visual\Asmed_EDM
$source = "A:\source\repos\ASMED-WPF-Application\src\ASMED_5\Views\{moduł}"
$dest = "D:\Visual\Asmed_EDM\src\ASMED.EDM.UI\Views\{NowyModuł}"

# Kopiuj pliki
Copy-Item -Path "$source\*.xaml" -Destination $dest
Copy-Item -Path "$source\*.cs" -Destination $dest
```

### Krok 3: ADAPTACJA
```csharp
// 1. Namespace
// BYŁO:
namespace ASMED_5.Views.wizytyview
// JEST:
namespace ASMED.EDM.UI.Views.Visits;

// 2. Using statements
// DODAJ:
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

// USUŃ:
using ASMED_5.Models; // stare modele

// 3. Konstruktor - DI injection
// BYŁO:
public VisitsView()
{
	InitializeComponent();
	DataContext = new VisitsViewModel();
}

// JEST:
public VisitsView()
{
	InitializeComponent();
	Loaded += OnLoaded;
}

private void OnLoaded(object sender, RoutedEventArgs e)
{
	if (DataContext == null 
		&& Application.Current is App app 
		&& app.Host != null)
	{
		DataContext = app.Host.Services.GetRequiredService<VisitsViewModel>();
	}
}
```

### Krok 4: XAML UPDATE
```xml
<!-- 1. Namespace -->
<!-- BYŁO: -->
<UserControl x:Class="ASMED_5.Views.wizytyview.VisitsView"
			 xmlns:local="clr-namespace:ASMED_5.Views.wizytyview"

<!-- JEST: -->
<UserControl x:Class="ASMED.EDM.UI.Views.Visits.VisitsView"
			 xmlns:local="clr-namespace:ASMED.EDM.UI.Views.Visits"
			 xmlns:vm="clr-namespace:ASMED.EDM.UI.ViewModels"

<!-- 2. Bindingi - sprawdź czy property istnieją w nowym ViewModel -->
<!-- 3. StaticResources - sprawdź czy są w App.xaml -->
```

### Krok 5: VIEWMODEL
```csharp
// Nowy ViewModel w D:\Visual\Asmed_EDM\src\ASMED.EDM.UI\ViewModels\

using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.UI.ViewModels;

public partial class VisitsViewModel : ViewModelBase
{
	private readonly IVisitService _visitService;  // Nowy service
	private readonly ILogger<VisitsViewModel> _logger;

	public VisitsViewModel(
		IVisitService visitService,
		ILogger<VisitsViewModel> logger)
	{
		_visitService = visitService ?? throw new ArgumentNullException(nameof(visitService));
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));

		LoadDataAsync();
	}

	[ObservableProperty]
	private ObservableCollection<Visit> _visits = new();

	// ... reszta logiki biznesowej
}
```

### Krok 6: REJESTRACJA DI
```csharp
// App.xaml.cs - ConfigureServices

services.AddTransient<Views.Visits.VisitsView>();
services.AddTransient<ViewModels.VisitsViewModel>();
```

### Krok 7: INTEGRACJA Z MAINWINDOW
```xml
<!-- MainWindow.xaml -->
<TabItem Header="📋 Rejestracja" FontSize="14" Padding="15,10" Background="#FFD367FF">
	<viewsvisits:VisitsView />
</TabItem>
```

### Krok 8: BUILD & TEST
```powershell
cd D:\Visual\Asmed_EDM
dotnet build src\ASMED.EDM.UI\ASMED.EDM.UI.csproj
```

**Checklist:**
- [ ] Compilation OK
- [ ] No warnings
- [ ] Runtime test - moduł się ładuje
- [ ] Bindingi działają
- [ ] Commands działają
- [ ] Services zwracają dane

### Krok 9: GIT COMMIT
```bash
git add .
git commit -m "feat(ui): migrate {moduł} view from legacy app

- Migrated from ASMED_5.Views.{stary}
- Added {NowyModuł}View with Syncfusion controls
- Implemented {NowyModuł}ViewModel with DI
- Integrated with MainWindow TabControl
- All bindings working, services connected"
```

---

## 📋 KOLEJNOŚĆ MIGRACJI MODUŁÓW (ETAP 3)

### ✅ ZROBIONE:
- [x] MainWindow - struktura TabControl ✅
- [x] MainWindowViewModel - zegar, DB info ✅
- [x] PatientsView - SfDataGrid z filtrowaniem ✅
- [x] PatientsViewModel - pełna funkcjonalność ✅

### 🚧 W TRAKCIE (PHASE 4):
**Iteration 1: Visits Module**
- [ ] VisitsView (wizytyview) - kalendarz wizyt
- [ ] VisitsViewModel - zarządzanie wizytami
- [ ] VisitDetailsView - szczegóły wizyty
- [ ] AddEditVisitView - dodawanie/edycja

**Iteration 2: Medical Tests**
- [ ] MedicalTestsView (badania)
- [ ] MedicalTestDetailsView
- [ ] AddEditMedicalTestView

**Iteration 3: Referrals**
- [ ] ReferralsView (Skierowania)
- [ ] ReferralDetailsView
- [ ] AddEditReferralView

### ⏳ TODO (Phase 5+):
**Financial Module**
- [ ] InvoicesView (faktura)
- [ ] PriceListsView (cenniki)
- [ ] InvoiceListsView (lista_do_faktur)

**Administrative Module**
- [ ] SettingsView (ustawienia)
- [ ] ReportsView (raporty)
- [ ] CompanyView (firma)

**Utilities**
- [ ] ImportExportView
- [ ] DialogsView

---

## 🛠️ NARZĘDZIA POMOCNICZE

### PowerShell Helper Functions:
```powershell
# Dodaj do profilu PowerShell:

function Copy-LegacyView {
	param(
		[string]$OldModule,  # np. "wizytyview"
		[string]$NewModule   # np. "Visits"
	)

	$source = "A:\source\repos\ASMED-WPF-Application\src\ASMED_5\Views\$OldModule"
	$dest = "D:\Visual\Asmed_EDM\src\ASMED.EDM.UI\Views\$NewModule"

	if (-not (Test-Path $dest)) {
		New-Item -ItemType Directory -Path $dest -Force
	}

	Copy-Item -Path "$source\*.xaml" -Destination $dest -Force
	Copy-Item -Path "$source\*.xaml.cs" -Destination $dest -Force

	Write-Host "✅ Skopiowano $OldModule -> $NewModule" -ForegroundColor Green
}

function New-ViewModel {
	param([string]$Name)

	$path = "D:\Visual\Asmed_EDM\src\ASMED.EDM.UI\ViewModels\$($Name)ViewModel.cs"

	$template = @"
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace ASMED.EDM.UI.ViewModels;

public partial class $($Name)ViewModel : ViewModelBase
{
	private readonly ILogger<$($Name)ViewModel> _logger;

	public $($Name)ViewModel(ILogger<$($Name)ViewModel> logger)
	{
		_logger = logger ?? throw new ArgumentNullException(nameof(logger));
	}

	// TODO: Implement properties and commands
}
"@

	Set-Content -Path $path -Value $template -Encoding UTF8
	Write-Host "✅ Utworzono $($Name)ViewModel.cs" -ForegroundColor Green
}
```

### VS Code Snippets:
Utwórz `.vscode/asmed-migration.code-snippets`:
```json
{
  "ASMED View": {
	"prefix": "asmed-view",
	"body": [
	  "using ASMED.EDM.UI.ViewModels;",
	  "using Microsoft.Extensions.DependencyInjection;",
	  "using System.Windows;",
	  "using System.Windows.Controls;",
	  "",
	  "namespace ASMED.EDM.UI.Views.${1:Module};",
	  "",
	  "public partial class ${1}View : UserControl",
	  "{",
	  "    public ${1}View()",
	  "    {",
	  "        InitializeComponent();",
	  "        Loaded += OnLoaded;",
	  "    }",
	  "",
	  "    private void OnLoaded(object sender, RoutedEventArgs e)",
	  "    {",
	  "        if (DataContext == null && Application.Current is App app && app.Host != null)",
	  "        {",
	  "            DataContext = app.Host.Services.GetRequiredService<${1}ViewModel>();",
	  "        }",
	  "    }",
	  "}"
	]
  }
}
```

---

## 🚨 CZĘSTE PROBLEMY I ROZWIĄZANIA

### Problem 1: "Type not found" w XAML
**Przyczyna:** Namespace nie pasuje  
**Rozwiązanie:**
```xml
<!-- Sprawdź czy namespace w x:Class pasuje do pliku .cs -->
<UserControl x:Class="ASMED.EDM.UI.Views.Visits.VisitsView"
```

### Problem 2: "No parameterless constructor"
**Przyczyna:** XAML wymaga konstruktora bez parametrów  
**Rozwiązanie:**
```csharp
// Zawsze dodaj konstruktor bezparametrowy
public VisitsView()
{
	InitializeComponent();
	Loaded += OnLoaded; // DI w Loaded event
}
```

### Problem 3: Bindings nie działają
**Przyczyna:** ViewModel nie jest ustawiony jako DataContext  
**Rozwiązanie:**
```csharp
// Debug w OnLoaded
private void OnLoaded(object sender, RoutedEventArgs e)
{
	var vm = app.Host.Services.GetRequiredService<VisitsViewModel>();
	DataContext = vm;
	System.Diagnostics.Debug.WriteLine($"DataContext set: {DataContext != null}");
}
```

### Problem 4: Service nie jest zarejestrowany
**Przyczyna:** Brak rejestracji w App.xaml.cs  
**Rozwiązanie:**
```csharp
// App.xaml.cs - ConfigureServices
services.AddTransient<IVisitService, VisitService>();
services.AddTransient<VisitsViewModel>();
```

---

## 📊 TRACKING PROGRESS

Aktualizuj ten plik po każdym zmigrowanym module:

```markdown
## MIGRATION PROGRESS

| Module | Legacy Path | New Path | Status | Date | Notes |
|--------|------------|----------|--------|------|-------|
| Patients | pacjent/ | Views/Patients/ | ✅ Done | 2025-01-22 | Fully working |
| Visits | wizytyview/ | Views/Visits/ | 🚧 In Progress | 2025-01-xx | Starting now |
| MedicalTests | badania/ | Views/MedicalTests/ | ⏳ Todo | - | - |
```

---

## 🎯 NASTĘPNE KROKI

### Teraz:
1. **Zacznij od Visits Module** - to Priorytet 1 z ETAP3_PHASE4_PLAN.md
2. Skopiuj `A:\...\Views\wizytyview` do czytania w VSCode
3. Utwórz `D:\Visual\Asmed_EDM\src\ASMED.EDM.UI\Views\Visits\VisitsView.xaml`
4. Adaptuj namespace, DI, bindingi
5. Build → Test → Commit

### Pytanie do rozważenia:
**Czy chcesz żebym teraz:**
- A) Przeprowadził migrację Visits Module krok po kroku?
- B) Utworzył pomocnicze PowerShell scripts?
- C) Przeanalizował legacy VisitsView przed rozpoczęciem?

---

**Autor**: GitHub Copilot  
**Data aktualizacji**: 2025-01-xx
