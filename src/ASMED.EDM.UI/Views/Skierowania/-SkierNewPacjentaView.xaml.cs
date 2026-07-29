using ASMED.EDM.UI.ViewModels.Skierowania;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Skierowania
{
    public partial class SkierNewPacjentaView : UserControl
    {
        public SkierNewPacjentaView()
        {
            InitializeComponent();
            Loaded += SkierNewPacjentaView_Loaded;
            IsVisibleChanged += SkierNewPacjentaView_IsVisibleChanged;
        }

        private void SkierNewPacjentaView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is SkierNewPacjentaViewModel vm)
            {
                vm.EditPatientRequested = EditPatient_Click;
                vm.RefreshAppointmentsFromDb();
            }
        }

        private void SkierNewPacjentaView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && DataContext is SkierNewPacjentaViewModel vm)
            {
                try { vm.RefreshAppointmentsFromDb(); } catch { }
            }
        }

        private void Rejestracja_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder
        }

        private void EditPatient_Click()
        {
            if (DataContext is not SkierNewPacjentaViewModel vm) return;

            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null) return;

            if (Application.Current is not App app || app.Host == null)
            {
                MessageBox.Show("Brak konfiguracji DI.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var editVm = app.Host.Services.GetService(typeof(SkierPacjentaEditViewModel)) as SkierPacjentaEditViewModel;
            if (editVm == null)
            {
                MessageBox.Show("Nie można uzyskać SkierPacjentaEditViewModel z kontenera DI.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            editVm.LoadPacjentById(vm.PatientId);

            var editView = new SkierPacjentaEditView(editVm);

            editVm.RequestPowrot += () =>
            {
                // Po powrocie przywróć ten widok (formularz karty badań)
                mainWindow.SetNowaKartaContent(this);
            };

            mainWindow.SetNowaKartaContent(editView);
        }

        private void KartyBadan_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder
        }

        private void ZakonczBadanie_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder
        }

        private void ListaDoFaktur_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder
        }
    }
}
