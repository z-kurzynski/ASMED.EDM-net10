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

    public LoginWindow(LoginViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;

        _viewModel.RequestClose = () =>
        {
            DialogResult = true;
            Close();
        };
    }

    private async void LoginButton_Click(object sender, RoutedEventArgs e)
    {
        await _viewModel.LoginCommand.ExecuteAsync(PasswordBox.Password);
    }

    private async void PasswordBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            await _viewModel.LoginCommand.ExecuteAsync(PasswordBox.Password);
        }
    }
}
