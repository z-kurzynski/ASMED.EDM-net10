using ASMED.WPF.Helpers;
using System;
using System.Collections.ObjectModel;
using System.Data.Odbc;
using System.Windows;

namespace ASMED.WPF.Views
{
    public partial class PacjentHistoriaDialog : Window
    {
        private readonly int _pacjentId;

        public PacjentHistoriaDialog(int pacjentId, string imie, string nazwisko, string pesel, string firma)
        {
            InitializeComponent();
            _pacjentId = pacjentId;

            // Ustaw dane pacjenta w nagłówku
            txtImieNazwisko.Text = $"{imie} {nazwisko}";
            txtPesel.Text = pesel ?? "(brak)";
            txtFirma.Text = firma ?? "(brak)";

            // Załaduj dane
            LoadSkierowania();
            LoadBadania();

            // ✅ NOWE: Ustaw liczbę skierowań po załadowaniu
            txtLiczbaSkierowan.Text = $"{(gridSkierowania.ItemsSource as ObservableCollection<SkierowanieHistoriaDto>)?.Count ?? 0}";
        }

        /// <summary>
        /// DTO dla skierowań (lewa kolumna)
        /// </summary>
        public class SkierowanieHistoriaDto
        {
            public int Lp { get; set; }
            public int? B_ID { get; set; }
            public DateTime? B_DataSkierowania { get; set; }
            public string? B_TypBadania { get; set; }
            public bool? B_ksiazeczka { get; set; }
            public string? B_Comments { get; set; }
        }

        /// <summary>
        /// DTO dla badań (prawa kolumna)
        /// </summary>
        public class BadanieHistoriaDto
        {
            public int Lp { get; set; }
            public int? Bad_ID { get; set; }
            public string? Bad_Typ { get; set; }
            public DateTime? Bad_Data { get; set; }
            public DateTime? Bad_Data_Do { get; set; }
            public string? Bad_Wynik { get; set; }
            public string? Bad_Fakt { get; set; }
        }

        /// <summary>
        /// ? LEWA KOLUMNA: Pobiera skierowania (karty badań) dla danego pacjenta
        /// SQL bezpośrednio w metodzie
        /// </summary>
        private void LoadSkierowania()
        {
            var result = new ObservableCollection<SkierowanieHistoriaDto>();

            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                // ? SQL z nagłówka
                var sql = @"
SELECT
    P_Pacjent.P_ID,
    P_Pacjent.P_imie,
    P_Pacjent.P_nazwisko,
    P_Pacjent.P_pesel,
    Firma.Nazwa,
    B_Skierowania.B_DataSkierowania,
    B_Skierowania.B_TypBadania,
    B_Skierowania.B_książeczka,
    B_Skierowania.B_ID,
    B_Skierowania.B_Comments
FROM
    Firma
    INNER JOIN (
        P_Pacjent
        INNER JOIN B_Skierowania ON P_Pacjent.P_ID = B_Skierowania.B_Pacjent_ID
    ) ON Firma.id = P_Pacjent.P_Firma_id
WHERE
    P_Pacjent.P_ID = ?
ORDER BY
    B_Skierowania.B_DataSkierowania DESC";

                using var cmd = new OdbcCommand(sql, conn);
                cmd.Parameters.AddWithValue("@p1", _pacjentId);

                int lp = 1;
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var dto = new SkierowanieHistoriaDto
                    {
                        Lp = lp++,
                        B_ID = reader["B_ID"] != DBNull.Value ? Convert.ToInt32(reader["B_ID"]) : (int?)null,
                        B_DataSkierowania = reader["B_DataSkierowania"] != DBNull.Value ? Convert.ToDateTime(reader["B_DataSkierowania"]) : (DateTime?)null,
                        B_TypBadania = reader["B_TypBadania"]?.ToString(),
                        B_ksiazeczka = reader["B_książeczka"] != DBNull.Value ? Convert.ToBoolean(reader["B_książeczka"]) : (bool?)null,
                        B_Comments = reader["B_Comments"]?.ToString()
                    };

                    result.Add(dto);
                }

                gridSkierowania.ItemsSource = result;

                // ✅ NOWE: Ustaw liczbę skierowań w nagłówku
                txtLiczbaSkierowan.Text = $"{result.Count}";

                // System.Diagnostics.Debug.WriteLine($"✅ Załadowano {result.Count} skierowań dla pacjenta ID={_pacjentId}");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ LoadSkierowania ERROR: {ex.Message}");
                MessageBox.Show($"Błąd ładowania skierowań:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                txtLiczbaSkierowan.Text = "0"; // ✅ W razie błędu ustaw 0
            }
        }

        /// <summary>
        /// ? PRAWA KOLUMNA: Pobiera badania (wizyty) dla danego pacjenta
        /// SQL bezpośrednio w metodzie
        /// </summary>
        private void LoadBadania()
        {
            var result = new ObservableCollection<BadanieHistoriaDto>();

            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                // ? SQL z prawej kolumny
                var sql = @"
SELECT
    P_Pacjent.P_ID,
    Badanie.Bad_ID,
    Badanie.Bad_Typ,
    Badanie.Bad_Data,
    Badanie.Bad_Data_Do,
    Badanie.Bad_Wynik,
    Badanie.Bad_Fakt
FROM
    (
        P_Pacjent
        INNER JOIN B_Skierowania ON P_Pacjent.P_ID = B_Skierowania.B_Pacjent_ID
    )
    INNER JOIN Badanie ON B_Skierowania.B_Badanie_ID = Badanie.Bad_ID
WHERE
    P_Pacjent.P_ID = ?
ORDER BY
    Badanie.Bad_Data DESC";

                using var cmd = new OdbcCommand(sql, conn);
                cmd.Parameters.AddWithValue("@p1", _pacjentId);

                int lp = 1;
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    var dto = new BadanieHistoriaDto
                    {
                        Lp = lp++,
                        Bad_ID = reader["Bad_ID"] != DBNull.Value ? Convert.ToInt32(reader["Bad_ID"]) : (int?)null,
                        Bad_Typ = reader["Bad_Typ"]?.ToString(),
                        Bad_Data = reader["Bad_Data"] != DBNull.Value ? Convert.ToDateTime(reader["Bad_Data"]) : (DateTime?)null,
                        Bad_Data_Do = reader["Bad_Data_Do"] != DBNull.Value ? Convert.ToDateTime(reader["Bad_Data_Do"]) : (DateTime?)null,
                        Bad_Wynik = reader["Bad_Wynik"]?.ToString(),
                        Bad_Fakt = reader["Bad_Fakt"]?.ToString()
                    };

                    result.Add(dto);
                }

                gridBadania.ItemsSource = result;
                // System.Diagnostics.Debug.WriteLine($"? Załadowano {result.Count} badań dla pacjenta ID={_pacjentId}");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"? LoadBadania ERROR: {ex.Message}");
                MessageBox.Show($"Błąd ładowania badań:\n{ex.Message}", "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Zamyka okno dialogu
        /// </summary>
        private void Zamknij_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
