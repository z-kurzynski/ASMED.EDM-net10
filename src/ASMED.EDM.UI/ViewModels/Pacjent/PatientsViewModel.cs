using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Services;
using ASMED.EDM.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;
using System.Linq;

namespace ASMED.EDM.UI.ViewModels;

/// <summary>
/// ViewModel zarządzający listą pacjentów
/// </summary>
public partial class PatientsViewModel : ViewModelBase
{
    private readonly IPatientService _patientService;
    private readonly IDialogService _dialogService;
    private readonly ILogger<PatientsViewModel> _logger;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private Patient? _selectedPatient;

    [ObservableProperty]
    private string _activeFilterType = "Nazwisko";

    public PatientsViewModel(
        IPatientService patientService,
        IDialogService dialogService,
        ILogger<PatientsViewModel> logger)
    {
        _patientService = patientService ?? throw new ArgumentNullException(nameof(patientService));
        _dialogService = dialogService ?? throw new ArgumentNullException(nameof(dialogService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Inicjalizuj typy filtrów
        FilterTypes = new ObservableCollection<string>
        {
            "Nazwisko",
            "PESEL",
            "Telefon"
        };
    }

    /// <summary>
    /// Lista pacjentów
    /// </summary>
    public ObservableCollection<Patient> Patients { get; } = new();

    /// <summary>
    /// Typy filtrów dostępne w ComboBox
    /// </summary>
    public ObservableCollection<string> FilterTypes { get; }

    /// <summary>
    /// Filtrowana lista pacjentów - używana przez SfDataGrid
    /// </summary>
    public ObservableCollection<Patient> PacjenciFiltered
    {
        get
        {
            if (string.IsNullOrWhiteSpace(SearchText))
            {
                return Patients;
            }

            var filtered = ActiveFilterType switch
            {
                "Nazwisko" => Patients.Where(p =>
                    p.LastName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    p.FirstName.Contains(SearchText, StringComparison.OrdinalIgnoreCase)),
                "PESEL" => Patients.Where(p =>
                    p.IdentificationNumber?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true),
                "Telefon" => Patients.Where(p =>
                    p.PhoneNumber?.Contains(SearchText, StringComparison.OrdinalIgnoreCase) == true),
                _ => Patients.Where(p =>
                    p.LastName.Contains(SearchText, StringComparison.OrdinalIgnoreCase) ||
                    p.FirstName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
            };

            return new ObservableCollection<Patient>(filtered);
        }
    }

    /// <summary>
    /// Wyczyść pole wyszukiwania
    /// </summary>
    [RelayCommand]
    private void ClearSearchText()
    {
        SearchText = string.Empty;
        OnPropertyChanged(nameof(PacjenciFiltered));
    }

    partial void OnSearchTextChanged(string value)
    {
        OnPropertyChanged(nameof(PacjenciFiltered));
    }

    partial void OnActiveFilterTypeChanged(string value)
    {
        OnPropertyChanged(nameof(PacjenciFiltered));
    }

    partial void OnSelectedPatientChanged(Patient? value)
    {
        EditPatientCommand.NotifyCanExecuteChanged();
        DeletePatientCommand.NotifyCanExecuteChanged();
        ViewPatientDetailsCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Ładowanie pacjentów przy inicjalizacji widoku
    /// </summary>
    public override async Task OnNavigatedToAsync()
    {
        await LoadPatientsAsync();
    }

    /// <summary>
    /// Załaduj wszystkich pacjentów
    /// </summary>
    [RelayCommand]
    private async Task LoadPatientsAsync()
    {
        try
        {
            IsBusy = true;
            BusyMessage = "Ładowanie pacjentów...";

            _logger.LogInformation("Ładowanie listy pacjentów");

            var patients = await _patientService.GetAllPatientsAsync();

            Patients.Clear();
            foreach (var patient in patients)
            {
                Patients.Add(patient);
            }

            _logger.LogInformation("Załadowano {Count} pacjentów", Patients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania pacjentów");
            await _dialogService.ShowErrorAsync("Błąd", $"Nie udało się załadować pacjentów: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    /// <summary>
    /// Wyszukaj pacjentów
    /// </summary>
    [RelayCommand]
    private async Task SearchPatientsAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchText))
        {
            await LoadPatientsAsync();
            return;
        }

        try
        {
            IsBusy = true;
            BusyMessage = "Wyszukiwanie...";

            _logger.LogInformation("Wyszukiwanie pacjentów: {SearchText}", SearchText);

            var patients = await _patientService.SearchPatientsAsync(SearchText);

            Patients.Clear();
            foreach (var patient in patients)
            {
                Patients.Add(patient);
            }

            _logger.LogInformation("Znaleziono {Count} pacjentów", Patients.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas wyszukiwania pacjentów");
            await _dialogService.ShowErrorAsync("Błąd", $"Nie udało się wyszukać pacjentów: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    /// <summary>
    /// Dodaj nowego pacjenta
    /// </summary>
    [RelayCommand]
    private async Task AddPatientAsync()
    {
        _logger.LogInformation("Dodawanie nowego pacjenta");

        // TODO: Otwarcie okna dialogowego edycji pacjenta
        await _dialogService.ShowMessageAsync("Info", "Okno dodawania pacjenta - do zaimplementowania");
    }

    /// <summary>
    /// Edytuj wybranego pacjenta
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanEditPatient))]
    private async Task EditPatientAsync()
    {
        if (SelectedPatient == null) return;

        _logger.LogInformation("Edycja pacjenta ID: {PatientId}", SelectedPatient.Id);

        // TODO: Otwarcie okna dialogowego edycji pacjenta
        await _dialogService.ShowMessageAsync("Info", $"Edycja pacjenta: {SelectedPatient.FullName}");
    }

    private bool CanEditPatient() => SelectedPatient != null;

    /// <summary>
    /// Usuń wybranego pacjenta
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanDeletePatient))]
    private async Task DeletePatientAsync()
    {
        if (SelectedPatient == null) return;

        var confirmed = await _dialogService.ShowConfirmationAsync(
            "Potwierdzenie",
            $"Czy na pewno chcesz usunąć pacjenta: {SelectedPatient.FullName}?");

        if (!confirmed) return;

        try
        {
            IsBusy = true;
            BusyMessage = "Usuwanie pacjenta...";

            _logger.LogInformation("Usuwanie pacjenta ID: {PatientId}", SelectedPatient.Id);

            // TODO: Pobierz ID zalogowanego użytkownika
            await _patientService.DeletePatientAsync(SelectedPatient.Id, userId: 1);

            Patients.Remove(SelectedPatient);
            SelectedPatient = null;

            await _dialogService.ShowMessageAsync("Sukces", "Pacjent został usunięty");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania pacjenta");
            await _dialogService.ShowErrorAsync("Błąd", $"Nie udało się usunąć pacjenta: {ex.Message}");
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    private bool CanDeletePatient() => SelectedPatient != null;

    /// <summary>
    /// Pokaż szczegóły pacjenta
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanViewPatientDetails))]
    private async Task ViewPatientDetailsAsync()
    {
        if (SelectedPatient == null) return;

        _logger.LogInformation("Wyświetlanie szczegółów pacjenta ID: {PatientId}", SelectedPatient.Id);

        // TODO: Nawigacja do widoku szczegółów
        await _dialogService.ShowMessageAsync("Info", $"Szczegóły pacjenta: {SelectedPatient.FullName}");
    }

    private bool CanViewPatientDetails() => SelectedPatient != null;
}
