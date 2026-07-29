using ASMED.EDM.UI.Models;
using ASMED.EDM.UI.ViewModels.Skierowania;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Skierowania
{
    public partial class SkierPacjentaView : UserControl
    {
        public SkierPacjentaView()
        {
            InitializeComponent();
            Loaded += SkierPacjentaView_Loaded;
            IsVisibleChanged += SkierPacjentaView_IsVisibleChanged;
            DataContextChanged += SkierPacjentaView_DataContextChanged;
        }

        /// <summary>
        /// Tworzy widok i od razu ładuje wskazaną kartę badań pacjenta.
        /// </summary>
        public SkierPacjentaView(SkierowanieListItem skierowanie) : this()
        {
            Loaded += (s, e) =>
            {
                if (DataContext is SkierPacjentaViewModel vm)
                {
                    vm.LoadFromSkierowanie(skierowanie);
                }
            };
        }

        private void SkierPacjentaView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SkierPacjentaViewModel
                && !System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)
                && Application.Current is App app
                && app.Host != null)
            {
                var vm = app.Host.Services.GetRequiredService<SkierPacjentaViewModel>();
                vm.EditPatientRequested = EditPatient_Click;
                DataContext = vm;
            }
        }

        private void SkierPacjentaView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.OldValue is SkierPacjentaViewModel oldVm)
            {
                oldVm.RequestPowrot -= OnRequestPowrot;
            }

            if (e.NewValue is SkierPacjentaViewModel newVm)
            {
                newVm.RequestPowrot -= OnRequestPowrot;
                newVm.RequestPowrot += OnRequestPowrot;
            }
        }

        private void OnRequestPowrot()
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.SetKartyBadanContent(new SkierowaniaView());
            }
        }

        private void SkierPacjentaView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && DataContext is SkierPacjentaViewModel vm)
            {
                try { vm.RefreshAppointmentsFromDb(); } catch { }
            }
        }

        private void EditPatient_Click()
        {
            if (DataContext is not SkierPacjentaViewModel vm) return;

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
                mainWindow.SetKartyBadanContent(this);
            };

            mainWindow.SetKartyBadanContent(editView);
        }

        private void NowaKarta_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.SelectNowaKartaTab();
            }
        }

        private void ZakonczBadanie_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.SelectZakonczBadanieTab();
            }
        }

        private void ListaDoFaktur_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.SelectListaDoFakturTab();
            }
        }

        private void Rejestracja_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.SelectRejestracjaTab();
            }
        }
    }
}

