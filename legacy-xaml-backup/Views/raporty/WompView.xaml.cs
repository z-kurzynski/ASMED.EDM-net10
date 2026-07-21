using System.Windows.Controls;
using ASMED.WPF.ViewModels;

namespace ASMED.WPF.Views
{
    public partial class WompView : UserControl
    {
        public WompView()
        {
            InitializeComponent();
            DataContext = new WompViewModel();
        }
    }
}
