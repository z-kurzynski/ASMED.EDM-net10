using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.ObjectModel;

namespace ASMED.EDM.UI.ViewModels.Faktura;

/// <summary>
/// ViewModel okna "Dodaj listę badań do faktury".
/// Migracja: ListaFaktAddViewModel (Access/OLE DB) → EF Core / MySQL, WPF MVVM.
///
/// Używa IServiceScopeFactory — każda operacja asynchroniczna tworzy własny
/// krótkotrwały scope, dzięki czemu Scoped DbContext/repozytoria działają
/// poprawnie w WPF (brak kontekstu HTTP).
/// </summary>
public partial class FakturaAddViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FakturaAddViewModel> _logger;

    // ── Firmy ─────────────────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<Firma> _firmy = [];

    [ObservableProperty]
    private Firma? _selectedFirma;

    // ── Cennik ────────────────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<string> _cennikNazwy = [];

    [ObservableProperty]
    private string? _selectedCennikNazwa;

    // ── Nagłówek faktury ──────────────────────────────────────────────────────
    [ObservableProperty]
    private string? _numerFaktury;

    [ObservableProperty]
    private DateTime _dataWystawienia = DateTime.Today;

    [ObservableProperty]
    private decimal? _kwotaBrutto;

    [ObservableProperty]
    private string? _uwagi;

    // ── Badania ───────────────────────────────────────────────────────────────
    [ObservableProperty]
    private ObservableCollection<BadanieListItemViewModel> _dostepneBadania = [];

    [ObservableProperty]
    private ObservableCollection<BadanieListItemViewModel> _wybraneBadania = [];

    [ObservableProperty]
    private decimal _sumaWybranych;

    // ── Statusy UI ────────────────────────────────────────────────────────────
    [ObservableProperty]
    private string? _komunikat;

    [ObservableProperty]
    private bool _isSuccess;

    // ── Zaznaczone wiersze w DataGrid ─────────────────────────────────────────
    [ObservableProperty]
    private BadanieListItemViewModel? _selectedDostepneBadanie;

    [ObservableProperty]
    private BadanieListItemViewModel? _selectedWybraneBadanie;

    // ── Konstruktor ───────────────────────────────────────────────────────────

    public FakturaAddViewModel(
        IServiceScopeFactory scopeFactory,
        ILogger<FakturaAddViewModel> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger       = logger       ?? throw new ArgumentNullException(nameof(logger));
    }

    // ── Inicjalizacja (wywoływana z Loaded event widoku) ──────────────────────

    public override async Task OnNavigatedToAsync()
        => await LoadInitialDataAsync();

    [RelayCommand]
    private async Task LoadInitialDataAsync(CancellationToken ct = default)
    {
        IsBusy      = true;
        BusyMessage = "Ładowanie danych...";
        Komunikat   = null;
        try
        {
            await using var scope   = _scopeFactory.CreateAsyncScope();
            var svc = scope.ServiceProvider.GetRequiredService<IFakturaService>();

            var firmy = await svc.GetActiveFirmyAsync(ct: ct);
            Firmy = new ObservableCollection<Firma>(firmy);

            var nazwy = await svc.GetActiveCennikNazwyAsync(ct);
            CennikNazwy = new ObservableCollection<string>(nazwy);
        }
        catch (Exception ex)
        {
            Komunikat = $"Błąd ładowania danych: {ex.Message}";
            _logger.LogError(ex, "Błąd inicjalizacji FakturaAddViewModel");
        }
        finally
        {
            IsBusy      = false;
            BusyMessage = null;
        }
    }

    // ── Reakcja na zmianę firmy ───────────────────────────────────────────────

    partial void OnSelectedFirmaChanged(Firma? value)
    {
        if (value != null)
            _ = LoadBadaniaForFirmaAsync(value.Id);
        else
        {
            DostepneBadania.Clear();
            WybraneBadania.Clear();
            SumaWybranych = 0;
        }
    }

    private async Task LoadBadaniaForFirmaAsync(int firmaId, CancellationToken ct = default)
    {
        IsBusy      = true;
        BusyMessage = "Ładowanie badań...";
        DostepneBadania.Clear();
        WybraneBadania.Clear();
        SumaWybranych = 0;
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var svc = scope.ServiceProvider.GetRequiredService<IFakturaService>();

            var badania = await svc.GetAvailableBadaniaAsync(firmaId, ct);
            foreach (var b in badania)
            {
                var item = new BadanieListItemViewModel(b);
                item.PropertyChanged += (_, e) =>
                {
                    if (e.PropertyName == nameof(BadanieListItemViewModel.IsSelected))
                        SynchronizujWybrane();
                };
                DostepneBadania.Add(item);
            }
        }
        catch (Exception ex)
        {
            Komunikat = $"Błąd ładowania badań: {ex.Message}";
            _logger.LogError(ex, "Błąd LoadBadaniaForFirmaAsync — FirmaId={Id}", firmaId);
        }
        finally
        {
            IsBusy      = false;
            BusyMessage = null;
        }
    }

    // ── Operacje na liście badań ──────────────────────────────────────────────

    [RelayCommand]
    private void DodajBadanie(BadanieListItemViewModel item)
    {
        if (!WybraneBadania.Contains(item))
        {
            item.IsSelected = true;
            WybraneBadania.Add(item);
            PrzeliczSume();
        }
    }

    [RelayCommand]
    private void UsunBadanie(BadanieListItemViewModel item)
    {
        item.IsSelected = false;
        WybraneBadania.Remove(item);
        PrzeliczSume();
    }

    [RelayCommand]
    private void DodajWszystkie()
    {
        foreach (var b in DostepneBadania.Where(b => !b.IsSelected))
        {
            b.IsSelected = true;
            WybraneBadania.Add(b);
        }
        PrzeliczSume();
    }

    [RelayCommand]
    private void UsunWszystkie()
    {
        foreach (var b in WybraneBadania)
            b.IsSelected = false;
        WybraneBadania.Clear();
        SumaWybranych = 0;
    }

    private void SynchronizujWybrane()
    {
        var doUsuniecia = WybraneBadania.Where(b => !b.IsSelected).ToList();
        foreach (var b in doUsuniecia)
            WybraneBadania.Remove(b);

        foreach (var b in DostepneBadania.Where(b => b.IsSelected && !WybraneBadania.Contains(b)))
            WybraneBadania.Add(b);

        PrzeliczSume();
    }

    private void PrzeliczSume()
    {
        SumaWybranych = WybraneBadania.Sum(b => b.Cena);
        if (KwotaBrutto is null or 0)
            KwotaBrutto = SumaWybranych;
    }

    // ── Zapis ─────────────────────────────────────────────────────────────────

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync(CancellationToken ct = default)
    {
        if (SelectedFirma is null || WybraneBadania.Count == 0)
            return;

        IsBusy      = true;
        BusyMessage = "Zapisywanie...";
        Komunikat   = null;
        try
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var svc = scope.ServiceProvider.GetRequiredService<IFakturaService>();

            var request = new SaveListaBadanRequest(
                FirmaId:         SelectedFirma.Id,
                NumerFaktury:    NumerFaktury,
                DataWystawienia: DataWystawienia,
                KwotaBrutto:     KwotaBrutto,
                Uwagi:           Uwagi,
                NazwaCennika:    SelectedCennikNazwa,
                BadanieIds:      WybraneBadania.Select(b => b.Id)
            );

            var result = await svc.SaveListaBadanAsync(request, ct: ct);

            IsSuccess = result.Success;
            Komunikat = result.Success
                ? $"✅ Zapisano listę badań (Id={result.ListaBadanId})" +
                  (result.FakturaId.HasValue ? $", faktura Id={result.FakturaId}" : string.Empty)
                : $"❌ Błąd zapisu: {result.ErrorMessage}";

            if (result.Success)
                _logger.LogInformation("Zapis listy badań OK — ListaBadanId={Id}", result.ListaBadanId);
        }
        catch (Exception ex)
        {
            IsSuccess = false;
            Komunikat = $"❌ Błąd: {ex.Message}";
            _logger.LogError(ex, "Wyjątek SaveAsync FakturaAddViewModel");
        }
        finally
        {
            IsBusy      = false;
            BusyMessage = null;
        }
    }

    private bool CanSave()
        => SelectedFirma is not null && WybraneBadania.Count > 0 && !IsBusy;
}
