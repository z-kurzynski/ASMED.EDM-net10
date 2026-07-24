using System.IO;
using System.Text.RegularExpressions;

namespace ASMED.EDM.UI.Services;

public class UIImportService
{
    private readonly CSharpMethodStubGenerator _stubGenerator;

    public UIImportService()
    {
        _stubGenerator = new CSharpMethodStubGenerator();
    }

    public UIImportResult ImportUIComponent(string xamlFilePath, string targetDirectory)
    {
        var result = new UIImportResult();

        try
        {
            // 1. Walidacja pliku XAML
            if (!File.Exists(xamlFilePath))
            {
                result.Success = false;
                result.ErrorMessage = $"Plik XAML nie istnieje: {xamlFilePath}";
                return result;
            }

            if (!xamlFilePath.EndsWith(".xaml", StringComparison.OrdinalIgnoreCase))
            {
                result.Success = false;
                result.ErrorMessage = "Wybrany plik nie jest plikiem XAML";
                return result;
            }

            // 2. Przygotowanie ścieżek
            var xamlFileName = Path.GetFileName(xamlFilePath);
            var baseFileName = Path.GetFileNameWithoutExtension(xamlFilePath);
            var sourceDirectory = Path.GetDirectoryName(xamlFilePath);

            if (string.IsNullOrEmpty(sourceDirectory))
            {
                result.Success = false;
                result.ErrorMessage = "Nie można określić katalogu źródłowego";
                return result;
            }

            // 3. Określ ścieżki plików
            var codeBehindFileName = baseFileName + ".xaml.cs";
            var codeBehindPath = Path.Combine(sourceDirectory, codeBehindFileName);

            result.Warnings.Add($"DEBUG: Szukam code-behind: {codeBehindPath}");

            // 4. Importuj XAML
            var targetXamlPath = Path.Combine(targetDirectory, xamlFileName);
            result.ImportedXamlPath = ImportXamlFile(xamlFilePath, targetXamlPath);

            // 5. Importuj Code-Behind jeśli istnieje
            if (File.Exists(codeBehindPath))
            {
                var targetCodeBehindPath = Path.Combine(targetDirectory, codeBehindFileName);
                result.ImportedCodeBehindPath = ImportCodeBehindFile(codeBehindPath, targetCodeBehindPath);
            }
            else
            {
                result.Warnings.Add($"Plik code-behind nie został znaleziony: {codeBehindPath}");
            }

            // 6. Znajdź i importuj ViewModel
            var viewModelFileName = FindViewModelFileName(baseFileName);
            result.Warnings.Add($"DEBUG: Nazwa ViewModel: {viewModelFileName}");

            if (!string.IsNullOrEmpty(viewModelFileName))
            {
                var sourceSubfolder = GetSourceSubfolder(sourceDirectory);
                result.Warnings.Add($"DEBUG: Podkatalog źródłowy: '{sourceSubfolder}'");

                var sourceProjectRoot = GetProjectRoot(sourceDirectory);
                result.Warnings.Add($"DEBUG: Root projektu źródłowego: {sourceProjectRoot}");

                var viewModelPath = FindViewModelFile(sourceDirectory, viewModelFileName);
                result.Warnings.Add($"DEBUG: Znaleziona ścieżka ViewModel: '{viewModelPath}'");

                if (!string.IsNullOrEmpty(viewModelPath) && File.Exists(viewModelPath))
                {
                    // Określ ścieżkę docelową dla ViewModel (ViewModels zamiast Views)
                    var projectRoot = GetProjectRoot(targetDirectory);
                    var viewModelsDir = Path.Combine(projectRoot, "ViewModels");

                    // Stwórz podkatalog w ViewModels jeśli Views ma podkatalog
                    var viewsSubfolder = GetSubfolderName(targetDirectory);
                    if (!string.IsNullOrEmpty(viewsSubfolder))
                    {
                        viewModelsDir = Path.Combine(viewModelsDir, viewsSubfolder);
                    }

                    Directory.CreateDirectory(viewModelsDir);
                    var targetViewModelPath = Path.Combine(viewModelsDir, viewModelFileName);
                    result.ImportedViewModelPath = ImportViewModelFile(viewModelPath, targetViewModelPath);
                }
                else
                {
                    result.Warnings.Add($"ViewModel nie został znaleziony: {viewModelFileName}");
                }
            }

            result.Success = true;
            result.Message = "Import zakończony pomyślnie";
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = $"Błąd podczas importu: {ex.Message}";
        }

        return result;
    }

    private string ImportXamlFile(string sourcePath, string targetPath)
    {
        // Skopiuj plik XAML bez zmian
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.Copy(sourcePath, targetPath, overwrite: true);
        return targetPath;
    }

    private string ImportCodeBehindFile(string sourcePath, string targetPath)
    {
        // Wczytaj kod źródłowy
        var sourceCode = File.ReadAllText(sourcePath);

        // Wygeneruj kod z samymi sygnaturami metod
        var stubCode = _stubGenerator.GenerateStubsFromSource(sourceCode);

        // Zmień namespace na prawidłowy
        stubCode = UpdateNamespace(stubCode, targetPath, isViewModel: false);

        // Zapisz do pliku docelowego
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.WriteAllText(targetPath, stubCode);

        return targetPath;
    }

    private string ImportViewModelFile(string sourcePath, string targetPath)
    {
        // Wczytaj kod źródłowy
        var sourceCode = File.ReadAllText(sourcePath);

        // Wygeneruj kod z samymi sygnaturami metod
        var stubCode = _stubGenerator.GenerateStubsFromSource(sourceCode);

        // Zmień namespace na prawidłowy
        stubCode = UpdateNamespace(stubCode, targetPath, isViewModel: true);

        // Zapisz do pliku docelowego
        Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
        File.WriteAllText(targetPath, stubCode);

        return targetPath;
    }

    private string FindViewModelFileName(string viewName)
    {
        // Usuń "View" z końca i dodaj "ViewModel"
        // np. PatientsView -> PatientsViewModel
        if (viewName.EndsWith("View"))
        {
            var baseName = viewName.Substring(0, viewName.Length - 4);
            return baseName + "ViewModel.cs";
        }

        // Jeśli nie kończy się na "View", po prostu dodaj "ViewModel"
        return viewName + "ViewModel.cs";
    }

    private string FindViewModelFile(string sourceDirectory, string viewModelFileName)
    {
        // PRIORYTET 1: Szukaj w tym samym podkatalogu struktury (Views/X -> ViewModels/X)
        var sourceSubfolder = GetSourceSubfolder(sourceDirectory);
        if (!string.IsNullOrEmpty(sourceSubfolder))
        {
            // Znajdź root projektu źródłowego
            var sourceProjectRoot = GetProjectRoot(sourceDirectory);
            var viewModelsInSameSubfolder = Path.Combine(sourceProjectRoot, "ViewModels", sourceSubfolder, viewModelFileName);

            if (File.Exists(viewModelsInSameSubfolder))
                return viewModelsInSameSubfolder;
        }

        // PRIORYTET 2: Szukaj w różnych lokalizacjach
        var searchPaths = new List<string>
        {
            // W tym samym katalogu co View
            Path.Combine(sourceDirectory, viewModelFileName),

            // W katalogu ViewModels na tym samym poziomie
            Path.Combine(Path.GetDirectoryName(sourceDirectory)!, "ViewModels", viewModelFileName),

            // W katalogu ViewModels w głównym katalogu projektu
            Path.Combine(GetProjectRoot(sourceDirectory), "ViewModels", viewModelFileName)
        };

        // PRIORYTET 3: Szukaj również w podkatalogach ViewModels (rekurencyjnie)
        var projectRoot = GetProjectRoot(sourceDirectory);
        var viewModelsRoot = Path.Combine(projectRoot, "ViewModels");
        if (Directory.Exists(viewModelsRoot))
        {
            var allViewModelFiles = Directory.GetFiles(viewModelsRoot, viewModelFileName, SearchOption.AllDirectories);
            searchPaths.AddRange(allViewModelFiles);
        }

        foreach (var path in searchPaths)
        {
            if (File.Exists(path))
                return path;
        }

        return string.Empty;
    }

    private string GetSourceSubfolder(string sourceDirectory)
    {
        // Jeśli sourceDirectory to np. "D:\OldProject\Views\Patients"
        // zwróć "Patients"
        var viewsIndex = sourceDirectory.LastIndexOf("Views", StringComparison.OrdinalIgnoreCase);
        if (viewsIndex >= 0)
        {
            var afterViews = sourceDirectory.Substring(viewsIndex + 5).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return afterViews;
        }

        return string.Empty;
    }

    private string GetProjectRoot(string currentPath)
    {
        // Szukaj katalogu zawierającego plik .csproj
        var directory = new DirectoryInfo(currentPath);

        while (directory != null)
        {
            var csprojFiles = directory.GetFiles("*.csproj");
            if (csprojFiles.Length > 0)
                return directory.FullName;

            // Jeśli znaleziono katalog Views lub ViewModels, zwróć katalog nadrzędny
            if (directory.Name.Equals("Views", StringComparison.OrdinalIgnoreCase) ||
                directory.Name.Equals("ViewModels", StringComparison.OrdinalIgnoreCase))
            {
                return directory.Parent?.FullName ?? directory.FullName;
            }

            directory = directory.Parent;
        }

        // Jeśli nie znaleziono, zwróć bieżący katalog
        return currentPath;
    }

    private string GetSubfolderName(string targetDirectory)
    {
        // Jeśli targetDirectory to np. "D:\Project\Views\Patients"
        // zwróć "Patients"
        var viewsIndex = targetDirectory.LastIndexOf("Views", StringComparison.OrdinalIgnoreCase);
        if (viewsIndex >= 0)
        {
            var afterViews = targetDirectory.Substring(viewsIndex + 5).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return afterViews;
        }

        return string.Empty;
    }

    private string UpdateNamespace(string sourceCode, string targetFilePath, bool isViewModel)
    {
        // Określ prawidłowy namespace na podstawie ścieżki docelowej
        var projectRoot = GetProjectRoot(targetFilePath);
        var relativePath = Path.GetDirectoryName(targetFilePath)!.Substring(projectRoot.Length).TrimStart(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        // Zamień separatory ścieżki na kropki dla namespace
        var namespaceParts = relativePath.Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
        var correctNamespace = "ASMED.EDM.UI." + string.Join(".", namespaceParts);

        // Znajdź i zamień namespace w kodzie
        // Wzorzec: namespace OldNamespace; lub namespace OldNamespace
        var namespacePattern = @"namespace\s+([a-zA-Z0-9_.]+)\s*;?";
        var match = Regex.Match(sourceCode, namespacePattern);

        if (match.Success)
        {
            var oldNamespace = match.Groups[1].Value;
            sourceCode = Regex.Replace(sourceCode, 
                $@"namespace\s+{Regex.Escape(oldNamespace)}\s*;?", 
                $"namespace {correctNamespace};");
        }

        return sourceCode;
    }
}

public class UIImportResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string ErrorMessage { get; set; } = string.Empty;
    public List<string> Warnings { get; set; } = new();

    public string? ImportedXamlPath { get; set; }
    public string? ImportedCodeBehindPath { get; set; }
    public string? ImportedViewModelPath { get; set; }
}
