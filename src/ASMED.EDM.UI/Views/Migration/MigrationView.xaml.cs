using ASMED.EDM.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Migration;

/// <summary>
/// Interaction logic for MigrationView.xaml
/// </summary>
public partial class MigrationView : UserControl
{
    // Konstruktor dla XAML Designer (bez DI)
    public MigrationView()
    {
        InitializeComponent();

        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            return;

        Loaded += MigrationView_Loaded;
    }

    // Konstruktor dla DI (wstrzyknięcie z kontenera)
    public MigrationView(MigrationViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private void MigrationView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (DataContext != null) return;
        if (System.Windows.Application.Current is App app && app.Host != null)
            DataContext = app.Host.Services.GetRequiredService<MigrationViewModel>();
    }
}
