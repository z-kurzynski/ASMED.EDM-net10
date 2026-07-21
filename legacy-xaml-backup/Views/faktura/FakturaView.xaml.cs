using ASMED.WPF.ViewModels;
using ASMED.WPF.Helpers;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using System;
using System.Reflection;
using ASMED.WPF.Views.lista_do_faktur;
using System.Linq;
using System.Threading.Tasks;

namespace ASMED.WPF.Views
{
    public partial class FakturaView : UserControl
    {
        private Window? _importWindow;

        public FakturaView()
        {
            InitializeComponent();

            var vm = new FakturaViewModel();
            DataContext = vm;

            // Pod��cz obs�ug� przycisku import_faktur
            try
            {
                import_faktur.Click += ImportFaktur_Click;
            }
            catch
            {
                // bezpiecze�stwo: je�li kontrolka nie istnieje w layout'cie, ignoruj
            }

            // Pod��cz obs�ug� przycisku soldo
            try
            {
                soldo_button.Click += SoldoButton_Click;
            }
            catch
            {
                // ignoruj je�eli kontrolka nie istnieje
            }

            // Pod��cz obs�ug� przycisku od�wie�enia listy (je�li kontrolka istnieje)
            try
            {
                refresh_list.Click += RefreshList_Click;
            }
            catch
            {
                // ignoruj je�eli przycisk nie istnieje (bezpieczne fallbacki s� w XAML - Click handler)
            }
            vm.RefreshFromDb();
        }

        private void ImportFaktur_Click(object? sender, RoutedEventArgs e)
        {
            // je�li okno ju� otwarte - aktywuj je
            if (_importWindow != null && _importWindow.IsVisible)
            {
                try
                {
                    _importWindow.Activate();
                }
                catch { }
                return;
            }

            // utw�rz widok importu i kontener okienkowy
            var importView = new FakturaImportView();
            var owner = Window.GetWindow(this) ?? Application.Current?.MainWindow;

            _importWindow = new Window
            {
                Title = "Import faktur",
                Content = importView,
                Owner = owner,
                Width = 980,
                Height = 640,
                WindowStartupLocation = owner != null ? WindowStartupLocation.CenterOwner : WindowStartupLocation.CenterScreen
            };

            // przy zamkni�ciu okna zresetuj stan przycisku i referencj�
            _importWindow.Closed += (s, args) =>
            {
                try
                {
                    import_faktur.IsChecked = false;
                }
                catch { }
                _importWindow = null;
            };

            // ustaw przycisk jako wci�ni�ty (je�li checkable)
            try { import_faktur.IsChecked = true; } catch { }

            // poka� okno (modeless), aby u�ytkownik m�g� dalej pracowa�
            _importWindow.Show();
        }

        private void BtnSelectFirma_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not FakturaViewModel vm) return;

            try
            {
                // Je�eli klasa dialogu istnieje w projekcie, u�yj jej.
                // W przeciwnym wypadku wyj�tek zostanie z�apany i wy�wietlona informacja.
                var dlg = new FirmaSelectDialog();
                var res = dlg.ShowDialog();
                if (res == true)
                {
                    // najpierw spr�buj pobra� bezpo�rednio z obiektu dialogu
                    var id = GetIntProperty(dlg, new[] { "SelectedFirmaId", "SelectedId", "WybranaFirmaId", "FirmaId" });
                    var name = GetStringProperty(dlg, new[] { "SelectedFirmaName", "SelectedFirmaNazwa", "SelectedFirmaNameTxt", "SelectedName" });

                    // je�eli brak, sprawd� DataContext dialogu (cz�sto tam s� w�a�ciwo�ci)
                    if ((id == null || string.IsNullOrEmpty(name)) && dlg.DataContext != null)
                    {
                        var dc = dlg.DataContext;
                        if (id == null)
                            id = GetIntProperty(dc, new[] { "SelectedFirmaId", "WybranaFirmaId", "WybranaFirmaId", "WybranaFirma?.Id", "WybranaFirmaId" });

                        if (string.IsNullOrEmpty(name))
                        {
                            // je�eli DataContext ma obiekt WybranaFirma/SelectedFirma -> odczytaj jego Name/Nazwa
                            var firmaObj = GetObjectProperty(dc, new[] { "WybranaFirma", "SelectedFirma", "Selected", "Wybrana" });
                            if (firmaObj != null)
                            {
                                if (string.IsNullOrEmpty(name))
                                    name = GetStringProperty(firmaObj, new[] { "Name", "Nazwa", "DisplayName" });

                                if (id == null)
                                    id = GetIntProperty(firmaObj, new[] { "Id", "id", "FirmaId" });
                            }

                            // je�eli bezpo�rednio w DataContext s� pola Name/Id
                            if (string.IsNullOrEmpty(name))
                                name = GetStringProperty(dc, new[] { "WybranaFirmaName", "SelectedFirmaName", "Name", "Nazwa" });
                        }
                    }

                    // fallback: je�eli �adna nazwa nie zosta�a znaleziona, sprawd� ToString()
                    if (string.IsNullOrEmpty(name))
                    {
                        var maybeText = GetStringProperty(dlg, new[] { "SelectedText", "ResultText" });
                        if (!string.IsNullOrEmpty(maybeText)) name = maybeText;
                    }

                    // ustaw w VM
                    if (id.HasValue) vm.SelectedFirmaId = id.Value;
                    if (!string.IsNullOrEmpty(name)) vm.NewFirmaName = name;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Nie odnaleziono dialogu wyboru firmy lub wyst�pi� b��d.\n" +
                    "Upewnij si�, �e klasa FirmaSelectDialog istnieje.\n\nSzczeg�y: " + ex.Message,
                    "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void BtnSelectPdf_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not FakturaViewModel vm) return;

            var dlg = new OpenFileDialog();
            dlg.Filter = "PDF files (*.pdf)|*.pdf|All files (*.*)|*.*";
            dlg.Multiselect = false;
            var ok = dlg.ShowDialog();
            if (ok == true)
            {
                vm.NewPdfPath = dlg.FileName;
            }
        }

        private async void SoldoButton_Click(object? sender, RoutedEventArgs e)
        {
            if (DataContext is not FakturaViewModel vm)
            {
                MessageBox.Show("Brak kontekstu ViewModel.", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var toProcess = vm.FilteredFaktura.Where(f => (f.Kwota_B ?? 0m) > 0m).ToList();
            if (toProcess.Count == 0)
            {
                MessageBox.Show("Brak pozycji z warto�ci� bada� > 0.", "Soldo", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var ask = MessageBox.Show($"Zaktualizowa� saldo (FK_Saldo = FK_Kwota - FK_Suma_Bad) dla {toProcess.Count} pozycji?", "Potwierd�", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (ask != MessageBoxResult.Yes) return;

            int updated = 0;
            await Task.Run(() =>
            {
                var ctx = new AccessDbContext();
                foreach (var f in toProcess)
                {
                    try
                    {
                        // wymagamy poprawnego id rekordu faktury
                        if (f.Id <= 0) continue;

                        var kwota = f.Kwota ?? 0m;
                        var kwotaB = f.Kwota_B ?? 0m;
                        var newSaldo = kwota - kwotaB;

                        var ok = ctx.UpdateFakturaSaldo(f.Id, newSaldo);
                        if (ok) updated++;
                    }
                    catch (Exception)
                    {
                        // System.Diagnostics.Debug.WriteLine($"Soldo update error for FK_ID={f.Id}: {ex}");
                    }
                }
            });

            // od�wie� list� i podsumowanie w UI
            vm.RefreshFromDb();

            MessageBox.Show($"Zaktualizowano salda: {updated}", "Soldo", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // Handler dla przycisku od�wie�enia listy faktur
        private void RefreshList_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is FakturaViewModel vm)
                {
                    vm.RefreshFromDb();
                    // kr�tkie potwierdzenie dla u�ytkownika
                    //MessageBox.Show("Lista faktur zosta�a od�wie�ona.", "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    //NotificationHelper.ShowNotification("Lista faktur zosta�a od�wie�ona.");
                }
                else
                {
                    MessageBox.Show("Brak kontekstu ViewModel. Nie mo�na od�wie�y� listy.", "B��d", MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"B��d podczas od�wie�ania listy faktur: {ex.Message}", "B��d", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void NewInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (NewInvoice.Visibility == Visibility.Visible)
            {
                NewInvoice.Visibility = Visibility.Collapsed;
                return;
            }
            else
            {
                NewInvoice.Visibility = Visibility.Visible;
                return;
            }
        }



        // Handler dla przycisku "Dodaj Faktur�" - zapisuje i ukrywa panel
        private async void AddInvoice_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is not FakturaViewModel vm) return;

                // Sprawd� czy mo�na zapisa�
                if (vm.SaveFakturaCommand?.CanExecute(null) != true) return;

                // Wywo�aj komend� zapisu
                vm.SaveFakturaCommand?.Execute(null);

                // Poczekaj a� async save si� wykona (komenda jest async Task)
                await Task.Delay(50);

                // Ukryj panel po zapisie

                NewInvoice.Visibility = Visibility.Collapsed;

            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"B��d w obs�udze dodawania faktury: {ex.Message}");
                MessageBox.Show($"B��d zapisywania faktury: {ex.Message}", "B��d",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        // --- pomocnicze funkcje refleksji ---
        private int? GetIntProperty(object obj, string[] names)
        {
            foreach (var n in names)
            {
                try
                {
                    var p = obj.GetType().GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (p != null)
                    {
                        var v = p.GetValue(obj);
                        if (v == null) continue;
                        if (v is int i) return i;
                        if (int.TryParse(v.ToString(), out var parsed)) return parsed;
                    }
                }
                catch { /* ignoruj */ }
            }
            return null;
        }

        private string? GetStringProperty(object obj, string[] names)
        {
            foreach (var n in names)
            {
                try
                {
                    var p = obj.GetType().GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (p != null)
                    {
                        var v = p.GetValue(obj);
                        if (v == null) continue;
                        return v.ToString();
                    }
                }
                catch { /* ignoruj */ }
            }
            return null;
        }

        private object? GetObjectProperty(object obj, string[] names)
        {
            foreach (var n in names)
            {
                try
                {
                    var p = obj.GetType().GetProperty(n, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (p != null)
                    {
                        var v = p.GetValue(obj);
                        if (v != null) return v;
                    }
                }
                catch { /* ignoruj */ }
            }
            return null;
        }
    }
}
// End of file FakturaView.xaml.cs
