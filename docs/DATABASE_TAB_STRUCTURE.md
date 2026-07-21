# Struktura zakładki "Baza Danych"

**Utworzono:** 2025-01-XX  
**Status:** ✅ Gotowe do implementacji połączenia z bazą danych

---

## 📋 Przegląd

Zakładka **"🗄️ Baza Danych"** w `MainWindow.xaml` została podzielona na **4 zagnieżdżone podsekcje** (TabControl z `TabStripPlacement="Left"`):

```
🗄️ Baza Danych
	├── ⚙️ Ustawienia
	├── 🧾 Faktura
	├── 🏢 Firma
	└── 📊 Raporty
```

---

## 1️⃣ **⚙️ Ustawienia**

**Widok:** `Views\Settings\SettingsView.xaml`  
**ViewModel:** (do zaimplementowania)

**Zawiera własne sub-zakładki** (Syncfusion TabControlExt):
- **Konfiguracja** - `ConfigurationView.xaml`
- **Cenniki** - `PriceListsView.xaml`
- **Dane Placówki** - `FacilityDataView.xaml`
- **Użytkownicy** - `UsersView.xaml`
- **Narzędzia** - `ToolsView.xaml`

### Cel
Centralne miejsce do:
- Konfiguracji połączenia z bazą MySQL
- Zarządzania użytkownikami systemu
- Ustawień globalnych aplikacji
- Administracji danymi placówki medycznej

---

## 2️⃣ **🧾 Faktura**

**Widok:** (Placeholder - do stworzenia)  
**ViewModel:** (do zaimplementowania)  
**Legacy source:** `legacy-xaml-backup\Views\faktura\`

### Cel
Zarządzanie fakturami:
- Szablony faktur
- Numery i serie dokumentów
- Konfiguracja VAT, stawek
- Drukowanie faktur

---

## 3️⃣ **🏢 Firma**

**Widok:** (Placeholder - do stworzenia)  
**ViewModel:** (do zaimplementowania)  
**Legacy source:** `legacy-xaml-backup\Views\firma\`

### Cel
Zarządzanie danymi firm/kontrahentów:
- Lista firm korzystających z usług
- Umowy i cenniki firmowe
- Dane kontaktowe
- Historia współpracy

---

## 4️⃣ **📊 Raporty**

**Widok:** (Placeholder - do stworzenia)  
**ViewModel:** (do zaimplementowania)  
**Legacy source:** `legacy-xaml-backup\Views\raporty\`

### Cel
Generowanie raportów i statystyk:
- Raporty wizyt
- Statystyki badań
- Rozliczenia finansowe
- Zestawienia pracowników
- Eksport do Excel/PDF

---

## 🔧 Implementacja w MainWindow.xaml

### Namespace
```xml
xmlns:viewssettings="clr-namespace:ASMED.EDM.UI.Views.Settings"
```

### Struktura TabControl
```xml
<TabItem Header="🗄️ Baza Danych" FontSize="14" Padding="15,10" Background="#FFFF6F61">
	<Grid Background="White">
		<TabControl Margin="10" TabStripPlacement="Left">

			<!-- Podzakładka 1: Ustawienia -->
			<TabItem Header="⚙️ Ustawienia" FontSize="13" Padding="12,8">
				<viewssettings:SettingsView />
			</TabItem>

			<!-- Podzakładka 2: Faktura (placeholder) -->
			<TabItem Header="🧾 Faktura" FontSize="13" Padding="12,8">
				<!-- Do zaimplementowania -->
			</TabItem>

			<!-- Podzakładka 3: Firma (placeholder) -->
			<TabItem Header="🏢 Firma" FontSize="13" Padding="12,8">
				<!-- Do zaimplementowania -->
			</TabItem>

			<!-- Podzakładka 4: Raporty (placeholder) -->
			<TabItem Header="📊 Raporty" FontSize="13" Padding="12,8">
				<!-- Do zaimplementowania -->
			</TabItem>

		</TabControl>
	</Grid>
</TabItem>
```

---

## 📁 Struktura plików

```
src/ASMED.EDM.UI/
├── Views/
│   ├── Settings/
│   │   ├── SettingsView.xaml          ✅ Zaimplementowane
│   │   ├── ConfigurationView.xaml     ✅ Zaimplementowane
│   │   ├── PriceListsView.xaml        ✅ Zaimplementowane
│   │   ├── FacilityDataView.xaml      ✅ Zaimplementowane
│   │   ├── UsersView.xaml             ✅ Zaimplementowane
│   │   └── ToolsView.xaml             ✅ Zaimplementowane
│   ├── Faktura/                       🚧 Do zaimplementowania
│   ├── Firma/                         🚧 Do zaimplementowania
│   └── Reports/                       🚧 Do zaimplementowania
├── ViewModels/
│   ├── Settings/                      🚧 Do zaimplementowania
│   ├── Faktura/                       🚧 Do zaimplementowania
│   ├── Firma/                         🚧 Do zaimplementowania
│   └── Reports/                       🚧 Do zaimplementowania
```

---

## ✅ Status

| Podsekcja | Widok | ViewModel | DB Connection | Status |
|-----------|-------|-----------|--------------|---------|
| ⚙️ Ustawienia | ✅ | 🚧 | 🚧 | **Częściowo gotowe** |
| 🧾 Faktura | 🚧 | 🚧 | 🚧 | **Placeholder** |
| 🏢 Firma | 🚧 | 🚧 | 🚧 | **Placeholder** |
| 📊 Raporty | 🚧 | 🚧 | 🚧 | **Placeholder** |

---

## 🎯 Kolejne kroki

### Priorytet 1: Połączenie z bazą danych
1. **Zaimplementować ViewModel dla ConfigurationView**
   - Właściwości dla connection string (Server, Port, Database, User, Password)
   - Command: `TestConnectionCommand`
   - Command: `SaveSettingsCommand`
   - Logika zapisu do `appsettings.json`

2. **Utworzyć DatabaseService**
   - Testowanie połączenia MySQL
   - Walidacja credentials
   - Zwracanie statusu połączenia

3. **Podpiąć ConfigurationView do DI**
   - Zarejestrować ViewModel w `App.xaml.cs`
   - Wstrzyknąć DatabaseService
   - Wstrzyknąć IConfiguration dla odczytu/zapisu settings

### Priorytet 2: Migracja legacy views
1. Faktura - skopiować z `legacy-xaml-backup\Views\faktura\`
2. Firma - skopiować z `legacy-xaml-backup\Views\firma\`
3. Raporty - skopiować z `legacy-xaml-backup\Views\raporty\`

---

## 📝 Notatki

- **TabStripPlacement="Left"** - zakładki po lewej stronie dla lepszej czytelności
- **Syncfusion TabControlExt** używany w SettingsView dla spójności z legacy
- **Placeholdery** zawierają kolorowe bordery z opisem funkcjonalności
- **Istniejące sub-views** w Settings już są zaimplementowane (ConfigurationView, PriceListsView, etc.)

---

**Następny krok:** Implementacja połączenia z bazą MySQL w ConfigurationView! 🚀
