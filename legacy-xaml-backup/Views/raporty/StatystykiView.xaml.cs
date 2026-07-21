using System.Windows.Controls;
using ASMED.WPF.ViewModels;

namespace ASMED.WPF.Views
{
    public partial class StatystykiView : UserControl
    {
        public StatystykiView()
        {
            InitializeComponent();
            DataContext = new StatystykiViewModel();
        }
    }
}
