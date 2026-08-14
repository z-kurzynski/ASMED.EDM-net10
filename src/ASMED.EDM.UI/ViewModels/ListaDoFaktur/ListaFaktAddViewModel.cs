using ASMED.EDM.Core.DTOs;
using ASMED.EDM.Core.Interfaces.Services;
using ASMED.EDM.Data.Services;
using ASMED.EDM.UI.Models.ListaDoFaktur;
using ASMED.EDM.UI.Services;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Input;

namespace ASMED.EDM.UI.ViewModels.ListaDoFaktur;

/// <summary>
/// ViewModel dla ListaFaktAddView.
/// Wzorzec jak BadaniaNewViewModel: własny serwis UI + synchroniczne raw SQL.
/// </summary>
public class ListaFaktAddViewModel : ViewModelBase
{
    private readonly ListaFaktAddService _svc;
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly CultureInfo _pl = CultureInfo.GetCultureInfo("pl-PL");

    // Akcja wywoływana przez VM żeby view zresetował stan selekcji
    public event Action? RequestResetSelectionState;
    // Nawigacja: powrót do listy faktur (ustawiana przez hosta/widok)
    public Action? NavigateBack { get; set; }

    // ===== CENNIK – ceny etykiet (z cennika) =====
    private decimal _priceBasic, _priceLaryngologist, _priceOphthalmologist,
                    _priceSanitary, _priceLipidogram, _priceEKG,
                    _priceHealthClinic, _priceOther;

    public string PriceBasicText         => FormatPrice(_priceBasic);
    public string PriceLaryngologistText  => FormatPrice(_priceLaryngologist);
    public string PriceOphthalmologistText=> FormatPrice(_priceOphthalmologist);
    public string PriceSanitaryText       => FormatPrice(_priceSanitary);
    public string PriceLipidogramText     => FormatPrice(_priceLipidogram);
    public string PriceEKGText            => FormatPrice(_priceEKG);
    public string PriceHealthClinicText   => FormatPrice(_priceHealthClinic);
    public string PriceOtherText          => FormatPrice(_priceOther);

    private string FormatPrice(decimal v) => v.ToString("N2", _pl) + " zł";

    private void NotifyPriceTexts()
    {
        OnPropertyChanged(nameof(PriceBasicText));
        OnPropertyChanged(nameof(PriceLaryngologistText));
        OnPropertyChanged(nameof(PriceOphthalmologistText));
        OnPropertyChanged(nameof(PriceSanitaryText));
        OnPropertyChanged(nameof(PriceLipidogramText));
        OnPropertyChanged(nameof(PriceEKGText));
        OnPropertyChanged(nameof(PriceHealthClinicText));
        OnPropertyChanged(nameof(PriceOtherText));
    }

    // ===== EDYTOWALNE POLA CEN (TextBox w Grid_1) =====
    private string? _editableBasic, _editableLaryngologist, _editableOphthalmologist,
                    _editableSanitary, _editableLipidogram, _editableEKG,
                    _editableHealthClinic, _editableOther;

    public string? EditablePriceBasic          { get => _editableBasic;          set { SetProperty(ref _editableBasic, value); } }
    public string? EditablePriceLaryngologist   { get => _editableLaryngologist;  set { SetProperty(ref _editableLaryngologist, value); } }
    public string? EditablePriceOphthalmologist { get => _editableOphthalmologist;set { SetProperty(ref _editableOphthalmologist, value); } }
    public string? EditablePriceSanitary        { get => _editableSanitary;       set { SetProperty(ref _editableSanitary, value); } }
    public string? EditablePriceLipidogram      { get => _editableLipidogram;     set { SetProperty(ref _editableLipidogram, value); } }
    public string? EditablePriceEKG             { get => _editableEKG;            set { SetProperty(ref _editableEKG, value); } }
    public string? EditablePriceHealthClinic    { get => _editableHealthClinic;   set { SetProperty(ref _editableHealthClinic, value); } }
    public string? EditablePriceOther           { get => _editableOther;          set { SetProperty(ref _editableOther, value); } }

    // ===== NAGŁÓWEK FAKTURY (Grid_0) =====
    private string? _numerFaktury;
    public string? NumerFaktury { get => _numerFaktury; set => SetProperty(ref _numerFaktury, value); }

    private DateTime? _dataWystawienia = DateTime.Today;
    public DateTime? DataWystawienia { get => _dataWystawienia; set => SetProperty(ref _dataWystawienia, value); }

    private string? _sumaBrutto;
    public string? SumaBrutto { get => _sumaBrutto; set => SetProperty(ref _sumaBrutto, value); }

    private string? _uwagi;
    public string? Uwagi { get => _uwagi; set => SetProperty(ref _uwagi, value); }

    private string? _fkEmail;
    public string? FKemail { get => _fkEmail; set => SetProperty(ref _fkEmail, value); }

    // ===== FIRMA =====
    private readonly List<FirmaDto> _allFirmaDtos = new();

    private string? _wybranaFirma;
    public string? WybranaFirma
    {
        get => _wybranaFirma;
        set
        {
            if (SetProperty(ref _wybranaFirma, value))
            {
                FilterFirmyItems(value);
                if (string.IsNullOrWhiteSpace(value))
                {
                    SelectedFirmaDto = null;
                    FKemail = null;
                }
                else
                {
                    var found = _allFirmaDtos.FirstOrDefault(f =>
                        string.Equals(f.Nazwa?.Trim(), value.Trim(), StringComparison.OrdinalIgnoreCase));
                    SelectedFirmaDto = found;
                    FKemail = found?.FkEmail;
                }
                ApplyAvailableFilter();
            }
        }
    }

    private FirmaDto? _selectedFirmaDto;
    public FirmaDto? SelectedFirmaDto
    {
        get => _selectedFirmaDto;
        private set
        {
            if (SetProperty(ref _selectedFirmaDto, value))
                OnPropertyChanged(nameof(SelectedFirmaId));
        }
    }

    public int? SelectedFirmaId => SelectedFirmaDto?.Id;

    public ObservableCollection<string> FirmyItems { get; } = new();
    private readonly List<string> _allFirmy = new();

    public void SetSelectedFirma(FirmaDto? dto)
    {
        if (dto == null)
        {
            SelectedFirmaDto = null;
            WybranaFirma = null;
            FKemail = null;
            return;
        }
        SelectedFirmaDto = dto;
        WybranaFirma = dto.Nazwa;
        FKemail = dto.FkEmail;

        // Ustaw cennik firmy jako domyślny jeśli cennik nie wybrany
        if (!string.IsNullOrWhiteSpace(dto.Cennik) && string.IsNullOrWhiteSpace(SelectedCennik))
            SelectedCennik = dto.Cennik;

        ApplyAvailableFilter();
    }

    private void FilterFirmyItems(string? query)
    {
        var dispatcher = Application.Current?.Dispatcher;
        void Update()
        {
            FirmyItems.Clear();
            if (string.IsNullOrWhiteSpace(query))
            {
                foreach (var s in _allFirmy) FirmyItems.Add(s);
                return;
            }
            var ci = CultureInfo.CurrentCulture.CompareInfo;
            var opts = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;
            foreach (var s in _allFirmy)
            {
                if (!string.IsNullOrWhiteSpace(s) && ci.IndexOf(s, query.Trim(), opts) >= 0)
                    if (!FirmyItems.Contains(s)) FirmyItems.Add(s);
            }
        }

        if (dispatcher?.CheckAccess() == true) Update();
        else dispatcher?.BeginInvoke(Update);
    }

    // ===== CENNIK (combo) =====
    public ObservableCollection<string> StatusOptions { get; } = new();

    private string? _selectedCennik;
    public string? SelectedCennik
    {
        get => _selectedCennik;
        set
        {
            if (SetProperty(ref _selectedCennik, value))
                _ = LoadPricesForSelectedCennikAsync();
        }
    }

    private async Task LoadPricesForSelectedCennikAsync()
    {
        _priceBasic = _priceLaryngologist = _priceOphthalmologist = _priceSanitary
            = _priceLipidogram = _priceEKG = _priceHealthClinic = _priceOther = 0m;

        if (!string.IsNullOrWhiteSpace(SelectedCennik) && _scopeFactory != null)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var svc = scope.ServiceProvider.GetRequiredService<IFakturaService>();
                var cennik = await svc.GetCennikByNazwaAsync(SelectedCennik);
                if (cennik != null)
                {
                    _priceBasic          = cennik.CenaPodstawowa  ?? 0m;
                    _priceLaryngologist  = cennik.CenaLaryngolog  ?? 0m;
                    _priceOphthalmologist= cennik.CenaOkulista    ?? 0m;
                    _priceSanitary       = cennik.CenaKsiazeczka  ?? 0m;
                    _priceLipidogram     = cennik.CenaLipidogram  ?? 0m;
                    _priceEKG            = cennik.CenaEKG         ?? 0m;
                    _priceHealthClinic   = cennik.CenaPoradnia    ?? 0m;
                    _priceOther          = cennik.CenaInne        ?? 0m;
                }
            }
            catch { /* ignoruj błędy cennika */ }
        }
        NotifyPriceTexts();
    }

    // ===== POLA BADANIA (Grid_1) =====
    public ObservableCollection<string> ImionaItems   { get; } = new();
    public ObservableCollection<string> NazwiskaItems { get; } = new();
    public ObservableCollection<string> WynikOptions  { get; } = new();

    private string? _wybraneImie;
    public string? WybraneImie
    {
        get => _wybraneImie;
        set { if (SetProperty(ref _wybraneImie, value)) ApplyAvailableFilter(); }
    }

    private string? _wybraneNazwisko;
    public string? WybraneNazwisko
    {
        get => _wybraneNazwisko;
        set { if (SetProperty(ref _wybraneNazwisko, value)) ApplyAvailableFilter(); }
    }

    private DateTime? _dataBadania;
    public DateTime? DataBadania
    {
        get => _dataBadania;
        set
        {
            if (SetProperty(ref _dataBadania, value) && value.HasValue)
                DataWaznosci = value.Value.AddYears(3);
        }
    }

    private DateTime? _dataWaznosci;
    public DateTime? DataWaznosci { get => _dataWaznosci; set => SetProperty(ref _dataWaznosci, value); }

    private string? _selectedWynik;
    public string? SelectedWynik { get => _selectedWynik; set => SetProperty(ref _selectedWynik, value); }

    private string? _nrKsiegi;
    public string? NrKsiegi { get => _nrKsiegi; set => SetProperty(ref _nrKsiegi, value); }

    private string? _typBadania;
    public string? TypBadania { get => _typBadania; set => SetProperty(ref _typBadania, value); }

    private bool _isKsiazeczkaChecked;
    public bool IsKsiazeczkaChecked { get => _isKsiazeczkaChecked; set => SetProperty(ref _isKsiazeczkaChecked, value); }

    private bool _isUrlopChecked;
    public bool IsUrlopChecked { get => _isUrlopChecked; set => SetProperty(ref _isUrlopChecked, value); }

    // ===== LISTY BADAŃ =====
    public ObservableCollection<BadanieRowDto> AvailableBadania { get; } = new();

    private ObservableCollection<BadanieWrapper> _filteredAvailableBadania = new();
    public ObservableCollection<BadanieWrapper> FilteredAvailableBadania
    {
        get => _filteredAvailableBadania;
        private set => SetProperty(ref _filteredAvailableBadania, value);
    }

    private ListyBadanDto _selectedLista = new();
    public ListyBadanDto SelectedLista
    {
        get => _selectedLista;
        set
        {
            if (SetProperty(ref _selectedLista, value ?? new ListyBadanDto()))
            {
                RecalculateTotalRazem();
                SaveButtonLabel = (_selectedLista.Identyfikator.HasValue) ? "Modyfikuj listę" : "Zapisz listę";
                OnPropertyChanged(nameof(SaveButtonLabel));
                OnPropertyChanged(nameof(WydrukiVisibility));
            }
        }
    }

    private decimal _totalRazem;
    public decimal TotalRazem { get => _totalRazem; private set => SetProperty(ref _totalRazem, value); }

    private string _saveButtonLabel = "Zapisz listę";
    public string SaveButtonLabel { get => _saveButtonLabel; set => SetProperty(ref _saveButtonLabel, value); }

    // Widoczność przycisków druku/email (tylko jeśli lista ma Id lub ma badania)
    public string WydrukiVisibility =>
        (SelectedLista?.Identyfikator.HasValue == true || SelectedLista?.Badania.Count > 0)
        ? "Visible" : "Collapsed";

    // ===== ODŚWIEŻANIE LISTY =====
    private int _newBadaniaAvailableCount;
    public int NewBadaniaAvailableCount
    {
        get => _newBadaniaAvailableCount;
        private set
        {
            if (SetProperty(ref _newBadaniaAvailableCount, value))
            {
                OnPropertyChanged(nameof(ShowRefreshBadaniaButton));
                OnPropertyChanged(nameof(RefreshBadaniaButtonText));
            }
        }
    }
    public bool   ShowRefreshBadaniaButton => NewBadaniaAvailableCount > 0;
    public string RefreshBadaniaButtonText  => $"🔄 Odśwież listę ({NewBadaniaAvailableCount} nowych)";

    private BadanieRowDto? _selectedAssignedBadanie;
    public BadanieRowDto? SelectedAssignedBadanie
    {
        get => _selectedAssignedBadanie;
        set
        {
            if (SetProperty(ref _selectedAssignedBadanie, value))
                PopulateGridFromSelected();
        }
    }

    // ===== KONSTRUKTOR =====
    public ListaFaktAddViewModel(DbConnectionFactory? dbFactory = null, IServiceScopeFactory? scopeFactory = null)
    {
        _svc          = new ListaFaktAddService(dbFactory);
        _scopeFactory = scopeFactory;

        WynikOptions.Add("1 - Pozytywne");
        WynikOptions.Add("2 - Negatywne");
        SelectedWynik = WynikOptions.FirstOrDefault();

        DataBadania     = DateTime.Today;
        DataWaznosci    = DateTime.Today.AddYears(3);
        DataWystawienia = DateTime.Today;

        LoadData();
        // LoadAvailableBadaniaAsync — wywoływane z ListaFaktAddView_Loaded (po wyrenderowaniu widoku)
    }

    // ===== ŁADOWANIE DANYCH POMOCNICZYCH (synchronicznie) =====
    private void LoadData()
    {
        try
        {
            var firmy = _svc.GetFirmy();
            _allFirmy.Clear();
            _allFirmaDtos.Clear();
            foreach (var dto in firmy)
            {
                if (!string.IsNullOrWhiteSpace(dto.Nazwa))
                {
                    _allFirmy.Add(dto.Nazwa!);
                    _allFirmaDtos.Add(dto);
                }
            }
            FirmyItems.Clear();
            foreach (var n in _allFirmy) FirmyItems.Add(n);

            var cenniki = _svc.GetCennikiNazwy();
            StatusOptions.Clear();
            foreach (var n in cenniki)
                if (!string.IsNullOrWhiteSpace(n) && !StatusOptions.Contains(n))
                    StatusOptions.Add(n);
            if (string.IsNullOrEmpty(SelectedCennik) && StatusOptions.Count > 0)
                SelectedCennik = StatusOptions[0];

            var imiona = _svc.GetImiona();
            ImionaItems.Clear();
            foreach (var i in imiona) ImionaItems.Add(i);

            var nazwiska = _svc.GetNazwiska();
            NazwiskaItems.Clear();
            foreach (var n in nazwiska) NazwiskaItems.Add(n);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ListaFakt] LoadData ERROR: {ex.Message}");
        }
    }

    public async Task LoadAvailableBadaniaAsync()
    {
        try
        {
            var rows = await Task.Run(() => _svc.GetAvailableBadania());

            Console.WriteLine($"[ListaFakt] GetAvailableBadania → {rows.Count} rekordów");

            var wrappers = rows.Select(r => new BadanieWrapper(r)).ToList();

            await Application.Current.Dispatcher.InvokeAsync(() =>
            {
                AvailableBadania.Clear();
                foreach (var r in rows) AvailableBadania.Add(r);

                FilteredAvailableBadania = new ObservableCollection<BadanieWrapper>(wrappers);
            });
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ListaFakt] LoadAvailableBadaniaAsync ERROR: {ex.Message}");
        }
    }

    // ===== FILTROWANIE LEWEJ LISTY =====
    private void ApplyAvailableFilter()
    {
        
        var dispatcher = Application.Current?.Dispatcher;
        void Update()
        {
          try
          {
            // Id badań już przypisanych do prawej listy — wyklucz je z lewej
            var assignedIds = SelectedLista.Badania
                .Where(b => b.Bad_ID.HasValue)
                .Select(b => b.Bad_ID!.Value)
                .ToHashSet();

            var ci   = CultureInfo.CurrentCulture.CompareInfo;
            var opts = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace;

            var result = new List<BadanieWrapper>();
            foreach (var row in AvailableBadania)
            {
                // Pomiń badania już na prawej liście
                if (row.Bad_ID.HasValue && assignedIds.Contains(row.Bad_ID.Value)) continue;

                // Filtr firma — pokaż tylko badania pacjentów wybranej firmy
                if (SelectedFirmaDto != null)
                {
                    if (string.IsNullOrWhiteSpace(row.FirmaNazwa) ||
                        ci.Compare(row.FirmaNazwa.Trim(), SelectedFirmaDto.Nazwa?.Trim() ?? string.Empty, opts) != 0)
                        continue;
                }

                // Filtr imię
                if (!string.IsNullOrWhiteSpace(WybraneImie))
                {
                    var imie = row.P_imie ?? string.Empty;
                    if (ci.IndexOf(imie, WybraneImie.Trim(), opts) < 0) continue;
                }

                // Filtr nazwisko
                if (!string.IsNullOrWhiteSpace(WybraneNazwisko))
                {
                    var naz = row.P_nazwisko ?? string.Empty;
                    if (ci.IndexOf(naz, WybraneNazwisko.Trim(), opts) < 0) continue;
                }

                result.Add(new BadanieWrapper(row));
            }

            FilteredAvailableBadania = new ObservableCollection<BadanieWrapper>(result);
          }
          catch (Exception ex)
          {
              System.Windows.MessageBox.Show($"ApplyAvailableFilter ERROR:\n{ex.GetType().Name}\n{ex.Message}", "DEBUG");
          }
        }

        if (dispatcher?.CheckAccess() == true) Update();
        else dispatcher?.BeginInvoke(Update);
        
    }

    // ===== KOMENDY =====

    private RelayCommand? _refreshAvailableBadaniaCommand;
    public ICommand RefreshAvailableBadaniaCommand => _refreshAvailableBadaniaCommand
        ??= new RelayCommand(() => _ = LoadAvailableBadaniaAsync());

    private RelayCommand<BadanieRowDto>? _addWithoutEditCommand;
    public ICommand AddWithoutEditCommand => _addWithoutEditCommand
        ??= new RelayCommand<BadanieRowDto>(AddWithoutEdit);

    private void AddWithoutEdit(BadanieRowDto? dto)
    {
        if (dto == null) return;
        AddDtoToSelected(dto);
    }

    private RelayCommand? _addSelectedCommand;
    public ICommand AddSelectedCommand => _addSelectedCommand
        ??= new RelayCommand(AddSelectedMarked);

    private void AddSelectedMarked()
    {
        var marked = FilteredAvailableBadania
            .Where(w => w.IsMarked && w.Dto != null)
            .Select(w => w.Dto)
            .ToList();

        foreach (var dto in marked)
            AddDtoToSelected(dto);
    }

    private void AddDtoToSelected(BadanieRowDto dto)
    {
        // Sprawdź czy nie ma już na liście
        bool exists = dto.Bad_ID.HasValue
            ? SelectedLista.Badania.Any(b => b.Bad_ID == dto.Bad_ID)
            : SelectedLista.Badania.Any(b => ReferenceEquals(b, dto));
        if (exists) return;

        // Kopia z cenami
        var copy = new BadanieRowDto
        {
            Bad_ID        = dto.Bad_ID,
            Bad_L_ID      = dto.Bad_L_ID,
            Bad_F_ID      = dto.Bad_F_ID,
            Bad_P_ID      = dto.Bad_P_ID,
            Bad_Data      = dto.Bad_Data,
            Bad_Data_Do   = dto.Bad_Data_Do,
            Bad_Typ       = dto.Bad_Typ,
            Bad_Wynik     = dto.Bad_Wynik,
            Bad_Nr_KS     = dto.Bad_Nr_KS,
            Bad_END       = dto.Bad_END,
            Bad_bn_cennik = dto.Bad_bn_cennik,
            P_imie        = dto.P_imie,
            P_nazwisko    = dto.P_nazwisko,
            P_zawod       = dto.P_zawod,
            FirmaNazwa    = dto.FirmaNazwa,
            FirmaCennik   = dto.FirmaCennik,
            Bad_Razem     = dto.Bad_Razem,
            Bad_Cena1     = dto.Bad_Cena1,
            Bad_Cena2     = dto.Bad_Cena2,
            Bad_Cena3     = dto.Bad_Cena3,
            Bad_Cena4     = dto.Bad_Cena4,
            Bad_Cena5     = dto.Bad_Cena5,
            Bad_Cena6     = dto.Bad_Cena6,
            Bad_Cena7     = dto.Bad_Cena7,
            Bad_Cena8     = dto.Bad_Cena8,
            Bad_Cena9     = dto.Bad_Cena9,
            Bad_Cena10    = dto.Bad_Cena10,
        };

        SelectedLista.Badania.Add(copy);

        // Usuń z kolekcji dostępnych
        var existing = AvailableBadania.FirstOrDefault(b =>
            dto.Bad_ID.HasValue ? b.Bad_ID == dto.Bad_ID : ReferenceEquals(b, dto));
        if (existing != null) AvailableBadania.Remove(existing);

        RenumberAndRecalculate();
        ApplyAvailableFilter();
    }

    private RelayCommand? _markCommand;
    public ICommand MarkCommand => _markCommand ??= new RelayCommand(() =>
    {
        foreach (var w in FilteredAvailableBadania) w.IsMarked = true;
    });

    private RelayCommand? _clearGridCommand;
    public ICommand ClearGridCommand => _clearGridCommand ??= new RelayCommand(ClearGridFields);

    private RelayCommand? _saveSelectedAssignedBadanieCommand;
    public ICommand SaveSelectedAssignedBadanieCommand => _saveSelectedAssignedBadanieCommand
        ??= new RelayCommand(SaveSelectedAssignedBadanie);

    private void SaveSelectedAssignedBadanie()
    {
        if (SelectedAssignedBadanie == null) return;

        // Przelicz sumę razem z edytowalnych pól
        var ba = SelectedAssignedBadanie;
        ba.Bad_Cena1 = ParseEditable(EditablePriceBasic);
        ba.Bad_Cena2 = ParseEditable(EditablePriceLaryngologist);
        ba.Bad_Cena3 = ParseEditable(EditablePriceOphthalmologist);
        ba.Bad_Cena4 = ParseEditable(EditablePriceSanitary);
        ba.Bad_Cena5 = ParseEditable(EditablePriceLipidogram);
        ba.Bad_Cena6 = ParseEditable(EditablePriceEKG);
        ba.Bad_Cena7 = ParseEditable(EditablePriceHealthClinic);
        ba.Bad_Cena8 = ParseEditable(EditablePriceOther);
        ba.Bad_Typ   = TypBadania;
        ba.Bad_Data  = DataBadania;
        ba.Bad_Data_Do = DataWaznosci;
        ba.Bad_Wynik = SelectedWynik;
        ba.Bad_Nr_KS = NrKsiegi;

        var razem = (ba.Bad_Cena1 ?? 0) + (ba.Bad_Cena2 ?? 0) + (ba.Bad_Cena3 ?? 0)
                  + (ba.Bad_Cena4 ?? 0) + (ba.Bad_Cena5 ?? 0) + (ba.Bad_Cena6 ?? 0)
                  + (ba.Bad_Cena7 ?? 0) + (ba.Bad_Cena8 ?? 0);
        ba.Bad_Razem = razem;

        RecalculateTotalRazem();
        ClearGridFields();
    }

    private RelayCommand<object>? _deleteAssignedBadanieCommand;
    public ICommand DeleteAssignedBadanieCommand => _deleteAssignedBadanieCommand
        ??= new RelayCommand<object>(DeleteAssignedBadanie);

    private void DeleteAssignedBadanie(object? param)
    {
        BadanieRowDto? dto = param as BadanieRowDto;
        if (dto == null) return;

        SelectedLista.Badania.Remove(dto);

        // Przywróć do dostępnych
        bool alreadyAvailable = dto.Bad_ID.HasValue
            ? AvailableBadania.Any(b => b.Bad_ID == dto.Bad_ID)
            : AvailableBadania.Contains(dto);

        if (!alreadyAvailable)
        {
            dto.Bad_L_ID = null;
            AvailableBadania.Insert(0, dto);
        }

        RenumberAndRecalculate();
        ApplyAvailableFilter();
    }

    private RelayCommand? _cancelSaveCommand;
    public ICommand CancelSaveCommand => _cancelSaveCommand ??= new RelayCommand(CancelAllAssignedToAvailable);

    private void CancelAllAssignedToAvailable()
    {
        if (SelectedLista.Badania.Count == 0) return;

        var toMove = SelectedLista.Badania.ToList();
        foreach (var dto in toMove)
        {
            bool alreadyIn = dto.Bad_ID.HasValue
                ? AvailableBadania.Any(b => b.Bad_ID == dto.Bad_ID)
                : AvailableBadania.Contains(dto);
            if (!alreadyIn) { dto.Bad_L_ID = null; AvailableBadania.Insert(0, dto); }
        }

        SelectedLista.Badania.Clear();
        RenumberAndRecalculate();
        ApplyAvailableFilter();
    }

    private RelayCommand? _newCommand;
    public ICommand NewCommand => _newCommand ??= new RelayCommand(ExecuteNewList);

    private void ExecuteNewList()
    {
        SelectedLista = new ListyBadanDto();
        WybranaFirma = null;
        SelectedFirmaDto = null;
        NumerFaktury = null;
        DataWystawienia = DateTime.Today;
        SumaBrutto = null;
        Uwagi = null;
        FKemail = null;
        ClearGridFields();
        _ = Task.Run(LoadAvailableBadaniaAsync);
        RequestResetSelectionState?.Invoke();
        OnPropertyChanged(nameof(WydrukiVisibility));
    }

    private RelayCommand? _saveListCommand;
    public ICommand SaveListCommand => _saveListCommand ??= new RelayCommand(async () => await SaveListAsync());

    private async Task SaveListAsync()
    {
        if (SelectedFirmaDto == null)
        {
            MessageBox.Show("Wybierz firmę przed zapisaniem listy.", "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        if (SelectedLista.Badania.Count == 0)
        {
            MessageBox.Show("Lista jest pusta — dodaj badania przed zapisaniem.", "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_scopeFactory == null)
        {
            MessageBox.Show("Brak dostępu do usług aplikacji.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        IsBusy = true;
        BusyMessage = "Zapisywanie listy...";
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var svc = scope.ServiceProvider.GetRequiredService<IFakturaService>();

            var request = new SaveListaBadanRequest(
                FirmaId               : SelectedFirmaDto.Id,
                NumerFaktury          : NumerFaktury,
                DataWystawienia       : DataWystawienia ?? DateTime.Today,
                KwotaBrutto           : TotalRazem > 0 ? TotalRazem : null,
                Uwagi                 : Uwagi,
                NazwaCennika          : SelectedCennik,
                BadanieIds            : SelectedLista.Badania.Where(b => b.Bad_ID.HasValue).Select(b => b.Bad_ID!.Value),
                IstniejacaListaBadanId: SelectedLista.Identyfikator
            );

            var progress = new Progress<string>(msg => BusyMessage = msg);
            var result = await svc.SaveListaBadanAsync(request, progress);

            if (result.Success)
            {
                SelectedLista.Identyfikator = result.ListaBadanId;
                SaveButtonLabel = "Modyfikuj listę";
                OnPropertyChanged(nameof(WydrukiVisibility));
                MessageBox.Show("Lista zapisana pomyślnie.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show($"Błąd zapisu: {result.ErrorMessage}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Wyjątek podczas zapisu: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsBusy = false;
            BusyMessage = null;
        }
    }

    private RelayCommand? _sendEmailCommand;
    public ICommand SendEmailCommand => _sendEmailCommand ??= new RelayCommand(SendEmail);

    private void SendEmail()
    {
        if (string.IsNullOrWhiteSpace(FKemail))
        {
            MessageBox.Show("Brak adresu e-mail dla wybranej firmy.", "E-mail", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        // Otwórz domyślnego klienta e-mail
        try
        {
            var subject = Uri.EscapeDataString($"Lista badań {DataWystawienia:dd.MM.yyyy}");
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName        = $"mailto:{FKemail}?subject={subject}",
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Nie udało się otworzyć klienta e-mail: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    // Komendy druku — wywołują zdarzenia/akcje; rzeczywisty wydruk obsługuje View/code-behind
    private RelayCommand? _printSzczegolowyCommand;
    public ICommand PrintSzczegolowyCommand => _printSzczegolowyCommand ??= new RelayCommand(() =>
        MessageBox.Show("Wydruk 'Lista Ceny' — funkcja do implementacji.", "Druk", MessageBoxButton.OK, MessageBoxImage.Information));

    private RelayCommand? _printPodstawowyCommand;
    public ICommand PrintPodstawowyCommand => _printPodstawowyCommand ??= new RelayCommand(() =>
        MessageBox.Show("Wydruk 'Lista Razem' — funkcja do implementacji.", "Druk", MessageBoxButton.OK, MessageBoxImage.Information));

    private RelayCommand? _printListaCommand;
    public ICommand PrintListaCommand => _printListaCommand ??= new RelayCommand(() =>
        MessageBox.Show("Wydruk 'Lista' — funkcja do implementacji.", "Druk", MessageBoxButton.OK, MessageBoxImage.Information));

    // ===== HELPERS =====

    private void PopulateGridFromSelected()
    {
        if (SelectedAssignedBadanie == null) { ClearGridFields(); return; }

        var ba = SelectedAssignedBadanie;
        WybraneImie      = ba.P_imie;
        WybraneNazwisko  = ba.P_nazwisko;
        TypBadania       = ba.Bad_Typ;
        DataBadania      = ba.Bad_Data;
        DataWaznosci     = ba.Bad_Data_Do;
        SelectedWynik    = ba.Bad_Wynik;
        NrKsiegi         = ba.Bad_Nr_KS;

        _priceBasic           = ba.Bad_Cena1 ?? 0m;
        _priceLaryngologist   = ba.Bad_Cena2 ?? 0m;
        _priceOphthalmologist = ba.Bad_Cena3 ?? 0m;
        _priceSanitary        = ba.Bad_Cena4 ?? 0m;
        _priceLipidogram      = ba.Bad_Cena5 ?? 0m;
        _priceEKG             = ba.Bad_Cena6 ?? 0m;
        _priceHealthClinic    = ba.Bad_Cena7 ?? 0m;
        _priceOther           = ba.Bad_Cena8 ?? 0m;
        NotifyPriceTexts();

        EditablePriceBasic          = FormatEditable(_priceBasic);
        EditablePriceLaryngologist  = FormatEditable(_priceLaryngologist);
        EditablePriceOphthalmologist= FormatEditable(_priceOphthalmologist);
        EditablePriceSanitary       = FormatEditable(_priceSanitary);
        EditablePriceLipidogram     = FormatEditable(_priceLipidogram);
        EditablePriceEKG            = FormatEditable(_priceEKG);
        EditablePriceHealthClinic   = FormatEditable(_priceHealthClinic);
        EditablePriceOther          = FormatEditable(_priceOther);

        IsKsiazeczkaChecked = ba.Bad_Cena4 is > 0m;
        IsUrlopChecked      = ba.Bad_Cena7 is > 0m;
    }

    private void ClearGridFields()
    {
        WybraneImie = WybraneNazwisko = TypBadania = NrKsiegi = null;
        DataBadania  = DateTime.Today;
        DataWaznosci = DateTime.Today.AddYears(3);
        SelectedWynik = WynikOptions.FirstOrDefault();
        _selectedAssignedBadanie = null;
        OnPropertyChanged(nameof(SelectedAssignedBadanie));

        EditablePriceBasic = EditablePriceLaryngologist = EditablePriceOphthalmologist =
        EditablePriceSanitary = EditablePriceLipidogram = EditablePriceEKG =
        EditablePriceHealthClinic = EditablePriceOther = string.Empty;

        IsKsiazeczkaChecked = IsUrlopChecked = false;
        RequestResetSelectionState?.Invoke();
    }

    private void RecalculateTotalRazem()
    {
        TotalRazem = SelectedLista.Badania.Sum(b => b.Bad_Razem ?? 0m);
    }

    private void RenumberAndRecalculate()
    {
        for (int i = 0; i < SelectedLista.Badania.Count; i++)
            SelectedLista.Badania[i].Lp = i + 1;
        RecalculateTotalRazem();
        OnPropertyChanged(nameof(TotalRazem));
        OnPropertyChanged(nameof(WydrukiVisibility));
    }

    private decimal ParseEditable(string? s)
    {
        if (string.IsNullOrWhiteSpace(s)) return 0m;
        return decimal.TryParse(s, NumberStyles.Any, _pl, out var v) ? v : 0m;
    }

    private string FormatEditable(decimal v) =>
        v != 0m ? v.ToString("N2", _pl) : string.Empty;

    private static void Dispatch(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher?.CheckAccess() == true) action();
        else dispatcher?.BeginInvoke(action);
    }
}
