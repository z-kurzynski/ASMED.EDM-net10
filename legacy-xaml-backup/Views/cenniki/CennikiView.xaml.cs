using ASMED.WPF.ViewModels;
using System.Windows.Controls;

namespace ASMED.WPF.Views
{
    /// <summary>
    /// Interaction logic for CennikiView.xaml
    /// Widok zarz�dzania cennikami firm
    /// </summary>
    public partial class CennikiView : UserControl
    {
        public CennikiView()
        {
            InitializeComponent();

            // Ustaw DataContext na CennikiViewModel
            if (!System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            {
                this.DataContext = new CennikiViewModel();
            }

            // Automatyczny fokus na pole wyszukiwania firm po za�adowaniu
            this.Loaded += (s, e) =>
            {
                this.Dispatcher.BeginInvoke(new System.Action(() =>
                {
                    try
                    {
                        txtSearchFirmy?.Focus();
                        // System.Diagnostics.Debug.WriteLine("CennikiView: Fokus ustawiony na pole wyszukiwania firm");
                    }
                    catch { }
                }), System.Windows.Threading.DispatcherPriority.Input);
            };

            this.IsVisibleChanged += (s, e) =>
            {
                if (this.IsVisible)
                {
                    this.Dispatcher.BeginInvoke(new System.Action(() =>
                    {
                        try
                        {
                            txtSearchFirmy?.Focus();
                        }
                        catch { }
                    }), System.Windows.Threading.DispatcherPriority.Input);
                }
            };
        }
    }
}
// End of Path: Views/cenniki/CennikiView.xaml.cs
