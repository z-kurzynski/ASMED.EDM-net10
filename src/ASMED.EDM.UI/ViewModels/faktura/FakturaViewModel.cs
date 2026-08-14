using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Interfaces.Repositories;
using FakturaEntity = ASMED.EDM.Core.Entities.Faktura;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.UI.ViewModels.faktura;

/// <summary>
/// ViewModel dla widoku Faktura (Baza Danych -> Faktura).
/// Migracja: FakturaViewModel (Access/OLE DB, legacy-xaml-backup) -> EF Core / MySQL, WPF MVVM.
/// Uzywa IServiceScopeFactory - kazda operacja asynchroniczna tworzy wlasny
/// krotkotrwaly scope, dzieki czemu Scoped DbContext/repozytoria dzialaja
/// poprawnie w WPF (brak kontekstu HTTP).
/// </summary>
public partial class FakturaViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FakturaViewModel> _logger;

    private ObservableCollection<FakturaEntity> _allFaktura = new();

    [ObservableProperty]
    private ObservableCollection<FakturaEntity> _filteredFaktura = new();

    public ObservableCollection<string> FilterTypes { get; } = new() { "All", "Firma", "Numer", "NIP", "Lista", "ID" };

    public ObservableCollection<string> DateFilterOptions { get; } = new()
    {
        "All",
        "Bieżący Miesiąc",
        "Poprzedni Miesiąc",
        "Bieżący Rok",
        "Poprzedni Rok",
        "Wybrany okres"
    };

    [ObservableProperty]
    private string? _searchText = string.Empty;

    [ObservableProperty]
    private string? _activeFilterType = "All";

    [ObservableProperty]
    private string? _selectedDateFilter = "All";

    [ObservableProperty]
    private DateTime? _filterDateFrom;

    [ObservableProperty]
    private DateTime? _filterDateTo;

    public bool IsCustomDateRangeVisible => SelectedDateFilter == "Wybrany okres";

    // --- nowa faktura ---
    [ObservableProperty]
    private ObservableCollection<Firma> _firmy = new();

    [ObservableProperty]
    private Firma? _selectedNewFirma;

    [ObservableProperty]
    private string? _newFirmaName;

    [ObservableProperty]
    private string? _newNumer;

    [ObservableProperty]
    private DateTime? _newData = DateTime.Today;

    [ObservableProperty]
    private decimal? _newKwota;

    [ObservableProperty]
    private decimal _newBadSuma;

    [ObservableProperty]
    private int _newStatus = 1;

    // --- sumy ---
    [ObservableProperty]
    private decimal _totalKwota;

    [ObservableProperty]
    private decimal _totalKwotaB;

    [ObservableProperty]
    private decimal _totalSaldo;

    public string TotalKwotaText => FormatMoney(TotalKwota);
    public string TotalKwotaBText => FormatMoney(TotalKwotaB);
    public string TotalSaldoText => FormatMoney(TotalSaldo);

    partial void OnTotalKwotaChanged(decimal value) => OnPropertyChanged(nameof(TotalKwotaText));
    partial void OnTotalKwotaBChanged(decimal value) => OnPropertyChanged(nameof(TotalKwotaBText));
    partial void OnTotalSaldoChanged(decimal value) => OnPropertyChanged(nameof(TotalSaldoText));

    public FakturaViewModel(IServiceScopeFactory scopeFactory, ILogger<FakturaViewModel> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task OnNavigatedToAsync() => await OdswiezAsync();

    partial void OnSearchTextChanged(string? value) => FilterFaktura();
    partial void OnActiveFilterTypeChanged(string? value) => FilterFaktura();
    partial void OnSelectedDateFilterChanged(string? value)
    {
        OnPropertyChanged(nameof(IsCustomDateRangeVisible));
        FilterFaktura();
    }
    partial void OnFilterDateFromChanged(DateTime? value)
    {
        if (SelectedDateFilter == "Wybrany okres") FilterFaktura();
    }
    partial void OnFilterDateToChanged(DateTime? value)
    {
        if (SelectedDateFilter == "Wybrany okres") FilterFaktura();
    }
    partial void OnSelectedNewFirmaChanged(Firma? value) => NewFirmaName = value?.Nazwa;

    [RelayCommand]
    private async Task OdswiezAsync()
    {
        IsBusy = true;
        BusyMessage = "Ładowanie faktur...";
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var faktury = await unitOfWork.Faktury.GetAllWithFirmaAsync();
            _allFaktura = new ObservableCollection<FakturaEntity>(faktury);

            var firmy = await unitOfWork.Firmy.GetActiveFirmyAsync();
            Firmy = new ObservableCollection<Firma>(firmy);

            FilterFaktura();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas ładowania listy faktur");
            MessageBox.Show($"Błąd ładowania faktur: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    [RelayCommand]
    private void ClearSearchText() => SearchText = string.Empty;

    [RelayCommand]
    private void ApplyCustomDateFilter() => FilterFaktura();

    [RelayCommand]
    private void ToggleNewInvoice() => IsNewInvoiceVisible = !IsNewInvoiceVisible;

    [ObservableProperty]
    private bool _isNewInvoiceVisible;

    [RelayCommand]
    private async Task AddInvoiceAsync()
    {
        if (SelectedNewFirma is null)
        {
            MessageBox.Show("Wybierz firmę.", "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (string.IsNullOrWhiteSpace(NewNumer))
        {
            MessageBox.Show("Podaj numer faktury.", "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        IsBusy = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            var faktura = new FakturaEntity
            {
                FirmaId = SelectedNewFirma.Id,
                NumerFaktury = NewNumer.Trim(),
                DataWystawienia = NewData ?? DateTime.Today,
                KwotaBrutto = NewKwota ?? 0m,
                SumaBadan = NewBadSuma,
                Status = ((FakturaStatus)NewStatus).ToString()
            };

            await unitOfWork.Faktury.AddAsync(faktura);
            await unitOfWork.SaveChangesAsync();

            SelectedNewFirma = null;
            NewFirmaName = string.Empty;
            NewNumer = string.Empty;
            NewData = DateTime.Today;
            NewKwota = null;
            NewBadSuma = 0m;
            NewStatus = 1;
            IsNewInvoiceVisible = false;

            await OdswiezAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas zapisu faktury");
            MessageBox.Show($"Błąd zapisu faktury: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UpdateFakturaAsync(FakturaEntity? faktura)
    {
        if (faktura is null) return;

        IsBusy = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            unitOfWork.Faktury.Update(faktura);
            await unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Faktura zaktualizowana Id={Id}", faktura.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas aktualizacji faktury {Id}", faktura.Id);
            MessageBox.Show($"Błąd podczas aktualizacji faktury:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanDeleteFaktura(FakturaEntity? faktura) => faktura is not null && faktura.ListaBadanId is null or 0;

    [RelayCommand(CanExecute = nameof(CanDeleteFaktura))]
    private async Task DeleteFakturaAsync(FakturaEntity? faktura)
    {
        if (faktura is null) return;

        if (!CanDeleteFaktura(faktura))
        {
            MessageBox.Show("Nie można usunąć faktury przypisanej do listy badań.\nOdłącz najpierw listę od faktury.",
                "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var result = MessageBox.Show(
            $"Czy na pewno usunąć fakturę?\n\nNumer: {faktura.NumerFaktury}\nFirma: {faktura.Firma?.Nazwa}\nKwota: {faktura.KwotaBrutto:N2} zł",
            "Potwierdzenie usunięcia",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

        IsBusy = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            unitOfWork.Faktury.Remove(faktura);
            await unitOfWork.SaveChangesAsync();

            _logger.LogInformation("Faktura usunięta Id={Id}", faktura.Id);
            await OdswiezAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd podczas usuwania faktury {Id}", faktura.Id);
            MessageBox.Show($"Błąd podczas usuwania faktury:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void FilterFaktura()
    {
        var raw = (SearchText ?? string.Empty).Trim();
        var text = raw.ToLowerInvariant();

        DateTime? dateFrom = null;
        DateTime? dateTo = null;

        switch (SelectedDateFilter)
        {
            case "Bieżący Miesiąc":
                dateFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                dateTo = dateFrom.Value.AddMonths(1).AddDays(-1);
                break;
            case "Poprzedni Miesiąc":
                dateFrom = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1).AddMonths(-1);
                dateTo = dateFrom.Value.AddMonths(1).AddDays(-1);
                break;
            case "Bieżący Rok":
                dateFrom = new DateTime(DateTime.Now.Year, 1, 1);
                dateTo = new DateTime(DateTime.Now.Year, 12, 31);
                break;
            case "Poprzedni Rok":
                dateFrom = new DateTime(DateTime.Now.Year - 1, 1, 1);
                dateTo = new DateTime(DateTime.Now.Year - 1, 12, 31);
                break;
            case "Wybrany okres":
                dateFrom = FilterDateFrom;
                dateTo = FilterDateTo;
                break;
        }

        var query = _allFaktura.AsEnumerable();

        query = query.Where(f =>
        {
            if (dateFrom.HasValue && f.DataWystawienia.HasValue && f.DataWystawienia.Value < dateFrom.Value) return false;
            if (dateTo.HasValue && f.DataWystawienia.HasValue && f.DataWystawienia.Value > dateTo.Value) return false;
            return true;
        });

        if (!string.IsNullOrEmpty(text))
        {
            query = ActiveFilterType switch
            {
                "Firma" => query.Where(f => (f.Firma?.Nazwa ?? string.Empty).ToLowerInvariant().Contains(text)),
                "Numer" => query.Where(f => (f.NumerFaktury ?? string.Empty).ToLowerInvariant().Contains(text)),
                "NIP" => query.Where(f => (f.Firma?.NIP ?? string.Empty).ToLowerInvariant().Contains(text)),
                "Lista" => query.Where(f => f.ListaBadanId.HasValue && f.ListaBadanId.Value.ToString().Contains(text)),
                "ID" => query.Where(f => f.Id.ToString().Contains(raw)),
                _ => query.Where(f =>
                    (f.Firma?.Nazwa ?? string.Empty).ToLowerInvariant().Contains(text)
                    || (f.NumerFaktury ?? string.Empty).ToLowerInvariant().Contains(text)
                    || (f.Firma?.NIP ?? string.Empty).ToLowerInvariant().Contains(text)
                    || f.Id.ToString().Contains(raw))
            };
        }

        FilteredFaktura = new ObservableCollection<FakturaEntity>(query);
        RecalculateTotals();
    }

    private void RecalculateTotals()
    {
        try
        {
            TotalKwota = FilteredFaktura.Sum(f => f.KwotaBrutto ?? 0m);
            TotalKwotaB = FilteredFaktura.Sum(f => f.SumaBadan ?? 0m);
            TotalSaldo = FilteredFaktura.Sum(f => f.Saldo ?? 0m);
        }
        catch
        {
            TotalKwota = TotalKwotaB = TotalSaldo = 0m;
        }
    }

    private static string FormatMoney(decimal v)
        => string.Format(CultureInfo.GetCultureInfo("pl-PL"), "{0:N2} zł", v);
}
