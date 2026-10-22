using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Enums;
using ASMED.EDM.Core.Interfaces.Services;
using ASMED.EDM.UI.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ASMED.EDM.UI.ViewModels.Orzeczenie;

/// <summary>
/// Pozycja listy wyboru lekarza (orzecznika) dostępna dla administratora.
/// </summary>
public sealed class OrzecznikOption
{
    public OrzecznikOption(Doctor doctor, string displayName)
    {
        Doctor = doctor;
        DisplayName = displayName;
    }

    public Doctor Doctor { get; }

    public int DoctorId => Doctor.Id;

    public int UserId => Doctor.UserId;

    public string DisplayName { get; }

    public string Label => string.IsNullOrWhiteSpace(Doctor.MedicalLicenseNumber)
        ? DisplayName
        : $"{DisplayName}  (PWZ {Doctor.MedicalLicenseNumber})";

    public override string ToString() => Label;
}

/// <summary>
/// Sekcja "Dane Orzecznika" — powiązanie zalogowanego lekarza (lub lekarza wybranego
/// przez administratora) z jego profilem EDM / P1.
/// </summary>
public partial class OrzeczenieViewModel
{
    private readonly IServiceScopeFactory? _scopeFactory;
    private readonly ICurrentUserService? _currentUserService;
    private readonly IDoctorEdmProfileDialogService? _doctorEdmProfileDialogService;
    private bool _isLoadingOrzecznik;
    private bool _suppressLekarzSelection;

    /// <summary>Id użytkownika (Users.Id), którego dane EDM są aktualnie pokazane.</summary>
    private int? _orzecznikUserId;

    /// <summary>Id profilu lekarza (Doctors.Id) orzecznika; null gdy brak profilu.</summary>
    [ObservableProperty]
    private int? _orzecznikDoctorId;

    [ObservableProperty]
    private string _orzecznikImieNazwisko = string.Empty;

    [ObservableProperty]
    private string _orzecznikPwz = "—";

    [ObservableProperty]
    private string _orzecznikSpecjalizacja = "—";

    [ObservableProperty]
    private string _orzecznikIdIzby = "—";

    [ObservableProperty]
    private string _orzecznikStatus = string.Empty;

    /// <summary>Czy orzecznik ma komplet danych wymaganych do podpisu w EDM.</summary>
    [ObservableProperty]
    private bool _isOrzecznikGotowy;

    /// <summary>Czy można uzupełnić/edytować dane EDM pokazywanego lekarza.</summary>
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(EdytujDaneOrzecznikaCommand))]
    private bool _canEdytujDaneOrzecznika;

    /// <summary>Czy zalogowany użytkownik (administrator) może wybrać lekarza z listy.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsOrzecznikNazwaWidoczna))]
    private bool _isWyborLekarzaDostepny;

    /// <summary>Imię i nazwisko jako tekst — gdy nie ma listy wyboru lekarza.</summary>
    public bool IsOrzecznikNazwaWidoczna => !IsWyborLekarzaDostepny;

    /// <summary>Lista lekarzy z profilem EDM (tylko dla administratora).</summary>
    public ObservableCollection<OrzecznikOption> DostepniLekarze { get; } = new();

    [ObservableProperty]
    private OrzecznikOption? _selectedLekarz;

    partial void OnSelectedLekarzChanged(OrzecznikOption? value)
    {
        if (_suppressLekarzSelection || !IsWyborLekarzaDostepny)
        {
            return;
        }

        if (value == null)
        {
            SetOrzecznikBrak(string.Empty, "⚠ Wybierz lekarza (orzecznika) z listy.", canEdit: false);
            return;
        }

        ApplyOrzecznik(value.Doctor, value.DisplayName);
    }

    /// <summary>
    /// Ponownie wczytuje dane orzecznika dla aktualnie zalogowanego użytkownika
    /// (wywoływane przy tworzeniu VM i przy każdym pokazaniu widoku).
    /// </summary>
    public async void RefreshOrzecznik()
    {
        try
        {
            await LoadOrzecznikAsync();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "OrzeczenieViewModel: błąd wczytywania danych orzecznika");
            SetOrzecznikBrak(OrzecznikImieNazwisko, "❌ Błąd wczytywania danych lekarza", canEdit: false);
        }
    }

    private async Task LoadOrzecznikAsync()
    {
        if (_isLoadingOrzecznik)
        {
            return;
        }

        _isLoadingOrzecznik = true;
        try
        {
            var user = _currentUserService?.CurrentUser;
            if (user == null)
            {
                ClearLekarzeList();
                IsWyborLekarzaDostepny = false;
                SetOrzecznikBrak(string.Empty, "Brak zalogowanego użytkownika", canEdit: false);
                return;
            }

            if (user.Role == UserRole.Administrator)
            {
                await LoadLekarzeForAdministratorAsync(user);
            }
            else
            {
                ClearLekarzeList();
                IsWyborLekarzaDostepny = false;
                await LoadOwnOrzecznikAsync(user);
            }
        }
        finally
        {
            _isLoadingOrzecznik = false;
        }
    }

    private async Task LoadOwnOrzecznikAsync(User user)
    {
        var displayName = GetUserDisplayName(user);

        Doctor? doctor = null;
        if (_scopeFactory != null && user.Id > 0)
        {
            using var scope = _scopeFactory.CreateScope();
            var doctorService = scope.ServiceProvider.GetRequiredService<IDoctorService>();
            doctor = await doctorService.GetDoctorByUserIdAsync(user.Id);
        }

        if (doctor != null)
        {
            ApplyOrzecznik(doctor, displayName);
            return;
        }

        if (user.Role == UserRole.Doctor && user.Id > 0)
        {
            SetOrzecznikBrak(displayName,
                "⚠ Brak danych EDM lekarza — uzupełnij je, aby móc podpisywać orzeczenia.",
                canEdit: _doctorEdmProfileDialogService != null);
            _orzecznikUserId = user.Id;
        }
        else
        {
            SetOrzecznikBrak(displayName,
                "⚠ Zalogowany użytkownik nie jest lekarzem — orzeczenie może podpisać tylko lekarz.",
                canEdit: false);
        }
    }

    private async Task LoadLekarzeForAdministratorAsync(User admin)
    {
        var options = new List<OrzecznikOption>();

        if (_scopeFactory != null)
        {
            using var scope = _scopeFactory.CreateScope();
            var doctorService = scope.ServiceProvider.GetRequiredService<IDoctorService>();
            var userService = scope.ServiceProvider.GetRequiredService<IUserService>();

            var doctors = await doctorService.GetAllDoctorsAsync();
            var activeUsers = (await userService.GetActiveUsersAsync()).ToDictionary(u => u.Id);

            foreach (var doctor in doctors)
            {
                if (!activeUsers.TryGetValue(doctor.UserId, out var doctorUser))
                {
                    continue;
                }

                var name = GetUserDisplayName(doctorUser);
                if (!string.IsNullOrWhiteSpace(doctor.Title))
                {
                    name = $"{doctor.Title} {name}";
                }

                options.Add(new OrzecznikOption(doctor, name));
            }
        }

        var previousDoctorId = SelectedLekarz?.DoctorId;

        _suppressLekarzSelection = true;
        try
        {
            DostepniLekarze.Clear();
            foreach (var option in options.OrderBy(o => o.DisplayName, StringComparer.CurrentCultureIgnoreCase))
            {
                DostepniLekarze.Add(option);
            }

            IsWyborLekarzaDostepny = true;

            // Zachowaj poprzedni wybór; domyślnie własny profil administratora (jeśli jest lekarzem).
            SelectedLekarz =
                DostepniLekarze.FirstOrDefault(o => o.DoctorId == previousDoctorId)
                ?? DostepniLekarze.FirstOrDefault(o => admin.Id > 0 && o.UserId == admin.Id);
        }
        finally
        {
            _suppressLekarzSelection = false;
        }

        if (SelectedLekarz != null)
        {
            ApplyOrzecznik(SelectedLekarz.Doctor, SelectedLekarz.DisplayName);
        }
        else if (DostepniLekarze.Count == 0)
        {
            SetOrzecznikBrak(string.Empty,
                "⚠ Brak lekarzy z danymi EDM — nadaj użytkownikowi rolę Doctor w Ustawienia → Użytkownicy.",
                canEdit: false);
        }
        else
        {
            SetOrzecznikBrak(string.Empty, "⚠ Wybierz lekarza (orzecznika) z listy.", canEdit: false);
        }
    }

    private void ApplyOrzecznik(Doctor doctor, string displayName)
    {
        _orzecznikUserId = doctor.UserId;
        OrzecznikDoctorId = doctor.Id;
        OrzecznikImieNazwisko = string.IsNullOrWhiteSpace(doctor.Title) || displayName.StartsWith(doctor.Title, StringComparison.Ordinal)
            ? displayName
            : $"{doctor.Title} {displayName}";
        OrzecznikPwz = ValueOrDash(doctor.MedicalLicenseNumber);
        OrzecznikSpecjalizacja = ValueOrDash(doctor.Specialization);
        OrzecznikIdIzby = ValueOrDash(doctor.IdIzbyLekarskiej);
        CanEdytujDaneOrzecznika = _doctorEdmProfileDialogService != null;

        var braki = new List<string>();
        if (string.IsNullOrWhiteSpace(doctor.MedicalLicenseNumber)) braki.Add("PWZ");
        if (string.IsNullOrEmpty(doctor.DoctorCertificatePassword)) braki.Add("hasło certyfikatu");
        if (!doctor.TotpEnabled || string.IsNullOrEmpty(doctor.TotpSecretKeyEncrypted)) braki.Add("parowanie authenticatora");

        IsOrzecznikGotowy = braki.Count == 0;
        OrzecznikStatus = IsOrzecznikGotowy
            ? "✔ Dane kompletne — gotowy do podpisu w EDM"
            : $"⚠ Brakuje: {string.Join(", ", braki)}";
    }

    private void SetOrzecznikBrak(string displayName, string status, bool canEdit)
    {
        _orzecznikUserId = null;
        OrzecznikDoctorId = null;
        OrzecznikImieNazwisko = displayName;
        OrzecznikPwz = "—";
        OrzecznikSpecjalizacja = "—";
        OrzecznikIdIzby = "—";
        OrzecznikStatus = status;
        IsOrzecznikGotowy = false;
        CanEdytujDaneOrzecznika = canEdit;
    }

    private void ClearLekarzeList()
    {
        _suppressLekarzSelection = true;
        try
        {
            SelectedLekarz = null;
            DostepniLekarze.Clear();
        }
        finally
        {
            _suppressLekarzSelection = false;
        }
    }

    private static string GetUserDisplayName(User user)
    {
        var name = $"{user.FirstName} {user.LastName}".Trim();
        return string.IsNullOrEmpty(name) ? user.Username : name;
    }

    private static string ValueOrDash(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    [RelayCommand(CanExecute = nameof(CanEdytujDaneOrzecznika))]
    private void EdytujDaneOrzecznika()
    {
        if (_orzecznikUserId is not int userId || userId <= 0 || _doctorEdmProfileDialogService == null)
        {
            return;
        }

        var displayName = string.IsNullOrWhiteSpace(OrzecznikImieNazwisko) ? $"Użytkownik #{userId}" : OrzecznikImieNazwisko;
        _doctorEdmProfileDialogService.ShowDialog(userId, displayName);
        RefreshOrzecznik();
    }
}
