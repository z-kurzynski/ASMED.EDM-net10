using ASMED.WPF.ViewModels;
using ASMED.WPF.ViewModels.Skierowania;
using ASMED.WPF.Helpers;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Data.Odbc;
using System;

namespace ASMED.WPF.Views
{
    public partial class SkierPacjentaEditView : UserControl
    {
        public SkierPacjentaEditView()
        {
            InitializeComponent();

            // ✅ WŁĄCZONE: Nasłuchuj zmiany DataContext
            this.DataContextChanged += OnDataContextChanged;
        }

        /// <summary>
        /// ✅ WŁĄCZONE: Obsługa zmiany DataContext - ładuj karty badań
        /// </summary>
        private void OnDataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            try
            {
                if (DataContext is SkierPacjentaEditViewModel viewModel)
                {
                    // Nasłuchuj zmiany ID pacjenta
                    viewModel.PropertyChanged += (s, args) =>
                    {
                        try
                        {
                            if (args.PropertyName == nameof(SkierPacjentaEditViewModel.ID))
                            {
                                int pacjentId = viewModel.ID ?? 0;
                                LoadKartyBadan(pacjentId);
                            }
                        }
                        catch (Exception)
                        {
                            // System.Diagnostics.Debug.WriteLine($"❌ PropertyChanged ERROR: {ex.Message}");
                            // System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
                        }
                    };

                    // Załaduj karty badań jeśli ID już jest dostępne
                    int currentId = viewModel.ID ?? 0;
                    if (currentId > 0)
                    {
                        LoadKartyBadan(currentId);
                    }
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ OnDataContextChanged ERROR: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            }
        }

        /// <summary>
        /// ✅ NOWE: Ładuje tylko karty badań (skierowania)
        /// </summary>
        private void LoadKartyBadan(int pacjentId)
        {
            try
            {
                if (pacjentId <= 0)
                {
                    // Wyczyść dane
                    if (gridHistoriaSkierowania != null) gridHistoriaSkierowania.ItemsSource = null;
                    if (txtLiczbaKartBadan != null) txtLiczbaKartBadan.Text = "0";
                    return;
                }

                // Załaduj karty badań (skierowania)
                var skierowania = LoadSkierowania(pacjentId);
                if (gridHistoriaSkierowania != null)
                {
                    gridHistoriaSkierowania.ItemsSource = skierowania;
                }
                if (txtLiczbaKartBadan != null)
                {
                    txtLiczbaKartBadan.Text = skierowania.Count.ToString();
                }

                // ✅ DODANE: Załaduj również wizyty/badania
                LoadWizyty(pacjentId);

                // System.Diagnostics.Debug.WriteLine($"✅ Karty badań załadowane: {skierowania.Count}");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ LoadKartyBadan ERROR: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");

                // Wyczyść dane w razie błędu
                try
                {
                    if (gridHistoriaSkierowania != null) gridHistoriaSkierowania.ItemsSource = null;
                    if (txtLiczbaKartBadan != null) txtLiczbaKartBadan.Text = "0";
                }
                catch { /* Ignoruj błędy czyszczenia */ }
            }
        }

        /// <summary>
        /// ✅ NOWE: Ładuje wizyty/badania
        /// </summary>
        private void LoadWizyty(int pacjentId)
        {
            try
            {
                if (pacjentId <= 0)
                {
                    // Wyczyść dane
                    if (gridHistoriaBadania != null) gridHistoriaBadania.ItemsSource = null;
                    if (txtLiczbaWizyt != null) txtLiczbaWizyt.Text = "0";
                    return;
                }

                // Załaduj wizyty/badania
                var badania = LoadBadania(pacjentId);
                if (gridHistoriaBadania != null)
                {
                    gridHistoriaBadania.ItemsSource = badania;
                }
                if (txtLiczbaWizyt != null)
                {
                    txtLiczbaWizyt.Text = badania.Count.ToString();
                }

                // System.Diagnostics.Debug.WriteLine($"✅ Wizyty/badania załadowane: {badania.Count}");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ LoadWizyty ERROR: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");

                // Wyczyść dane w razie błędu
                try
                {
                    if (gridHistoriaBadania != null) gridHistoriaBadania.ItemsSource = null;
                    if (txtLiczbaWizyt != null) txtLiczbaWizyt.Text = "0";
                }
                catch { /* Ignoruj błędy czyszczenia */ }
            }
        }

        /// <summary>
        /// Pobiera skierowania (karty badań) dla danego pacjenta
        /// </summary>
        private ObservableCollection<SkierowanieHistoriaDto> LoadSkierowania(int pacjentId)
        {
            var result = new ObservableCollection<SkierowanieHistoriaDto>();

            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                var sql = @"
SELECT
    B_Skierowania.B_ID,
    B_Skierowania.B_DataSkierowania,
    B_Skierowania.B_TypBadania,
    B_Skierowania.B_Comments
FROM
    B_Skierowania
WHERE
    B_Skierowania.B_Pacjent_ID = ?
ORDER BY
    B_Skierowania.B_DataSkierowania DESC";

                using var cmd = new OdbcCommand(sql, conn);
                cmd.Parameters.AddWithValue("@p1", pacjentId);

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
                        B_Comments = reader["B_Comments"]?.ToString()
                    };

                    result.Add(dto);
                }

                // System.Diagnostics.Debug.WriteLine($"✅ LoadSkierowania: Pobrano {result.Count} rekordów dla pacjenta ID={pacjentId}");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ LoadSkierowania ERROR: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            }

            return result;
        }

        /// <summary>
        /// Pobiera badania (wizyty) dla danego pacjenta
        /// </summary>
        private ObservableCollection<BadanieHistoriaDto> LoadBadania(int pacjentId)
        {
            var result = new ObservableCollection<BadanieHistoriaDto>();

            try
            {
                var dbHelper = new AccessDbHelper();
                using var conn = dbHelper.GetConnection();
                conn.Open();

                var sql = @"
SELECT
    Badanie.Bad_ID,
    Badanie.Bad_Typ,
    Badanie.Bad_Data,
    Badanie.Bad_Data_Do,
    Badanie.Bad_Wynik
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
                cmd.Parameters.AddWithValue("@p1", pacjentId);

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
                        Bad_Wynik = reader["Bad_Wynik"]?.ToString()
                    };

                    result.Add(dto);
                }

                // System.Diagnostics.Debug.WriteLine($"✅ LoadBadania: Pobrano {result.Count} rekordów dla pacjenta ID={pacjentId}");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ LoadBadania ERROR: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"❌ StackTrace: {ex.StackTrace}");
            }

            return result;
        }

        private void Button_Anuluj_Click(object sender, RoutedEventArgs e)
        {

        }

        /// <summary>
        /// DTO dla skierowań (karty badań)
        /// </summary>
        public class SkierowanieHistoriaDto
        {
            public int Lp { get; set; }
            public int? B_ID { get; set; }
            public DateTime? B_DataSkierowania { get; set; }
            public string? B_TypBadania { get; set; }
            public string? B_Comments { get; set; }
        }

        /// <summary>
        /// DTO dla badań (wizyty)
        /// </summary>
        public class BadanieHistoriaDto
        {
            public int Lp { get; set; }
            public int? Bad_ID { get; set; }
            public string? Bad_Typ { get; set; }
            public DateTime? Bad_Data { get; set; }
            public DateTime? Bad_Data_Do { get; set; }
            public string? Bad_Wynik { get; set; }
        }
    }
}

