using System.Windows.Controls;
using ASMED.WPF.ViewModels;

namespace ASMED.WPF.Views
{
    public partial class NarzedziaView : UserControl
    {
        public NarzedziaView()
        {
            InitializeComponent();
            DataContext = new NarzedziaViewModel();
        }
    }
}
