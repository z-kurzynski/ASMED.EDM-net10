using ASMED.EDM.Printing.Models;
using System.IO;

namespace ASMED.EDM.Printing.Services;

public class PatientFormPrintOptions
{
    /// <summary>Folder szablonów PDF - legacy hardcode: A:\formularz\</summary>
    public string TemplatesFolder { get; set; } = @"A:\formularz";

    /// <summary>Folder wyjściowy dla wypełnionych PDF - legacy hardcode: A:\Formuarz_Druki</summary>
    public string OutputFolder { get; set; } = @"A:\Formuarz_Druki";
}

public class PatientFormPrintResult
{
    public bool Success { get; init; }
    public string? OutputPath { get; init; }
    public string? ErrorMessage { get; init; }

    public static PatientFormPrintResult Ok(string outputPath) => new() { Success = true, OutputPath = outputPath };
    public static PatientFormPrintResult Fail(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}

/// <summary>
/// Nazwy szablonów formularzy pacjenta - identyczne z plikami PDF używanymi przez legacy program (ASMED.WPF),
/// aby oba programy mogły korzystać z tych samych, gotowych szablonów.
/// </summary>
public static class PatientFormTemplates
{
    public const string Karta = "ASMED__Karta badania profilaktycznego.pdf";
    public const string Orzeczenie = "ASMED_Orzeczenie.pdf";
    public const string Sanitarne = "ASMED__Sanitarne.pdf";
    public const string Ankieta = "ASMED_Ankieta.pdf";
}

/// <summary>
/// Wysokopoziomowy serwis drukowania formularzy pacjenta (Karta/Orzeczenie/Sanitarne/Ankieta).
/// Łączy mapowanie pól (<see cref="PatientFormFieldMapper"/>), wypełnianie PDF (<see cref="PdfFormService"/>)
/// i podgląd/druk (<see cref="PrintService"/>/<see cref="Views.PdfPreviewWindow"/>).
/// Przeznaczony do wywoływania z różnych zakładek (Wizyty, Skierowania) tymi samymi poleceniami.
/// </summary>
public class PatientFormPrintService
{
    private readonly PatientFormPrintOptions _options;
    private readonly IPrintSettingsStore _settingsStore;

    public PatientFormPrintService(PatientFormPrintOptions? options = null, IPrintSettingsStore? settingsStore = null)
    {
        _options = options ?? new PatientFormPrintOptions();
        _settingsStore = settingsStore ?? new FilePrintSettingsStore();
    }

    /// <summary>
    /// Wypełnia wskazany szablon danymi pacjenta i otwiera okno podglądu PDF z opcją druku.
    /// </summary>
    /// <param name="patient">Dane pacjenta/skierowania do wstawienia w formularz</param>
    /// <param name="templateFileName">Nazwa pliku szablonu, np. <see cref="PatientFormTemplates.Karta"/></param>
    /// <param name="owner">Okno-właściciel dla podglądu PDF (opcjonalne)</param>
    public PatientFormPrintResult ShowPreview(PatientPrintData patient, string templateFileName, System.Windows.Window? owner = null)
    {
        try
        {
            var templateFile = Path.Combine(_options.TemplatesFolder, templateFileName);

            if (!Directory.Exists(_options.OutputFolder))
                Directory.CreateDirectory(_options.OutputFolder);

            var safeLastName = string.IsNullOrWhiteSpace(patient.P_Nazwisko) ? "patient" : patient.P_Nazwisko;
            var outputFile = Path.Combine(_options.OutputFolder,
                $"{Path.GetFileNameWithoutExtension(templateFileName)}_{patient.B_ID}_{safeLastName}.pdf");

            var values = PatientFormFieldMapper.Map(patient, templateFileName);
            var filled = PdfFormService.FillForm(templateFile, values, outputFile);

            if (!filled.Success || filled.OutputPath == null)
            {
                return PatientFormPrintResult.Fail(filled.ErrorMessage ?? $"Nie udało się wypełnić formularza: {templateFileName}");
            }

            var previewWindow = new Views.PdfPreviewWindow(_settingsStore);
            previewWindow.LoadFile(filled.OutputPath);
            if (owner != null)
                previewWindow.Owner = owner;

            previewWindow.ShowDialog();

            return PatientFormPrintResult.Ok(filled.OutputPath);
        }
        catch (Exception ex)
        {
            return PatientFormPrintResult.Fail($"Błąd otwierania podglądu PDF: {ex.Message}");
        }
    }
}
