using System.Windows;
using ASMED.WPF.Models;
using ASMED.WPF.Helpers;

namespace ASMED.WPF.Views.firma
{
    public partial class UmowaEditDialog : Window
    {
        public Umowa Umowa { get; }
        public bool IsNewUmowa { get; }
        private readonly Firma? _firma;

        public UmowaEditDialog(Umowa umowa, bool isNew, Firma? firma = null)
        {
            InitializeComponent();
            Umowa = umowa;
            IsNewUmowa = isNew;
            _firma = firma;
            DataContext = Umowa;
            Title = isNew ? "Dodaj nową umowę" : "Edytuj umowę";
        }

        /// <summary>
        /// Obsługa zmiany checkboxa "Umowa terminowa"
        /// Dla umów bezterminowych: IloscMiesiecy = 0, Budzet = 100 000
        /// </summary>
        private void ChkCzyTerminowa_Changed(object sender, RoutedEventArgs e)
        {
            if (Umowa == null) return;

            if (!Umowa.CzyTerminowa)
            {
                // Umowa bezterminowa
                Umowa.IloscMiesiecy = 0;
                Umowa.Budzet = 100000.00m;
            }
            else
            {
                // Umowa terminowa - ustaw domyślne wartości jeśli puste
                if (Umowa.IloscMiesiecy == 0)
                {
                    Umowa.IloscMiesiecy = 12;
                }
                if (Umowa.Budzet == 100000.00m)
                {
                    Umowa.Budzet = 0;
                }
            }
        }

        private void BtnZapisz_Click(object sender, RoutedEventArgs e)
        {
            // Walidacja daty końcowej względem obecnej umowy w Firmie
            if (_firma != null && Umowa.DataKoncowa.HasValue)
            {
                var db = new AccessDbHelper();
                try
                {
                    using (var conn = db.GetConnection())
                    {
                        conn.Open();
                        var cmd = new System.Data.Odbc.OdbcCommand(
                            "SELECT umowa_do FROM Firma WHERE id = ?", conn);
                        cmd.Parameters.AddWithValue("@id", _firma.id);

                        var result = cmd.ExecuteScalar();
                        if (result != null && result != System.DBNull.Value)
                        {
                            DateTime obecnaUmowaDo = System.Convert.ToDateTime(result);

                            if (Umowa.DataKoncowa.Value <= obecnaUmowaDo)
                            {
                                MessageBox.Show(
                                    $"Data końcowa nowej umowy ({Umowa.DataKoncowa.Value:dd.MM.yyyy}) " +
                                    $"musi być późniejsza niż obecna data końcowa w firmie ({obecnaUmowaDo:dd.MM.yyyy}).",
                                    "Błąd walidacji",
                                    MessageBoxButton.OK,
                                    MessageBoxImage.Warning);
                                return;
                            }
                        }
                    }
                }
                catch (System.Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"❌ Błąd walidacji daty: {ex.Message}");
                }
            }

            DialogResult = true;
            Close();
        }

        private void BtnAnuluj_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
