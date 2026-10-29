using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using ASMED.EDM.Core.Helpers;
using ASMED.EDM.UI.Models.Orzeczenie;
using ASMED.EDM.UI.Services;
using ASMED.EDM.UI.ViewModels.Badania;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.UI.ViewModels.Orzeczenie;

/// <summary>
/// Formularz orzeczenia (zakładki Orzeczenie / Laryngolog / Okulista / Sanitarne / Urlop),
/// podpis lekarza i zatwierdzenie (zapis całości do bazy przez OrzeczenieService).
/// Przebieg: wypełnienie → "Podpisz" (walidacja + migawka danych, blokada edycji)
/// → "Zatwierdź" (zapis w jednej transakcji, nadanie numeru, status wizyty "Orzeczenie").
/// </summary>
public partial class OrzeczenieViewModel
{
    private const string DomyslnyWynikSanitarny = "Pozytywny";

    private OrzeczenieService? _orzeczenieService;
    private OrzeczenieZapisDto? _podpisanyDto;
    private int? _formularzBId;
    private bool _formularzZBazy;
    private int _formularzLoadVersion;

    private OrzeczenieService? OrzeczenieSvc =>
        _orzeczenieService ??= _dbFactory != null ? new OrzeczenieService(_dbFactory) : null;

    // ── Zakładka Orzeczenie ─────────────────────────────────────────────────

    /// <summary>Werdykt — symbol z WynikBadania: "21" zdolny / "22" niezdolny / "23" utracił zdolność.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWerdykt21), nameof(IsWerdykt22), nameof(IsWerdykt23))]
    private string _werdykt = WynikBadania.BrakPrzeciwwskazan;

    public bool IsWerdykt21
    {
        get => Werdykt == WynikBadania.BrakPrzeciwwskazan;
        set { if (value) Werdykt = WynikBadania.BrakPrzeciwwskazan; }
    }

    public bool IsWerdykt22
    {
        get => Werdykt == WynikBadania.PrzeciwwskazaniaZdrowotne;
        set { if (value) Werdykt = WynikBadania.PrzeciwwskazaniaZdrowotne; }
    }

    public bool IsWerdykt23
    {
        get => Werdykt == WynikBadania.UtrataZdolnosci;
        set { if (value) Werdykt = WynikBadania.UtrataZdolnosci; }
    }

    /// <summary>Data utraty zdolności do pracy (tylko dla symbolu 23).</summary>
    [ObservableProperty]
    private DateTime? _dataUtratyZdolnosci;

    [ObservableProperty]
    private string _uzasadnienie = string.Empty;

    /// <summary>Numer orzeczenia z rejestru (gdy orzeczenie było już zatwierdzone).</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(OrzeczenieNumerInfo))]
    private string? _orzeczenieNumer;

    public string OrzeczenieNumerInfo => string.IsNullOrWhiteSpace(OrzeczenieNumer)
        ? string.Empty
        : $"Nr {OrzeczenieNumer}";

    /// <summary>O_ID ostatnio zatwierdzonego orzeczenia (pod drukowanie).</summary>
    [ObservableProperty]
    private int? _ostatnieZatwierdzoneOrzeczenieId;

    // ── Zakładka Laryngolog (pola ukryte w UI zachowują wartości domyślne) ───

    [ObservableProperty] private bool _larWykonano = true;
    [ObservableProperty] private bool _larVhiWypelniona;
    [ObservableProperty] private string _larVhiOpis = string.Empty;
    [ObservableProperty] private bool _larKrtanPrawidlowa = true;
    [ObservableProperty] private bool _larNosogardloPrawidlowe = true;
    [ObservableProperty] private bool _larSluchPrawidlowy = true;
    [ObservableProperty] private bool _larBlonyPrawidlowe = true;
    [ObservableProperty] private bool _larRownowagaPrawidlowa = true;
    [ObservableProperty] private bool _larPrzeciwwskazaniaGlos;
    [ObservableProperty] private bool _larWynikPrawidlowy = true;
    [ObservableProperty] private string _larUwagi = string.Empty;

    // ── Zakładka Okulista (pola ukryte w UI zachowują wartości domyślne) ─────

    [ObservableProperty] private bool _okuWykonano = true;
    [ObservableProperty] private bool _okuBarwyPrawidlowe = true;
    [ObservableProperty] private bool _okuPolePrawidlowe = true;
    [ObservableProperty] private bool _okuObuocznePrawidlowe = true;
    [ObservableProperty] private bool _okuWynikPrawidlowy = true;
    [ObservableProperty] private bool _okuPrzeciwwskazaniaMonitor;
    [ObservableProperty] private bool _okuUzywaOkularow;
    [ObservableProperty] private string _okuUwagi = string.Empty;

    // ── Zakładka Bad. sanitarne ─────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSanitWynikPozytywny), nameof(IsSanitWynikNegatywny), nameof(IsSanitWynikWymagaUzupelnienia))]
    private string _sanitarneWynik = DomyslnyWynikSanitarny;

    public bool IsSanitWynikPozytywny
    {
        get => SanitarneWynik == DomyslnyWynikSanitarny;
        set { if (value) SanitarneWynik = DomyslnyWynikSanitarny; }
    }

    public bool IsSanitWynikNegatywny
    {
        get => SanitarneWynik == "Negatywny";
        set { if (value) SanitarneWynik = "Negatywny"; }
    }

    public bool IsSanitWynikWymagaUzupelnienia
    {
        get => SanitarneWynik == "Wymaga uzupełnienia";
        set { if (value) SanitarneWynik = "Wymaga uzupełnienia"; }
    }

    [ObservableProperty] private string _sanitarneUwagi = string.Empty;

    // ── Zakładka Urlop ──────────────────────────────────────────────────────

    [ObservableProperty] private string _urlopUzasadnienie = string.Empty;

    // ── Podpis / zatwierdzenie ──────────────────────────────────────────────

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEdycjaMozliwa), nameof(IsNiePodpisane), nameof(PodpiszLabel))]
    [NotifyCanExecuteChangedFor(nameof(ZatwierdzCommand))]
    private bool _isPodpisane;

    [ObservableProperty]
    private string _podpisInfo = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsEdycjaMozliwa))]
    [NotifyCanExecuteChangedFor(nameof(ZatwierdzCommand), nameof(PodpiszCommand))]
    private bool _isZapisywanie;

    public bool IsNiePodpisane => !IsPodpisane;

    public string PodpiszLabel => IsPodpisane ? "Cofnij podpis" : "Podpisz";

    /// <summary>
    /// Czy formularz orzeczenia można edytować: wybrana wizyta, brak podpisu, brak trwającego zapisu.
    /// </summary>
    public bool IsEdycjaMozliwa => SelectedWizyta != null && !IsPodpisane && !IsZapisywanie;

    partial void OnOrzecznikDoctorIdChanged(int? value)
    {
        if (IsPodpisane && value != _podpisanyDto?.DoctorId)
        {
            CofnijPodpis();
        }
    }

    private bool CanPodpisz() => SelectedWizyta != null && !IsZapisywanie;

    [RelayCommand(CanExecute = nameof(CanPodpisz))]
    private void Podpisz()
    {
        if (IsPodpisane)
        {
            CofnijPodpis();
            return;
        }

        var bledy = WalidujOrzeczenie();
        if (bledy.Count > 0)
        {
            MessageBox.Show(
                "Nie można podpisać orzeczenia:\n\n• " + string.Join("\n• ", bledy),
                "Podpis orzeczenia", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var teraz = DateTime.Now;
        _podpisanyDto = BuildZapisDto(teraz);
        IsPodpisane = true;
        PodpisInfo = $"✔ Podpisano: {_podpisanyDto.Lekarz} (PWZ {_podpisanyDto.LekarzPwz}), {teraz:dd.MM.yyyy HH:mm} — zatwierdź, aby zapisać.";
        _logger?.LogInformation("Orzeczenie podpisane (B_ID={B_ID}, Doctor_ID={DoctorId})", _podpisanyDto.BId, _podpisanyDto.DoctorId);
    }

    private void CofnijPodpis()
    {
        _podpisanyDto = null;
        IsPodpisane = false;
        PodpisInfo = string.Empty;
    }

    private bool CanExecuteZatwierdz() => SelectedWizyta != null && IsPodpisane && !IsZapisywanie;

    private async Task ExecuteZatwierdzAsync()
    {
        var wizyta = SelectedWizyta;
        var dto = _podpisanyDto;
        if (wizyta?.B_ID == null || dto == null || dto.BId != wizyta.B_ID)
        {
            return;
        }

        var svc = OrzeczenieSvc;
        if (svc == null)
        {
            _logger?.LogWarning("DbConnectionFactory not available, cannot save Orzeczenie");
            return;
        }

        IsZapisywanie = true;
        try
        {
            var (oId, numer) = await svc.ZatwierdzAsync(dto);

            OstatnieZatwierdzoneOrzeczenieId = oId;
            wizyta.R_Status = OrzeczenieService.StatusWizytyOrzeczenie;
            _logger?.LogInformation("Orzeczenie zatwierdzone: O_ID={OId}, Nr={Numer}, B_ID={B_ID}, R_ID={R_ID}", oId, numer, dto.BId, dto.RId);

            ToastHelper.ShowWindowsToast(
                "Orzeczenie zatwierdzone",
                $"Nr {numer} — {wizyta.P_nazwisko} {wizyta.P_imie}".Trim());

            // Pozycja ze statusem "Orzeczenie" nie spełnia już kryteriów filtra listy.
            ApplyFilter();
            SelectedWizyta = null;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Błąd zatwierdzania orzeczenia dla B_ID={B_ID}", dto.BId);
            MessageBox.Show(
                $"Nie udało się zapisać orzeczenia.\n\n{ex.Message}",
                "Zatwierdzenie orzeczenia", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            IsZapisywanie = false;
        }
    }

    private List<string> WalidujOrzeczenie()
    {
        var bledy = new List<string>();
        var w = SelectedWizyta;

        if (w?.B_ID == null)
        {
            bledy.Add("Nie wybrano pacjenta / skierowania.");
            return bledy;
        }

        if (string.IsNullOrWhiteSpace(w.B_typ))
            bledy.Add("Wybierz typ badania (Wstępne / Okresowe / Kontrolne).");

        if (TabDataBadania == null)
            bledy.Add("Uzupełnij datę badania.");

        if (w.B_DataSkierowania == null)
            bledy.Add("Uzupełnij datę skierowania.");
        else if (TabDataBadania != null && w.B_DataSkierowania.Value.Date > TabDataBadania.Value.Date)
            bledy.Add("Data skierowania nie może być późniejsza niż data badania.");

        if (OrzecznikDoctorId == null)
            bledy.Add("Brak lekarza orzecznika (profil lekarza).");
        else if (string.IsNullOrWhiteSpace(OrzecznikPwz) || OrzecznikPwz == "—")
            bledy.Add("Lekarz orzecznik nie ma numeru PWZ — uzupełnij „Dane EDM”.");

        if (IsWerdykt21 && TabDataBadania != null && WaznoscBadaniaDo.Date <= TabDataBadania.Value.Date)
            bledy.Add("Data następnego badania musi być późniejsza niż data badania.");

        if (IsWerdykt23 && DataUtratyZdolnosci == null)
            bledy.Add("Podaj datę utraty zdolności do pracy (symbol 23).");

        if (IsLaryngologActive && LaryngDataBadania == null)
            bledy.Add("Uzupełnij datę badania laryngologicznego.");

        if (IsOkulistaActive && OkuDataBadania == null)
            bledy.Add("Uzupełnij datę badania okulistycznego.");

        if (IsSanitarneActive && SanitarneDataBadania == null)
            bledy.Add("Uzupełnij datę badania sanitarno-epidemiologicznego.");

        if (IsUrlopActive)
        {
            if (UrlopDataOd == null || UrlopDataDo == null)
                bledy.Add("Uzupełnij okres urlopu dla poratowania zdrowia (od – do).");
            else if (UrlopDataOd.Value.Date > UrlopDataDo.Value.Date)
                bledy.Add("Data końca urlopu nie może być wcześniejsza niż data początku.");
        }

        return bledy;
    }

    private OrzeczenieZapisDto BuildZapisDto(DateTime podpisanoAt)
    {
        var w = SelectedWizyta!;
        var zdolny = IsWerdykt21;

        return new OrzeczenieZapisDto
        {
            BId = w.B_ID!.Value,
            RId = w.R_ID,
            PId = w.P_ID,
            Numer = OrzeczenieNumer,

            TypBadania = w.B_typ,
            Stanowisko = w.B_Stanowisko,
            DataBadania = TabDataBadania?.Date,
            DataSkierowania = w.B_DataSkierowania?.Date,
            DataNastepnegoBadania = zdolny ? WaznoscBadaniaDo.Date : null,
            Werdykt = Werdykt,
            DataUtratyZdolnosci = IsWerdykt23 ? DataUtratyZdolnosci?.Date : null,
            Uzasadnienie = Uzasadnienie,

            DoctorId = OrzecznikDoctorId,
            Lekarz = OrzecznikImieNazwisko,
            LekarzPwz = OrzecznikPwz,
            PodpisanoAt = podpisanoAt,
            CreatedByUserId = _currentUserService?.CurrentUser?.Id,

            Laryngolog = IsLaryngologActive,
            LarData = LaryngDataBadania?.Date,
            LarWykonano = LarWykonano,
            LarVhiWypelniona = LarVhiWypelniona,
            LarVhiOpis = LarVhiOpis,
            LarKrtanPrawidlowa = LarKrtanPrawidlowa,
            LarNosogardloPrawidlowe = LarNosogardloPrawidlowe,
            LarSluchPrawidlowy = LarSluchPrawidlowy,
            LarBlonyPrawidlowe = LarBlonyPrawidlowe,
            LarRownowagaPrawidlowa = LarRownowagaPrawidlowa,
            LarPrzeciwwskazaniaGlos = LarPrzeciwwskazaniaGlos,
            LarWynikPrawidlowy = LarWynikPrawidlowy,
            LarUwagi = LarUwagi,

            Okulista = IsOkulistaActive,
            OkuData = OkuDataBadania?.Date,
            OkuWykonano = OkuWykonano,
            OkuBarwyPrawidlowe = OkuBarwyPrawidlowe,
            OkuPolePrawidlowe = OkuPolePrawidlowe,
            OkuObuocznePrawidlowe = OkuObuocznePrawidlowe,
            OkuWynikPrawidlowy = OkuWynikPrawidlowy,
            OkuPrzeciwwskazaniaMonitor = OkuPrzeciwwskazaniaMonitor,
            OkuUzywaOkularow = OkuUzywaOkularow,
            OkuUwagi = OkuUwagi,

            Sanitarne = IsSanitarneActive,
            SanData = SanitarneDataBadania?.Date,
            SanWynik = SanitarneWynik,
            SanUwagi = SanitarneUwagi,

            Urlop = IsUrlopActive,
            UrlopOd = UrlopDataOd?.Date,
            UrlopDo = UrlopDataDo?.Date,
            UrlopUzasadnienie = UrlopUzasadnienie,
        };
    }

    // ── Reset / wczytanie formularza ────────────────────────────────────────

    /// <summary>
    /// Wywoływane przy zmianie wybranej wizyty. Ten sam rekord (np. po cichym odświeżeniu
    /// listy) nie resetuje formularza ani podpisu.
    /// </summary>
    private void OnWizytaFormularzChanged(Models.Orzeczenie.OrzeczenieListItem? value)
    {
        OnPropertyChanged(nameof(IsEdycjaMozliwa));
        PodpiszCommand.NotifyCanExecuteChanged();
        ZatwierdzCommand.NotifyCanExecuteChanged();

        var bId = value?.B_ID;
        if (bId != null && bId == _formularzBId)
        {
            return;
        }

        _formularzBId = bId;
        ResetFormularz();

        if (bId != null)
        {
            _ = WczytajFormularzAsync(bId.Value);
        }
    }

    private void ResetFormularz()
    {
        _formularzLoadVersion++;

        CofnijPodpis();
        OrzeczenieNumer = null;

        Werdykt = WynikBadania.BrakPrzeciwwskazan;
        DataUtratyZdolnosci = null;
        Uzasadnienie = string.Empty;

        ResetLaryngologPola();
        ResetOkulistaPola();
        ResetSanitarnePola();
        ResetUrlopPola();

        var dataDomyslna = CustomDefaultDate;
        TabDataBadania = dataDomyslna;
        LaryngDataBadania = dataDomyslna;
        OkuDataBadania = dataDomyslna;
        SanitarneDataBadania = dataDomyslna;

        if (_formularzZBazy)
        {
            UstawWaznosc((dataDomyslna ?? DateTime.Today).AddYears(3));
            _formularzZBazy = false;
        }
    }

    private void ResetLaryngologPola()
    {
        LarWykonano = true;
        LarVhiWypelniona = false;
        LarVhiOpis = string.Empty;
        LarKrtanPrawidlowa = true;
        LarNosogardloPrawidlowe = true;
        LarSluchPrawidlowy = true;
        LarBlonyPrawidlowe = true;
        LarRownowagaPrawidlowa = true;
        LarPrzeciwwskazaniaGlos = false;
        LarWynikPrawidlowy = true;
        LarUwagi = string.Empty;
    }

    private void ResetOkulistaPola()
    {
        OkuWykonano = true;
        OkuBarwyPrawidlowe = true;
        OkuPolePrawidlowe = true;
        OkuObuocznePrawidlowe = true;
        OkuWynikPrawidlowy = true;
        OkuPrzeciwwskazaniaMonitor = false;
        OkuUzywaOkularow = false;
        OkuUwagi = string.Empty;
    }

    private void ResetSanitarnePola()
    {
        SanitarneWynik = DomyslnyWynikSanitarny;
        SanitarneUwagi = string.Empty;
    }

    private void ResetUrlopPola()
    {
        UrlopDataOd = null;
        UrlopDataDo = null;
        UrlopUzasadnienie = string.Empty;
    }

    private void UstawWaznosc(DateTime data)
    {
        WaznoscBadaniaDo = data.Date;
        _validityYears = Math.Clamp(data.Year - (CustomDefaultDate ?? DateTime.Today).Year, 0, 5);
        OnPropertyChanged(nameof(ValidityYears));
    }

    /// <summary>
    /// Wczytuje istniejące orzeczenie (np. ponowna edycja) — gdy brak, formularz zostaje z wartościami domyślnymi.
    /// </summary>
    private async Task WczytajFormularzAsync(int bId)
    {
        var svc = OrzeczenieSvc;
        if (svc == null) return;

        var version = _formularzLoadVersion;
        OrzeczenieZapisDto? dto;
        try
        {
            dto = await svc.LoadAsync(bId);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Nie udało się wczytać orzeczenia dla B_ID={B_ID} (tabela może jeszcze nie istnieć)", bId);
            return;
        }

        // Użytkownik zdążył wybrać inną wizytę lub już podpisał — nie nadpisuj formularza.
        if (dto == null || version != _formularzLoadVersion || _formularzBId != bId || IsPodpisane)
        {
            return;
        }

        _formularzZBazy = true;
        OrzeczenieNumer = dto.Numer;

        Werdykt = string.IsNullOrWhiteSpace(dto.Werdykt) ? WynikBadania.BrakPrzeciwwskazan : dto.Werdykt;
        DataUtratyZdolnosci = dto.DataUtratyZdolnosci;
        Uzasadnienie = dto.Uzasadnienie ?? string.Empty;

        if (dto.DataBadania != null) TabDataBadania = dto.DataBadania;
        if (dto.DataSkierowania != null && SelectedWizyta != null)
        {
            SelectedWizyta.B_DataSkierowania = dto.DataSkierowania;
            OnPropertyChanged(nameof(SelectedWizyta));
        }
        if (dto.DataNastepnegoBadania != null) UstawWaznosc(dto.DataNastepnegoBadania.Value);

        if (dto.LarData != null) LaryngDataBadania = dto.LarData;
        LarWykonano = dto.LarWykonano;
        LarVhiWypelniona = dto.LarVhiWypelniona;
        LarVhiOpis = dto.LarVhiOpis ?? string.Empty;
        LarKrtanPrawidlowa = dto.LarKrtanPrawidlowa;
        LarNosogardloPrawidlowe = dto.LarNosogardloPrawidlowe;
        LarSluchPrawidlowy = dto.LarSluchPrawidlowy;
        LarBlonyPrawidlowe = dto.LarBlonyPrawidlowe;
        LarRownowagaPrawidlowa = dto.LarRownowagaPrawidlowa;
        LarPrzeciwwskazaniaGlos = dto.LarPrzeciwwskazaniaGlos;
        LarWynikPrawidlowy = dto.LarWynikPrawidlowy;
        LarUwagi = dto.LarUwagi ?? string.Empty;

        if (dto.OkuData != null) OkuDataBadania = dto.OkuData;
        OkuWykonano = dto.OkuWykonano;
        OkuBarwyPrawidlowe = dto.OkuBarwyPrawidlowe;
        OkuPolePrawidlowe = dto.OkuPolePrawidlowe;
        OkuObuocznePrawidlowe = dto.OkuObuocznePrawidlowe;
        OkuWynikPrawidlowy = dto.OkuWynikPrawidlowy;
        OkuPrzeciwwskazaniaMonitor = dto.OkuPrzeciwwskazaniaMonitor;
        OkuUzywaOkularow = dto.OkuUzywaOkularow;
        OkuUwagi = dto.OkuUwagi ?? string.Empty;

        if (dto.SanData != null) SanitarneDataBadania = dto.SanData;
        SanitarneWynik = string.IsNullOrWhiteSpace(dto.SanWynik) ? DomyslnyWynikSanitarny : dto.SanWynik;
        SanitarneUwagi = dto.SanUwagi ?? string.Empty;

        UrlopDataOd = dto.UrlopOd;
        UrlopDataDo = dto.UrlopDo;
        UrlopUzasadnienie = dto.UrlopUzasadnienie ?? string.Empty;
    }

    // ── Przyciski "Wyczyść" w zakładkach ────────────────────────────────────

    [RelayCommand]
    private void WyczyscLaryngolog()
    {
        if (IsPodpisane) return;
        ResetLaryngologPola();
        LaryngDataBadania = CustomDefaultDate;
    }

    [RelayCommand]
    private void WyczyscOkulista()
    {
        if (IsPodpisane) return;
        ResetOkulistaPola();
        OkuDataBadania = CustomDefaultDate;
    }

    [RelayCommand]
    private void WyczyscSanitarne()
    {
        if (IsPodpisane) return;
        ResetSanitarnePola();
        SanitarneDataBadania = CustomDefaultDate;
    }

    [RelayCommand]
    private void WyczyscUrlop()
    {
        if (IsPodpisane) return;
        ResetUrlopPola();
    }
}
