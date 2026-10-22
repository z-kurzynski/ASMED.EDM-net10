using ASMED.EDM.UI.ViewModels.Skierowania;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Skierowania
{
    public partial class SkierPacjentaEditView : UserControl
    {
        public SkierPacjentaEditView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void DodajKarteBadan_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SkierPacjentaEditViewModel vm)
            {
                MessageBox.Show("Brak kontekstu widoku.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null) return;

            if (Application.Current is not App app || app.Host == null)
            {
                MessageBox.Show("Brak konfiguracji DI.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Zdobądź VM formularza nowej karty z kontenera i załaduj dane pacjenta
            var referralVm = app.Host.Services.GetService(typeof(SkierNewPacjentaViewModel)) as SkierNewPacjentaViewModel;
            if (referralVm == null)
            {
                MessageBox.Show("Nie można uzyskać SkierNewPacjentaViewModel z kontenera DI.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Uzupełnij dane pacjenta. Używamy pól z aktualnego VM edycji pacjenta.
            var pacjent = new ASMED.EDM.UI.Models.PacjentSkier
            {
                P_ID = vm.PacjentId,
                FirstName = vm.Imie ?? string.Empty,
                LastName = vm.Nazwisko ?? string.Empty,
                PESEL = vm.PESEL ?? string.Empty,
                Company = vm.WybranaFirma?.Nazwa ?? string.Empty
            };

            referralVm.LoadPacjent(pacjent);

            var referralView = new SkierNewPacjentaView { DataContext = referralVm };

            referralVm.RequestPowrot += () =>
            {
                // Po powrocie przywróć ten widok edycji pacjenta
                mainWindow.SetNowaKartaContent(this);
            };

            mainWindow.SetNowaKartaContent(referralView);
        }

        public SkierPacjentaEditView(SkierPacjentaEditViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SkierPacjentaEditViewModel
                && !System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)
                && Application.Current is App app && app.Host != null)
            {
                DataContext = app.Host.Services.GetService(typeof(SkierPacjentaEditViewModel))
                              as SkierPacjentaEditViewModel;
            }

            if (DataContext is SkierPacjentaEditViewModel vm)
                vm.NavigateCopyPacjent = OpenKopiaWNowejFirmie;
        }

        /// <summary>
        /// Kopiuje dane osobowe i adresowe aktualnego pacjenta, otwiera pusty formularz
        /// nowego pacjenta z wstępnie uzupełnionymi polami — ten sam wzorzec co OpenNowyPacjent
        /// w SkierListaPacjentowView, ale z pre-fill danych.
        /// </summary>
        private void OpenKopiaWNowejFirmie()
        {
            if (DataContext is not SkierPacjentaEditViewModel sourceVm) return;

            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null) return;

            if (Application.Current is not App app || app.Host == null) return;

            var newVm = app.Host.Services.GetService(typeof(SkierPacjentaEditViewModel))
                        as SkierPacjentaEditViewModel;
            if (newVm == null) return;

            // Skopiuj dane osobowe i kontaktowe — ID = 0 (nowy pacjent)
            newVm.Imie        = sourceVm.Imie;
            newVm.Nazwisko    = sourceVm.Nazwisko;
            newVm.PESEL       = sourceVm.PESEL;
            newVm.Ulica       = sourceVm.Ulica;
            newVm.KodPocztowy = sourceVm.KodPocztowy;
            newVm.Miejscowosc = sourceVm.Miejscowosc;
            newVm.Email       = sourceVm.Email;
            newVm.Telefon     = sourceVm.Telefon;

            var newView = new SkierPacjentaEditView(newVm);

            newVm.RequestPowrot += () =>
            {
                // Po zapisie/anulowaniu wróć do tego widoku edycji
                mainWindow.SetNowaKartaContent(this);
            };

            mainWindow.SetNowaKartaContent(newView);
        }

        private void PobierzZEdm_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SkierPacjentaEditViewModel vm)
            {
                MessageBox.Show("Brak kontekstu widoku.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (vm.WeryfikujPobierzZP1Command.CanExecute(null))
                vm.WeryfikujPobierzZP1Command.Execute(null);
        }

        private void SprawdzPesel_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SkierPacjentaEditViewModel vm)
            {
                MessageBox.Show("Brak kontekstu widoku.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var pesel = vm.PESEL?.Trim();
            if (string.IsNullOrEmpty(pesel))
            {
                MessageBox.Show("Pole PESEL jest puste.", "Sprawdź PESEL", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                using var conn = vm.GetDbConnection();
                conn.Open();

                // Różne przypadki nazwy kolumny występują w kodzie (P_pesel, P_PESEL). Spróbuj po kolei.
                var candidates = new[] { "P_pesel", "P_PESEL", "P_Pesel", "P_PESel" };
                bool found = false;

                foreach (var col in candidates)
                {
                    try
                    {
                        using var cmd = (MySqlConnector.MySqlCommand)conn.CreateCommand();
                        cmd.CommandText = $@"SELECT P_nazwisko, P_imie, IFNULL(F.Nazwa, '') AS Firma
                                            FROM P_Pacjent P
                                            LEFT JOIN Firma F ON P.P_Firma_id = F.id
                                            WHERE TRIM(P.{col}) = @pesel";
                        cmd.Parameters.AddWithValue("@pesel", pesel);

                        using var reader = cmd.ExecuteReader();
                        if (reader.Read())
                        {
                            var nazwisko = reader["P_nazwisko"]?.ToString() ?? string.Empty;
                            var imie = reader["P_imie"]?.ToString() ?? string.Empty;
                            var firma = reader["Firma"]?.ToString() ?? string.Empty;
                            MessageBox.Show($"Znaleziono: {nazwisko} {imie} {firma}", "Sprawdź PESEL", MessageBoxButton.OK, MessageBoxImage.Information);
                            found = true;
                            break;
                        }
                    }
                    catch (MySqlConnector.MySqlException)
                    {
                        // Kolumna nie istnieje w schemacie — spróbuj następnej nazwy
                        continue;
                    }
                }

                if (!found)
                {
                    MessageBox.Show("brak", "Sprawdź PESEL", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd sprawdzania PESEL:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
