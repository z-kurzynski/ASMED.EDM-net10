using System.Windows.Controls;
using ASMED.WPF.ViewModels;

namespace ASMED.WPF.Views
{
    public partial class MedyczneView : UserControl
    {
        public MedyczneView()
        {
            InitializeComponent();
            DataContext = new MedyczneViewModel();
        }
    }
}
