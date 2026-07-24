using CommunityToolkit.Mvvm.ComponentModel;

namespace ASMED.EDM.UI.ViewModels;

/// <summary>
/// Bazowa klasa dla wszystkich ViewModels w aplikacji
/// </summary>
public abstract class ViewModelBase : ObservableObject
{
    private bool _isBusy;
    private string? _busyMessage;

    /// <summary>
    /// Czy ViewModel jest w trakcie operacji
    /// </summary>
    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (SetProperty(ref _isBusy, value))
            {
                OnPropertyChanged(nameof(IsNotBusy));
            }
        }
    }

    /// <summary>
    /// Negacja IsBusy (użyteczne dla bindingu IsEnabled)
    /// </summary>
    public bool IsNotBusy => !IsBusy;

    /// <summary>
    /// Komunikat podczas operacji
    /// </summary>
    public string? BusyMessage
    {
        get => _busyMessage;
        set => SetProperty(ref _busyMessage, value);
    }

    /// <summary>
    /// Metoda wywoływana przy aktywacji widoku
    /// </summary>
    public virtual Task OnNavigatedToAsync()
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Metoda wywoływana przy opuszczaniu widoku
    /// </summary>
    public virtual Task OnNavigatedFromAsync()
    {
        return Task.CompletedTask;
    }

    public static implicit operator ViewModelBase(PatientsViewModel v)
    {
        throw new NotImplementedException();
    }
}
