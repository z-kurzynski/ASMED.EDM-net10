using ASMED.EDM.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Visits;

/// <summary>
/// VisitsView - Moduł Kalendarza Wizyt
/// Legacy: Views\wizytyview\WizytyViewView.xaml (1648 linii - SfScheduler + complex logic)
/// </summary>
public partial class VisitsView : UserControl
{
    public VisitsView()
    {
        InitializeComponent();
        Loaded += VisitsView_Loaded;
    }

    private void VisitsView_Loaded(object sender, RoutedEventArgs e)
    {
        // Resolve ViewModel from DI container if DataContext not already set
        if (DataContext == null 
            && !System.ComponentModel.DesignerProperties.GetIsInDesignMode(this) 
            && Application.Current is App app 
            && app.Host != null)
        {
            DataContext = app.Host.Services.GetRequiredService<VisitsViewModel>();
        }
    }

    // Constructor for DI injection (when instance is created programmatically)
    public VisitsView(VisitsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
