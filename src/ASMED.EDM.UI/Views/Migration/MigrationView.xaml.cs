using ASMED.EDM.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Migration;

/// <summary>
/// Interaction logic for MigrationView.xaml
/// </summary>
public partial class MigrationView : UserControl
{
    // Bezparametrowy konstruktor wymagany przez WPF XAML parser
    public MigrationView() : this(null)
    {
    }

    public MigrationView(MigrationViewModel? viewModel)
    {
        InitializeComponent();

        // Jeśli viewModel nie został przekazany, pobierz z kontenera DI
        DataContext = viewModel
            ?? ((App)System.Windows.Application.Current).Host.Services
                .GetRequiredService<MigrationViewModel>();
    }
}
