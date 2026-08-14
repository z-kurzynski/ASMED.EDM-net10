using ASMED.EDM.UI.ViewModels.Faktura;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Faktura;

/// <summary>
/// Code-behind dla FakturaAddView.xaml — minimalne, cała logika w ViewModelu.
/// </summary>
public partial class FakturaAddView : UserControl
{
    public FakturaAddView()
    {
        InitializeComponent();
        Loaded += async (_, _) =>
        {
            if (DataContext is FakturaAddViewModel vm)
                await vm.OnNavigatedToAsync();
        };
    }
}
