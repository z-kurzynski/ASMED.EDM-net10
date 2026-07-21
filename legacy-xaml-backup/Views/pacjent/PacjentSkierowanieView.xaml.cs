using ASMED.WPF.ViewModels;
using System.Windows.Controls;

namespace ASMED.WPF.Views
{
    public partial class PacjentSkierowanieView : UserControl
    {
        public PacjentSkierowanieView()
        {
            InitializeComponent();
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                this.DataContext = new PacjentSkierowanieViewModel();
            }
        }
    }
}
