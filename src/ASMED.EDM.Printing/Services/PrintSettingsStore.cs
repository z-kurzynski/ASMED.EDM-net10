using System.IO;
using System.Text.Json;

namespace ASMED.EDM.Printing.Services;

/// <summary>
/// Przechowuje ostatnio wybrane ustawienia druku (drukarka, duplex, orientacja, kopie).
/// Zastępuje legacy mechanizm Properties.Settings.Default prostym plikiem JSON w %AppData%.
/// </summary>
public interface IPrintSettingsStore
{
    Models.PrintSettings Load();
    void Save(Models.PrintSettings settings);
}

public class FilePrintSettingsStore : IPrintSettingsStore
{
    private readonly string _filePath;

    public FilePrintSettingsStore()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "ASMED.EDM", "Printing");
        Directory.CreateDirectory(folder);
        _filePath = Path.Combine(folder, "print-settings.json");
    }

    public Models.PrintSettings Load()
    {
        try
        {
            if (!File.Exists(_filePath))
                return new Models.PrintSettings();

            var json = File.ReadAllText(_filePath);
            return JsonSerializer.Deserialize<Models.PrintSettings>(json) ?? new Models.PrintSettings();
        }
        catch
        {
            return new Models.PrintSettings();
        }
    }

    public void Save(Models.PrintSettings settings)
    {
        try
        {
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_filePath, json);
        }
        catch
        {
            // Ignorujemy błędy zapisu ustawień - nie są krytyczne dla drukowania
        }
    }
}
