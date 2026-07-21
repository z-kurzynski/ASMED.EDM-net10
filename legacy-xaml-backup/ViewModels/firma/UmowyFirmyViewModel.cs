using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data.Odbc;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ASMED.WPF.Models;
using ASMED.WPF.Helpers;

namespace ASMED.WPF.ViewModels
{
    public class UmowyFirmyViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private readonly Firma _firma;

        public ICommand ?DodajUmoweCommand { get; }
        public ICommand ?EdytujUmoweCommand { get; }
        public ICommand ?UsunUmoweCommand { get; }
        public ICommand ?ZamknijCommand { get; }

        private ObservableCollection<Umowa> _umowy = new ObservableCollection<Umowa>();
        public ObservableCollection<Umowa> Umowy
        {
            get => _umowy;
            set { _umowy = value; OnPropertyChanged(); }
        }

        private string ?_tytul;
        public string ?Tytul
        {
            get => _tytul;
            set { _tytul = value; OnPropertyChanged(); }
        }

        public UmowyFirmyViewModel(Firma firma)
        {
            _firma = firma ?? throw new ArgumentNullException(nameof(firma));
            Tytul = $"Umowy firmy: {firma.Nazwa}";

            DodajUmoweCommand = new RelayCommand(_ => DodajUmowe());
            EdytujUmoweCommand = new RelayCommand<Umowa>(EdytujUmowe);
            UsunUmoweCommand = new RelayCommand<Umowa>(UsunUmowe);
            ZamknijCommand = new RelayCommand<Window>(w => w?.Close());

            LoadUmowy();
        }

        private void LoadUmowy()
        {
            var db = new AccessDbHelper();
            var umowy = new ObservableCollection<Umowa>();

            try
            {
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    var cmd = new OdbcCommand(@"
                        SELECT 
                            Id, 
                            Firma_ID,
                            nr_umowy,
                            Data_Umowy, 
                            Ilosc_Miesiecy, 
                            Czy_Terminowa, 
                            Status, 
                            Budzet, 
                            Data_Koncowa
                        FROM Umowy_Firm
                        WHERE Firma_ID = ?
                        ORDER BY Data_Umowy DESC", conn);

                    cmd.Parameters.AddWithValue("@FirmaId", _firma.id);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            try
                            {
                                var umowa = new Umowa
                                {
                                    Id = reader["Id"] != DBNull.Value ? Convert.ToInt32(reader["Id"]) : 0,
                                    FirmaId = reader["Firma_ID"] != DBNull.Value ? Convert.ToInt32(reader["Firma_ID"]) : 0,
                                    FirmaNazwa = _firma.Nazwa,
                                    NrUmowy = reader["nr_umowy"]?.ToString() ?? "",
                                    DataUmowy = reader["Data_Umowy"] != DBNull.Value ? Convert.ToDateTime(reader["Data_Umowy"]) : DateTime.Now,
                                    IloscMiesiecy = reader["Ilosc_Miesiecy"] != DBNull.Value ? Convert.ToInt32(reader["Ilosc_Miesiecy"]) : 0,
                                    Status = reader["Status"]?.ToString() ?? "Aktywna",
                                    Budzet = reader["Budzet"] != DBNull.Value ? Convert.ToDecimal(reader["Budzet"]) : 0,
                                    DataKoncowa = reader["Data_Koncowa"] != DBNull.Value ? Convert.ToDateTime(reader["Data_Koncowa"]) : (DateTime?)null
                                };

                                // Obsługa pola Czy_Terminowa (różne formaty w Access)
                                if (reader["Czy_Terminowa"] != DBNull.Value)
                                {
                                    var czyTerminowaValue = reader["Czy_Terminowa"];
                                    if (czyTerminowaValue is bool boolValue)
                                    {
                                        umowa.CzyTerminowa = boolValue;
                                    }
                                    else if (czyTerminowaValue is short shortValue)
                                    {
                                        umowa.CzyTerminowa = shortValue != 0;
                                    }
                                    else if (czyTerminowaValue is int intValue)
                                    {
                                        umowa.CzyTerminowa = intValue != 0;
                                    }
                                    else
                                    {
                                        umowa.CzyTerminowa = Convert.ToBoolean(czyTerminowaValue);
                                    }
                                }
                                else
                                {
                                    umowa.CzyTerminowa = true;
                                }

                                // Wylicz wartość wykonanych badań dla tej umowy
                                umowa.WartoscWykonanychBadan = GetWartoscWykonanychBadan(umowa.FirmaId, umowa.DataUmowy, umowa.DataKoncowa);

                                umowy.Add(umowa);
                            }
                            catch (Exception ex)
                            {
                                // System.Diagnostics.Debug.WriteLine($"Błąd wczytywania umowy: {ex.Message}");
                                MessageBox.Show($"Błąd wczytywania umowy: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd wczytywania umów: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            Umowy = umowy;
        }

        /// <summary>
        /// Wylicza wartość wykonanych badań dla danej firmy w okresie umowy
        /// </summary>
        /// <param name="firmaId">ID firmy</param>
        /// <param name="dataOd">Data rozpoczęcia umowy</param>
        /// <param name="dataDo">Data zakończenia umowy (null dla umów bezterminowych)</param>
        private decimal GetWartoscWykonanychBadan(int firmaId, DateTime dataOd, DateTime? dataDo)
        {
            var db = new AccessDbHelper();
            try
            {
                using (var conn = db.GetConnection())
                {
                    conn.Open();

                    string query;
                    if (dataDo.HasValue)
                    {
                        // Umowa terminowa - filtruj po zakresie dat
                        query = @"
                            SELECT SUM(Badanie.Bad_Razem) as TotalValue
                            FROM B_Skierowania
                            INNER JOIN Badanie ON B_Skierowania.B_Badanie_ID = Badanie.Bad_ID
                            WHERE B_Skierowania.B_Firma_ID = ?
                              AND Badanie.Bad_Data >= ?
                              AND Badanie.Bad_Data <= ?";
                    }
                    else
                    {
                        // Umowa bezterminowa - od daty rozpoczęcia do dzisiaj
                        query = @"
                            SELECT SUM(Badanie.Bad_Razem) as TotalValue
                            FROM B_Skierowania
                            INNER JOIN Badanie ON B_Skierowania.B_Badanie_ID = Badanie.Bad_ID
                            WHERE B_Skierowania.B_Firma_ID = ?
                              AND Badanie.Bad_Data >= ?";
                    }

                    var cmd = new OdbcCommand(query, conn);
                    cmd.Parameters.AddWithValue("@FirmaId", firmaId);
                    cmd.Parameters.AddWithValue("@DataOd", dataOd);

                    if (dataDo.HasValue)
                    {
                        cmd.Parameters.AddWithValue("@DataDo", dataDo.Value);
                    }

                    var result = cmd.ExecuteScalar();
                    return result != DBNull.Value && result != null ? Convert.ToDecimal(result) : 0;
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ GetWartoscWykonanychBadan error: {ex.Message}");
                return 0;
            }
        }

        private void DodajUmowe()
        {
            var nowaUmowa = new Umowa
            {
                FirmaId = _firma.id,
                FirmaNazwa = _firma.Nazwa,
                NrUmowy = "",
                DataUmowy = DateTime.Now,
                IloscMiesiecy = 12,
                CzyTerminowa = true,
                Status = "Aktywna",
                Budzet = 0,
                WartoscWykonanychBadan = 0
            };

            var dialog = new Views.firma.UmowaEditDialog(nowaUmowa, true, _firma)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                if (ZapiszUmowe(nowaUmowa))
                {
                    LoadUmowy();
                }
            }
        }

        private void EdytujUmowe(Umowa? umowa)
        {
            if (umowa == null) return;

            var kopiaDlaEdycji = new Umowa
            {
                Id = umowa.Id,
                FirmaId = umowa.FirmaId,
                FirmaNazwa = umowa.FirmaNazwa,
                NrUmowy = umowa.NrUmowy,
                DataUmowy = umowa.DataUmowy,
                IloscMiesiecy = umowa.IloscMiesiecy,
                CzyTerminowa = umowa.CzyTerminowa,
                Status = umowa.Status,
                Budzet = umowa.Budzet,
                WartoscWykonanychBadan = umowa.WartoscWykonanychBadan,
                DataKoncowa = umowa.DataKoncowa
            };

            var dialog = new Views.firma.UmowaEditDialog(kopiaDlaEdycji, false, _firma)
            {
                Owner = Application.Current.MainWindow
            };

            if (dialog.ShowDialog() == true)
            {
                if (ZapiszUmowe(kopiaDlaEdycji))
                {
                    LoadUmowy();
                }
            }
        }

        private void UsunUmowe(Umowa? umowa)
        {
            if (umowa == null) return;

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć umowę z dnia {umowa.DataUmowy:dd.MM.yyyy}?",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes) return;

            var db = new AccessDbHelper();
            try
            {
                using (var conn = db.GetConnection())
                {
                    conn.Open();
                    var cmd = new OdbcCommand("DELETE FROM Umowy_Firm WHERE Id = ?", conn);
                    cmd.Parameters.AddWithValue("@Id", umowa.Id);
                    cmd.ExecuteNonQuery();

                    MessageBox.Show("Umowa została usunięta.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                    LoadUmowy();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd usuwania umowy: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private bool ZapiszUmowe(Umowa umowa)
        {
            var db = new AccessDbHelper();
            try
            {
                using (var conn = db.GetConnection())
                {
                    conn.Open();

                    using (var transaction = conn.BeginTransaction())
                    {
                        try
                        {
                            OdbcCommand cmd;

                            if (umowa.Id == 0)
                            {
                                // Insert
                                cmd = new OdbcCommand(@"
                                    INSERT INTO Umowy_Firm 
                                    (Firma_ID, nr_umowy, Data_Umowy, Ilosc_Miesiecy, Czy_Terminowa, Status, Budzet, Data_Koncowa)
                                    VALUES (?, ?, ?, ?, ?, ?, ?, ?)", conn, transaction);
                            }
                            else
                            {
                                // Update
                                cmd = new OdbcCommand(@"
                                    UPDATE Umowy_Firm 
                                    SET Firma_ID = ?,
                                        nr_umowy = ?,
                                        Data_Umowy = ?, 
                                        Ilosc_Miesiecy = ?, 
                                        Czy_Terminowa = ?, 
                                        Status = ?, 
                                        Budzet = ?,
                                        Data_Koncowa = ?
                                    WHERE Id = ?", conn, transaction);
                            }

                            cmd.Parameters.AddWithValue("@FirmaId", umowa.FirmaId);
                            cmd.Parameters.AddWithValue("@NrUmowy", umowa.NrUmowy ?? "");
                            cmd.Parameters.AddWithValue("@DataUmowy", umowa.DataUmowy);
                            cmd.Parameters.AddWithValue("@IloscMiesiecy", umowa.CzyTerminowa ? (object)umowa.IloscMiesiecy : DBNull.Value);
                            cmd.Parameters.AddWithValue("@CzyTerminowa", umowa.CzyTerminowa);
                            cmd.Parameters.AddWithValue("@Status", umowa.Status ?? "Aktywna");
                            cmd.Parameters.AddWithValue("@Budzet", umowa.Budzet);
                            cmd.Parameters.AddWithValue("@DataKoncowa", umowa.DataKoncowa.HasValue ? (object)umowa.DataKoncowa.Value : DBNull.Value);

                            if (umowa.Id != 0)
                            {
                                cmd.Parameters.AddWithValue("@Id", umowa.Id);
                            }

                            cmd.ExecuteNonQuery();

                            // ✅ NOWE: Aktualizuj pola umowa_do i czas_nieokreslon w tabeli Firma
                            var cmdUpdateFirma = new OdbcCommand(@"
                                UPDATE Firma 
                                SET umowa_do = ?, 
                                    czas_nieokreslon = ?
                                WHERE id = ?", conn, transaction);

                            // Ustaw datę końcową lub NULL dla umów bezterminowych
                            cmdUpdateFirma.Parameters.AddWithValue("@umowa_do", 
                                umowa.DataKoncowa.HasValue ? (object)umowa.DataKoncowa.Value : DBNull.Value);

                            // Ustaw flagę "czas nieokreślony" (TRUE jeśli bezterminowa)
                            cmdUpdateFirma.Parameters.AddWithValue("@czas_nieokreslon", !umowa.CzyTerminowa);

                            cmdUpdateFirma.Parameters.AddWithValue("@id", umowa.FirmaId);
                            cmdUpdateFirma.ExecuteNonQuery();

                            transaction.Commit();
                            MessageBox.Show("Umowa została zapisana.", "Sukces", MessageBoxButton.OK, MessageBoxImage.Information);
                            return true;
                        }
                        catch
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd zapisu umowy: {ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }
    }
}
