using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Globalization;
using Syncfusion.UI.Xaml.Scheduler;

namespace ASMED.EDM.UI.ViewModels;

/// <summary>
/// ViewModel dla modułu Wizyt
/// Legacy: ViewModels\Wizyty\WizytyViewViewModel.cs
/// TODO: Migrować SfScheduler logic, appointments handling, filtering
/// </summary>
public partial class VisitsViewModel : ViewModelBase
{
    private readonly ILogger<VisitsViewModel> _logger;

    public VisitsViewModel(ILogger<VisitsViewModel> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _logger.LogInformation("VisitsViewModel initialized");

        // Inicjalizacja testowych danych
        InitializeTestData();
    }

    #region Properties

    [ObservableProperty]
    private DateTime? _selectedDate = DateTime.Today;

    [ObservableProperty]
    private DateTime _displayDate = DateTime.Today;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private ObservableCollection<VisitPatientItem> _pacjenciNaDzien = new();

    [ObservableProperty]
    private ObservableCollection<VisitPatientItem> _filteredPacjenciNaDzien = new();

    [ObservableProperty]
    private VisitPatientItem? _selectedPacjent;

    [ObservableProperty]
    private int _totalCount;

    [ObservableProperty]
    private int _completedCount;

    /// <summary>
    /// Kolekcja appointmentów dla Syncfusion SfScheduler
    /// </summary>
    [ObservableProperty]
    private ScheduleAppointmentCollection _appointments = new();

    /// <summary>
    /// Widoczność widoku szczegółów karty badań (gdy pacjent wybrany)
    /// </summary>
    [ObservableProperty]
    private bool _showDetailsView;

    /// <summary>
    /// Widoczność widoku statystyk (domyślnie widoczny gdy brak wyboru)
    /// </summary>
    [ObservableProperty]
    private bool _showStatsView = true;

    public string SelectedDateFormatted =>
        SelectedDate?.ToString("dddd, dd MMMM yyyy", new CultureInfo("pl-PL")) ?? "Wybierz datę w kalendarzu";

    #endregion

    #region Commands

    [RelayCommand]
    private void Refresh()
    {
        _logger.LogInformation("Refreshing visits list");
        LoadPacjenciNaDzien();
    }

    /// <summary>
    /// Alias dla RefreshCommand - zgodny z legacy binding "OdswiezCommand"
    /// </summary>
    public IRelayCommand OdswiezCommand => RefreshCommand;

    [RelayCommand]
    private void AddNewVisit()
    {
        _logger.LogInformation("Adding new visit");
        // TODO: Open add visit dialog
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchText = string.Empty;
    }

    [RelayCommand]
    private void WydrukListy()
    {
        _logger.LogInformation("Printing patient list");
        // TODO: Implement patient list printing
    }

    #endregion

    #region Methods

    partial void OnSelectedDateChanged(DateTime? value)
    {
        OnPropertyChanged(nameof(SelectedDateFormatted));
        LoadPacjenciNaDzien();
    }

    partial void OnSearchTextChanged(string value)
    {
        FilterPacjenci();
    }

    partial void OnSelectedPacjentChanged(VisitPatientItem? value)
    {
        // Przełączamy widoki w prawej kolumnie
        if (value != null)
        {
            ShowDetailsView = true;
            ShowStatsView = false;
            _logger.LogInformation("Selected patient: {Name}, switching to details view", value.FullName);
        }
        else
        {
            ShowDetailsView = false;
            ShowStatsView = true;
            _logger.LogInformation("No patient selected, switching to stats view");
        }
    }

    private void LoadPacjenciNaDzien()
    {
        // TODO: Load from database
        _logger.LogInformation("Loading patients for date: {Date}", SelectedDate);

        // Obecnie używamy testowych danych
        FilterPacjenci();
    }

    private void FilterPacjenci()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            FilteredPacjenciNaDzien = new ObservableCollection<VisitPatientItem>(PacjenciNaDzien);
        }
        else
        {
            var filtered = PacjenciNaDzien.Where(p =>
                p.FullName.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            FilteredPacjenciNaDzien = new ObservableCollection<VisitPatientItem>(filtered);
        }

        UpdateStatistics();
    }

    private void UpdateStatistics()
    {
        TotalCount = FilteredPacjenciNaDzien.Count;
        CompletedCount = FilteredPacjenciNaDzien.Count(p => p.Status == "Odbyta");
    }

    private void InitializeTestData()
    {
        // Testowe dane - do usunięcia po połączeniu z bazą
        PacjenciNaDzien.Add(new VisitPatientItem
        {
            SkierowanieNumer = "SK/001/2024",
            P_Imie = "Jan",
            P_Nazwisko = "Kowalski",
            FullName = "Jan Kowalski",
            AppointmentTime = "08:00",
            Status = "zaplanowana",
            StatusWizytyTekst = "Zaplanowana",
            Firma_Nazwa = "Firma ABC Sp. z o.o."
        });
        PacjenciNaDzien.Add(new VisitPatientItem
        {
            SkierowanieNumer = "SK/002/2024",
            P_Imie = "Anna",
            P_Nazwisko = "Nowak",
            FullName = "Anna Nowak",
            AppointmentTime = "09:30",
            Status = "odbyta",
            StatusWizytyTekst = "Odbyta",
            Firma_Nazwa = "XYZ S.A."
        });
        PacjenciNaDzien.Add(new VisitPatientItem
        {
            SkierowanieNumer = "SK/003/2024",
            P_Imie = "Piotr",
            P_Nazwisko = "Wiśniewski",
            FullName = "Piotr Wiśniewski",
            AppointmentTime = "11:00",
            Status = "w trakcie",
            StatusWizytyTekst = "W trakcie",
            Firma_Nazwa = "TESCO Polska"
        });

        FilterPacjenci();
    }

    #endregion
}

/// <summary>
/// Item reprezentujący pacjenta na liście wizyt
/// Legacy: RejestracjaItem
/// </summary>
public class VisitPatientItem
{
    public string FullName { get; set; } = string.Empty;
    public string AppointmentTime { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;

    // Dodatkowe pola z legacy
    public string SkierowanieNumer { get; set; } = string.Empty;
    public string P_Imie { get; set; } = string.Empty;
    public string P_Nazwisko { get; set; } = string.Empty;
    public string Firma_Nazwa { get; set; } = string.Empty;
    public string StatusWizytyTekst { get; set; } = string.Empty;
}
