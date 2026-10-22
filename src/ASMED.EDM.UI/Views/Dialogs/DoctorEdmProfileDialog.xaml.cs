using System.Windows;
using ASMED.EDM.Core.DTOs;
using ASMED.EDM.Core.Entities;
using ASMED.EDM.Core.Helpers;
using ASMED.EDM.Core.Interfaces.Services;
using Microsoft.Extensions.DependencyInjection;

namespace ASMED.EDM.UI.Views.Dialogs;

/// <summary>
/// Dialog uzupełniania danych lekarza wymaganych do autoryzacji w EDM / P1
/// (NPWZ, dane autora, hasło certyfikatu podpisu, parowanie TOTP).
/// DialogResult = true, jeśli profil lekarza został zapisany.
/// </summary>
public partial class DoctorEdmProfileDialog : Window
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly int _userId;
    private readonly int? _currentUserId;

    private int? _doctorId;
    private bool _totpEnabled;
    private bool _saved;
    private bool _syncingPassword;

    public DoctorEdmProfileDialog(IServiceScopeFactory scopeFactory, int userId, string userDisplayName, int? currentUserId)
    {
        InitializeComponent();

        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _userId = userId;
        _currentUserId = currentUserId;

        txtHeader.Text = $"🩺 Dane lekarza do autoryzacji w EDM — {userDisplayName}";

        Loaded += DoctorEdmProfileDialog_Loaded;
    }

    private async void DoctorEdmProfileDialog_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var doctorService = scope.ServiceProvider.GetRequiredService<IDoctorService>();
            var doctor = await doctorService.GetDoctorByUserIdAsync(_userId);

            if (doctor != null)
            {
                PopulateFrom(doctor);
            }
        }
        catch (Exception ex)
        {
            ShowError($"Nie udało się wczytać danych lekarza: {ex.GetBaseException().Message}");
        }

        UpdateTotpStatus();
        txtPwz.Focus();
    }

    private void PopulateFrom(Doctor doctor)
    {
        _doctorId = doctor.Id;
        _totpEnabled = doctor.TotpEnabled && !string.IsNullOrEmpty(doctor.TotpSecretKeyEncrypted);

        txtPwz.Text = doctor.MedicalLicenseNumber;
        cmbTitle.Text = doctor.Title ?? string.Empty;
        txtSpecialization.Text = doctor.Specialization ?? string.Empty;
        txtIdIzbyLekarskiej.Text = doctor.IdIzbyLekarskiej ?? string.Empty;
        txtAuthorMedycynaPracyId.Text = doctor.AuthorMedycynaPracyId ?? string.Empty;

        txtPasswordHint.Visibility = string.IsNullOrEmpty(doctor.DoctorCertificatePassword)
            ? Visibility.Collapsed
            : Visibility.Visible;
    }

    private void UpdateTotpStatus()
    {
        if (_doctorId is null)
        {
            txtTotpStatus.Text = "Najpierw zapisz dane lekarza — parowanie będzie dostępne po zapisie.";
            btnPairTotp.Content = "📱 Zapisz i sparuj aplikację authenticator";
        }
        else if (_totpEnabled)
        {
            txtTotpStatus.Text = "✅ Aplikacja authenticator jest sparowana.";
            btnPairTotp.Content = "📱 Sparuj ponownie (nowe urządzenie)";
        }
        else
        {
            txtTotpStatus.Text = "⚠️ Aplikacja authenticator nie jest sparowana — lekarz nie będzie mógł podpisywać dokumentów.";
            btnPairTotp.Content = "📱 Sparuj aplikację authenticator";
        }
    }

    private async Task<bool> SaveAsync()
    {
        HideError();

        var pwz = txtPwz.Text?.Trim() ?? string.Empty;
        if (!MedicalLicenseValidator.IsValidDoctorPwz(pwz))
        {
            ShowError("Nieprawidłowy numer PWZ lekarza. Wymagane 7 cyfr z poprawną cyfrą kontrolną.");
            txtPwz.Focus();
            return false;
        }

        var password = pwdCertificate.Password;
        var profile = new DoctorEdmProfileDto
        {
            MedicalLicenseNumber = pwz,
            Title = cmbTitle.Text,
            Specialization = txtSpecialization.Text,
            IdIzbyLekarskiej = txtIdIzbyLekarskiej.Text,
            AuthorMedycynaPracyId = txtAuthorMedycynaPracyId.Text,
            // Puste pole = bez zmiany zapisanego hasła
            DoctorCertificatePassword = string.IsNullOrEmpty(password) ? null : password,
            ModifiedById = _currentUserId
        };

        SetBusy(true);
        try
        {
            using var scope = _scopeFactory.CreateScope();
            var doctorService = scope.ServiceProvider.GetRequiredService<IDoctorService>();
            var doctor = await doctorService.SaveEdmProfileAsync(_userId, profile);

            _doctorId = doctor.Id;
            _totpEnabled = doctor.TotpEnabled && !string.IsNullOrEmpty(doctor.TotpSecretKeyEncrypted);
            _saved = true;

            if (!string.IsNullOrEmpty(doctor.DoctorCertificatePassword))
            {
                txtPasswordHint.Visibility = Visibility.Visible;
            }

            UpdateTotpStatus();
            return true;
        }
        catch (Exception ex)
        {
            ShowError($"Błąd zapisu: {ex.GetBaseException().Message}");
            return false;
        }
        finally
        {
            SetBusy(false);
        }
    }

    private bool PairTotp()
    {
        if (_doctorId is not int doctorId)
        {
            return false;
        }

        using var scope = _scopeFactory.CreateScope();
        var authService = scope.ServiceProvider.GetRequiredService<IDoctorSignatureAuthorizationService>();

        var dialog = new DoctorSignatureAuthDialog(authService, doctorId, isEnrollment: true)
        {
            Owner = this
        };

        var paired = dialog.ShowDialog() == true;

        // Parowanie wykonuje administrator — nie autoryzujemy podpisu lekarza w tej sesji.
        authService.ClearSessionAuthorization(doctorId);

        if (paired)
        {
            _totpEnabled = true;
            UpdateTotpStatus();
        }

        return paired;
    }

    private async void btnSave_Click(object sender, RoutedEventArgs e)
    {
        if (!await SaveAsync())
        {
            return;
        }

        if (!_totpEnabled)
        {
            var answer = MessageBox.Show(
                "Dane lekarza zostały zapisane.\n\nCzy sparować teraz aplikację authenticator (wymaganą do podpisu dokumentów w EDM)?",
                "Autoryzacja podpisu",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (answer == MessageBoxResult.Yes)
            {
                PairTotp();
            }
        }

        DialogResult = true;
        Close();
    }

    private async void btnPairTotp_Click(object sender, RoutedEventArgs e)
    {
        if (_doctorId is null && !await SaveAsync())
        {
            return;
        }

        PairTotp();
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = _saved;
        Close();
    }

    private void btnTogglePassword_Click(object sender, RoutedEventArgs e)
    {
        var reveal = pwdCertificate.Visibility == Visibility.Visible;
        pwdCertificate.Visibility = reveal ? Visibility.Collapsed : Visibility.Visible;
        txtCertificate.Visibility = reveal ? Visibility.Visible : Visibility.Collapsed;
        btnTogglePassword.Content = reveal ? "🙈" : "👁️";
    }

    private void pwdCertificate_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_syncingPassword) return;
        _syncingPassword = true;
        txtCertificate.Text = pwdCertificate.Password;
        _syncingPassword = false;
    }

    private void txtCertificate_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        if (_syncingPassword) return;
        _syncingPassword = true;
        pwdCertificate.Password = txtCertificate.Text;
        _syncingPassword = false;
    }

    private void SetBusy(bool busy)
    {
        btnSave.IsEnabled = !busy;
        btnPairTotp.IsEnabled = !busy;
        Cursor = busy ? System.Windows.Input.Cursors.Wait : null;
    }

    private void ShowError(string message)
    {
        txtError.Text = message;
        txtError.Visibility = Visibility.Visible;
    }

    private void HideError()
    {
        txtError.Text = string.Empty;
        txtError.Visibility = Visibility.Collapsed;
    }
}
