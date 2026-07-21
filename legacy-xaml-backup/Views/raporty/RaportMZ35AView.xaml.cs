using System.Windows.Controls;
using ASMED.WPF.ViewModels;

namespace ASMED.WPF.Views
{
    public partial class RaportMZ35AView : UserControl
    {
        public RaportMZ35AView()
        {
            InitializeComponent();
            DataContext = new RaportMZ35AViewModel();
        }
    }
}
