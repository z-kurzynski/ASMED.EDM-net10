using System.Windows.Controls;
using ASMED.EDM.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ASMED.EDM.UI.Views.Settings;

public partial class ConfigurationView : UserControl
{
    private ConfigurationViewModel? _viewModel;

    // Konstruktor dla XAML Designer (bez DI)
    public ConfigurationView()
    {
        InitializeComponent();

        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            return;

        Loaded += ConfigurationView_Loaded;
    }

    // Konstruktor dla DI (wstrzyknięcie z kontenera)
    public ConfigurationView(ConfigurationViewModel viewModel) : this()
    {
        SetViewModel(viewModel);
    }

    private void ConfigurationView_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        if (_viewModel != null) return;
        if (System.Windows.Application.Current is App app && app.Host != null)
            SetViewModel(app.Host.Services.GetRequiredService<ConfigurationViewModel>());
    }

    private void SetViewModel(ConfigurationViewModel viewModel)
    {
        _viewModel = viewModel;
        DataContext = _viewModel;

        // PasswordBox nie obsługuje bindingu XAML — podpinamy ręcznie
        PrimaryPasswordBox.PasswordChanged += (s, e) => _viewModel.PrimaryPassword = PrimaryPasswordBox.Password;
        BackupPasswordBox.PasswordChanged  += (s, e) => _viewModel.BackupPassword  = BackupPasswordBox.Password;
        LocalPasswordBox.PasswordChanged   += (s, e) => _viewModel.LocalPassword   = LocalPasswordBox.Password;

        PrimaryPasswordBox.Password = _viewModel.PrimaryPassword;
        BackupPasswordBox.Password  = _viewModel.BackupPassword;
        LocalPasswordBox.Password   = _viewModel.LocalPassword;
    }
}

