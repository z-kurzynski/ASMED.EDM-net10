namespace ASMED.EDM.UI.Services;

/// <summary>
/// Serwis nawigacji między widokami
/// </summary>
public interface INavigationService
{
    /// <summary>
    /// Przejdź do widoku
    /// </summary>
    Task NavigateToAsync<TViewModel>() where TViewModel : class;

    /// <summary>
    /// Przejdź do widoku z parametrem
    /// </summary>
    Task NavigateToAsync<TViewModel>(object parameter) where TViewModel : class;

    /// <summary>
    /// Powrót do poprzedniego widoku
    /// </summary>
    Task GoBackAsync();

    /// <summary>
    /// Czy można wrócić do poprzedniego widoku
    /// </summary>
    bool CanGoBack { get; }
}

/// <summary>
/// Serwis dialogów i komunikatów
/// </summary>
public interface IDialogService
{
    /// <summary>
    /// Pokaż komunikat informacyjny
    /// </summary>
    Task ShowMessageAsync(string title, string message);

    /// <summary>
    /// Pokaż komunikat z pytaniem (Yes/No)
    /// </summary>
    Task<bool> ShowConfirmationAsync(string title, string message);

    /// <summary>
    /// Pokaż błąd
    /// </summary>
    Task ShowErrorAsync(string title, string message);

    /// <summary>
    /// Pokaż okno dialogowe z własnym contentem
    /// </summary>
    Task<T?> ShowDialogAsync<T>(string title, object content) where T : class;
}
