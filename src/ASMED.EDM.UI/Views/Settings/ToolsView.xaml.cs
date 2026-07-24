using System.Windows.Controls;
using ASMED.EDM.UI.ViewModels.ustawienia;

namespace ASMED.EDM.UI.Views.Settings;

public partial class ToolsView : UserControl
{
    public ToolsView()
    {
        InitializeComponent();
        DataContext = new ToolsViewModel();
    }
}
