using ASMED.WPF.ViewModels;
using ASMED.WPF.ViewModels.lista_do_faktur;
using ASMED.WPF.Helpers;
using ASMED.WPF.Views.lista_do_faktur;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;
using System.Windows.Media;
using Syncfusion.UI.Xaml.Grid;
using System.ComponentModel;
using System.Linq;

namespace ASMED.WPF.Views
{
    public partial class ListaDoFakturView : UserControl
    {
        public ListaDoFakturView()
        {
            InitializeComponent();
            var vm = new ListaDoFakturViewModel();
            DataContext = vm;

            // load detail view into RightContent placeholder and share DataContext
            try
            {
                var detail = new ListaDoFaktur_DetailView();
                detail.DataContext = vm;
                RightContent.Content = detail;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"Failed to load detail view: {ex}");
            }

            // Po powrocie do tego widoku: je�li wcze�niej ustawiono flag� � od�wie� VM
            this.Loaded += (s, e) =>
            {
                try
                {
                    if (Application.Current != null && Application.Current.Properties.Contains("ListaDoFaktur_NeedsRefresh"))
                    {
                        var needs = Application.Current.Properties["ListaDoFaktur_NeedsRefresh"];
                        if ((needs is bool b && b) || (needs != null && needs.ToString()?.Equals("True", StringComparison.OrdinalIgnoreCase) == true))
                        {
                            Application.Current.Properties["ListaDoFaktur_NeedsRefresh"] = false;
                            TryRefreshViewModel(DataContext);
                        }
                    }
                }
                catch { /* bezpiecznie ignorujemy */ }
            };
        }

        // Handler sortowania - sortowanie niestandardowe po FK_Numer z obs�ug� NULL
        private void DgListyBadan_SortColumnsChanging(object sender, GridSortColumnsChangingEventArgs e)
        {
            try
            {
                if (e.AddedItems.Any(col => col.ColumnName == "FK_Numer"))
                {
                    e.Cancel = true;

                    var vm = DataContext as ListaDoFakturViewModel;
                    if (vm == null || vm.ListyBadan == null) return;

                    var sortColumn = e.AddedItems.FirstOrDefault(col => col.ColumnName == "FK_Numer");
                    var sortDirection = sortColumn?.SortDirection ?? ListSortDirection.Ascending;

                    var sorted = sortDirection == ListSortDirection.Ascending
                        ? vm.ListyBadan.OrderBy(item => string.IsNullOrEmpty(item.FK_Numer) ? "ZZZZZ" : item.FK_Numer).ToList()
                        : vm.ListyBadan.OrderByDescending(item => string.IsNullOrEmpty(item.FK_Numer) ? "" : item.FK_Numer).ToList();

                    vm.ListyBadan.Clear();
                    foreach (var item in sorted)
                    {
                        vm.ListyBadan.Add(item);
                    }

                    dgListyBadan.SortColumnDescriptions.Clear();
                    dgListyBadan.SortColumnDescriptions.Add(new SortColumnDescription
                    {
                        ColumnName = "FK_Numer",
                        SortDirection = sortDirection
                    });
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"B��d sortowania: {ex.Message}");
            }
        }

        // Pr�ba od�wie�enia ViewModelu przez reflection
        private void TryRefreshViewModel(object? vmObj)
        {
            try
            {
                if (vmObj == null) return;

                var refreshCandidates = new[] { "RefreshFromDb", "RefreshBadania", "Refresh", "Reload", "LoadData", "RefreshBadaniaList" };

                foreach (var name in refreshCandidates)
                {
                    try
                    {
                        var m = vmObj.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
                        if (m != null)
                        {
                            Dispatcher.Invoke(() =>
                            {
                                try { m.Invoke(vmObj, null); }
                                catch { }
                            });
                            return;
                        }
                    }
                    catch { /* ignore */ }
                }
            }
            catch { }
            finally
            {
                try
                {
                    Dispatcher.BeginInvoke(new Action(() =>
                    {
                        try
                        {
                            RightContent?.InvalidateMeasure();
                            RightContent?.InvalidateArrange();
                            RightContent?.UpdateLayout();
                        }
                        catch { }
                    }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
                }
                catch { }
            }
        }

        // Handler dla przycisku "nowa_lista_do_faktur"
        private void Nowa_Lista_Do_Faktur_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                var main = Application.Current.MainWindow as MainWindow;
                if (main == null) return;

                var targetTab = main.FindName("ListaDoFaktur") as TabItem;
                if (targetTab == null)
                {
                    MessageBox.Show("Nie znaleziono zak�adki 'ListaDoFaktur' w MainWindow.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var addView = new ASMED.WPF.Views.lista_do_faktur.ListaFaktAddView();
                var addVm = new ASMED.WPF.ViewModels.ListaDoFaktur.ListaFaktAddViewModel();
                addView.DataContext = addVm;

                try
                {
                    if (Application.Current != null)
                        Application.Current.Properties["ListaDoFaktur_NeedsRefresh"] = true;
                }
                catch { }

                targetTab.Content = addView;

                try
                {
                    if (targetTab.Parent is TabControl parentTabControl)
                        parentTabControl.SelectedItem = targetTab;
                }
                catch { }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"B��d podczas otwierania widoku dodawania listy: {ex.Message}", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void DeleteList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var btn = sender as Button;
                var dto = btn?.DataContext as AccessDbContext.ListyBadanDto;
                if (dto == null)
                {
                    MessageBox.Show("Nie uda�o si� odczyta� rekordu listy do usuni�cia.", "B��d", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (!dto.Identyfikator.HasValue)
                {
                    MessageBox.Show("Wybrany rekord nie ma identyfikatora. Nie mo�na usun��.", "B��d", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var confirm = MessageBox.Show($"Czy na pewno usun�� list�:\n{dto.Nazwa}\nID listy: {dto.Identyfikator} ?", "Potwierd� usuni�cie", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                var db = new AccessDbContext();
                int listId = dto.Identyfikator.Value;

                // 1) Pobierz przypisane badania i dla ka�dego wykonaj unassign
                var badania = db.GetBadaniaForLista(listId);
                int failedUnassign = 0;
                foreach (var b in badania)
                {
                    try
                    {
                        if (b.Bad_ID.HasValue)
                        {
                            var ok = db.UnassignBadanieFromLista(b.Bad_ID.Value, "DeleteList");
                            if (!ok) failedUnassign++;
                        }
                    }
                    catch
                    {
                        failedUnassign++;
                    }
                }

                // 2) Je�eli lista powi�zana z faktur� -> ustaw FK_Num_Listy = 0
                var fakturaId = db.GetFakturaIdForList(listId);
                if (fakturaId.HasValue)
                {
                    try
                    {
                        db.ClearFakturaNumListByFakturaId(fakturaId.Value);
                    }
                    catch { /* ignore */ }
                }

                // 3) Usu� rekord ListyBadan
                var deleted = db.DeleteListyBadan(listId);
                if (!deleted)
                {
                    MessageBox.Show("Nie uda�o si� usun�� rekordu listy z tabeli ListyBadan.", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                else
                {
                    var vm = DataContext as ListaDoFakturViewModel;
                    try { vm?.RefreshFromDb(); vm?.RefreshAssignedForSelected(); }
                    catch { TryRefreshViewModel(DataContext); }

                    var msg = "Usuni�to list�.";
                    if (failedUnassign > 0) msg += $" Niekt�re powi�zania bada� nie zosta�y od��czone ({failedUnassign}).";
                    MessageBox.Show(msg, "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"B��d podczas usuwania listy: {ex.Message}", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ImportArchiwum_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var importWindow = new Window
                {
                    Title = "Import z archiwum - Listy do Faktur",
                    Content = new ArchiveImportView(),
                    Owner = Window.GetWindow(this),
                    Width = 1200,
                    Height = 700,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner
                };

                importWindow.ShowDialog();

                if (DataContext is ListaDoFakturViewModel vm)
                {
                    vm.RefreshFromDb();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"B��d otwierania okna importu:\n{ex.Message}", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
