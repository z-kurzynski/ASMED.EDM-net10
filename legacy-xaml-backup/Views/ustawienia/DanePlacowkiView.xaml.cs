using System.Windows.Controls;
using ASMED.WPF.ViewModels;

namespace ASMED.WPF.Views
{
    public partial class DanePlacowkiView : UserControl
    {
        public DanePlacowkiView()
        {
            InitializeComponent();
            DataContext = new DanePlacowkiViewModel();
        }
    }
}
