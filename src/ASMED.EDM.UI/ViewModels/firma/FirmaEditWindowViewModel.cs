using System.Collections.ObjectModel;
using ASMED.EDM.Core.Interfaces.Repositories;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.UI.ViewModels.Firmy;

/// <summary>
/// ViewModel okna dodawania/edycji firmy (FirmaEditWindow).
/// Migracja: FirmaEditViewModel (Access/ODBC) -> EF Core / MySQL, WPF MVVM.
/// </summary>
public partial class FirmaEditWindowViewModel : ViewModelBase
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<FirmaEditWindowViewModel> _logger;

    private Core.Entities.Firma? _firma;

    [ObservableProperty]
    private string _windowTitle = "Dodaj firme";

    [ObservableProperty]
    private string _nazwa = string.Empty;

    [ObservableProperty]
    private string? _nip;

    [ObservableProperty]
    private string? _ulica;

    [ObservableProperty]
    private string? _kodPocztowy;

    [ObservableProperty]
    private string? _miejscowosc;

    [ObservableProperty]
    private string? _fKemail;

    [ObservableProperty]
    private string? _cennik;

    [ObservableProperty]
    private string? _osoba_kontaktowa;

    [ObservableProperty]
    private string? _telefon;

    [ObservableProperty]
    private bool _isLoading;

    public ObservableCollection<string> CennikiOptions { get; } = new();

    public bool? DialogResultValue { get; private set; }

    public event Action? RequestClose;

    public FirmaEditWindowViewModel(IServiceScopeFactory scopeFactory, ILogger<FirmaEditWindowViewModel> logger)
    {
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void Initialize(Core.Entities.Firma? firma)
    {
        _firma = firma;
        WindowTitle = firma == null ? "Dodaj firme" : "Edytuj firme";

        Nazwa = firma?.Nazwa ?? string.Empty;
        Nip = firma?.NIP;
        Ulica = firma?.Ulica;
        KodPocztowy = firma?.Kod;
        Miejscowosc = firma?.Miejscowosc;
        FKemail = firma?.FkEmail;
        Cennik = firma?.NazwaCennika;
        Osoba_kontaktowa = firma?.Osoba_kontaktowa;
        Telefon = firma?.Telefon;

        _ = LoadCennikiAsync();
    }

    private async Task LoadCennikiAsync()
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var repository = scope.ServiceProvider.GetRequiredService<ICennikRepository>();
            var cenniki = await repository.GetActiveCennikiAsync();

            CennikiOptions.Clear();
            foreach (var cennik in cenniki.Select(c => c.Nazwa).OrderBy(n => n))
                CennikiOptions.Add(cennik);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blad podczas ladowania listy cennikow");
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(Nazwa))
        {
            System.Windows.MessageBox.Show(
                "Nazwa firmy jest wymagana.",
                "Blad walidacji",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        IsLoading = true;
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

            if (_firma == null)
            {
                _firma = new Core.Entities.Firma();
                ApplyToEntity(_firma);
                await unitOfWork.Firmy.AddAsync(_firma);
            }
            else
            {
                ApplyToEntity(_firma);
                unitOfWork.Firmy.Update(_firma);
            }

            await unitOfWork.SaveChangesAsync();

            DialogResultValue = true;
            RequestClose?.Invoke();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Blad podczas zapisu firmy");
            System.Windows.MessageBox.Show(
                $"Blad zapisu firmy: {ex.Message}",
                "Blad",
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Error);
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        DialogResultValue = false;
        RequestClose?.Invoke();
    }

    private void ApplyToEntity(Core.Entities.Firma firma)
    {
        firma.Nazwa = Nazwa.Trim();
        firma.NIP = Nip;
        firma.Ulica = Ulica;
        firma.Kod = KodPocztowy;
        firma.Miejscowosc = Miejscowosc;
        firma.FkEmail = FKemail;
        firma.NazwaCennika = Cennik;
        firma.Osoba_kontaktowa = Osoba_kontaktowa;
        firma.Telefon = Telefon;
    }
}
