# ETAP 3 - PHASE 4: Reorganizacja Struktury + Kolejne Moduły ✅
**Status**: In Progress  
**Data**: 2025-01-22  
**Cel**: Zachowanie struktury katalogów z legacy aplikacji + migracja kolejnych widoków

---

## ✅ PHASE 4.1: Reorganizacja Struktury Folderów (Complete)

### Zmiany w Strukturze Views

**BEFORE**:
```
Views/
└── PatientsView.xaml
```

**AFTER**:
```
Views/
├── Patients/
│   ├── PatientsView.xaml
│   └── PatientsView.xaml.cs
├── Visits/         (gotowy folder)
├── Settings/       (gotowy folder)
└── Reports/        (gotowy folder)
```

### Zaktualizowane Pliki

1. **PatientsView.xaml.cs**
   - Namespace: `ASMED.EDM.UI.Views` → `ASMED.EDM.UI.Views.Patients`

2. **PatientsView.xaml**
   - x:Class: `ASMED.EDM.UI.Views.PatientsView` → `ASMED.EDM.UI.Views.Patients.PatientsView`

3. **MainWindow.xaml.cs**
   - Using: `using ASMED.EDM.UI.Views;` → `using ASMED.EDM.UI.Views.Patients;`

4. **MainWindow.xaml**
   - xmlns: `xmlns:views="clr-namespace:ASMED.EDM.UI.Views"` → `xmlns:viewspatients="clr-namespace:ASMED.EDM.UI.Views.Patients"`
   - DataTemplate: `<views:PatientsView />` → `<viewspatients:PatientsView />`

5. **App.xaml.cs**
   - Registration: `services.AddTransient<Views.PatientsView>();` → `services.AddTransient<Views.Patients.PatientsView>();`

### Build Status
✅ **Success** - 0 błędów, 7 ostrzeżeń (Pomelo)

---

## 📋 Struktura Legacy Aplikacji (A:\source\repos\ASMED-WPF-Application\src\ASMED_5\Views\)

| Legacy Folder | Nowy Folder | Status | Priorytet |
|--------------|-------------|--------|-----------|
| `pacjent/` | `Patients/` | ✅ Done | - |
| `wizytyview/` | `Visits/` | 🔄 Next | **HIGH** (1648 linii, SfScheduler!) |
| `ustawienia/` | `Settings/` | ⏳ Planned | **MED** (prosty TabControl) |
| `raporty/` | `Reports/` | ⏳ Planned | **MED** (6 sub-views) |
| `badania/` | `MedicalTests/` | ⏳ Planned | LOW |
| `Skierowania/` | `Referrals/` | ⏳ Planned | LOW |
| `faktura/` | `Invoices/` | ⏳ Planned | LOW |
| `cenniki/` | `PriceLists/` | ⏳ Planned | LOW |
| `lista_do_faktur/` | `InvoiceLists/` | ⏳ Planned | LOW |
| `firma/` | `Company/` | ⏳ Planned | LOW |
| `Import-Export/` | `ImportExport/` | ⏳ Planned | LOW |
| `Dialogs/` | `Dialogs/` | ⏳ Planned | LOW |

---

## 🎯 PHASE 4.2: Następny Moduł - Settings (Ustawienia)

### Dlaczego Settings jako drugi?
- ✅ **Prosty** - to tylko shell z nested TabControl
- ✅ **Niezależny** - nie wymaga skomplikowanej logiki biznesowej
- ✅ **Wzorzec** - pokazuje jak integrować sub-views
- ⏳ Visits jest za duży (1648 linii + SfScheduler) - zrobimy go później

### Legacy UstawieniaView.xaml (39 linii)
```xaml
<UserControl x:Class="ASMED.WPF.Views.UstawieniaView" ...>
	<Grid>
		<TabControl>
			<TabItem Header="Konfiguracja">
				<local:KonfiguracjaView />
			</TabItem>
			<TabItem Header="Cenniki">
				<local:CennikiView />
			</TabItem>
			<TabItem Header="Dane Placowki">
				<local:DanePlacowkiView />
			</TabItem>
			<TabItem Header="Uzytkownicy">
				<local:UzytkownicyView />
			</TabItem>
			<TabItem Header="Narzędzia">
				<local:NarzedziaView />
			</TabItem>
		</TabControl>
	</Grid>
</UserControl>
```

### Plan Implementacji SettingsView

**Struktura**:
```
Views/Settings/
├── SettingsView.xaml          (główny shell)
├── SettingsView.xaml.cs
├── ConfigurationView.xaml     (sub-view 1)
├── ConfigurationView.xaml.cs
├── PriceListsSubView.xaml     (sub-view 2)
├── FacilityDataView.xaml      (sub-view 3)
├── UsersView.xaml             (sub-view 4)
└── ToolsView.xaml             (sub-view 5)

ViewModels/
└── SettingsViewModel.cs       (może być prosty lub pusty)
```

**Kroki**:
1. ✅ Sprawdzić legacy UstawieniaView + sub-views
2. Stworzyć SettingsView.xaml (shell z TabControl)
3. Stworzyć placeholder sub-views (puste UserControls)
4. Zarejestrować w App.xaml.cs (DI)
5. Podpiąć do MainWindow tab
6. Build + test

---

## 🚀 Next Actions

### Immediate (Phase 4.2 - Start)
- [ ] Sprawdzić sub-views w legacy UstawieniaView
- [ ] Stworzyć SettingsView.xaml + SettingsView.xaml.cs
- [ ] Stworzyć placeholder sub-views
- [ ] Zarejestrować SettingsView w DI
- [ ] Dodać SettingsView do MainWindow tabs
- [ ] Build + test

### Future (Phase 4.3+)
- [ ] Implementować Visits (duży moduł z SfScheduler)
- [ ] Implementować Reports
- [ ] Wypełnić placeholder sub-views w Settings rzeczywistą logiką

---

**Ready to continue**: Phase 4.2 - Settings Module
