using System.Windows.Controls;
using ASMED.WPF.ViewModels;

namespace ASMED.WPF.Views
{
    public partial class WizytyView : UserControl
    {
        public WizytyView()
        {
            InitializeComponent();
            DataContext = new WizytyViewModel();
        }
    }
}
