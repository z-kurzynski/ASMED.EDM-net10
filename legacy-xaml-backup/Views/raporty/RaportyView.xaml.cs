using ASMED.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.WPF.Views
{
    public partial class RaportyView : UserControl
    {
        public RaportyView()
        {
            InitializeComponent();
            DataContext = new RaportyViewModel();
        }
    }
}
