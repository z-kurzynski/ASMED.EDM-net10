using ASMED.WPF; // model FirmaDto
using ASMED.WPF.ViewModels.ListaDoFaktur;
using System;
using System.Windows;

namespace ASMED.WPF.Views.lista_do_faktur
{
    public partial class FirmaSelectDialog : Window
    {
        public FirmaSelectDialog()
        {
            InitializeComponent();
            this.DataContext = new FirmaSelectViewModel();
        }

        // zwraca wybrany model z VM (ASMED.WPF.FirmaDto)
        public global::ASMED.WPF.FirmaDto? SelectedFirma
        {
            get
            {
                var vm = DataContext as FirmaSelectViewModel;
                var sel = vm?.SelectedFirma;
                if (sel == null) return null;

                // upewnij się, że przekazujemy przycięty cennik
                string? cennik = sel.Cennik;
                if (!string.IsNullOrWhiteSpace(cennik))
                    cennik = cennik.Trim();
                else
                    cennik = null;

                return new global::ASMED.WPF.FirmaDto
                {
                    Id = sel.Id,
                    Activ = sel.Activ,
                    Nazwa = sel.Nazwa,
                    NIP = sel.NIP,
                    Cennik = cennik,
                    FkEmail = sel.FkEmail
                };
            }
        }

        public int? SelectedFirmaId { get; internal set; }
        public string SelectedFirmaName { get; internal set; } = string.Empty;

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is FirmaSelectViewModel vm)
                vm.SearchText = string.Empty;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            var sel = SelectedFirma;
            if (sel == null) { DialogResult = false; return; }

            SelectedFirmaId = sel.Id;
            SelectedFirmaName = sel.Nazwa ?? string.Empty;

            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        // Handler dla przycisku "Wybierz" umieszczonego w każdym wierszu listy.
        // Pobiera dane z DataContext przycisku (wiersza) i ustawia SelectedFirmaId/SelectedFirmaName,
        // następnie zamyka dialog z DialogResult = true.
        private void RowChoose_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is not System.Windows.Controls.Button btn) return;
                var item = btn.DataContext;
                if (item == null)
                {
                    MessageBox.Show("Nie udało się odczytać wybranego elementu.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Bezpieczne wydobycie właściwości przez refleksję (typ elementu może być wewnętrznym VM)
                var idProp = item.GetType().GetProperty("Id");
                var nameProp = item.GetType().GetProperty("Nazwa") ?? item.GetType().GetProperty("Name");

                if (idProp != null)
                {
                    var idVal = idProp.GetValue(item);
                    if (idVal != null && int.TryParse(idVal.ToString(), out var idInt))
                        SelectedFirmaId = idInt;
                }

                if (nameProp != null)
                {
                    var nameVal = nameProp.GetValue(item);
                    SelectedFirmaName = nameVal?.ToString() ?? string.Empty;
                }

                // Zaznacz również w wewnętrznym VM (jeśli to możliwe), aby SelectedFirma getter działał spójnie
                if (DataContext is FirmaSelectViewModel vm)
                {
                    // spróbuj ustawić SelectedFirma na obiekt, jeśli typ pasuje
                    var selProp = vm.GetType().GetProperty("SelectedFirma");
                    if (selProp != null && selProp.PropertyType.IsAssignableFrom(item.GetType()))
                    {
                        selProp.SetValue(vm, item);
                    }
                }

                DialogResult = true;
                Close();
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"RowChoose_Click error: {ex}");
                MessageBox.Show("Wystąpił błąd podczas wybierania firmy.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
