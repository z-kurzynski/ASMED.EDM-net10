using ASMED.EDM.Data.Services;
using ASMED.EDM.UI.Models;
using ASMED.EDM.UI.Views.Skierowania;
using CommunityToolkit.Mvvm.Input;
using System.Collections.ObjectModel;
using System.Windows.Input;

namespace ASMED.EDM.UI.ViewModels.Skierowania;

/// <summary>
/// ViewModel dla widoku listy pacjentów w zakładce Skierowania.
/// Ładuje pacjentów z tabeli P_Pacjent (MySQL) wraz z liczbą kart badań.
/// </summary>
public class SkierListaPacjentowViewModel : ViewModelBase
{
    private readonly DbConnectionFactory _dbFactory;

    // ─── Kolekcje ───────────────────────────────────────────────────────────

    public ObservableCollection<PacjentSkier> Pacjenci { get; } = new();
    public ObservableCollection<PacjentSkier> PacjenciFiltered { get; } = new();

    // ─── Filtry ─────────────────────────────────────────────────────────────

    public List<string> FilterTypes { get; } = new() { "All", "Imię", "Nazwisko", "PESEL", "Firma" };

    private string? _searchText;
    public string? SearchText
    {
        get => _searchText;
        set
        {
            if (SetProperty(ref _searchText, value))
                FilterPacjenci();
        }
    }

    private string _activeFilterType = "All";
    public string ActiveFilterType
    {
        get => _activeFilterType;
        set
        {
            if (SetProperty(ref _activeFilterType, value))
                FilterPacjenci();
        }
    }

    // ─── Komendy ────────────────────────────────────────────────────────────

    public ICommand ClearSearchTextCommand { get; }
    public ICommand EditPatientNewCommand { get; }
    public ICommand OpenHistoriaCommand { get; }

    // ─── Konstruktor ────────────────────────────────────────────────────────

    public SkierListaPacjentowViewModel(DbConnectionFactory dbFactory)
    {
        _dbFactory = dbFactory;

        ClearSearchTextCommand = new RelayCommand(() => SearchText = string.Empty);
        EditPatientNewCommand  = new RelayCommand(OpenNowyPacjent);
        OpenHistoriaCommand    = new RelayCommand<object?>(OpenHistoria);

        LoadPacjenciFromDb();
    }

    // ─── Ładowanie danych ───────────────────────────────────────────────────

    /// <summary>
    /// Ładuje listę pacjentów z bazy MySQL.
    /// JOIN z B_Skierowania żeby policzyć karty badań.
    /// </summary>
    private void LoadPacjenciFromDb()
    {
        Pacjenci.Clear();

        try
        {
            using var conn = _dbFactory.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT
                    P.P_ID,
                    P.P_Imie,
                    P.P_Nazwisko,
                    P.P_PESEL,
                    P.P_Firma,
                    COUNT(B.B_ID) AS LiczbaKartBadan
                FROM
                    P_Pacjent AS P
                    LEFT JOIN B_Skierowania AS B ON P.P_ID = B.B_Pacjent_ID
                GROUP BY
                    P.P_ID, P.P_Imie, P.P_Nazwisko, P.P_PESEL, P.P_Firma
                ORDER BY
                    P.P_Nazwisko, P.P_Imie";

            using var reader = cmd.ExecuteReader();
            int lineNumber = 1;
            while (reader.Read())
            {
                Pacjenci.Add(new PacjentSkier
                {
                    LineNumber    = lineNumber++,
                    P_ID          = Convert.ToInt32(reader["P_ID"]),
                    FirstName     = reader["P_Imie"]?.ToString()    ?? string.Empty,
                    LastName      = reader["P_Nazwisko"]?.ToString() ?? string.Empty,
                    PESEL         = reader["P_PESEL"]?.ToString()    ?? string.Empty,
                    Company       = reader["P_Firma"]?.ToString()    ?? string.Empty,
                    LiczbaKartBadan = Convert.ToInt32(reader["LiczbaKartBadan"])
                });
            }
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                $"Błąd ładowania listy pacjentów:\n\n{ex.Message}",
                "Błąd", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }

        FilterPacjenci();
    }

    /// <summary>
    /// Odświeża listę z bazy danych (wywoływana z przycisku ↺).
    /// </summary>
    public void RefreshList()
    {
        Pacjenci.Clear();
        PacjenciFiltered.Clear();
        LoadPacjenciFromDb();
    }

    // ─── Filtrowanie ────────────────────────────────────────────────────────

    private void FilterPacjenci()
    {
        PacjenciFiltered.Clear();

        // Normalizujemy zapytanie – użytkownik może pisać bez polskich znaków
        var query = NormalizePL(SearchText?.Trim() ?? string.Empty);

        if (string.IsNullOrEmpty(query))
        {
            foreach (var p in Pacjenci)
                PacjenciFiltered.Add(p);
            return;
        }

        foreach (var p in Pacjenci)
        {
            bool match = ActiveFilterType switch
            {
                "Imię"     => NormalizePL(p.FirstName).Contains(query, StringComparison.OrdinalIgnoreCase),
                "Nazwisko" => NormalizePL(p.LastName).Contains(query, StringComparison.OrdinalIgnoreCase),
                "PESEL"    => p.PESEL.Contains(query, StringComparison.OrdinalIgnoreCase),
                "Firma"    => NormalizePL(p.Company).Contains(query, StringComparison.OrdinalIgnoreCase),
                _          => NormalizePL(p.FullName).Contains(query, StringComparison.OrdinalIgnoreCase)
                           || p.PESEL.Contains(query, StringComparison.OrdinalIgnoreCase)
                           || NormalizePL(p.Company).Contains(query, StringComparison.OrdinalIgnoreCase)
            };

            if (match)
                PacjenciFiltered.Add(p);
        }
    }

    /// <summary>
    /// Sprowadza ciąg do wersji bez polskich znaków diakrytycznych (lowercase).
    /// Dzięki temu wpisanie "l" znajdzie "ł", "a" → "ą", "s" → "ś" itp.
    /// </summary>
    private static string NormalizePL(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        var sb = new System.Text.StringBuilder(input.Length);
        foreach (var ch in input.ToLowerInvariant())
        {
            sb.Append(ch switch
            {
                'ą' => 'a',
                'ć' => 'c',
                'ę' => 'e',
                'ł' => 'l',
                'ń' => 'n',
                'ó' => 'o',
                'ś' => 's',
                'ź' => 'z',
                'ż' => 'z',
                _   => ch
            });
        }
        return sb.ToString();
    }

    // ─── Akcje przycisków ───────────────────────────────────────────────────

    private void OpenNowyPacjent()
    {
        // TODO: nawigacja do widoku dodawania nowego pacjenta
        // (wdrożymy w kolejnym kroku)
    }

    private void OpenHistoria(object? obj)
    {
        if (obj is not PacjentSkier pacjent || pacjent.P_ID <= 0)
        {
            System.Windows.MessageBox.Show("Wybierz pacjenta z listy.",
                "Informacja", System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Information);
            return;
        }

        var dialog = new Views.Skierowania.PacjentHistoriaDialog(
            pacjentId: pacjent.P_ID,
            imie:      pacjent.FirstName,
            nazwisko:  pacjent.LastName,
            pesel:     pacjent.PESEL,
            firma:     pacjent.Company)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        dialog.ShowDialog();
    }
}
