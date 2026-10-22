using ASMED.EDM.UI.ViewModels;
using System.Windows;
using System.Windows.Input;

namespace ASMED.EDM.UI;

/// <summary>
/// Interaction logic for LoginWindow.xaml
/// </summary>
public partial class LoginWindow : Window
{
    private readonly LoginViewModel _viewModel;
    private bool _closeRequested;

    public LoginViewModel ViewModel => _viewModel;

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.RequestClose = () =>
        {
            _closeRequested = true;
            DialogResult = true;
            Close();
        };

        Closing += (_, e) =>
        {
            // Nie pozwól zamknąć okna w trakcie ładowania programu po zalogowaniu
            if (_viewModel.IsLoadingApplication && !_closeRequested)
                e.Cancel = true;
        };
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        await LoginAsync();
    }

    private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await LoginAsync();
        }
    }

    private async Task LoginAsync()
    {
        if (_viewModel.IsBusy)
            return;

        await _viewModel.LoginCommand.ExecuteAsync(PasswordBox.Password);

        if (!_closeRequested)
            PasswordBox.Focus();
    }
}
