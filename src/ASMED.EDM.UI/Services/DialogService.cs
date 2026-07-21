using System.Windows;

namespace ASMED.EDM.UI.Services;

/// <summary>
/// Implementacja dialogów za pomocą MessageBox
/// </summary>
public class DialogService : IDialogService
{
    public Task ShowMessageAsync(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        return Task.CompletedTask;
    }

    public Task<bool> ShowConfirmationAsync(string title, string message)
    {
        var result = MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question);
        return Task.FromResult(result == MessageBoxResult.Yes);
    }

    public Task ShowErrorAsync(string title, string message)
    {
        MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        return Task.CompletedTask;
    }

    public Task<T?> ShowDialogAsync<T>(string title, object content) where T : class
    {
        // TODO: Implementacja dla custom dialogów
        throw new NotImplementedException("Custom dialogs not implemented yet");
    }
}
