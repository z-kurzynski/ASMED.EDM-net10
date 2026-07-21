# ETAP 3 - Plan Migracji UI (Zachowanie Oryginalnej Struktury)

## ✅ WYKONANE (2025-01-22)

### Krok 1: Pakiety Syncfusion ✅
```powershell
✅ Syncfusion.SfGrid.WPF 27.1.58
✅ Syncfusion.SfSkinManager.WPF 27.1.58
✅ Syncfusion.Themes.Windows11Light.WPF 27.1.58
✅ Syncfusion.Tools.WPF 27.1.58
```

### Krok 2: Nowy MainWindow.xaml ✅
- ✅ Przebudowa na `TabControlExt` z zakładkami
- ✅ Header z logo "❤️ ASMED EDM"
- ✅ Footer z zegarem, copyright, info DB, przycisk Zakończ
- ✅ DataTemplates dla PatientsViewModel
- ✅ Kolory zakładek jak w oryginale
- ✅ Zakładki: Pacjenci, Wizyty, Karty Badań, Baza Danych/Raporty (z zagnieżdżonym TabControl)

### Krok 3: MainWindow.xaml.cs ✅
- ✅ Timer zegara z DispatcherTimer
- ✅ CloseApp_Click
- ✅ TopMost_Checked/Unchecked
- ✅ OnClosed cleanup

---

## 🚧 DO ZROBIENIA

### Krok 4: MainViewModel
- [ ] Utworzyć MainViewModel  (z CommunityToolkit.Mvvm)
- [ ] Właściwość `PacjentWidok` (bindowana do TabPacjenci)
- [ ] Właściwość `DatabaseInfo` (dla stopki)
- [ ] Timer działa w code-behind (można zostawić lub przenieść do VM)

### Krok 5: Konwersja PatientsView
- [ ] Z `Window` → `UserControl`
- [ ] Z `DataGrid` → `SfDataGrid`
- [ ] Z `Button` → `ButtonAdv`
- [ ] Layout nagłówka: wyszukiwarka + filtr + przycisk dodaj
- [ ] Style nagłówków GridHeaderCellControl (#FF1976D2, bold, white)

### Krok 6: Aktualizacja PatientsViewModel
- [ ] Dodać `FilterTypes` ObservableCollection
- [ ] Dodać `ActiveFilterType` z binding
- [ ] Dodać `PacjenciFiltered` computed collection
- [ ] Dodać `ClearSearchTextCommand`

## 🎯 Cel
Zachować oryginalną strukturę UI z ASMED_5 podczas migracji do ASMED_EDM

## 📊 Architektura Oryginalnego UI (ASMED_5)

### MainWindow.xaml
- **Nawigacja**: `Syncfusion.TabControlExt` (nie sidebar!)
- **Layout**: Header (37px) + TabControl + Footer (30px)
- **Motywy**: Windows11Light + kolory dla każdej zakładki
- **Zakładki główne**:
  1. 📝 **Rejestracja** (Wizyty) - `#FD3A345E`
  2. 📄 **Nowa Karta** (Badania) - `#FF0078D7`
  3. 📝 **Karty Badań** (Skierowania) - `#FF4EE7FB`
  4. 📝 **Zakończ Badanie** - `#FF4CAF50`
  5. 📝 **Edycja Badań** - `#FF90EE90`
  6. 📝 **Lista do Faktur** - `#F6607D8B`
  7. 🗄️ **Baza Danych/Raporty** - `#FFFFB347` (zagnieżdżone TabControl):
	 - 📝 Faktura - `#FFFF6F61`
	 - 📝 Pacjent - `#FFD367FF`
	 - 📝 Firma - `#FF90C6FD`
	 - 📝 Raporty - `#FFFFA07A`
	 - 📝 Ustawienia - `#FF06F537`

### Komponenty Syncfusion
- `SfDataGrid` zamiast standardowego DataGrid
- `ButtonAdv` zamiast zwykłych Button
- `TabControlExt` / `TabItemExt` zamiast TabControl/TabItem
- Style dla nagłówków (#FF1976D2, bold, white text)

### Footer
- Zegar w czasie rzeczywistym
- Copyright "© 2025 ASMED. All rights reserved."
- Checkbox "📌 Zawsze na wierzchu"
- Info o bazie danych
- Przycisk "❌ Zakończ"

### Widoki jako UserControl
- `ListaPacjentowView` jest UserControl, nie Window
- Wyszukiwarka + filtr typu w headerze
- Przycisk "➕ Dodaj Pacjenta"
- `SfDataGrid` z filtrowaniem, sortowaniem, edycją inline

---

## 🔧 Plan Adaptacji ASMED_EDM.UI

### Krok 1: Pakiety Syncfusion
```powershell
cd D:\Visual\Asmed_EDM\src\ASMED.EDM.UI
dotnet add package Syncfusion.SfGrid.WPF --version 27.1.58
dotnet add package Syncfusion.SfSkinManager.WPF --version 27.1.58
dotnet add package Syncfusion.Themes.Windows11Light.WPF --version 27.1.58
dotnet add package Syncfusion.Tools.WPF --version 27.1.58
```

### Krok 2: Nowy MainWindow.xaml
- Przebudowa na `TabControlExt` z zakładkami
- Header z logo "❤️ ASMED"
- Footer z zegarem, copyright, info DB, przycisk Zakończ
- DataTemplates dla ViewModels zamiast ContentControl
- Kolory zakładek jak w oryginale

### Krok 3: Konwersja PatientsView
- Z `Window` na `UserControl`
- Z `DataGrid` na `SfDataGrid`
- Z `Button` na `ButtonAdv`
- Layout nagłówka: wyszukiwarka + filtr + przycisk dodaj
- Usunięcie busy overlay (TabControl będzie to obsługiwać)

### Krok 4: Aktualizacja ViewModels
- `MainViewModel`: właściwości dla zakładek, timer dla zegara, DatabaseInfo
- `PatientsViewModel`: FilterTypes, ActiveFilterType, PacjenciFiltered
- Polecenia: ClearSearchTextCommand, DodajPacjenta_Click

### Krok 5: App.xaml.cs
- Startup: otwórz MainWindow, nie PatientsView
- Rejestracja wszystkich widoków jako Transient
- MainWindow i MainViewModel jako Singleton

### Krok 6: Style i Resources
- Przeniesienie stylów nagłówków GridHeaderCellControl
- FilterToggleButton styles
- ButtonAdv styles
- Converters (jeśli potrzebne)

---

## ✅ Checklist Migracji

- [ ] Dodać pakiety Syncfusion
- [ ] Zaktualizować MainWindow.xaml (TabControlExt)
- [ ] Dodać MainWindowViewModel (Timer, DatabaseInfo)
- [ ] Przekonwertować PatientsView na UserControl
- [ ] Zastąpić DataGrid → SfDataGrid
- [ ] Zastąpić Button → ButtonAdv
- [ ] Zaktualizować PatientsViewModel (FilterTypes, Filtered collection)
- [ ] Dodać Footer z zegarem i info DB
- [ ] Zaktualizować App.xaml.cs (uruchamianie MainWindow)
- [ ] Usunąć stare converters (jeśli nieużywane)
- [ ] Build i test

---

## 🚀 Następne Widoki Do Migracji (ETAP 3 ciąg dalszy)

Po zakończeniu migracji struktury MainWindow i PatientsView:

1. **WizytyView** (Rejestracja)
2. **BadaniaNewView** (Nowa Karta Badań)
3. **SkierowaniaView** (Karty Badań)
4. **BadaniaEditView** (Edycja)
5. **ListaFaktAddView** (Lista do Faktur)
6. **FakturaView**, **FirmaView**, **RaportyView**, **UstawieniaView** (Baza Danych)
7. **LoginWindow** (przed MainWindow)

---

## 📝 Uwagi
- **Syncfusion Trial/License**: Upewnienie się że licencja jest zarejestrowana w `App.xaml.cs`
- **Nawigacja**: W oryginale nie ma INavigationService - przełączanie zakładek to właściwość IsSelected
- **DialogService**: W oryginale często code-behind z MessageBox - zachować lub użyć naszego DialogService?
- **DataContext wiring**: Oryginał używa DataTemplates + binding CurrentViewModel
