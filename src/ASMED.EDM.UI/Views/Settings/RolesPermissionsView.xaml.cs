using ASMED.EDM.UI.ViewModels.Settings;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Settings;

public partial class RolesPermissionsView : UserControl
{
    public RolesPermissionsView()
    {
        InitializeComponent();

        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            return;

        Loaded += RolesPermissionsView_Loaded;
    }

    private async void RolesPermissionsView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not RolesPermissionsViewModel viewModel)
        {
            if (Application.Current is App app && app.Host != null)
            {
                viewModel = app.Host.Services.GetRequiredService<RolesPermissionsViewModel>();
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
