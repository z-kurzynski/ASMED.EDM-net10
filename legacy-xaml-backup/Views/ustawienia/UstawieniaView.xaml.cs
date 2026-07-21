using System.Windows;
using System.Windows.Controls;
using ASMED.WPF.Helpers;
using ASMED.WPF.ViewModels;

namespace ASMED.WPF.Views
{
    public partial class UstawieniaView : UserControl
    {
        public UstawieniaView()
        {
            InitializeComponent();
            DataContext = new UstawieniaViewModel();
        }
    }
}
// koniec pliku UstawieniaView.xaml.cs