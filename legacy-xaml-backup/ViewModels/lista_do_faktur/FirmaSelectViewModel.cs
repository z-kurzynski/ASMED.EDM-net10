using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;
using ASMED.WPF;
using ASMED.WPF.Helpers;

namespace ASMED.WPF.ViewModels.ListaDoFaktur
{
    public class FirmaSelectViewModel : INotifyPropertyChanged
    {
        private readonly AccessDbHelper _db = new AccessDbHelper();

        public ObservableCollection<FirmaDto> Firms { get; } = new ObservableCollection<FirmaDto>();

        // publiczny widok do bindowania w XAML (obs�uguje filtrowanie)
        public ICollectionView FirmsView { get; }

        private FirmaDto? _selectedFirma;
        public FirmaDto? SelectedFirma
        {
            get => _selectedFirma;
            set { if (_selectedFirma != value) { _selectedFirma = value; OnPropertyChanged(); } }
        }

        private string? _searchText;
        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (_searchText == value) return;
                _searchText = value;
                OnPropertyChanged();
                // od�wie� filtr od razu przy ka�dej zmianie
                try { FirmsView.Refresh(); } catch { }
            }
        }

        public FirmaSelectViewModel()
        {
            // utw�rz widok i filtr zanim wczytasz dane
            FirmsView = CollectionViewSource.GetDefaultView(Firms);
            FirmsView.Filter = FilterFirma;
            LoadFirmy();
        }

        private bool FilterFirma(object? obj)
        {
            if (obj is not FirmaDto f) return false;
            if (string.IsNullOrWhiteSpace(SearchText)) return true;
            var q = SearchText.Trim();
            return (f.Nazwa?.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0)
                   // || (f.NIP?.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0)
                   || (f.Cennik?.IndexOf(q, StringComparison.CurrentCultureIgnoreCase) >= 0);
        }

        private void LoadFirmy()
        {
            try
            {
                var items = new System.Collections.Generic.List<FirmaDto>();
                using var conn = _db.GetConnection();
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = "SELECT id, Nazwa, activ, NIP, cennik, FKemail FROM Firma WHERE activ = TRUE ORDER BY Nazwa";
                using var reader = cmd.ExecuteReader();

                try
                {
                    var colNames = new System.Text.StringBuilder();
                    for (int i = 0; i < reader.FieldCount; i++)
                        colNames.Append(reader.GetName(i)).Append(i == reader.FieldCount - 1 ? "" : ", ");
                    // Debug.WriteLine($"[FirmaSelect] Columns: {colNames}");
                }
                catch { }

                while (reader.Read())
                {
                    object? rawObj = null;
                    try { rawObj = reader["cennik"]; } catch (IndexOutOfRangeException) { rawObj = null; }
                    // Debug.WriteLine($"[FirmaSelect] raw cennik for row: '{rawObj?.ToString()}'");

                    var name = reader["Nazwa"] != DBNull.Value ? reader["Nazwa"].ToString() ?? string.Empty : string.Empty;
                    var id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0;
                    var activ = reader["activ"] != DBNull.Value ? Convert.ToBoolean(reader["activ"]) : false;
                    var nip = reader["NIP"] != DBNull.Value ? reader["NIP"].ToString() : null;
                    var email = reader["FKemail"] != DBNull.Value ? reader["FKemail"].ToString() : null;

                    string? cennik = null;
                    if (reader["cennik"] != DBNull.Value)
                    {
                        var raw = reader["cennik"]?.ToString();
                        if (!string.IsNullOrWhiteSpace(raw))
                        {
                            cennik = raw.Trim();
                            if (string.IsNullOrWhiteSpace(cennik)) cennik = null;
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(name))
                    {
                        items.Add(new FirmaDto
                        {
                            Id = id,
                            Activ = activ,
                            Nazwa = name,
                            NIP = nip,
                            Cennik = cennik,
                            FkEmail = email
                        });
                    }
                }

                Application.Current.Dispatcher.BeginInvoke(new Action(() =>
                {
                    Firms.Clear();
                    foreach (var f in items) Firms.Add(f);
                    // od�wie� widok po wczytaniu
                    try { FirmsView.Refresh(); } catch { }
                    NotificationHelper.ShowInfo($"Za�adowano {items.Count} firm", "Debug");
                }));
            }
            catch (Exception ex)
            {
                // Debug.WriteLine($"LoadFirmy (FirmaSelectViewModel) failed: {ex}");
                NotificationHelper.ShowError($"B��d �adowania firm: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
