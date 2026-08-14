using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Extensions.DependencyInjection;

namespace ASMED.EDM.UI.Views.raporty
{
    public partial class WizytyView : UserControl
    {
        public WizytyView()
        {
            InitializeComponent();

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                if (Application.Current is App app && app.Host != null)
                {
                    var vm = app.Host.Services.GetRequiredService<ViewModels.Raporty.WizytyViewModel>();
                    DataContext = vm;
                }
            }
        }
    }
}
