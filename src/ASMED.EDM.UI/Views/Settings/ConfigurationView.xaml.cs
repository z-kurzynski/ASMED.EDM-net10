using System.Windows.Controls;
using ASMED.EDM.UI.ViewModels;

namespace ASMED.EDM.UI.Views.Settings;

public partial class ConfigurationView : UserControl
{
    public ConfigurationView(ConfigurationViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        // Bind PasswordBox values (cannot be done via XAML binding for security)
        PrimaryPasswordBox.PasswordChanged += (s, e) => viewModel.PrimaryPassword = PrimaryPasswordBox.Password;
        BackupPasswordBox.PasswordChanged += (s, e) => viewModel.BackupPassword = BackupPasswordBox.Password;
        LocalPasswordBox.PasswordChanged += (s, e) => viewModel.LocalPassword = LocalPasswordBox.Password;

        // Initialize PasswordBox values from ViewModel
        PrimaryPasswordBox.Password = viewModel.PrimaryPassword;
        BackupPasswordBox.Password = viewModel.BackupPassword;
        LocalPasswordBox.Password = viewModel.LocalPassword;
    }
}

