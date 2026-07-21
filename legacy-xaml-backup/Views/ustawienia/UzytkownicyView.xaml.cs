using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using ASMED.WPF.ViewModels;
using ASMED.WPF.Models;

namespace ASMED.WPF.Views
{
    public partial class UzytkownicyView : UserControl
    {
        public UzytkownicyView()
        {
            InitializeComponent();

            // ✅ Subskrybuj zmiany FormPassword z ViewModelu (gdy generator generuje hasło lub toggle)
            Loaded += (s, e) =>
            {
                if (DataContext is UzytkownicyViewModel vm)
                {
                    vm.PropertyChanged += (sender, args) =>
                    {
                        if (args.PropertyName == nameof(UzytkownicyViewModel.FormPassword))
                        {
                            // Ustaw hasło w PasswordBox gdy ViewModel je zmieni
                            PasswordBox.Password = vm.FormPassword;
                        }

                        // ✅ NOWE: Wyczyść PasswordBox gdy użytkownik jest wybierany
                        if (args.PropertyName == nameof(UzytkownicyViewModel.SelectedUser))
                        {
                            if (vm.SelectedUser != null)
                            {
                                // Tryb edycji - wyczyść hasło (użytkownik musi wpisać nowe jeśli chce zmienić)
                                PasswordBox.Password = string.Empty;
                                PasswordTextBox.Text = string.Empty;
                            }
                            else
                            {
                                // Tryb dodawania - wyczyść hasło
                                PasswordBox.Password = string.Empty;
                                PasswordTextBox.Text = string.Empty;
                            }
                        }

                        // ✅ NOWE: Wyczyść PasswordBox gdy IsEditMode się zmienia
                        if (args.PropertyName == nameof(UzytkownicyViewModel.IsEditMode))
                        {
                            if (!vm.IsEditMode)
                            {
                                // Tryb dodawania - wyczyść hasło
                                PasswordBox.Password = string.Empty;
                                PasswordTextBox.Text = string.Empty;
                            }
                        }
                    };
                }
            };
        }

        /// <summary>
        /// ✅ Obsługa dwukrotnego kliknięcia - pokaż historię logowań
        /// </summary>
        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is UzytkownicyViewModel vm && vm.SelectedUser != null)
            {
                // System.Diagnostics.Debug.WriteLine($"📜 Dwukrotne kliknięcie na użytkownika: {vm.SelectedUser.Username}");

                // Załaduj historię logowań
                vm.LoadLoginHistoryForUser(vm.SelectedUser);
            }
        }

        /// <summary>
        /// ✅ Obsługa zmiany hasła w PasswordBox (binding nie działa standardowo)
        /// Synchronizuje PasswordBox → ViewModel → TextBox
        /// </summary>
        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (sender is PasswordBox passwordBox && DataContext is UzytkownicyViewModel vm)
            {
                // Zapobiegaj cyklicznej aktualizacji
                if (vm.FormPassword != passwordBox.Password)
                {
                    vm.FormPassword = passwordBox.Password;

                    // ✅ KLUCZOWE: Wymuś odświeżenie CanExecute
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        /// <summary>
        /// ✅ Obsługa przycisku Save (Click handler zamiast Command, bo Command nie działał)
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is UzytkownicyViewModel vm)
            {
                // ✅ Wywołaj SaveUser() z ViewModelu
                var saveMethod = vm.GetType().GetMethod("SaveUser",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                saveMethod?.Invoke(vm, null);
            }
        }
    }
}



