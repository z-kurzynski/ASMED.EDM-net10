using ASMED.WPF.Helpers;
using ASMED.WPF.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;

namespace ASMED.WPF.ViewModels
{
    public class UzytkownicyViewModel : INotifyPropertyChanged
    {
        private readonly AccessDbContext _db = new AccessDbContext();

        public ObservableCollection<User> Users { get; } = new();

        // ✅ Historia logowań
        public ObservableCollection<LoginHistory> LoginHistory { get; } = new();

        private User? _selectedUser;
        public User? SelectedUser
        {
            get => _selectedUser;
            set
            {
                if (_selectedUser != value)
                {
                    _selectedUser = value;
                    OnPropertyChanged();
                    PopulateFormFromSelected();
                }
            }
        }

        // ✅ Pola formularza
        private string ?_formUsername;
        public string ?FormUsername
        {
            get => _formUsername;
            set
            {
                if (_formUsername != value)
                {
                    _formUsername = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested(); // ✅ Odśwież CanExecute
                }
            }
        }

        private string ?_formPassword;
        public string ?FormPassword
        {
            get => _formPassword;
            set
            {
                if (_formPassword != value)
                {
                    _formPassword = value;
                    OnPropertyChanged();
                    CommandManager.InvalidateRequerySuggested(); // ✅ Odśwież CanExecute
                }
            }
        }

        private string ?_formEmail;
        public string ?FormEmail
        {
            get => _formEmail;
            set { if (_formEmail != value) { _formEmail = value; OnPropertyChanged(); } }
        }

        private string ?_formFullName;
        public string ?FormFullName
        {
            get => _formFullName;
            set { if (_formFullName != value) { _formFullName = value; OnPropertyChanged(); } }
        }

        public ObservableCollection<UserRole> AvailableRoles { get; } = new()
        {
            UserRole.SuperAdmin,
            UserRole.Admin,
            UserRole.Recepcja,
            UserRole.Lekarz,
            UserRole.Biuro
        };

        private UserRole _formRole = UserRole.Recepcja;
        public UserRole FormRole
        {
            get => _formRole;
            set { if (_formRole != value) { _formRole = value; OnPropertyChanged(); } }
        }

        private bool _formIsActive = true;
        public bool FormIsActive
        {
            get => _formIsActive;
            set { if (_formIsActive != value) { _formIsActive = value; OnPropertyChanged(); } }
        }

        private bool _showPassword = false;
        public bool ShowPassword
        {
            get => _showPassword;
            set
            {
                if (_showPassword != value)
                {
                    _showPassword = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowPasswordIcon));
                }
            }
        }

        public string ?ShowPasswordIcon => ShowPassword ? "🙈" : "👁️";

        private bool _isEditMode;
        public bool IsEditMode
        {
            get => _isEditMode;
            set { if (_isEditMode != value) { _isEditMode = value; OnPropertyChanged(); OnPropertyChanged(nameof(SaveButtonLabel)); } }
        }

        public string ?SaveButtonLabel => IsEditMode ? "💾 Zapisz zmiany" : "➕ Dodaj użytkownika";

        // ✅ Komendy
        public ICommand ?AddUserCommand { get; }
        public ICommand ?SaveUserCommand { get; }
        public ICommand ?DeleteUserCommand { get; }
        public ICommand ?ClearFormCommand { get; }
        public ICommand ?ChangePasswordCommand { get; }
        public ICommand ?GeneratePasswordCommand { get; }
        public ICommand ?TogglePasswordVisibilityCommand { get; }

        public UzytkownicyViewModel()
        {
            AddUserCommand = new RelayCommand(_ => SaveUser(), _ => CanSaveUser());
            SaveUserCommand = new RelayCommand(_ => SaveUser(), _ => CanSaveUser());
            DeleteUserCommand = new RelayCommand(_ => DeleteUser(), _ => SelectedUser != null);
            ClearFormCommand = new RelayCommand(_ => ClearForm());
            ChangePasswordCommand = new RelayCommand(_ => ChangePassword(), _ => SelectedUser != null);
            GeneratePasswordCommand = new RelayCommand(_ => GeneratePassword());
            TogglePasswordVisibilityCommand = new RelayCommand(_ => TogglePasswordVisibility());

            LoadUsers();
        }

        private void LoadUsers()
        {
            try
            {
                var users = _db.GetAllUsers();
                Users.Clear();
                foreach (var user in users)
                {
                    Users.Add(user);
                }

                // System.Diagnostics.Debug.WriteLine($"✅ Załadowano {Users.Count} użytkowników");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ Błąd ładowania użytkowników: {ex}");
                NotificationHelper.ShowError($"Błąd ładowania użytkowników: {ex.Message}");
            }
        }

        private void PopulateFormFromSelected()
        {
            if (SelectedUser == null)
            {
                ClearForm();
                return;
            }

            IsEditMode = true;
            FormUsername = SelectedUser.Username;
            FormPassword = string.Empty; // ✅ Wyczyść hasło (użytkownik musi wpisać nowe jeśli chce zmienić)
            FormEmail = SelectedUser.Email;
            FormFullName = SelectedUser.FullName;
            FormRole = SelectedUser.Role;
            FormIsActive = SelectedUser.IsActive;

            // System.Diagnostics.Debug.WriteLine($"📝 PopulateFormFromSelected: IsEditMode={IsEditMode}, User={FormUsername}");
        }

        private void ClearForm()
        {
            IsEditMode = false;
            SelectedUser = null;
            FormUsername = string.Empty;
            FormPassword = string.Empty;
            FormEmail = string.Empty;
            FormFullName = string.Empty;
            FormRole = UserRole.Recepcja;
            FormIsActive = true;
        }

        private bool CanSaveUser()
        {
            var canSave = false;

            // Dla nowego użytkownika: wymagane Username i Password
            if (!IsEditMode)
            {
                canSave = !string.IsNullOrWhiteSpace(FormUsername) &&
                          !string.IsNullOrWhiteSpace(FormPassword);
            }
            else
            {
                // Dla edycji: wymagane tylko Username
                canSave = !string.IsNullOrWhiteSpace(FormUsername);
            }

            return canSave;
        }

        private void SaveUser()
        {
            try
            {
                if (IsEditMode && SelectedUser != null)
                {
                    // Aktualizacja
                    bool success = _db.UpdateUser(
                        SelectedUser.Id,
                        FormEmail,
                        FormFullName,
                        FormRole,
                        FormIsActive);

                    if (success)
                    {
                        NotificationHelper.ShowSuccess($"Użytkownik {FormUsername} zaktualizowany");
                        LoadUsers();
                        ClearForm();
                    }
                    else
                    {
                        NotificationHelper.ShowError("Nie udało się zaktualizować użytkownika");
                    }
                }
                else
                {
                    // Dodawanie nowego
                    bool success = _db.AddUser(
                        FormUsername,
                        FormPassword,
                        FormEmail,
                        FormFullName,
                        FormRole);

                    if (success)
                    {
                        NotificationHelper.ShowSuccess($"Użytkownik {FormUsername} dodany");
                        LoadUsers();
                        ClearForm();
                    }
                    else
                    {
                        NotificationHelper.ShowError("Nie udało się dodać użytkownika (możliwe duplikat nazwy)");
                    }
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ SaveUser error: {ex}");
                NotificationHelper.ShowError($"Błąd zapisu: {ex.Message}");
            }
        }

        private void DeleteUser()
        {
            if (SelectedUser == null)
                return;

            var result = MessageBox.Show(
                $"Czy na pewno dezaktywować użytkownika?\n\n" +
                $"Użytkownik: {SelectedUser.Username}\n" +
                $"Rola: {SelectedUser.RoleDisplay}",
                "Potwierdzenie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                bool success = _db.DeleteUser(SelectedUser.Id);

                if (success)
                {
                    NotificationHelper.ShowSuccess($"Użytkownik {SelectedUser.Username} dezaktywowany");
                    LoadUsers();
                    ClearForm();
                }
                else
                {
                    NotificationHelper.ShowError("Nie udało się dezaktywować użytkownika");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ DeleteUser error: {ex}");
                NotificationHelper.ShowError($"Błąd: {ex.Message}");
            }
        }

        private void ChangePassword()
        {
            if (SelectedUser == null)
                return;

            // ✅ Prosty prompt używając własnego generatora
            var newPassword = PasswordHelper.GenerateRandomPassword();

            var result = MessageBox.Show(
                $"Nowe hasło dla użytkownika: {SelectedUser.Username}\n\n" +
                $"Hasło: {newPassword}\n\n" +
                $"Czy zastosować to hasło?",
                "Zmiana hasła",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            bool success = _db.ChangePassword(SelectedUser.Id, newPassword);

            if (success)
            {
                MessageBox.Show(
                    $"Hasło zmienione dla: {SelectedUser.Username}\n\nNowe hasło: {newPassword}\n\nSKOPIUJ I ZAPISZ TO HASŁO!",
                    "Sukces",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            else
            {
                NotificationHelper.ShowError("Nie udało się zmienić hasła");
            }
        }

        private void GeneratePassword()
        {
            FormPassword = PasswordHelper.GenerateRandomPassword();
            MessageBox.Show(
                $"Wygenerowano hasło: {FormPassword}\n\nSkopiuj i zapisz to hasło!",
                "Hasło wygenerowane",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void TogglePasswordVisibility()
        {
            ShowPassword = !ShowPassword;
        }

        // ✅ Ładowanie historii logowań dla wybranego użytkownika
        public void LoadLoginHistoryForUser(User user)
        {
            if (user == null) return;

            try
            {
                // System.Diagnostics.Debug.WriteLine($"📜 Ładuję historię logowań dla: {user.Username} (ID={user.Id})");

                var history = _db.GetLoginHistory(user.Id, maxRecords: 50);

                LoginHistory.Clear();
                foreach (var entry in history)
                {
                    LoginHistory.Add(entry);
                }

                // System.Diagnostics.Debug.WriteLine($"📜 Załadowano {LoginHistory.Count} wpisów historii");

                NotificationHelper.ShowInfo(
                    "Historia logowań",
                    $"Załadowano {LoginHistory.Count} wpisów dla {user.Username}");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ LoadLoginHistoryForUser error: {ex.Message}");
                NotificationHelper.ShowError($"Błąd ładowania historii: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

