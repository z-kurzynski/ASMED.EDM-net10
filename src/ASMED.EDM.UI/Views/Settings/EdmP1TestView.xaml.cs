using ASMED.EDM.P1.Middleware;
using ASMED.EDM.P1.Middleware.Models;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Settings;

public partial class EdmP1TestView : UserControl
{
    public EdmP1TestView()
    {
        InitializeComponent();
    }

    private async void SearchButton_Click(object sender, RoutedEventArgs e)
    {
        var pesel = PeselTextBox.Text.Trim();
        if (string.IsNullOrEmpty(pesel))
        {
            ResultTextBox.Text = "Podaj PESEL pacjenta.";
            return;
        }

        SearchButton.IsEnabled = false;
        ResultTextBox.Text = "Trwa wyszukiwanie...";

        try
        {
            var client = ((App)Application.Current).GetService<IMiddlewareMedycynaPracyClient>();

            var kryteria = new MpKryteriaWyszukiwaniaPacjenta
            {
                Identyfikator = pesel,
                Imie = ImieTextBox.Text.Trim(),
                Nazwisko = NazwiskoTextBox.Text.Trim()
            };
            var wynik = await client.WyszukajPacjentaAsync(kryteria);

            if (wynik.IsError)
            {
                ResultTextBox.Text = $"Błąd API: {wynik.MessagesText ?? wynik.ErrorMessage ?? "(brak szczegółów)"}";
            }
            else
            {
                ResultTextBox.Text = JsonSerializer.Serialize(wynik.Pacjent, new JsonSerializerOptions { WriteIndented = true });

                var resource = wynik.Pacjent?.Entry?.FirstOrDefault()?.Resource;
                PacjentIdTextBox.Text = resource?.Id ?? string.Empty;

                if (resource is not null)
                {
                    if (!string.IsNullOrEmpty(resource.Imie))
                    {
                        ImieTextBox.Text = resource.Imie;
                    }
                    if (!string.IsNullOrEmpty(resource.Nazwisko))
                    {
                        NazwiskoTextBox.Text = resource.Nazwisko;
                    }
                    if (!string.IsNullOrEmpty(resource.Plec))
                    {
                        PlecTextBox.Text = resource.Plec;
                    }
                    if (!string.IsNullOrEmpty(resource.BirthDate))
                    {
                        DataUrodzeniaTextBox.Text = resource.BirthDate;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            ResultTextBox.Text = $"Błąd: {ex.Message}";
        }
        finally
        {
            SearchButton.IsEnabled = true;
        }
    }

    private async void ReadButton_Click(object sender, RoutedEventArgs e)
    {
        var id = PacjentIdTextBox.Text.Trim();
        if (string.IsNullOrEmpty(id))
        {
            ResultTextBox.Text = "Najpierw wyszukaj pacjenta, aby uzyskać ID (lub wpisz je ręcznie).";
            return;
        }

        ReadButton.IsEnabled = false;
        ResultTextBox.Text = "Trwa odczyt pacjenta...";

        try
        {
            var client = ((App)Application.Current).GetService<IMiddlewareMedycynaPracyClient>();

            var wynik = await client.OdczytajPacjentaAsync(id);

            if (wynik.IsError)
            {
                ResultTextBox.Text = $"Błąd API: {wynik.MessagesText ?? wynik.ErrorMessage ?? "(brak szczegółów)"}";
            }
            else
            {
                ResultTextBox.Text = JsonSerializer.Serialize(wynik.Pacjent, new JsonSerializerOptions { WriteIndented = true });
            }
        }
        catch (Exception ex)
        {
            ResultTextBox.Text = $"Błąd: {ex.Message}";
        }
        finally
        {
            ReadButton.IsEnabled = true;
        }
    }

    private MpOrzeczenie? BuildOrzeczenieFromForm()
    {
        var pesel = PeselTextBox.Text.Trim();
        if (string.IsNullOrEmpty(pesel))
        {
            OrzeczenieResultTextBox.Text = "Podaj PESEL pacjenta (pole powyżej, w sekcji odczytu pacjenta).";
            return null;
        }

        return new MpOrzeczenie
        {
            NumerDokumentu = NumerDokumentuTextBox.Text.Trim(),
            NumerSkierowania = NumerSkierowaniaTextBox.Text.Trim(),
            DataSkierowania = DataSkierowaniaTextBox.Text.Trim(),
            Pacjent = new MpPacjent
            {
                Identyfikator = pesel,
                Imie = ImieTextBox.Text.Trim(),
                Nazwisko = NazwiskoTextBox.Text.Trim(),
                Plec = PlecTextBox.Text.Trim(),
                DataUrodzenia = DataUrodzeniaTextBox.Text.Trim()
            },
            RodzajBadania = RodzajBadaniaTextBox.Text.Trim(),
            RodzajZatrudnienia = RodzajZatrudnieniaTextBox.Text.Trim(),
            Stanowiska =
            [
                new MpStanowisko
                {
                    Zawod = ZawodTextBox.Text.Trim(),
                    Pracodawca = new MpPracodawca
                    {
                        Nip = PracodawcaNipTextBox.Text.Trim(),
                        Regon9 = PracodawcaRegon9TextBox.Text.Trim(),
                        Nazwa = PracodawcaNazwaTextBox.Text.Trim(),
                        Adres = new MpAdres
                        {
                            Miejscowosc = PracodawcaMiejscowoscTextBox.Text.Trim(),
                            KodPocztowy = PracodawcaKodPocztowyTextBox.Text.Trim(),
                            Ulica = PracodawcaUlicaTextBox.Text.Trim(),
                            NrDomu = PracodawcaNrDomuTextBox.Text.Trim()
                        },
                        Telefon = PracodawcaTelefonTextBox.Text.Trim()
                    }
                }
            ],
            Werdykt = new MpWerdykt
            {
                Symbol = WerdyktSymbolTextBox.Text.Trim()
            },
            DataNastepnegoBadania = DataNastepnegoBadaniaTextBox.Text.Trim(),
            MiejsceWystawienia = MiejsceWystawieniaTextBox.Text.Trim()
        };
    }

    private async void WeryfikujOrzeczenieButton_Click(object sender, RoutedEventArgs e)
    {
        var orzeczenie = BuildOrzeczenieFromForm();
        if (orzeczenie is null)
        {
            return;
        }

        WeryfikujOrzeczenieButton.IsEnabled = false;
        OrzeczenieResultTextBox.Text = "Trwa weryfikacja orzeczenia (bez zapisu)...";

        try
        {
            var client = ((App)Application.Current).GetService<IMiddlewareMedycynaPracyClient>();
            var wynik = await client.WeryfikujOrzeczenieAsync(orzeczenie);

            OrzeczenieResultTextBox.Text = wynik.IsError
                ? $"Błąd API: {wynik.MessagesText ?? wynik.ErrorMessage ?? "(brak szczegółów)"}"
                : $"Weryfikacja OK.{(string.IsNullOrEmpty(wynik.MessagesText) ? string.Empty : $" Komunikaty: {wynik.MessagesText}")}";
        }
        catch (Exception ex)
        {
            OrzeczenieResultTextBox.Text = $"Błąd: {ex.Message}";
        }
        finally
        {
            WeryfikujOrzeczenieButton.IsEnabled = true;
        }
    }

    private async void WystawOrzeczenieButton_Click(object sender, RoutedEventArgs e)
    {
        var orzeczenie = BuildOrzeczenieFromForm();
        if (orzeczenie is null)
        {
            return;
        }

        WystawOrzeczenieButton.IsEnabled = false;
        OrzeczenieResultTextBox.Text = "Trwa wysyłka orzeczenia do P1...";

        try
        {
            var client = ((App)Application.Current).GetService<IMiddlewareMedycynaPracyClient>();
            var wynik = await client.WystawOrzeczenieAsync(orzeczenie);

            OrzeczenieResultTextBox.Text = wynik.IsError
                ? $"Błąd API: {wynik.MessagesText ?? wynik.ErrorMessage ?? "(brak szczegółów)"}"
                : $"Orzeczenie wystawione poprawnie.{(string.IsNullOrEmpty(wynik.MessagesText) ? string.Empty : $" Komunikaty: {wynik.MessagesText}")}";
        }
        catch (Exception ex)
        {
            OrzeczenieResultTextBox.Text = $"Błąd: {ex.Message}";
        }
        finally
        {
            WystawOrzeczenieButton.IsEnabled = true;
        }
    }

    private MpPacjent BuildPacjentFromRegForm()
    {
        return new MpPacjent
        {
            Identyfikator = RegPeselTextBox.Text.Trim(),
            Imie = RegImieTextBox.Text.Trim(),
            Nazwisko = RegNazwiskoTextBox.Text.Trim(),
            Plec = RegPlecTextBox.Text.Trim(),
            DataUrodzenia = RegDataUrodzeniaTextBox.Text.Trim(),
            Adres = new MpAdres
            {
                Miejscowosc = RegMiejscowoscTextBox.Text.Trim(),
                KodPocztowy = RegKodPocztowyTextBox.Text.Trim(),
                Ulica = RegUlicaTextBox.Text.Trim(),
                NrDomu = RegNrDomuTextBox.Text.Trim(),
                NrLokalu = RegNrLokaluTextBox.Text.Trim()
            }
        };
    }

    private async void RegisterPatientButton_Click(object sender, RoutedEventArgs e)
    {
        var pesel = RegPeselTextBox.Text.Trim();
        if (string.IsNullOrEmpty(pesel))
        {
            RegResultTextBox.Text = "Podaj PESEL pacjenta.";
            return;
        }

        RegisterPatientButton.IsEnabled = false;
        RegResultTextBox.Text = "Trwa rejestracja pacjenta...";

        try
        {
            var client = ((App)Application.Current).GetService<IMiddlewareMedycynaPracyClient>();
            var pacjent = BuildPacjentFromRegForm();
            var wynik = await client.ZarejestrujPacjentaAsync(pacjent);

            if (wynik.IsError)
            {
                RegResultTextBox.Text = $"Błąd API: {wynik.MessagesText ?? wynik.ErrorMessage ?? "(brak szczegółów)"}";
            }
            else
            {
                RegResultTextBox.Text = JsonSerializer.Serialize(wynik.Pacjent, new JsonSerializerOptions { WriteIndented = true });

                var id = wynik.Pacjent?.Id;
                if (!string.IsNullOrEmpty(id))
                {
                    RegPacjentIdTextBox.Text = id;
                }
            }
        }
        catch (Exception ex)
        {
            RegResultTextBox.Text = $"Błąd: {ex.Message}";
        }
        finally
        {
            RegisterPatientButton.IsEnabled = true;
        }
    }

    private async void UpdatePatientButton_Click(object sender, RoutedEventArgs e)
    {
        var id = RegPacjentIdTextBox.Text.Trim();
        if (string.IsNullOrEmpty(id))
        {
            RegResultTextBox.Text = "Podaj ID pacjenta do aktualizacji (z rejestracji lub wyszukania po lewej).";
            return;
        }

        UpdatePatientButton.IsEnabled = false;
        RegResultTextBox.Text = "Trwa aktualizacja pacjenta...";

        try
        {
            var client = ((App)Application.Current).GetService<IMiddlewareMedycynaPracyClient>();
            var pacjent = BuildPacjentFromRegForm();
            var wynik = await client.AktualizujPacjentaAsync(id, pacjent);

            RegResultTextBox.Text = wynik.IsError
                ? $"Błąd API: {wynik.MessagesText ?? wynik.ErrorMessage ?? "(brak szczegółów)"}"
                : JsonSerializer.Serialize(wynik.Pacjent, new JsonSerializerOptions { WriteIndented = true });
        }
        catch (Exception ex)
        {
            RegResultTextBox.Text = $"Błąd: {ex.Message}";
        }
        finally
        {
            UpdatePatientButton.IsEnabled = true;
        }
    }
}

