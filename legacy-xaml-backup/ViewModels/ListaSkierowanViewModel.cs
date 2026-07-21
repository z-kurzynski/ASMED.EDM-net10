using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Input;
using ASMED.WPF.Helpers;
using ASMED.WPF.Models;

namespace ASMED.WPF.ViewModels
{
    public class ListaSkierowanViewModel : INotifyPropertyChanged
    {
        private const string SKIEROWANIA_DIR = @"A:\Skierowania";
        private const string ARCHIWUM_DIR = @"A:\Skierowania\Archiwum";

        // ? Obsługiwane rozszerzenia plików
        private static readonly string[] SUPPORTED_EXTENSIONS = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".xlsm", ".rtf" };

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private ObservableCollection<SkierowanieFileInfo> _plikiSkierowan;
        public ObservableCollection<SkierowanieFileInfo> PlikiSkierowan
        {
            get => _plikiSkierowan;
            set
            {
                _plikiSkierowan = value;
                OnPropertyChanged();
            }
        }

        private SkierowanieFileInfo? _wybranyPlik;
        public SkierowanieFileInfo? WybranyPlik
        {
            get => _wybranyPlik;
            set
            {
                _wybranyPlik = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(PdfFilePath));
                OnPropertyChanged(nameof(IsPdfFile));
            }
        }

        public string ?PdfFilePath => WybranyPlik?.FullPath;

        // ? NOWE: Właściwość sprawdzająca czy wybrany plik to PDF
        public bool IsPdfFile
        {
            get
            {
                if (WybranyPlik == null) return false;
                var ext = Path.GetExtension(WybranyPlik.FullPath).ToLowerInvariant();
                return ext == ".pdf";
            }
        }

        public ICommand ?UsunPlikCommand { get; }
        public ICommand ?ArchiwizujPlikCommand { get; }
        public ICommand ?OdswiezListeCommand { get; }
        public ICommand ?ZamknijOknoCommand { get; }

        public Action CloseAction { get; set; }

        #region ? NOWE: Przełącznik Aktywne/Archiwum

        private bool _showActiveFiles = true;
        public bool ShowActiveFiles
        {
            get => _showActiveFiles;
            set
            {
                if (_showActiveFiles != value)
                {
                    _showActiveFiles = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(ShowArchivedFiles));
                    OnPropertyChanged(nameof(CurrentFolderPath));
                    OdswiezListe(); // Odśwież listę po zmianie
                }
            }
        }

        public bool ShowArchivedFiles
        {
            get => !_showActiveFiles;
            set
            {
                if (value)
                {
                    ShowActiveFiles = false;
                }
            }
        }

        public string ?CurrentFolderPath => ShowActiveFiles ? SKIEROWANIA_DIR : ARCHIWUM_DIR;

        #endregion

        public ListaSkierowanViewModel()
        {
            PlikiSkierowan = new ObservableCollection<SkierowanieFileInfo>();

            UsunPlikCommand = new RelayCommand(_ => UsunPlik());
            ArchiwizujPlikCommand = new RelayCommand(_ => ArchiwizujPlik());
            OdswiezListeCommand = new RelayCommand(_ => OdswiezListe());
            ZamknijOknoCommand = new RelayCommand(_ => ZamknijOkno());

            EnsureDirectoriesExist();
            OdswiezListe();
        }

        private void EnsureDirectoriesExist()
        {
            try
            {
                if (!Directory.Exists(SKIEROWANIA_DIR))
                {
                    Directory.CreateDirectory(SKIEROWANIA_DIR);
                }
                if (!Directory.Exists(ARCHIWUM_DIR))
                {
                    Directory.CreateDirectory(ARCHIWUM_DIR);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd tworzenia katalogów:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void OdswiezListe()
        {
            try
            {
                PlikiSkierowan.Clear();

                // ? ZMIENIONE: Wybierz katalog w zależności od przełącznika
                string targetDir = ShowActiveFiles ? SKIEROWANIA_DIR : ARCHIWUM_DIR;

                if (!Directory.Exists(targetDir))
                {
                    MessageBox.Show($"Katalog nie istnieje:\n{targetDir}",
                        "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // ? Pobierz wszystkie obsługiwane pliki (PDF + Word + Excel)
                var pliki = Directory.GetFiles(targetDir, "*.*")
                    .Where(f => SUPPORTED_EXTENSIONS.Contains(Path.GetExtension(f).ToLowerInvariant()))
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.LastWriteTime)
                    .Select(f => SkierowanieFileInfo.FromFileInfo(f))
                    .ToList();

                foreach (var plik in pliki)
                {
                    PlikiSkierowan.Add(plik);
                }

                string folderType = ShowActiveFiles ? "Aktywne" : "Archiwum";
                // System.Diagnostics.Debug.WriteLine($"OdswiezListe [{folderType}]: Znaleziono {pliki.Count} plik(ów): " +
                //     $"PDF={pliki.Count(p => p.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))}, " +
                //     $"Word={pliki.Count(p => p.FileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase) || p.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))}, " +
                //     $"Excel={pliki.Count(p => p.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) || p.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase))}");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd odświeżania listy plików:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UsunPlik()
        {
            if (WybranyPlik == null)
            {
                MessageBox.Show("Wybierz plik do usunięcia.",
                    "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Czy na pewno chcesz USUNĄĆ plik?\n\n{WybranyPlik.FileName}\n\nTej operacji NIE MOŻNA cofnąć!",
                "Potwierdzenie usunięcia",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    File.Delete(WybranyPlik.FullPath);
                    MessageBox.Show($"Plik został usunięty:\n\n{WybranyPlik.FileName}",
                        "Usunięto", MessageBoxButton.OK, MessageBoxImage.Information);
                    OdswiezListe();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd usuwania pliku:\n\n{ex.Message}",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ArchiwizujPlik()
        {
            if (WybranyPlik == null)
            {
                MessageBox.Show("Wybierz plik do zarchiwizowania.",
                    "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // ? ZMIENIONE: Blokuj archiwizację jeśli już w archiwum
            if (!ShowActiveFiles)
            {
                MessageBox.Show("Ten plik jest już w archiwum. Przełącz się na widok 'Aktywne' aby archiwizować pliki.",
                    "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Czy chcesz przenieść plik do archiwum?\n\n{WybranyPlik.FileName}",
                "Potwierdzenie archiwizacji",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    EnsureDirectoriesExist();

                    string targetPath = Path.Combine(ARCHIWUM_DIR, WybranyPlik.FileName);

                    // Jeśli plik o tej samej nazwie już istnieje w archiwum, dodaj timestamp
                    if (File.Exists(targetPath))
                    {
                        string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(WybranyPlik.FileName);
                        string extension = Path.GetExtension(WybranyPlik.FileName);
                        targetPath = Path.Combine(ARCHIWUM_DIR, $"{fileNameWithoutExt}_{timestamp}{extension}");
                    }

                    File.Move(WybranyPlik.FullPath, targetPath);

                    MessageBox.Show($"Plik został przeniesiony do archiwum:\n\n{Path.GetFileName(targetPath)}",
                        "Zarchiwizowano", MessageBoxButton.OK, MessageBoxImage.Information);
                    OdswiezListe();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Błąd archiwizacji pliku:\n\n{ex.Message}",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ZamknijOkno()
        {
            CloseAction?.Invoke();
        }
    }
}
