using ASMED.EDM.Data.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Windows;

namespace ASMED.EDM.UI.Views.Skierowania
{
    public partial class PacjentHistoriaDialog : Window
    {
        private readonly int _pacjentId;
        private readonly DbConnectionFactory _dbFactory;

        // ─── DTOs ───────────────────────────────────────────────────────────────

        public class SkierowanieHistoriaDto
        {
            public int Lp { get; set; }
            public int? B_ID { get; set; }
            public DateTime? B_DataSkierowania { get; set; }
            public string? B_TypBadania { get; set; }
            public bool? B_ksiazeczka { get; set; }
            public string? B_Comments { get; set; }
        }

        public class BadanieHistoriaDto
        {
            public int Lp { get; set; }
            public int? Bad_ID { get; set; }
            public string? Bad_Typ { get; set; }
            public DateTime? Bad_Data { get; set; }
            public DateTime? Bad_Data_Do { get; set; }
            public string? Bad_Wynik { get; set; }
        }

        // ─── Konstruktor ────────────────────────────────────────────────────────

        public PacjentHistoriaDialog(int pacjentId, string imie, string nazwisko,
                                     string pesel, string firma)
        {
            InitializeComponent();

            _pacjentId = pacjentId;
            _dbFactory = ((App)Application.Current).Host.Services
                             .GetRequiredService<DbConnectionFactory>();

            // Wypełnij nagłówek
            txtImieNazwisko.Text = $"{imie} {nazwisko}".Trim();
            txtPesel.Text        = string.IsNullOrWhiteSpace(pesel) ? "(brak)" : pesel;
            txtFirma.Text        = string.IsNullOrWhiteSpace(firma) ? "(brak)" : firma;

            LoadSkierowania();
            LoadBadania();
        }

        // ─── Lewa kolumna: Karty Badań (skierowania) ───────────────────────────

        /// <summary>
        /// Pobiera karty badań (B_Skierowania) dla danego pacjenta.
        /// LEFT JOIN Firma – pacjent bez firmy też jest widoczny.
        /// </summary>
        private void LoadSkierowania()
        {
            var result = new ObservableCollection<SkierowanieHistoriaDto>();
            try
            {
                using var conn = _dbFactory.CreateConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT
                        B.B_ID,
                        B.B_DataSkierowania,
                        B.B_TypBadania,
                        B.B_ksiazeczka,
                        B.B_Comments
                    FROM B_Skierowania AS B
                    WHERE B.B_Pacjent_ID = @pacjentId
                    ORDER BY B.B_DataSkierowania DESC";

                var param = cmd.CreateParameter();
                param.ParameterName = "@pacjentId";
                param.Value = _pacjentId;
                cmd.Parameters.Add(param);

                int lp = 1;
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new SkierowanieHistoriaDto
                    {
                        Lp                = lp++,
                        B_ID              = reader["B_ID"] == DBNull.Value ? null : Convert.ToInt32(reader["B_ID"]),
                        B_DataSkierowania = reader["B_DataSkierowania"] == DBNull.Value ? null : Convert.ToDateTime(reader["B_DataSkierowania"]),
                        B_TypBadania      = reader["B_TypBadania"]?.ToString(),
                        B_ksiazeczka      = reader["B_ksiazeczka"] == DBNull.Value ? null : Convert.ToBoolean(reader["B_ksiazeczka"]),
                        B_Comments        = reader["B_Comments"]?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd ładowania kart badań:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            gridSkierowania.ItemsSource = result;
            txtLiczbaSkierowan.Text = result.Count.ToString();
        }

        // ─── Prawa kolumna: Wyniki Badań ────────────────────────────────────────

        /// <summary>
        /// Pobiera wyniki badań (tabela Badanie) powiązane przez B_Skierowania.
        /// </summary>
        private void LoadBadania()
        {
            var result = new ObservableCollection<BadanieHistoriaDto>();
            try
            {
                using var conn = _dbFactory.CreateConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT
                        Bad.Bad_ID,
                        Bad.Bad_Typ,
                        Bad.Bad_Data,
                        Bad.Bad_Data_Do,
                        Bad.Bad_Wynik
                    FROM P_Pacjent AS P
                        INNER JOIN B_Skierowania AS B ON P.P_ID = B.B_Pacjent_ID
                        INNER JOIN Badanie AS Bad   ON B.B_Badanie_ID = Bad.Bad_ID
                    WHERE P.P_ID = @pacjentId
                    ORDER BY Bad.Bad_Data DESC";

                var param = cmd.CreateParameter();
                param.ParameterName = "@pacjentId";
                param.Value = _pacjentId;
                cmd.Parameters.Add(param);

                int lp = 1;
                using var reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    result.Add(new BadanieHistoriaDto
                    {
                        Lp         = lp++,
                        Bad_ID     = reader["Bad_ID"] == DBNull.Value ? null : Convert.ToInt32(reader["Bad_ID"]),
                        Bad_Typ    = reader["Bad_Typ"]?.ToString(),
                        Bad_Data   = reader["Bad_Data"] == DBNull.Value ? null : Convert.ToDateTime(reader["Bad_Data"]),
                        Bad_Data_Do= reader["Bad_Data_Do"] == DBNull.Value ? null : Convert.ToDateTime(reader["Bad_Data_Do"]),
                        Bad_Wynik  = reader["Bad_Wynik"]?.ToString()
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd ładowania wyników badań:\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            gridBadania.ItemsSource = result;
        }

        // ─── Przycisk ───────────────────────────────────────────────────────────

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
