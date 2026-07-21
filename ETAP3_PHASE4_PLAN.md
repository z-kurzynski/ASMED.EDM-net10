# ETAP 3 - PHASE 4: Migracja Pozostałych Modułów UI
**Data**: 2025-01-22  
**Cel**: Zachowanie struktury katalogów z legacy aplikacji

---

## 📁 Struktura Katalogów Views (Legacy App)

```
Views/
├── badania/                    # Badania medyczne
├── cenniki/                    # Cenniki usług
├── Dialogs/                    # Dialogi aplikacji
├── faktura/                    # Faktury
├── firma/                      # Dane firmy
├── Import-Export/              # Import/Export danych
├── lista_do_faktur/            # Listy faktur
├── pacjent/                    # ✅ DONE: PatientsView
├── raporty/                    # Raporty
├── Skierowania/                # Skierowania medyczne
├── skierowanialistapacjentow/  # Lista pacjentów ze skierowaniami
├── skierowaniepacjentdodajview/# Dodawanie skierowań
├── ustawienia/                 # Ustawienia aplikacji
├── wizytyview/                 # Wizyty
└── wz_template/                # Szablony WZ
```

---

## 🎯 Plan Migracji - Priorytet Modułów

### **Priorytet 1: Core Business Logic** (Start Phase 4)
1. **wizytyview/** → `Views/Visits/`
   - VisitsView.xaml (lista wizyt)
   - VisitDetailsView.xaml (szczegóły wizyty)
   - AddEditVisitView.xaml (dodaj/edytuj wizytę)
   - ViewModel: VisitsViewModel

2. **pacjent/** → `Views/Patients/` (rozszerzenie)
   - ✅ PatientsView.xaml już zrobiony
   - PatientDetailsView.xaml (szczegóły pacjenta)
   - AddEditPatientView.xaml (dodaj/edytuj pacjenta)

### **Priorytet 2: Medical Features**
3. **badania/** → `Views/MedicalTests/`
   - MedicalTestsView.xaml
   - MedicalTestsViewModel

4. **Skierowania/** → `Views/Referrals/`
   - ReferralsView.xaml
   - ReferralsViewModel

### **Priorytet 3: Financial Features**
5. **faktura/** → `Views/Invoices/`
   - InvoicesView.xaml
   - InvoiceDetailsView.xaml

6. **cenniki/** → `Views/PriceLists/`
   - PriceListsView.xaml

7. **lista_do_faktur/** → `Views/InvoiceLists/`
   - InvoiceListsView.xaml

### **Priorytet 4: Administrative**
8. **ustawienia/** → `Views/Settings/`
   - SettingsView.xaml
   - DatabaseSettingsView.xaml
   - UserSettingsView.xaml

9. **raporty/** → `Views/Reports/`
   - ReportsView.xaml

10. **firma/** → `Views/Company/`
	- CompanyView.xaml

### **Priorytet 5: Utilities**
11. **Import-Export/** → `Views/ImportExport/`
	- ImportExportView.xaml

12. **Dialogs/** → `Views/Dialogs/`
	- Wspólne dialogi (confirmation, input, etc.)

---

## 🚀 PHASE 4 - Iteration 1: Visits Module

### Krok 1: Sprawdzenie legacy Views/wizytyview
- Przeczytać strukturę XAML
- Zidentyfikować użyte kontrolki Syncfusion
- Sprawdzić binding w ViewModel

### Krok 2: Struktura katalogów w nowym projekcie
```
D:\Visual\Asmed_EDM\src\ASMED.EDM.UI\
├── Views\
│   ├── Patients\
│   │   └── PatientsView.xaml ✅
│   └── Visits\              ← NOWY
│       ├── VisitsView.xaml
│       └── (inne views)
└── ViewModels\
	├── PatientsViewModel.cs ✅
	└── VisitsViewModel.cs   ← NOWY
```

### Krok 3: Implementacja VisitsView
1. Skopiować wzorzec z PatientsView (UserControl + Syncfusion)
2. Dostosować do entity Visit
3. Zaimplementować VisitsViewModel z filtrami
4. Zarejestrować w App.xaml.cs (DI)
5. Podpiąć do MainWindow tab

### Krok 4: Testing & Validation
- Build test
- Runtime test
- Sprawdzenie nawigacji między tabami

---

## 📝 Konwencje Nazewnictwa

### Foldery:
- **Legacy**: małe litery, underscore (`wizytyview`, `lista_do_faktur`)
- **Nowy**: PascalCase, angielski (`Visits`, `InvoiceLists`)

### Pliki:
- **Views**: `{Module}View.xaml` (np. `VisitsView.xaml`)
- **ViewModels**: `{Module}ViewModel.cs` (np. `VisitsViewModel.cs`)

### Namespace:
```csharp
namespace ASMED.EDM.UI.Views.Visits;
namespace ASMED.EDM.UI.ViewModels;
```

---

## ✅ Gotowe do startu Phase 4 - Iteration 1

**Next Action**: Sprawdzić `A:\source\repos\ASMED-WPF-Application\src\ASMED_5\Views\wizytyview\` i rozpocząć migrację Visits module.
