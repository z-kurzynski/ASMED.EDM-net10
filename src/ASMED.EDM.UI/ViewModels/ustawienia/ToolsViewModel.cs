using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;

namespace ASMED.EDM.UI.ViewModels.ustawienia;

/// <summary>
/// ViewModel dla narzędzi pomocniczych
/// </summary>
public partial class ToolsViewModel : ViewModelBase
{
    private readonly Services.UIImportService _importService;

    [ObservableProperty]
    private string _selectedXamlPath = string.Empty;

    [ObservableProperty]
    private string _targetCategory = "Patients";

    [ObservableProperty]
    private string _importLog = string.Empty;

    public ToolsViewModel()
    {
        _importService = new Services.UIImportService();

        // Dynamicznie ładuj katalogi z Views
        AvailableCategories = new ObservableCollection<string>();
        LoadViewsDirectories();
    }

    /// <summary>
    /// Dostępne kategorie widoków
    /// </summary>
    public ObservableCollection<string> AvailableCategories { get; }

    /// <summary>
    /// Czy wybrano plik XAML
    /// </summary>
    public bool IsXamlSelected => !string.IsNullOrEmpty(SelectedXamlPath);

    /// <summary>
    /// Wybierz plik XAML do importu
    /// </summary>
    [RelayCommand]
    private void SelectXamlFile()
    {
        var openFileDialog = new OpenFileDialog
        {
            Filter = "Pliki XAML (*.xaml)|*.xaml|Wszystkie pliki (*.*)|*.*",
            Title = "Wybierz plik XAML do importu",
            CheckFileExists = true,
            CheckPathExists = true
        };

        if (openFileDialog.ShowDialog() == true)
        {
            SelectedXamlPath = openFileDialog.FileName;
            AppendLog($"✅ Wybrano plik: {Path.GetFileName(SelectedXamlPath)}");
            OnPropertyChanged(nameof(IsXamlSelected));
            ImportUICommand.NotifyCanExecuteChanged();
        }
    }

    /// <summary>
    /// Importuj UI
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanImportUI))]
    private async Task ImportUI()
    {
        try
        {
            IsBusy = true;
            BusyMessage = "Importowanie UI...";
            AppendLog("\n🔄 Rozpoczynam import...");

            // Określ katalog docelowy
            var projectPath = GetProjectPath();
            var targetDirectory = Path.Combine(projectPath, "Views", TargetCategory);

            AppendLog($"📁 Katalog docelowy: Views/{TargetCategory}");

            // Wykonaj import
            var result = await Task.Run(() => _importService.ImportUIComponent(SelectedXamlPath, targetDirectory));

            if (result.Success)
            {
                AppendLog("\n✅ Import zakończony pomyślnie!");

                if (!string.IsNullOrEmpty(result.ImportedXamlPath))
                {
                    AppendLog($"  ✓ XAML: {GetRelativePath(result.ImportedXamlPath)}");
                }

                if (!string.IsNullOrEmpty(result.ImportedCodeBehindPath))
                {
                    AppendLog($"  ✓ Code-Behind: {GetRelativePath(result.ImportedCodeBehindPath)}");
                }

                if (!string.IsNullOrEmpty(result.ImportedViewModelPath))
                {
                    AppendLog($"  ✓ ViewModel: {GetRelativePath(result.ImportedViewModelPath)}");
                }

                if (result.Warnings.Count > 0)
                {
                    AppendLog("\n⚠️ Ostrzeżenia:");
                    foreach (var warning in result.Warnings)
                    {
                        AppendLog($"  ⚠ {warning}");
                    }
                }

                AppendLog("\n📝 Pamiętaj aby:");
                AppendLog("  1. Dodać zaimportowane pliki do projektu (kliknij prawym -> Dodaj istniejący element)");
                AppendLog("  2. Sprawdzić namespace - powinien być automatycznie poprawiony");
                AppendLog("  3. Wypełnić metody rzeczywistą logiką");
                AppendLog("  4. Skompilować projekt aby sprawdzić błędy");

                MessageBox.Show(
                    "Import zakończony pomyślnie!\n\n" +
                    "Sprawdź logi aby zobaczyć szczegóły.\n" +
                    "Pamiętaj aby dodać pliki do projektu i wypełnić metody logiką.",
                    "Import UI",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                AppendLog($"\n❌ Błąd: {result.ErrorMessage}");

                MessageBox.Show(
                    $"Błąd podczas importu:\n\n{result.ErrorMessage}",
                    "Błąd importu",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            AppendLog($"\n❌ Wyjątek: {ex.Message}");

            MessageBox.Show(
                $"Nieoczekiwany błąd:\n\n{ex.Message}",
                "Błąd",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    private bool CanImportUI() => IsXamlSelected && !string.IsNullOrEmpty(TargetCategory);

    partial void OnSelectedXamlPathChanged(string value)
    {
        OnPropertyChanged(nameof(IsXamlSelected));
        ImportUICommand.NotifyCanExecuteChanged();
    }

    partial void OnTargetCategoryChanged(string value)
    {
        ImportUICommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Wyczyść logi
    /// </summary>
    [RelayCommand]
    private void ClearLog()
    {
        ImportLog = string.Empty;
        AppendLog("📋 Logi wyczyszczone");
    }

    private void AppendLog(string message)
    {
        ImportLog += message + "\n";
    }

    private void LoadViewsDirectories()
    {
        try
        {
            var projectPath = GetProjectPath();
            var viewsPath = Path.Combine(projectPath, "Views");

            if (Directory.Exists(viewsPath))
            {
                var directories = Directory.GetDirectories(viewsPath)
                    .Select(d => new DirectoryInfo(d).Name)
                    .OrderBy(name => name)
                    .ToList();

                AvailableCategories.Clear();
                foreach (var dir in directories)
                {
                    AvailableCategories.Add(dir);
                }

                // Ustaw domyślną kategorię jeśli istnieje
                if (AvailableCategories.Count > 0)
                {
                    TargetCategory = AvailableCategories.FirstOrDefault(c => c == "Patients") 
                                   ?? AvailableCategories.FirstOrDefault(c => c == "pacjent")
                                   ?? AvailableCategories[0];
                }
            }
            else
            {
                // Fallback - dodaj podstawowe katalogi
                AvailableCategories.Add("Patients");
                AvailableCategories.Add("Visits");
                AvailableCategories.Add("Settings");
                TargetCategory = "Patients";
            }
        }
        catch
        {
            // W razie błędu - dodaj podstawowe katalogi
            AvailableCategories.Add("Patients");
            AvailableCategories.Add("Visits");
            AvailableCategories.Add("Settings");
            TargetCategory = "Patients";
        }
    }

    private string GetProjectPath()
    {
        // Pobierz ścieżkę do projektu UI
        var currentDirectory = AppDomain.CurrentDomain.BaseDirectory;

        // Idź w górę do znajdowania katalogu src/ASMED.EDM.UI
        var directory = new DirectoryInfo(currentDirectory);

        while (directory != null)
        {
            var uiProject = Path.Combine(directory.FullName, "src", "ASMED.EDM.UI");
            if (Directory.Exists(uiProject))
            {
                return uiProject;
            }

            // Sprawdź czy jesteśmy w katalogu projektu
            var csprojFiles = directory.GetFiles("ASMED.EDM.UI.csproj");
            if (csprojFiles.Length > 0)
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        // Fallback - zwróć bieżący katalog
        return currentDirectory;
    }

    private string GetRelativePath(string fullPath)
    {
        var projectPath = GetProjectPath();

        if (fullPath.StartsWith(projectPath, StringComparison.OrdinalIgnoreCase))
        {
            return fullPath.Substring(projectPath.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        return Path.GetFileName(fullPath);
    }
}
