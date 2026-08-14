using ASMED.EDM.UI.Models;
using ASMED.EDM.UI.ViewModels.Skierowania;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.UI.Xaml.Grid;
using System;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace ASMED.EDM.UI.Views.Skierowania
{
    public partial class SkierListaPacjentowView : UserControl
    {
        private SkierListaPacjentowViewModel? _vm;

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

                if (_vm != null)
                    _vm.PacjenciFiltered.CollectionChanged -= PacjenciFiltered_CollectionChanged;

                _vm = vm;
                _vm.PacjenciFiltered.CollectionChanged += PacjenciFiltered_CollectionChanged;

                vm.RefreshList();
            }

            if (lista_pacjentów_ADD_karty_badań is SfDataGrid grid)
            {
                grid.FilterChanged -= Grid_FilterChanged;
                grid.FilterChanged += Grid_FilterChanged;
            }

            UpdateRecordCount();
        }

        private void PacjenciFiltered_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Odczekaj aż SfDataGrid przetworzy zmianę kolekcji i zastosuje własne filtry kolumn
            Dispatcher.BeginInvoke(new Action(UpdateRecordCount), DispatcherPriority.Background);
        }

        private void Grid_FilterChanged(object? sender, GridFilterEventArgs e)
        {
            UpdateRecordCount();
        }

        /// <summary>
        /// Aktualizuje licznik rekordów uwzględniając zarówno filtr tekstowy (ViewModel),
        /// jak i filtry kolumn nałożone bezpośrednio na SfDataGrid.
        /// </summary>
        private void UpdateRecordCount()
        {
            if (txtRecordCount == null || _vm == null)
                return;

            int total = _vm.Pacjenci.Count;
            int visible = (lista_pacjentów_ADD_karty_badań as SfDataGrid)?.View?.Records?.Count
                          ?? _vm.PacjenciFiltered.Count;

            txtRecordCount.Text = $"{visible} / {total}";
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
        private void prinlist_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SkierListaPacjentowViewModel vm)
            {
                try { vm.PrintList(GetVisibleItems()); } catch { }
            }
        }
        private void exportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SkierListaPacjentowViewModel vm)
            {
                try { vm.ExportToExcel(GetVisibleItems()); } catch { }
            }
        }

        /// <summary>
        /// Zwraca rekordy aktualnie widoczne w tabeli (po uwzgl\u0119dnieniu
        /// filtr\u00F3w kolumn SfDataGrid), a nie tylko filtra tekstowego z ViewModelu.
        /// </summary>
        private System.Collections.Generic.List<Models.PacjentSkier>? GetVisibleItems()
        {
            if (lista_pacjent\u00F3w_ADD_karty_bada\u0144 is SfDataGrid grid && grid.View?.Records != null)
            {
                return grid.View.Records
                    .Select(r => r.Data)
                    .OfType<Models.PacjentSkier>()
                    .ToList();
            }

            return null;
        }
    }
}
