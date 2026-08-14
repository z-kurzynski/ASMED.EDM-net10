namespace ASMED.EDM.Printing.Services;

/// <summary>
/// Wynik operacji wypełniania formularza PDF - bez zależności na UI (brak MessageBox).
/// Warstwa wywołująca (ViewModel) decyduje jak zaprezentować błąd (np. INotificationService.ShowToast).
/// </summary>
public class PdfFormResult
{
    public bool Success { get; init; }
    public string? OutputPath { get; init; }
    public string? ErrorMessage { get; init; }

    public static PdfFormResult Ok(string outputPath) => new() { Success = true, OutputPath = outputPath };
    public static PdfFormResult Fail(string errorMessage) => new() { Success = false, ErrorMessage = errorMessage };
}
