using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using ASMED.EDM.Core.Interfaces.Services;
using QRCoder;

namespace ASMED.EDM.UI.Views.Dialogs;

/// <summary>
/// Okno dialogowe autoryzacji podpisu lekarza (TOTP). Obsługuje zarówno pierwsze
/// parowanie z aplikacją authenticator (QR + potwierdzenie kodem), jak i zwykłą
/// weryfikację kodu przy kolejnych podpisach w ramach sesji.
/// </summary>
public partial class DoctorSignatureAuthDialog : Window
{
    private readonly IDoctorSignatureAuthorizationService _authService;
    private readonly int _doctorId;
    private readonly bool _isEnrollment;

    /// <summary>Tworzy dialog. Ustaw <paramref name="isEnrollment"/> na true przy pierwszym parowaniu.</summary>
    public DoctorSignatureAuthDialog(IDoctorSignatureAuthorizationService authService, int doctorId, bool isEnrollment)
    {
        InitializeComponent();

        _authService = authService;
        _doctorId = doctorId;
        _isEnrollment = isEnrollment;

        Loaded += DoctorSignatureAuthDialog_Loaded;
    }

    private async void DoctorSignatureAuthDialog_Loaded(object sender, RoutedEventArgs e)
    {
        if (_isEnrollment)
        {
            txtHeader.Text = "Sparuj aplikację authenticator, a następnie wpisz wygenerowany kod";
            pnlEnrollment.Visibility = Visibility.Visible;

            var enrollment = await _authService.BeginEnrollmentAsync(_doctorId);
            txtSecret.Text = enrollment.SecretBase32;
            imgQrCode.Source = GenerateQrCodeImage(enrollment.OtpAuthUri);
        }

        txtCode.Focus();
    }

    private static BitmapImage GenerateQrCodeImage(string payload)
    {
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(payload, QRCodeGenerator.ECCLevel.Q);
        var pngQrCode = new PngByteQRCode(qrData);
        var bytes = pngQrCode.GetGraphic(10);

        var bitmap = new BitmapImage();
        using var stream = new MemoryStream(bytes);
        bitmap.BeginInit();
        bitmap.CacheOption = BitmapCacheOption.OnLoad;
        bitmap.StreamSource = stream;
        bitmap.EndInit();
        bitmap.Freeze();
        return bitmap;
    }

    private async void btnConfirm_Click(object sender, RoutedEventArgs e)
    {
        var code = txtCode.Text?.Trim() ?? string.Empty;
        if (code.Length != 6)
        {
            ShowError("Kod musi składać się z 6 cyfr.");
            return;
        }

        bool success = _isEnrollment
            ? await _authService.ConfirmEnrollmentAsync(_doctorId, code)
            : await _authService.VerifyCodeAsync(_doctorId, code);

        if (!success)
        {
            ShowError("Nieprawidłowy kod. Spróbuj ponownie.");
            txtCode.Clear();
            txtCode.Focus();
            return;
        }

        DialogResult = true;
        Close();
    }

    private void btnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ShowError(string message)
    {
        txtError.Text = message;
        txtError.Visibility = Visibility.Visible;
    }
}
