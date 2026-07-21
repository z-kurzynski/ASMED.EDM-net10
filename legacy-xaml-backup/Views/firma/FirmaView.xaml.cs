using ASMED.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.WPF.Views
{
    public partial class FirmaView : UserControl
    {
        public FirmaView()
        {
            InitializeComponent();
            DataContext = new FirmaViewModel();
        }
    }
}
