using ASMED.EDM.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Patients;

/// <summary>
/// Interaction logic for PatientsView.xaml
/// </summary>
public partial class PatientsView : UserControl
{
    public PatientsView()
    {
        InitializeComponent();
        Loaded += PatientsView_Loaded;
    }

    private void PatientsView_Loaded(object sender, RoutedEventArgs e)
    {
        // Resolve ViewModel from DI container if DataContext not already set
        if (DataContext == null 
            && !System.ComponentModel.DesignerProperties.GetIsInDesignMode(this) 
            && Application.Current is App app 
            && app.Host != null)
        {
            DataContext = app.Host.Services.GetRequiredService<PatientsViewModel>();
        }
    }

    // Constructor for DI injection (when instance is created programmatically)
    public PatientsView(PatientsViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }
}
