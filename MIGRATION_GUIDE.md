# ASMED EDM - Automatyczna Migracja XAML/ViewModels

## 🤖 Rozwiązania automatyczne

### Opcja 1: Copilot Edits (ZALECANE)
Użyj GitHub Copilot w VS Code lub VS 2026 z trybem "Edits":

**Prompt do Copilota:**
```
Przejrzyj wszystkie pliki .xaml w projekcie ASMED.EDM.UI i:
1. Usuń niepotrzebne xmlns (zostaw tylko: xmlns, xmlns:x, xmlns:syncfusion, xmlns:vm gdy używane)
2. Usuń xmlns:local jeśli nie jest używane
3. Usuń xmlns:d i xmlns:mc jeśli nie używane
4. Upewnij się że Background="White" lub odpowiedni kolor

Dla plików .cs ViewModels:
1. Zmień namespace na file-scoped (namespace X;)
2. Usuń podwójne deklaracje namespace
3. Usuń niepotrzebne using'i
```

### Opcja 2: PowerShell Bulk Migration Script
```powershell
# Automatyczne czyszczenie XAML
$xamlFiles = Get-ChildItem -Path "src\ASMED.EDM.UI" -Filter "*.xaml" -Recurse

foreach ($file in $xamlFiles) {
	$content = Get-Content $file.FullName -Raw

	# Usuń niepotrzebne xmlns
	$content = $content -replace 'xmlns:local="[^"]*"\s*', ''
	$content = $content -replace 'xmlns:d="[^"]*"\s*', ''
	$content = $content -replace 'xmlns:mc="[^"]*"\s*', ''
	$content = $content -replace 'mc:Ignorable="d"\s*', ''
	$content = $content -replace 'd:DesignHeight="[^"]*"\s*', ''
	$content = $content -replace 'd:DesignWidth="[^"]*"\s*', ''

	# Normalizuj białe znaki
	$content = $content -replace '\s+xmlns:', ' xmlns:'

	Set-Content $file.FullName -Value $content -NoNewline
	Write-Host "✓ Cleaned: $($file.Name)" -ForegroundColor Green
}
```

### Opcja 3: Roslyn Analyzer (zaawansowane)
Stwórz custom Roslyn Analyzer który podświetli:
- Niepotrzebne xmlns w XAML
- Nieużywane using'i w C#
- Stare namespace syntax

## 💡 Zalecane podejście

**Hybrydowe - Copilot + manualne review:**

1. **Skup się na nowych plikach** - twórz je już czyste
2. **Stare pliki migruj stopniowo** - gdy je edytujesz
3. **Użyj Copilot Edits dla bulk** - gdy chcesz wyczyścić wszystko naraz

## 🎯 Prompt dla Copilot (gotowy do użycia)

```
Analiza i czyszczenie kodu ASMED.EDM.UI:

XAML Files (src/ASMED.EDM.UI/**/*.xaml):
- Usuń xmlns:local jeśli nie używane w markup'ie
- Usuń xmlns:d, xmlns:mc jeśli tylko dla design-time
- Usuń xmlns:helpers jeśli nie używane
- Zostaw tylko: xmlns, xmlns:x, xmlns:syncfusion (gdy używane), xmlns:vm (gdy używane)
- Upewnij się że każdy UserControl ma Background

ViewModel Files (src/ASMED.EDM.UI/ViewModels/**/*.cs):
- Zamień namespace blocks na file-scoped: namespace X;
- Usuń duplikaty namespace deklaracji
- Usuń nieużywane using'i
- Upewnij się że używa CommunityToolkit.Mvvm.ComponentModel

Code-behind Files (src/ASMED.EDM.UI/Views/**/*.xaml.cs):
- Zamień na file-scoped namespace
- Usuń zbędne using'i
- Upewnij się że dziedziczy z UserControl

Priorytet: Pliki w Views/Settings/ i ViewModels/ustawienia/
```

## 📋 Checklist migracji ręcznej

Gdy edytujesz plik, sprawdź:

**XAML:**
- [ ] Tylko potrzebne xmlns
- [ ] Background ustawiony
- [ ] Brak design-time attributes (d:, mc:)

**ViewModel:**
- [ ] File-scoped namespace: `namespace ASMED.EDM.UI.ViewModels;`
- [ ] Dziedziczy z ObservableObject
- [ ] Konstruktor z ILogger (jeśli DI)
- [ ] Używa [ObservableProperty] z MVVM Toolkit

**Code-behind:**
- [ ] File-scoped namespace
- [ ] Tylko using System.Windows.Controls;
- [ ] Tylko InitializeComponent() w konstruktorze

## 🔧 Czy warto?

**TAK**, jeśli:
- Zespół lubi clean code
- Masz dużo plików legacy
- Używasz Copilot do bulk edits

**NIE**, jeśli:
- Projekt działa i nie ma problemów
- Skupiasz się na nowych feature'ach
- Migration może złamać coś w legacy

## Rekomendacja

**Stopniowa migracja:**
1. Edytujesz plik? → wyczyść go
2. Nowy plik? → od razu czysty
3. Legacy? → zostaw do czasu edycji

Nie trać czasu na czyszczenie plików, których nie dotykasz.
