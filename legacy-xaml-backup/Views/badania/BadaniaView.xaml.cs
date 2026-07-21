using ASMED.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.ComponentModel;
using System;
using ASMED.WPF.ViewModels.Badania;
using System.Linq;
using ASMED.WPF.Helpers;

namespace ASMED.WPF.Views
{
    public partial class BadaniaView : UserControl
    {
        public BadaniaView()
        {
            InitializeComponent();

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                var vm = new BadaniaListViewModel();
                DataContext = vm;

                // subscribe to VM property changes if needed
                if (vm is INotifyPropertyChanged inpc)
                {
                    inpc.PropertyChanged += Vm_PropertyChanged;
                }

                this.Loaded += (s, e) => { try { vm.RefreshFromDb(); LoadEditView(); } catch { } };
                this.IsVisibleChanged += (s, e) => { if (this.IsVisible) { try { vm.RefreshFromDb(); LoadEditView(); } catch { } } };
            }
        }

        private void Vm_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // react to selection changes if required (kept minimal)
            if (string.Equals(e.PropertyName, "SelectedWizyta", StringComparison.OrdinalIgnoreCase))
            {
                // reload the edit view so it binds to the updated selection
                try { LoadEditView(); } catch { }
            }
        }

        private void ClearFilter_Click(object sender, RoutedEventArgs e)
        {
            // Wyczyść pole tekstowe filtra jeśli jest TextBox
            if (txtFilterTop is TextBox tb)
                tb.Text = string.Empty;

            // Jeśli ViewModel ma właściwość FilterText, wyczyść ją również
            var vm = DataContext as dynamic;
            if (vm != null)
            {
                try { vm.FilterText = string.Empty; } catch { }
            }
        }

        private void Lista_Badan_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Find main window and its Badania tab, then replace tab content with BadaniaListaView
                var main = Application.Current.MainWindow as MainWindow;
                if (main == null) return;

                var badaniaTab = main.FindName("Badania") as System.Windows.Controls.TabItem;
                if (badaniaTab == null) return;

                var listView = new BadaniaListaView();
                badaniaTab.Content = listView;
                // optionally select the tab
                try { badaniaTab.IsSelected = true; } catch { }
            }
            catch { }
        }

        // Wstawia dynamicznie kontrolkę BadaniaEditView do panelu Edit_prawa
        private void LoadEditView()
        {
            try
            {
                if (Edit_prawa == null) return;

                // if an editor already exists, update its DataContext with current selection
                var existingEditor = Edit_prawa.Children.OfType<BadaniaEditView>().FirstOrDefault();
                if (existingEditor != null)
                {
                    if (DataContext is BadaniaListViewModel listVm)
                    {
                        var editVm = new BadaniaEditViewModel();
                        // copy available cennik options from list VM so ComboBox is populated
                        try
                        {
                            foreach (var c in listVm.CennikOptions)
                                if (!editVm.CennikOptions.Contains(c)) editVm.CennikOptions.Add(c);
                        }
                        catch { }

                        editVm.SelectedWizyta = listVm.SelectedWizyta;

                        // load existing Badanie from DB when linked
                        if (listVm.SelectedWizyta != null)
                        {
                            int? badId = listVm.SelectedWizyta.B_Badanie_ID;
                            if (badId.HasValue && badId.Value >0)
                            {
                                var db = new AccessDbContext();
                                var rec = db.GetBadanieById(badId.Value);
                                if (rec != null)
                                {
                                    editVm.DataBadania = rec.Bad_Data ?? DateTime.Today;
                                    editVm.DataWaznosci = rec.Bad_Data_Do;
                                    editVm.SelectedWynik = rec.Bad_Wynik;
                                    editVm.NrKsiegi = rec.Bad_Nr_KS;
                                    editVm.SelectedCennik = rec.Bad_bn_cennik;
                                    editVm.SetPriceFields(rec.Bad_Cena1, rec.Bad_Cena2, rec.Bad_Cena3, rec.Bad_Cena4, rec.Bad_Cena5, rec.Bad_Cena6, rec.Bad_Cena7, rec.Bad_Cena8);
                                }
                                else
                                {
                                    // if no rec found, fallback to company cennik
                                    var firmCennik = listVm.SelectedWizyta.Firma_Cennik;
                                    if (!string.IsNullOrEmpty(firmCennik) && !editVm.CennikOptions.Contains(firmCennik)) editVm.CennikOptions.Add(firmCennik);
                                    if (!string.IsNullOrEmpty(firmCennik)) editVm.SelectedCennik = firmCennik;
                                }
                            }
                            else
                            {
                                // no existing badanie: prefer company's default cennik
                                var firmCennik = listVm.SelectedWizyta.Firma_Cennik;
                                if (!string.IsNullOrEmpty(firmCennik) && !editVm.CennikOptions.Contains(firmCennik)) editVm.CennikOptions.Add(firmCennik);
                                if (!string.IsNullOrEmpty(firmCennik)) editVm.SelectedCennik = firmCennik;
                            }
                        }

                        existingEditor.DataContext = editVm;
                    }
                    return;
                }

                var edit = new BadaniaEditView();

                if (DataContext is BadaniaListViewModel listVm2)
                {
                    var editVm = new BadaniaEditViewModel();
                    // copy cennik options so ComboBox shows values
                    try
                    {
                        foreach (var c in listVm2.CennikOptions)
                            if (!editVm.CennikOptions.Contains(c)) editVm.CennikOptions.Add(c);
                    }
                    catch { }

                    editVm.SelectedWizyta = listVm2.SelectedWizyta;

                    if (listVm2.SelectedWizyta != null)
                    {
                        int? badId = listVm2.SelectedWizyta.B_Badanie_ID;
                        if (badId.HasValue && badId.Value >0)
                        {
                            var db = new AccessDbContext();
                            var rec = db.GetBadanieById(badId.Value);
                            if (rec != null)
                            {
                                editVm.DataBadania = rec.Bad_Data ?? DateTime.Today;
                                editVm.DataWaznosci = rec.Bad_Data_Do;
                                editVm.SelectedWynik = rec.Bad_Wynik;
                                editVm.NrKsiegi = rec.Bad_Nr_KS;
                                editVm.SelectedCennik = rec.Bad_bn_cennik;
                                editVm.SetPriceFields(rec.Bad_Cena1, rec.Bad_Cena2, rec.Bad_Cena3, rec.Bad_Cena4, rec.Bad_Cena5, rec.Bad_Cena6, rec.Bad_Cena7, rec.Bad_Cena8);
                            }
                            else
                            {
                                var firmCennik = listVm2.SelectedWizyta.Firma_Cennik;
                                if (!string.IsNullOrEmpty(firmCennik) && !editVm.CennikOptions.Contains(firmCennik)) editVm.CennikOptions.Add(firmCennik);
                                if (!string.IsNullOrEmpty(firmCennik)) editVm.SelectedCennik = firmCennik;
                            }
                        }
                        else
                        {
                            // no linked badanie, use company's cennik if present
                            var firmCennik = listVm2.SelectedWizyta.Firma_Cennik;
                            if (!string.IsNullOrEmpty(firmCennik) && !editVm.CennikOptions.Contains(firmCennik)) editVm.CennikOptions.Add(firmCennik);
                            if (!string.IsNullOrEmpty(firmCennik)) editVm.SelectedCennik = firmCennik;
                        }
                    }

                    edit.DataContext = editVm;
                }
                else
                {
                    edit.DataContext = this.DataContext;
                }

                Edit_prawa.Children.Clear();
                Edit_prawa.Children.Add(edit);
            }
            catch { }
        }
    }
}
