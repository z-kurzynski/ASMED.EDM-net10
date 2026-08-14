using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ASMED.EDM.UI.ViewModels.Settings;
using Microsoft.Extensions.DependencyInjection;

namespace ASMED.EDM.UI.Views.Settings;

public partial class UsersView : UserControl
{
    public UsersView()
    {
        InitializeComponent();

        if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
            return;

        Loaded += UsersView_Loaded;
    }

    private async void UsersView_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not UsersViewModel viewModel)
        {
            if (Application.Current is App app && app.Host != null)
            {
                viewModel = app.Host.Services.GetRequiredService<UsersViewModel>();
                DataContext = viewModel;
            }
            else
            {
                return;
            }
        }

        viewModel.PropertyChanged += (s, args) =>
        {
            if (args.PropertyName == nameof(UsersViewModel.FormPassword))
            {
                if (PasswordBox.Password != viewModel.FormPassword)
                {
                    PasswordBox.Password = viewModel.FormPassword;
                }
            }

            if (args.PropertyName == nameof(UsersViewModel.SelectedUser) ||
                args.PropertyName == nameof(UsersViewModel.IsEditMode))
            {
                PasswordBox.Password = string.Empty;
                PasswordTextBox.Text = string.Empty;
            }
        };

        await viewModel.OnNavigatedToAsync();
    }

    private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (sender is PasswordBox passwordBox && DataContext is UsersViewModel viewModel)
        {
            if (viewModel.FormPassword != passwordBox.Password)
            {
                viewModel.FormPassword = passwordBox.Password;
            }
        }
    }

    private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is UsersViewModel viewModel && viewModel.SelectedUser != null)
        {
            viewModel.LoadLoginHistoryCommand.Execute(null);
        }
    }
}
