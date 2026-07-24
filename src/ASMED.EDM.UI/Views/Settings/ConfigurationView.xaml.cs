using System.Windows.Controls;
using ASMED.EDM.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMED.EDM.UI.Views.Settings;

public partial class ConfigurationView : UserControl
{
    private ConfigurationViewModel _viewModel;

    // Domyślny konstruktor dla XAML
    public ConfigurationView() : this(null)
    {
    }

    public ConfigurationView(ConfigurationViewModel? viewModel)
    {
        InitializeComponent();

        // Jeśli viewModel nie został przekazany, pobierz z DI
        _viewModel = viewModel ?? ((App)System.Windows.Application.Current).Host.Services.GetRequiredService<ConfigurationViewModel>();
        DataContext = _viewModel;

        // Bind PasswordBox values (cannot be done via XAML binding for security)
        PrimaryPasswordBox.PasswordChanged += (s, e) => _viewModel.PrimaryPassword = PrimaryPasswordBox.Password;
        BackupPasswordBox.PasswordChanged += (s, e) => _viewModel.BackupPassword = BackupPasswordBox.Password;
        LocalPasswordBox.PasswordChanged += (s, e) => _viewModel.LocalPassword = LocalPasswordBox.Password;

        // Initialize PasswordBox values from ViewModel
        PrimaryPasswordBox.Password = _viewModel.PrimaryPassword;
        BackupPasswordBox.Password = _viewModel.BackupPassword;
        LocalPasswordBox.Password = _viewModel.LocalPassword;
    }
}

