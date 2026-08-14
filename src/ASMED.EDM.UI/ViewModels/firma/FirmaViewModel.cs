using System.Collections.ObjectModel;
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Interfaces.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.UI.ViewModels.Firmy;

/// <summary>
/// ViewModel dla widoku Firma (Baza Danych -> Firma).
/// Migracja: FirmaViewModel (Access/ODBC) -> EF Core / MySQL, WPF MVVM.
/// Uzywa IServiceScopeFactory - kazda operacja asynchroniczna tworzy wlasny
/// krotkotrwaly scope, dzieki czemu Scoped DbContext/repozytoria dzialaja
/// poprawnie w WPF (brak kontekstu HTTP).
/// </summary>
public partial class FirmaViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FirmaViewModel> _logger;

    private ObservableCollection<Core.Entities.Firma> _allFirmy = new();

    [ObservableProperty]
    private ObservableCollection<Core.Entities.Firma> _filteredFirmy = new();

    [ObservableProperty]
    private string? _searchText;

    [ObservableProperty]
    private string _activeFilterType = "Nazwa";

    public ObservableCollection<string> FilterTypes { get; } = new() { "Nazwa", "NIP" };

    public FirmaViewModel(IServiceScopeFactory scopeFactory, ILogger<FirmaViewModel> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task OnNavigatedToAsync()
        => await OdswiezAsync();

    partial void OnSearchTextChanged(string? value) => FilterFirmy();
    partial void OnActiveFilterTypeChanged(string value) => FilterFirmy();

    [RelayCommand]
    private async Task OdswiezAsync()
    {
        IsBusy = true;
        BusyMessage = "Ladowanie firm...";
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var firmy = await unitOfWork.Firmy.GetActiveFirmyAsync();

            _allFirmy = new ObservableCollection<Core.Entities.Firma>(firmy);
            FilterFirmy();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blad podczas ladowania listy firm");
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
    private void DodajFirme()
    {
        var window = CreateEditWindow(null);
        if (window.ShowDialog() == true)
        {
            _ = OdswiezAsync();
        }
    }

    [RelayCommand]
    private void EdytujFirme(Core.Entities.Firma? firma)
    {
        if (firma == null) return;

        var window = CreateEditWindow(firma);
        if (window.ShowDialog() == true)
        {
            _ = OdswiezAsync();
        }
    }

    [RelayCommand]
    private async Task SaveFirmaAsync(Core.Entities.Firma? firma)
    {
        if (firma == null) return;

        IsBusy = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            unitOfWork.Firmy.Update(firma);
            await unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blad podczas zapisu firmy {FirmaId}", firma.Id);
            System.Windows.MessageBox.Show(
                $"Blad zapisu firmy: {ex.Message}",
                "Blad",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void PokazUmowy(Core.Entities.Firma? firma)
    {
        if (firma == null) return;

        // Modul zarzadzania umowami firm zostanie dodany po wprowadzeniu encji Umowa.
        System.Windows.MessageBox.Show(
            $"Zarzadzanie umowami dla firmy \"{firma.Nazwa}\" zostanie zaimplementowane po dodaniu modulu Umowy.",
            "Umowy firmy",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    private Views.Firma.FirmaEditWindow CreateEditWindow(Core.Entities.Firma? firma)
    {
        using var scope = _scopeFactory.CreateScope();
        var vm = scope.ServiceProvider.GetRequiredService<FirmaEditWindowViewModel>();
        vm.Initialize(firma);

        return new Views.Firma.FirmaEditWindow(vm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
    }

    private void FilterFirmy()
    {
        IEnumerable<Core.Entities.Firma> query = _allFirmy;

        if (!string.IsNullOrWhiteSpace(SearchText))
        {
            var search = SearchText.Trim();
            query = ActiveFilterType switch
            {
                "NIP" => query.Where(f => f.NIP != null && f.NIP.Contains(search, StringComparison.OrdinalIgnoreCase)),
                _ => query.Where(f => f.Nazwa.Contains(search, StringComparison.OrdinalIgnoreCase))
            };
        }

        FilteredFirmy = new ObservableCollection<Core.Entities.Firma>(query);
    }
}
