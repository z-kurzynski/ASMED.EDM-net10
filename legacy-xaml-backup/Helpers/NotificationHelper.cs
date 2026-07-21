//using Notifications.Wpf;
using Notifications.Wpf.Core;
using System;
using System.Windows;

namespace ASMED.WPF.Helpers
{
    public static class NotificationHelper
    {
        // lazy manager, ensure created on UI thread
        private static NotificationManager? _manager;

        private static void EnsureManagerInitialized()
        {
            if (_manager != null) return;
            // Try to create on UI thread if possible
            try
            {
                if (Application.Current?.Dispatcher != null)
                {
                    Application.Current.Dispatcher.Invoke(() => { if (_manager == null) _manager = new NotificationManager(); });
                }
                else
                {
                    _manager = new NotificationManager();
                }
            }
            catch
            {
                // fallback
                _manager ??= new NotificationManager();
            }
        }

        private static void ShowOnUiThread(NotificationContent content, TimeSpan expirationTime)
        {
            try
            {
                if (Application.Current?.Dispatcher != null)
                {
                    Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        EnsureManagerInitialized();
                        try { _manager?.ShowAsync(content, expirationTime: expirationTime); } catch { }
                    }));
                }
                else
                {
                    EnsureManagerInitialized();
                    try { _manager?.ShowAsync(content, expirationTime: expirationTime); } catch { }
                }
            }
            catch
            {
                // swallow to avoid crashing the app due to notification failures
            }
        }

        public static void ShowPatientSaved()
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Pacjent zapisany",
                Message = "Dane pacjenta zostały zapisane.",
                Type = NotificationType.Success,
            }, TimeSpan.FromSeconds(1));
        }

        public static void ShowPatientUpdate()
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Pacjent zapisany",
                Message = "Dane pacjenta zostały Zmienione.",
                Type = NotificationType.Success,
            }, TimeSpan.FromSeconds(1));
        }

        public static void ShowPatientDeleted()
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Pacjent usunięty",
                Message = "Dane pacjenta zostały usunięte.",
                Type = NotificationType.Error,
            }, TimeSpan.FromSeconds(1));
        }
        public static void ShowPatientNotFound()
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Pacjent nie znaleziony",
                Message = "Nie znaleziono pacjenta o podanym identyfikatorze.",
                Type = NotificationType.Warning,
            }, TimeSpan.FromSeconds(1));
        }
        public static void ShowPatientAlreadyExists()
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Pacjent już istnieje",
                Message = "Pacjent o podanym identyfikatorze już istnieje.",
                Type = NotificationType.Warning,
            }, TimeSpan.FromSeconds(1));
        }
        public static void ShowRefferalSaved()
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Skierowanie Zapisane",
                Message = "Dane Skierowaniw]a zapisane.",
                Type = NotificationType.Success,
            }, TimeSpan.FromSeconds(1));
        }
        public static void ShowRefferalDeleted()
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Skierowanie Usunięte",
                Message = "Dane Skierowania zostały usunięte.",
                Type = NotificationType.Error,
            }, TimeSpan.FromSeconds(1));
        }
        public static void ShowRefferalNotFound()
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Skierowanie nie znalezione",
                Message = "Nie znaleziono skierowania o podanym identyfikatorze.",
                Type = NotificationType.Warning,
            }, TimeSpan.FromSeconds(1));
        }
        public static void ShowRefferalAlreadyExists()
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Skierowanie już istnieje",
                Message = "Skierowanie o podanym identyfikatorze już istnieje.",
                Type = NotificationType.Warning,
            }, TimeSpan.FromSeconds(1));
        }
        public static void ShowRegistrationSaved()
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Dodany do Kalendarza",
                Message = "Data jest zapisana.",
                Type = NotificationType.Success,
            }, TimeSpan.FromSeconds(1));
        }
        public static void ShowRegistrationDeleted()
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Usunięty z Kalendarza",
                Message = "Data jest usunięta.",
                Type = NotificationType.Error,
            }, TimeSpan.FromSeconds(1));
        }
        public static void ShowRegistrationUpdate()
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Rejestracja Zmieniona",
                Message = "Zmiana.",
                Type = NotificationType.Warning,
            }, TimeSpan.FromSeconds(1));
        }
        public static void ShowRegistrationAlreadyExists()
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Rejestracja już istnieje",
                Message = "Rejestracja o podanym identyfikatorze już istnieje.",
                Type = NotificationType.Warning,
            }, TimeSpan.FromSeconds(1));
        }


        public static void ShowValidationError(string message)
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Błąd walidacji",
                Message = message,
                Type = NotificationType.Warning,
            }, TimeSpan.FromSeconds(1));
        }

        public static void ShowError(string message)
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Błąd",
                Message = message,
                Type = NotificationType.Error,
            }, TimeSpan.FromSeconds(1));
        }
        public static void ShowInfo(string message, string v)
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Informacja",
                Message = message,
                Type = NotificationType.Information,
            }, TimeSpan.FromSeconds(1));
        }
        public static void ShowWarning(string message)
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Ostrzeżenie",
                Message = message,
                Type = NotificationType.Warning,
            }, TimeSpan.FromSeconds(1));
        }
        public static void PrintSuccess(string message)
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = "Sukces",
                Message = message,
                Type = NotificationType.Success,
            }, TimeSpan.FromSeconds(1));
        }

        /// <summary>
        /// Wyświetla ogólne powiadomienie z własnym tytułem i wiadomością
        /// </summary>
        public static void ShowNotification(string title, string message, NotificationType type = NotificationType.Information, int durationSeconds = 3)
        {
            ShowOnUiThread(new NotificationContent
            {
                Title = title,
                Message = message,
                Type = type,
            }, TimeSpan.FromSeconds(durationSeconds));
        }

        /// <summary>
        /// Wyświetla powiadomienie sukcesu
        /// </summary>
        public static void ShowSuccess(string message)
        {
            ShowNotification("Sukces", message, NotificationType.Success, 1);
        }
    }
}
