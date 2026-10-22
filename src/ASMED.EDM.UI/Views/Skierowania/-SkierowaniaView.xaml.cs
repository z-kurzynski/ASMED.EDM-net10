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
    public partial class SkierowaniaView : UserControl
    {
        private SkierowaniaViewModel? _vm;

        public SkierowaniaView()
        {
            InitializeComponent();
            Loaded += SkierowaniaView_Loaded;
            IsVisibleChanged += SkierowaniaView_IsVisibleChanged;
        }

        private void SkierowaniaView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SkierowaniaViewModel
                && !System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)
                && Application.Current is App app
                && app.Host != null)
            {
                DataContext = app.Host.Services.GetRequiredService<SkierowaniaViewModel>();
            }

            if (DataContext is SkierowaniaViewModel vm)
            {
                vm.RefreshFromDb();
                vm.NavigateToEdit = OpenSkierPacjentaCard;
                vm.NavigateToRejestracja = NavigateToRejestracjaTab;
                vm.NavigateToNowaKartaBadan = NavigateToNowaKartaTab;

                if (_vm != null)
                    _vm.SkierowaniaFiltered.CollectionChanged -= SkierowaniaFiltered_CollectionChanged;

                _vm = vm;
                _vm.SkierowaniaFiltered.CollectionChanged += SkierowaniaFiltered_CollectionChanged;
            }

            if (Lista_kart_Badan is SfDataGrid grid)
            {
                grid.FilterChanged -= Grid_FilterChanged;
                grid.FilterChanged += Grid_FilterChanged;
            }

            UpdateRecordCount();
        }

        private void SkierowaniaFiltered_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            // Odczekaj aż SfDataGrid przetworzy zmianę kolekcji i zastosuje własne filtry kolumn
            Dispatcher.BeginInvoke(new Action(UpdateRecordCount), DispatcherPriority.Background);
        }

        private void Grid_FilterChanged(object? sender, GridFilterEventArgs e)
        {
            UpdateRecordCount();
        }

        /// <summary>
        /// Aktualizuje licznik rekordów uwzględniając zarówno filtr tekstowy/datowy (ViewModel),
        /// jak i filtry kolumn nałożone bezpośrednio na SfDataGrid.
        /// </summary>
        private void UpdateRecordCount()
        {
            if (txtRecordCount == null || _vm == null)
                return;

            int total = _vm.Skierowania.Count;
            int visible = (Lista_kart_Badan as SfDataGrid)?.View?.Records?.Count
                          ?? _vm.SkierowaniaFiltered.Count;

            txtRecordCount.Text = $"{visible} / {total}";
        }

        private void SkierowaniaView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && DataContext is SkierowaniaViewModel vm)
            {
                try { vm.RefreshFromDb(); } catch { }
                UpdateRecordCount();
            }
        }

        private void Rejestracja_Click(object sender, RoutedEventArgs e)
        {
            if (Window.GetWindow(this) is MainWindow mainWindow)
            {
                mainWindow.SelectRejestracjaTab();
            }
        }

        private void OpenSkierPacjentaCard(SkierowanieListItem item)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null) return;

            if (Application.Current is not App app || app.Host == null)
            {
                MessageBox.Show("Brak konfiguracji DI.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cardVm = app.Host.Services.GetRequiredService<SkierPacjentaViewModel>();
            cardVm.LoadFromSkierowanie(item);

            var cardView = new SkierPacjentaView { DataContext = cardVm };

            mainWindow.SetKartyBadanContent(cardView);
            mainWindow.SelectKartyBadanTab();
        }

        private void NavigateToRejestracjaTab(string nazwisko)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null) return;

            mainWindow.OpenRejestracjaWithFilter(nazwisko);
        }

        private void NavigateToNowaKartaTab(string searchText)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null) return;

            mainWindow.OpenNowaKartaWithFilter(searchText);
        }

        private void RefreshList_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SkierowaniaViewModel vm)
            {
                try { vm.RefreshFromDb(); } catch { }
            }
        }
        private void Lista_kart_Badan_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateRecordCount();
        }
        private void prinlist_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SkierowaniaViewModel vm)
            {
                try { vm.PrintList(GetVisibleItems()); } catch { }
            }
        }
        private void exportExcel_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SkierowaniaViewModel vm)
            {
                try { vm.ExportToExcel(GetVisibleItems()); } catch { }
            }
        }

        /// <summary>
        /// Zwraca rekordy aktualnie widoczne w tabeli (po uwzględnieniu
        /// filtrów kolumn SfDataGrid), a nie tylko filtra tekstowego/datowego z ViewModelu.
        /// </summary>
        private System.Collections.Generic.List<SkierowanieListItem>? GetVisibleItems()
        {
            if (Lista_kart_Badan is SfDataGrid grid && grid.View?.Records != null)
            {
                return grid.View.Records
                    .Select(r => r.Data)
                    .OfType<SkierowanieListItem>()
                    .ToList();
            }

            return null;
        }
    }

}
