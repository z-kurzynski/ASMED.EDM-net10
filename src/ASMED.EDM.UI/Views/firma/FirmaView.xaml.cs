using System.Windows.Controls;
using ASMED.EDM.UI.ViewModels.Firmy;
using Microsoft.Extensions.DependencyInjection;

namespace ASMED.EDM.UI.Views.Firma;

public partial class FirmaView : UserControl
{
    public FirmaView()
    {
        InitializeComponent();

        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            return;

        Loaded += FirmaView_Loaded;
    }

    public FirmaView(FirmaViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private async void FirmaView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext is not FirmaViewModel viewModel)
        {
            if (System.Windows.Application.Current is App app && app.Host != null)
            {
                viewModel = app.Host.Services.GetRequiredService<FirmaViewModel>();
                DataContext = viewModel;
            }
            else
            {
                return;
            }
        }

        await viewModel.OnNavigatedToAsync();
    }
}
