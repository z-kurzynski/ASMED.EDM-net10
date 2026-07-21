using ASMED.WPF.Helpers;
using ASMED.WPF.ViewModels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace ASMED.WPF.ViewModels.Faktura
{
    public class FakturaImportViewModel : INotifyPropertyChanged
    {
        private const string ImportPath = @"A:\Import\FK";

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public ObservableCollection<string> Files { get; } = new ObservableCollection<string>();
        private string? _selectedFile;
        public string? SelectedFile
        {
            get => _selectedFile;
            set
            {
                if (_selectedFile != value)
                {
                    _selectedFile = value;
                    OnPropertyChanged(nameof(SelectedFile));
                    LoadSelectedFile();
                }
            }
        }

        // w�a�ciwo�� pozostawiona dla komunikat�w (podgl�d usuni�ty z XAML)
        private string _fileContentText = string.Empty;
        public string FileContentText
        {
            get => _fileContentText;
            set { _fileContentText = value; OnPropertyChanged(nameof(FileContentText)); }
        }

        public ObservableCollection<InvoiceImportItem> Invoices { get; } = new ObservableCollection<InvoiceImportItem>();

        public ICommand SelectAllCommand { get; }
        public ICommand ImportSelectedCommand { get; }
        public ICommand CancelCommand { get; }

        public FakturaImportViewModel()
        {
            SelectAllCommand = new RelayCommand(_ => ToggleSelectAll());
            ImportSelectedCommand = new RelayCommand(_ => ImportSelected());
            CancelCommand = new RelayCommand(_ => Cancel());

            LoadFiles();
        }

        private void LoadFiles()
        {
            Files.Clear();
            try
            {
                if (!Directory.Exists(ImportPath))
                {
                    FileContentText = $"Katalog '{ImportPath}' nie istnieje.";
                    return;
                }

                var files = Directory.EnumerateFiles(ImportPath)
                    .Where(f => !f.EndsWith(".db", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(File.GetCreationTime)
                    .ToList();

                foreach (var f in files) Files.Add(Path.GetFileName(f));
                if (Files.Count == 0) FileContentText = "Brak plik�w w katalogu.";
            }
            catch (Exception ex)
            {
                FileContentText = "B��d podczas odczytu katalogu: " + ex.Message;
            }
        }

        private void LoadSelectedFile()
        {
            Invoices.Clear();
            FileContentText = string.Empty;
            if (string.IsNullOrEmpty(SelectedFile)) return;

            var fullPath = Path.Combine(ImportPath, SelectedFile);
            if (!File.Exists(fullPath))
            {
                FileContentText = "Plik nie istnieje.";
                return;
            }

            string[] lines;
            try
            {
                // u�yj Encoding.Default aby zachowa� polskie znaki z plik�w ANSI/Windows
                lines = File.ReadAllLines(fullPath, Encoding.Default)
                            .Select(l => l ?? string.Empty)
                            .ToArray();
            }
            catch (Exception ex)
            {
                FileContentText = "B��d odczytu pliku: " + ex.Message;
                return;
            }

            try
            {
                // znajd� nag��wek (linia zawieraj�ca "Data wystawienia")
                var headerIndex = -1;
                for (int i = 0; i < lines.Length; i++)
                {
                    if (lines[i].IndexOf("Data wystawienia", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        headerIndex = i;
                        break;
                    }
                }

                if (headerIndex < 0)
                {
                    FileContentText = "Nie znaleziono nag��wka CSV (Data wystawienia).";
                    return;
                }

                int lp = 1;
                // przetwarzaj linie po nag��wku
                foreach (var raw in lines.Skip(headerIndex + 1))
                {
                    var line = raw?.Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    // split z uwzgl�dnieniem cytowania i separatorem ';'
                    var cols = SplitCsvRespectQuotes(line, ';');
                    if (cols.Length < 6) continue; // oczekujemy przynajmniej 6 kolumn

                    // Data wystawienia (kolumna 0) - format yyyy-MM-dd
                    DateTime? date = null;
                    var dateStr = cols.ElementAtOrDefault(0)?.Trim();
                    if (!string.IsNullOrWhiteSpace(dateStr))
                    {
                        if (DateTime.TryParseExact(dateStr, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var d1))
                            date = d1;
                        else if (DateTime.TryParse(dateStr, CultureInfo.GetCultureInfo("pl-PL"), DateTimeStyles.None, out var d2))
                            date = d2;
                    }

                    // Numer dokumentu (kolumna 2) - usu� prefiks "FS " je�li wyst�puje
                    var rawNumber = cols.ElementAtOrDefault(2) ?? string.Empty;
                    var number = Unquote(rawNumber).Trim();
                    number = Regex.Replace(number, @"^\s*FS\s+", string.Empty, RegexOptions.IgnoreCase).Trim();

                    // Nazwa (kolumna 3) - usu� dodatkowe cudzys�owy i normalizuj spacje oraz kropki
                    var rawCompany = cols.ElementAtOrDefault(3) ?? string.Empty;
                    var company = Unquote(rawCompany);

                    // usu� wszelkie pozosta�e cudzys�owy
                    company = company.Replace("\"", string.Empty);

                    // zamie� kropki na spacj�
                    company = company.Replace('.', ' ');

                    // zredukuj wielokrotne spacje do jednej i przytnij ko�ce
                    company = Regex.Replace(company, @"\s+", " ").Trim();

                    // Warto�� (kolumna 5)
                    decimal value = 0m;
                    var rawValue = cols.ElementAtOrDefault(5) ?? string.Empty;
                    var v = TryParseDecimalInternal(rawValue);
                    if (v.HasValue) value = v.Value;

                    // Utw�rz rekord importu
                    var item = new InvoiceImportItem
                    {
                        Lp = lp++,
                        Number = number,
                        Date = date,
                        Value = value,
                        Company = company,
                        IsSelected = false
                    };
                    Invoices.Add(item);

                    // ograniczenie bezpiecze�stwa przy bardzo du�ych plikach
                    if (Invoices.Count > 20000) break;
                }

                if (Invoices.Count == 0)
                    FileContentText = "Brak rozpoznanych rekord�w w pliku.";
            }
            catch (Exception ex)
            {
                FileContentText = "B��d parsowania pliku: " + ex.Message;
            }
        }

        // Split CSV z uwzgl�dnieniem cytowanych p�l (delimiter dowolny, np. ';' lub ',')
        private static string[] SplitCsvRespectQuotes(string line, char delimiter = ',')
        {
            if (line == null) return Array.Empty<string>();
            var fields = new List<string>();
            var sb = new StringBuilder();
            bool inQuotes = false;
            for (int i = 0; i < line.Length; i++)
            {
                var ch = line[i];
                if (ch == '"')
                {
                    // escaped quote
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        sb.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (ch == delimiter && !inQuotes)
                {
                    fields.Add(sb.ToString());
                    sb.Clear();
                }
                else
                {
                    sb.Append(ch);
                }
            }
            fields.Add(sb.ToString());
            for (int i = 0; i < fields.Count; i++)
                fields[i] = fields[i]?.Trim() ?? string.Empty;
            return fields.ToArray();
        }

        private static string Unquote(string s)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
            var t = s.Trim();
            if (t.StartsWith("\"") && t.EndsWith("\"") && t.Length >= 2)
                t = t.Substring(1, t.Length - 2);
            // zamie� podw�jne cudzys�owy na pojedynczy
            t = t.Replace("\"\"", "\"");
            return t.Trim();
        }

        // wewn�trzna parse decimal uproszczona
        private static decimal? TryParseDecimalInternal(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return null;
            var raw = Unquote(s);
            raw = raw.Replace("z�", "", StringComparison.OrdinalIgnoreCase).Replace(" ", "");
            if (decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("pl-PL"), out var d)) return d;
            if (decimal.TryParse(raw, NumberStyles.Number | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out d)) return d;
            var filtered = new string(raw.Where(c => char.IsDigit(c) || c == ',' || c == '.').ToArray());
            if (decimal.TryParse(filtered, NumberStyles.Number | NumberStyles.AllowDecimalPoint | NumberStyles.AllowThousands, CultureInfo.GetCultureInfo("pl-PL"), out d)) return d;
            return null;
        }

        private void ToggleSelectAll()
        {
            var anyUnselected = Invoices.Any(i => !i.IsSelected);
            foreach (var it in Invoices) it.IsSelected = anyUnselected;
            OnPropertyChanged(nameof(Invoices));
        }

        private void ImportSelected()
        {
            var toImport = Invoices.Where(i => i.IsSelected).ToList();
            if (toImport.Count == 0)
            {
                MessageBox.Show("Brak zaznaczonych faktur do importu.", "Info", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var ctx = new AccessDbContext();
            int inserted = 0, updated = 0, skipped = 0;

            foreach (var item in toImport)
            {
                try
                {
                    if (string.IsNullOrWhiteSpace(item.Company))
                    {
                        skipped++;
                        continue;
                    }

                    // znajd� firm� po nazwie (dok�adne dopasowanie trimowane)
                    var (firmaId, firmaCennik) = ctx.GetFirmaIdAndCennikByName(item.Company);
                    if (!firmaId.HasValue)
                    {
                        // spr�buj dopasowania cz�ciowego (fallback)
                        var possible = ctx.GetFirmy(item.Company, 1).FirstOrDefault();
                        if (possible != null) firmaId = possible.Id;
                    }

                    if (!firmaId.HasValue)
                    {
                        // nie znaleziono firmy � pomijamy i raportujemy
                        skipped++;
                        continue;
                    }

                    var numer = item.Number ?? string.Empty;
                    var data = item.Date;
                    var kwota = item.Value;
                    var cennik = firmaCennik;

                    // usu� prefiks FS je�li pozosta�
                    numer = Regex.Replace(numer, @"^\s*FS\s+", string.Empty, RegexOptions.IgnoreCase).Trim();

                    // sprawd� czy faktura istnieje
                    var existingId = ctx.FindFakturaByFirmaAndNumer(firmaId.Value, numer);

                    if (existingId.HasValue)
                    {
                        // update p�l (FK_Firma_ID, FK_Numer, FK_Data, FK_Kwota, FK_Cennik)
                        var ok = ctx.UpdateFakturaFields(existingId.Value, firmaId, numer, data, kwota, cennik);
                        if (ok) updated++;
                        else skipped++;
                    }
                    else
                    {
                        // insert nowej faktury
                        var newId = ctx.InsertFakturaSimple(firmaId, numer, data, kwota, cennik);
                        if (newId > 0) inserted++;
                        else skipped++;
                    }
                }
                catch (Exception)
                {
                    // System.Diagnostics.Debug.WriteLine($"ImportSelected item error: {ex}");
                    skipped++;
                }
            }

            // po zapisaniu przenie� plik do katalogu WCZYTANE
            try
            {
                var src = Path.Combine(ImportPath, SelectedFile ?? string.Empty);
                if (File.Exists(src))
                {
                    var destDir = Path.Combine(ImportPath, "WCZYTANE");
                    if (!Directory.Exists(destDir)) Directory.CreateDirectory(destDir);

                    var fileName = Path.GetFileName(src);
                    var dest = Path.Combine(destDir, fileName);
                    if (File.Exists(dest))
                    {
                        // dopisz timestamp aby unikn�� kolizji
                        var name = Path.GetFileNameWithoutExtension(fileName);
                        var ext = Path.GetExtension(fileName);
                        var suffix = DateTime.Now.ToString("yyyyMMddHHmmss");
                        dest = Path.Combine(destDir, $"{name}_{suffix}{ext}");
                    }
                    File.Move(src, dest);
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"Move imported file error: {ex}");
            }

            MessageBox.Show($"Import zako�czony.\nDodano: {inserted}\nZaktualizowano: {updated}\nPomini�to: {skipped}", "Import", MessageBoxButton.OK, MessageBoxImage.Information);

            // od�wie� widok plik�w
            LoadFiles();
            Invoices.Clear();
            FileContentText = "Import wykonany.";
        }

        private void Cancel()
        {
            Invoices.Clear();
            FileContentText = string.Empty;
            SelectedFile = null;
        }
    }

    public class InvoiceImportItem : INotifyPropertyChanged
    {
        private bool _isSelected;
        public bool IsSelected { get => _isSelected; set { _isSelected = value; OnPropertyChanged(nameof(IsSelected)); } }

        public int Lp { get; set; }
        public string? Number { get; set; }
        public DateTime? Date { get; set; }
        public decimal Value { get; set; }
        public string? Company { get; set; }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string? name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
