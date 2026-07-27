# Plan Migracji XAML - legacy-xaml-backup → src/ASMED.EDM.UI

## 📊 Podsumowanie
- **Źródło:** `legacy-xaml-backup/Views/`
- **Cel:** `src/ASMED.EDM.UI/Views/`
- **Plików do migracji:** ~50 plików XAML
- **Strategia:** Migracja plik po pliku z automatycznym czyszczeniem

## 🎯 Proces migracji (per plik)

### Krok 1: Kopiowanie
```
legacy-xaml-backup/Views/{folder}/{File}.xaml 
→ src/ASMED.EDM.UI/Views/{folder}/{File}.xaml
```

### Krok 2: Czyszczenie XAML
1. ✅ Zamień namespace: `ASMED.WPF` → `ASMED.EDM.UI`
2. ✅ Usuń `xmlns:local` (jeśli nieużywane)
3. ✅ Usuń `xmlns:d`, `xmlns:mc`, `mc:Ignorable`, `d:DesignHeight/Width`
4. ✅ Usuń `xmlns:av` i inne custom xmlns jeśli nieużywane
5. ✅ Zostaw tylko: `xmlns`, `xmlns:x`, `xmlns:syncfusion` (gdy używane)
6. ✅ Ustaw `Background="White"` lub odpowiedni kolor

### Krok 3: Utworzenie Code-behind (.xaml.cs)
```csharp
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.{Folder};

public partial class {ClassName} : UserControl
{
	public {ClassName}()
	{
		InitializeComponent();
	}
}
```

### Krok 4: Utworzenie ViewModel (jeśli potrzebny)
```csharp
using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.UI.ViewModels;

public partial class {ClassName}ViewModel : ObservableObject
{
	private readonly ILogger<{ClassName}ViewModel> _logger;

	public {ClassName}ViewModel(ILogger<{ClassName}ViewModel> logger)
	{
		_logger = logger;
	}
}
```

### Krok 5: Rejestracja w DI (App.xaml.cs)
```csharp
services.AddTransient<Views.{Folder}.{ClassName}>();
services.AddTransient<ViewModels.{ClassName}ViewModel>();
```

---

## 📋 Lista plików do migracji (priorytet)

### ✅ USTAWIENIA (7 plików - PRIORYTET 1)
- [x] ~~`ustawienia/UstawieniaView.xaml`~~ (główny - już istnieje w projekcie)
- [ ] `ustawienia/KonfiguracjaView.xaml` → `Settings/ConfigurationView.xaml` (KONFLIKT - sprawdzić)
- [ ] `ustawienia/DanePlacowkiView.xaml` → `Settings/FacilityDataView.xaml` (KONFLIKT - sprawdzić)
- [ ] `ustawienia/UzytkownicyView.xaml` → `Settings/UsersView.xaml` (KONFLIKT - sprawdzić)
- [ ] `ustawienia/NarzedziaView.xaml` → `Settings/ToolsView.xaml` (KONFLIKT - sprawdzić)
- [ ] `ustawienia/DuplikatyScalDialog.xaml` → `Settings/Dialogs/MergeDuplicatesDialog.xaml`

### 📝 PACJENCI (4 pliki - PRIORYTET 2)
- [ ] `pacjent/ListaPacjentowView.xaml` → `Patients/PatientsListView.xaml`
- [ ] `pacjent/PacjentDodajView.xaml` → `Patients/AddPatientView.xaml`
- [ ] `pacjent/PatientAdd.xaml` → `Patients/PatientAddDialog.xaml`
- [ ] `pacjent/PacjentSkierowanieView.xaml` → `Patients/PatientReferralView.xaml`

### 🔬 BADANIA (5 plików - PRIORYTET 3)
- [ ] `badania/BadaniaView.xaml` → `Examinations/ExaminationsView.xaml`
- [ ] `badania/BadaniaListaView.xaml` → `Examinations/ExaminationsListView.xaml`
- [x] ~~`badania/badania_Edit_View.xaml`~~ → `Examinations/EditExaminationView.xaml` ✅ **ZMIGROWANE**
- [ ] `badania/BadaniaNewView.xaml` → `Examinations/NewExaminationView.xaml`
- [ ] `badania/BadaniaEditNewView.xaml` → `Examinations/EditNewExaminationView.xaml`

### 📅 WIZYTY (2 pliki - PRIORYTET 4)
- [ ] `wizytyview/WizytyViewView.xaml` → `Visits/VisitsView.xaml`
- [ ] `raporty/WizytyView.xaml` → `Reports/VisitsReportView.xaml`

### 🧾 FAKTURY (3 pliki - PRIORYTET 5)
- [ ] `faktura/FakturaView.xaml` → `Invoices/InvoiceView.xaml`
- [ ] `faktura/FakturaImportView.xaml` → `Invoices/ImportInvoiceView.xaml`

### 📊 LISTA DO FAKTUR (6 plików - PRIORYTET 6)
- [ ] `lista_do_faktur/ListaDoFakturView.xaml` → `InvoiceList/InvoiceListView.xaml`
- [ ] `lista_do_faktur/ListaDoFaktur_DetailView.xaml` → `InvoiceList/InvoiceListDetailView.xaml`
- [ ] `lista_do_faktur/ListaDoFaktur_EditView.xaml` → `InvoiceList/EditInvoiceListView.xaml`
- [ ] `lista_do_faktur/ListaFaktAddView.xaml` → `InvoiceList/AddInvoiceItemView.xaml`
- [ ] `lista_do_faktur/ArchiveImportView.xaml` → `InvoiceList/ArchiveImportView.xaml`
- [ ] `lista_do_faktur/FirmaSelectDialog.xaml` → `InvoiceList/Dialogs/SelectCompanyDialog.xaml`

### 📄 SKIEROWANIA (5 plików - PRIORYTET 7)
- [ ] `Skierowania/SkierowaniaView.xaml` → `Referrals/ReferralsView.xaml`
- [ ] `Skierowania/SkierListaPacjentowView.xaml` → `Referrals/PatientReferralsListView.xaml`
- [ ] `Skierowania/SkierPacjentaView.xaml` → `Referrals/PatientReferralView.xaml`
- [ ] `Skierowania/SkierPacjentaEditView.xaml` → `Referrals/EditPatientReferralView.xaml`
- [ ] `Skierowania/SkierNewPacjentaView.xaml` → `Referrals/NewPatientReferralView.xaml`
- [ ] `Skierowania/PacjentHistoriaDialog.xaml` → `Referrals/Dialogs/PatientHistoryDialog.xaml`

### 🏢 FIRMY (4 pliki - PRIORYTET 8)
- [ ] `firma/FirmaView.xaml` → `Companies/CompaniesView.xaml`
- [ ] `firma/UmowyFirmyWindow.xaml` → `Companies/CompanyContractsWindow.xaml`
- [ ] `firma/UmowaEditDialog.xaml` → `Companies/Dialogs/EditContractDialog.xaml`

### 💰 CENNIKI (1 plik - PRIORYTET 9)
- [ ] `cenniki/CennikiView.xaml` → `PriceLists/PriceListsView.xaml` (KONFLIKT - sprawdzić)

### 📈 RAPORTY (5 plików - PRIORYTET 10)
- [ ] `raporty/RaportyView.xaml` → `Reports/ReportsView.xaml`
- [ ] `raporty/StatystykiView.xaml` → `Reports/StatisticsView.xaml`
- [ ] `raporty/MedyczneView.xaml` → `Reports/MedicalReportsView.xaml`
- [ ] `raporty/RaportMZ35AView.xaml` → `Reports/MZ35AReportView.xaml`
- [ ] `raporty/WompView.xaml` → `Reports/WompView.xaml`

### 📥 IMPORT/EXPORT (3 pliki - PRIORYTET 11)
- [ ] `Import-Export/OutlookImportWindow.xaml` → `Import-Export/OutlookImportWindow.xaml`
- [ ] `Import-Export/OutlookAttachmentPreviewWindow.xaml` → `Import-Export/OutlookAttachmentPreviewWindow.xaml`
- [ ] `Import-Export/ListaSkierowanWindow.xaml` → `Import-Export/ReferralsListWindow.xaml`

### 🪟 DIALOGI (2 pliki - PRIORYTET 12)
- [ ] `Dialogs/OtwarteKartyBadanDialog.xaml` → `Dialogs/OpenExaminationCardsDialog.xaml`
- [ ] `Dialogs/CustomPrintDialog.xaml` → `Dialogs/CustomPrintDialog.xaml`

### 📂 POZOSTAŁE (4 pliki - PRIORYTET 13)
- [ ] `TenplateView.xaml` → `TemplateView.xaml`
- [ ] `LoginWindow.xaml` → (pominąć - prawdopodobnie nie używane)
- [ ] `SplashWindow.xaml` → (pominąć - prawdopodobnie nie używane)
- [ ] `PdfPreviewWindow.xaml` → `Dialogs/PdfPreviewWindow.xaml`

---

## ⚠️ KONFLIKTY DO SPRAWDZENIA

Przed migracją sprawdź czy te pliki już istnieją w projekcie:
1. `Settings/ConfigurationView.xaml` - **ISTNIEJE!** (sprawdzić zawartość)
2. `Settings/FacilityDataView.xaml` - **ISTNIEJE!** (sprawdzić zawartość)
3. `Settings/UsersView.xaml` - **ISTNIEJE!** (sprawdzić zawartość)
4. `Settings/ToolsView.xaml` - **ISTNIEJE!** (sprawdzić zawartość)
5. `Settings/PriceListsView.xaml` - **ISTNIEJE!** (sprawdzić zawartość)

**Strategia konfliktu:**
- Jeśli nowy plik ma więcej funkcjonalności → merge
- Jeśli stary legacy jest bardziej kompletny → zastąp
- Jeśli różnice minimalne → zostaw nowy

---

## 🚀 Jak używać tego planu?

### Automatyczna migracja (przez agenta)
Powiedz mi:
```
"Zmigruj plik ustawienia/KonfiguracjaView.xaml do Settings/"
```

Lub:
```
"Zmigruj wszystkie pliki z kategorii PACJENCI"
```

### Ręczna migracja
1. Znajdź plik w legacy-xaml-backup
2. Skopiuj do odpowiedniego folderu w src/ASMED.EDM.UI
3. Wyczyść XAML według kroków
4. Utwórz code-behind
5. Utwórz ViewModel (jeśli potrzebny)
6. Zarejestruj w DI

---

## 📝 Status migracji

**Rozpoczęto:** (data startu)  
**Zakończono:** (data końca)  
**Plików zmigrowanych:** 0 / ~50  
**Konfliktów rozwiązanych:** 0 / 5  

---

## 🎯 Następne kroki

1. **Sprawdź konflikty** - porównaj istniejące pliki w Settings/
2. **Zacznij od USTAWIENIA** - to najważniejsza część
3. **Migruj plik po pliku** - nie rób wszystkiego naraz
4. **Testuj każdy widok** - upewnij się że działa po migracji
