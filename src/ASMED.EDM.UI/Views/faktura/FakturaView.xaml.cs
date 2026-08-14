using System.Windows.Controls;
using ASMED.EDM.UI.ViewModels.faktura;
using Microsoft.Extensions.DependencyInjection;

namespace ASMED.EDM.UI.Views.faktura
{
    public partial class FakturaView : UserControl
    {
        public FakturaView()
        {
            InitializeComponent();

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                return;

            Loaded += FakturaView_Loaded;
        }

        public FakturaView(FakturaViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private async void FakturaView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            if (DataContext is not FakturaViewModel viewModel)
            {
                if (System.Windows.Application.Current is App app && app.Host != null)
                {
                    viewModel = app.Host.Services.GetRequiredService<FakturaViewModel>();
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
}
