using System.ComponentModel;
using System.Windows.Controls;
using System.Windows;

namespace ASMED.EDM.UI.Views.wizytyview;

public partial class WizytyViewView : UserControl
{
    public WizytyViewView()
    {
        InitializeComponent();

        // Nie ustawiamy DataContext w trybie projektanta — zapobiega błędom w designerze.
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            // DataContext = new WizytyViewViewModel();
        }

        this.Loaded += WizytyViewView_Loaded;
    }

    private void WizytyViewView_Loaded(object sender, RoutedEventArgs e)
    {
        // Loaded event placeholder
    }

    private void WizytyScheduler_SelectionChanged(object sender, object e)
    {
        // Selection changed event placeholder
    }
}
