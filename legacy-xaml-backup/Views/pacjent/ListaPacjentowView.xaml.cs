using ASMED.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.WPF.Views
{
    public partial class ListaPacjentowView : UserControl
    {

        //  private void DodajPacjenta_Click(object sender, RoutedEventArgs e)
        //   {
        //        var dialog = new PatientAdd();
        //        dialog.DataContext = new PatientAddViewModel();
        //        dialog.ShowDialog();
        //        // odśwież listę pacjentów jeśli trzeba
        //    }

        private void DodajPacjenta_Click(object sender, RoutedEventArgs e)
        {
            if (Application.Current.MainWindow?.DataContext is MainWindowViewModel vm)
            {
                vm.PacjentWidok = new PacjentDodajViewModel(vm);
            }
            // odśwież listę pacjentów jeśli trzeba
        }

        public ListaPacjentowView()
        {
            InitializeComponent();
            // this.DataContext = new ViewModels.ListaPacjentowViewModel();
            this.DataContext = new ListaPacjentowViewModel();
            //  this.DataContext = new ViewModels.ListaPacjentowViewModel(); // Ustawienie DataContext na instancję ViewModel
        }
    }
}
