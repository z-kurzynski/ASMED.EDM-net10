# 📊 STATUS MIGRACJI UI - ASMED.EDM

**Data aktualizacji**: 2025-01-xx  
**Lokalizacja**: D:\Visual\Asmed_EDM  
**Projekt źródłowy**: A:\source\repos\ASMED-WPF-Application\src\ASMED_5

---

## ✅ PODSUMOWANIE

### **STRATEGIA**: Migracja z poziomu NOWEGO projektu (ASMED.EDM)

**Dlaczego?**
- ✅ Brak problemów z zapisem na dysku A:\
- ✅ .NET 10 + nowy model biznesowy gotowy
- ✅ DI skonfigurowane i działające
- ✅ Entity Framework migrowany
- ✅ Git flow kontrolowany
- ✅ Tests on-the-fly

**Podejście**: Kopiuj → Adaptuj → Integruj → Testuj → Commituj

---

## 📋 POSTĘP MIGRACJI

### ✅ **ETAP 1 & 2: DONE** (Data Layer + Core)
- [x] Entity Framework Core
- [x] Repository Pattern
- [x] Services Layer
- [x] Database Migration

### ✅ **ETAP 3 - PHASE 1-3: DONE** (UI Foundation)
- [x] MainWindow - struktura TabControl ✅
- [x] MainWindowViewModel - zegar, DB info ✅  
- [x] DI Infrastructure - App.xaml.cs ✅
- [x] Syncfusion packages (27.1.58) ✅
- [x] PatientsView - pełna funkcjonalność ✅
- [x] PatientsViewModel ✅

### 🚧 **ETAP 3 - PHASE 4: IN PROGRESS** (Visits Module)

#### **VisitsView** - Status: 🟡 Basic implementation
**Lokalizacja**: `src\ASMED.EDM.UI\Views\Visits\VisitsView.xaml`

**Co jest:**
- ✅ Layout podstawowy (Header + Calendar placeholder + Patient list)
- ✅ Bindingi do ViewModel
- ✅ Patient list z filtrowaniem
- ✅ Statistics footer
- ✅ DI wiring w code-behind

**Co brakuje** (do migracji z legacy):
- ❌ **SfScheduler** - kalendarz wizyt (108KB XAML w legacy!)
- ❌ Appointment handling
- ❌ Date selection w kalendarzu
- ❌ Cell tapping events
- ❌ Appointment templates
- ❌ Resource mapping (lekarze, gabinety)

#### **VisitsViewModel** - Status: 🟡 Basic scaffolding
**Lokalizacja**: `src\ASMED.EDM.UI\ViewModels\VisitsViewModel.cs`

**Co jest:**
- ✅ Podstawowe properties (SelectedDate, SearchText, etc.)
- ✅ Test data initialization
- ✅ Filter logic
- ✅ Statistics calculation
- ✅ DI with ILogger

**Co brakuje**:
- ❌ IVisitService integration
- ❌ Real database loading
- ❌ Scheduler appointment collection
- ❌ Date navigation commands
- ❌ Appointment CRUD operations
- ❌ Resource allocation logic

---

## 🎯 NASTĘPNE KROKI - PHASE 4 Iteration 1

### **Priorytet 1: Analiza Legacy Scheduler**

#### Krok 1: Zbadaj legacy `WizytyViewView.xaml`
**Lokalizacja**: `A:\source\repos\ASMED-WPF-Application\src\ASMED_5\Views\wizytyview\WizytyViewView.xaml`
**Rozmiar**: 108,630 bajtów (!!!)

**Co sprawdzić:**
```powershell
# W terminalu
code "A:\source\repos\ASMED-WPF-Application\src\ASMED_5\Views\wizytyview\WizytyViewView.xaml"
```

**Checklist analizy:**
- [ ] `<syncfusion:SfScheduler>` - konfiguracja główna
- [ ] `ViewMode` (Day/Week/Month/Agenda?)
- [ ] `AppointmentsSource` binding
- [ ] `ResourceCollection` (lekarze/gabinety)
- [ ] `CellTapped` event handlers
- [ ] Appointment templates (DataTemplate)
- [ ] Time slot configuration
- [ ] Working hours setup
- [ ] Styling (colors, fonts, borders)

#### Krok 2: Analiza Legacy ViewModel
**Lokalizacja**: `A:\source\repos\ASMED-WPF-Application\src\ASMED_5\ViewModels\Wizyty\WizytyViewViewModel.cs`

**Co sprawdzić:**
- [ ] Appointment model class
- [ ] ObservableCollection<Appointment>
- [ ] Date navigation commands
- [ ] Filter criteria
- [ ] Services dependencies (VisitService, PatientService?)
- [ ] Event handlers

#### Krok 3: Plan Migracji Scheduler

**Opcja A: Pełna Migracja SfScheduler**
```xml
<!-- D:\Visual\Asmed_EDM\src\ASMED.EDM.UI\Views\Visits\VisitsView.xaml -->
<Border Grid.Column="0" BorderBrush="#FFDDDDDD" BorderThickness="1" CornerRadius="4">
	<syncfusion:SfScheduler 
		x:Name="Scheduler"
		ViewType="Week"
		DisplayDate="{Binding DisplayDate}"
		AppointmentsSource="{Binding Appointments}"
		ResourceCollection="{Binding Resources}"
		CellTapped="Scheduler_CellTapped">

		<!-- Appointment template -->
		<syncfusion:SfScheduler.AppointmentTemplate>
			<DataTemplate>
				<!-- Migracja z legacy -->
			</DataTemplate>
		</syncfusion:SfScheduler.AppointmentTemplate>
	</syncfusion:SfScheduler>
</Border>
```

**Opcja B: Postpone Scheduler (tymczasowo)**
- Zamiast SfScheduler użyć prostego Calendar + ListBox
- Skupić się na CRUD wizyt
- Scheduler migrować później jako osobny task

**Rekomendacja**: **Opcja A** - pełna funkcjonalność od razu

---

## 📋 TODO LIST - Iteration 1

### **VisitsView.xaml**
- [ ] Skopiować SfScheduler z legacy XAML
- [ ] Adaptować namespaces
- [ ] Zmienić bindingi na nowy ViewModel format
- [ ] Zintegrować template appointment
- [ ] Dodać resources (lekarze, gabinety)

### **VisitsViewModel.cs**
- [ ] Dodać `IVisitService` dependency
- [ ] Utworzyć `Appointment` model/entity
- [ ] Dodać `ObservableCollection<SchedulerAppointmentInfo> Appointments`
- [ ] Dodać `ObservableCollection<SchedulerResource> Resources`
- [ ] Implementować `LoadAppointmentsAsync()`
- [ ] Dodać commands: NextDay, PrevDay, Today
- [ ] Implementować `CellTappedCommand` (dodawanie wizyt)
- [ ] Implementować CRUD operations

### **Services**
- [ ] Sprawdzić czy `IVisitService` istnieje w `ASMED.EDM.Core`
- [ ] Jeśli nie - utworzyć interface + implementation
- [ ] Zarejestrować w DI (App.xaml.cs)

### **Entities**
- [ ] Sprawdzić czy `Visit` entity ma wszystkie pola:
  - [ ] StartTime, EndTime
  - [ ] DoctorId, RoomId
  - [ ] PatientId
  - [ ] Status (Scheduled/InProgress/Completed/Cancelled)
  - [ ] Notes

---

## 🛠️ POMOCNICZE ZASOBY

### PowerShell Helpers
**Lokalizacja**: `D:\Visual\Asmed_EDM\scripts\MigrationHelpers.ps1`

**Funkcje** (wymagają execution policy):
- `Copy-LegacyView -OldModule "wizytyview" -NewModule "Visits"`
- `New-AsmedViewModel -Name "Visits"`
- `Analyze-LegacyView -Module "wizytyview"`
- `Start-ModuleMigration -OldModule "wizytyview" -NewModule "Visits"`

### Manual Commands (bez execution policy)
```powershell
# Analiza legacy
code "A:\source\repos\ASMED-WPF-Application\src\ASMED_5\Views\wizytyview\WizytyViewView.xaml"

# Lista plików w legacy
Get-ChildItem "A:\source\repos\ASMED-WPF-Application\src\ASMED_5\Views\wizytyview"

# Kopiowanie (jeśli potrzeba)
Copy-Item "A:\source\repos\ASMED-WPF-Application\src\ASMED_5\Views\wizytyview\*.xaml" `
		  -Destination "D:\Visual\Asmed_EDM\src\ASMED.EDM.UI\Views\Visits\" -Force
```

---

## 📊 MODUŁY DO MIGRACJI (Priorytet)

| # | Module | Legacy Path | New Path | Status | Priority |
|---|--------|------------|----------|--------|----------|
| 1 | Visits | wizytyview/ | Views/Visits/ | 🚧 In Progress | P1 |
| 2 | Patients Details | pacjent/ | Views/Patients/ | ⏳ Todo | P1 |
| 3 | Medical Tests | badania/ | Views/MedicalTests/ | ⏳ Todo | P2 |
| 4 | Referrals | Skierowania/ | Views/Referrals/ | ⏳ Todo | P2 |
| 5 | Invoices | faktura/ | Views/Invoices/ | ⏳ Todo | P3 |
| 6 | Price Lists | cenniki/ | Views/PriceLists/ | ⏳ Todo | P3 |
| 7 | Settings | ustawienia/ | Views/Settings/ | ⏳ Todo | P4 |
| 8 | Reports | raporty/ | Views/Reports/ | ⏳ Todo | P4 |

**Legend**:
- ✅ Done
- 🚧 In Progress
- ⏳ Todo
- P1-P4 = Priority levels

---

## 🚨 ZNANE PROBLEMY I ROZWIĄZANIA

### Problem 1: SfScheduler 108KB XAML
**Symptom**: Legacy XAML jest ogromny (108,630 bytes)  
**Przyczyna**: Wiele inline templates, styles, resources  
**Rozwiązanie**:
- Przenieść styles do `App.xaml` Resources
- Użyć `StaticResource` zamiast inline definitions
- Modularyzacja templates do osobnych plików

### Problem 2: Execution Policy PowerShell
**Symptom**: Cannot run `.ps1` scripts  
**Rozwiązanie**:
```powershell
# Tymczasowo (current session)
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass

# Lub użyj manual commands (bez .ps1)
```

### Problem 3: Legacy code używa starych modeli
**Symptom**: `ASMED_5.Models.VisitModel` vs `ASMED.EDM.Core.Entities.Visit`  
**Rozwiązanie**:
- Mapowanie podczas kopiowania
- Użyć nowych entities z Core
- AutoMapper (optional, jeśli dużo konwersji)

---

## 📖 DOKUMENTY REFERENCYJNE

### W tym repozytorium:
1. **`ETAP3_PLAN_MIGRACJI_UI.md`** - Główny plan Etapu 3
2. **`ETAP3_PHASE4_PLAN.md`** - Plan Phase 4 (moduły)
3. **`MIGRATION_STRATEGY_FROM_NEW_PROJECT.md`** - Strategia migracji (ten dokument)
4. **`scripts/MigrationHelpers.ps1`** - Pomocnicze skrypty

### Legacy projekt:
- `A:\source\repos\ASMED-WPF-Application\src\ASMED_5\` - kod źródłowy

---

## 🎯 NASTĘPNY KROK: START

### **Opcja A: Zacznij teraz Visits migration**
```powershell
# 1. Otwórz legacy w VSCode (read-only)
code "A:\source\repos\ASMED-WPF-Application\src\ASMED_5\Views\wizytyview"

# 2. Analizuj SfScheduler configuration
# 3. Skopiuj SfScheduler do VisitsView.xaml
# 4. Adaptuj bindingi
# 5. Build and test
```

### **Opcja B: Setup automation first**
```powershell
# Enable PowerShell execution
Set-ExecutionPolicy -Scope Process -ExecutionPolicy Bypass

# Load helpers
. D:\Visual\Asmed_EDM\scripts\MigrationHelpers.ps1

# Start migration
Start-ModuleMigration -OldModule "wizytyview" -NewModule "Visits"
```

### **Opcja C: Stwórz Services first**
Przygotuj backend (IVisitService, Visit entity complete) przed migracją UI

---

## ❓ PYTANIA DO ROZWAŻENIA

1. **Czy używać pełnego SfScheduler od razu, czy zacząć prościej?**
   - Pełny = dłużej, ale complete
   - Prosty = szybciej, ale limited

2. **Czy migrować wszystkie features legacy Visits, czy MVP?**
   - All features = 1:1 parity
   - MVP = basic CRUD, reszta later

3. **Priorytet: calendar UI czy CRUD backend?**
   - UI first = widoczny postęp
   - Backend first = solidne fundamenty

**Moja rekomendacja**: Backend services → Basic Calendar UI → CRUD operations → Advanced features

---

**Autor**: GitHub Copilot  
**Status**: Living document - aktualizuj po każdym postępie!
