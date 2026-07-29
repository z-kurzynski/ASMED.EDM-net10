using ASMED.EDM.UI.Models;
using ASMED.EDM.UI.ViewModels.Skierowania;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Skierowania
{
    public partial class SkierListaPacjentowView : UserControl
    {
        public SkierListaPacjentowView()
        {
            InitializeComponent();
            Loaded += SkierListaPacjentowView_Loaded;
        }

        // Konstruktor dla DI (gdy tworzony programowo z gotowym VM)
        public SkierListaPacjentowView(SkierListaPacjentowViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
            Loaded += SkierListaPacjentowView_Loaded;
        }

        private void SkierListaPacjentowView_Loaded(object sender, RoutedEventArgs e)
        {
            // DataContext dziedziczy MainWindowViewModel z rodzica – przypisz właściwy VM.
            if (DataContext is not SkierListaPacjentowViewModel
                && !System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)
                && Application.Current is App app
                && app.Host != null)
            {
                DataContext = app.Host.Services.GetRequiredService<SkierListaPacjentowViewModel>();
            }

            // Załaduj dane po przypisaniu DataContext
            if (DataContext is SkierListaPacjentowViewModel vm)
            {
                // Zawsze przypisz NavigateToEdit do bieżącej instancji widoku
                // (??= zostawiłoby starą instancję, gdy Window.GetWindow zwraca null)
                vm.NavigateToEdit     = OpenEditorInSameTab;
                vm.NavigateToReferral = OpenReferralInSameTab;
                vm.RefreshList();
            }
        }

        /// <summary>
        /// Podmienia zawartość zakładki "Nowa Karta" na edytor pacjenta.
        /// Po powrocie (RequestPowrot) przywraca listę i odświeża dane.
        /// </summary>
        private void OpenEditorInSameTab(SkierPacjentaEditViewModel editVm)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null) return;

            var editView = new SkierPacjentaEditView(editVm);

            // Po kliknięciu Anuluj / Zapisz – wróć do listy
            editVm.RequestPowrot += () =>
            {
                mainWindow.SetNowaKartaContent(new SkierListaPacjentowView(
                    (SkierListaPacjentowViewModel)DataContext!));
            };

            mainWindow.SetNowaKartaContent(editView);
        }

        /// <summary>
        /// Podmienia zawartość zakładki "Nowa Karta" na formularz nowej karty badań.
        /// Po powrocie (RequestPowrot) przywraca listę i odświeża dane.
        /// </summary>
        private void OpenReferralInSameTab(Models.PacjentSkier pacjent)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null) return;

            if (Application.Current is not App app || app.Host == null) return;

            var referralVm = app.Host.Services
                .GetRequiredService<SkierNewPacjentaViewModel>();

            referralVm.LoadPacjent(pacjent);

            var referralView = new SkierNewPacjentaView { DataContext = referralVm };

            referralVm.RequestPowrot += () =>
            {
                mainWindow.SetNowaKartaContent(new SkierListaPacjentowView(
                    (SkierListaPacjentowViewModel)DataContext!));
            };

            mainWindow.SetNowaKartaContent(referralView);
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SkierListaPacjentowViewModel vm)
                vm.SearchText = string.Empty;
        }

        private void RefreshList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is SkierListaPacjentowViewModel vm)
                {
                    vm.SearchText = string.Empty;

                    if (FindName("txtSearchPacjent") is TextBox searchBox)
                        searchBox.Text = string.Empty;

                    vm.RefreshList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd odświeżania listy:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WrocLista_Click(object sender, RoutedEventArgs e)
        {
            // TODO: nawigacja wstecz
        }

        private void Rejestracja_Click(object sender, RoutedEventArgs e)
        {
            // TODO: przełączenie na zakładkę Rejestracja
        }
    }
}
