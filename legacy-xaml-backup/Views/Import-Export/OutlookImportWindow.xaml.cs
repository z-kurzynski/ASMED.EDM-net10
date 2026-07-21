using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using ASMED.WPF.Helpers;

namespace ASMED.WPF.Views
{
    public partial class OutlookImportWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private readonly string? _pstPath;
        private readonly string? _exportPath;
        private DateTime? _dateFrom;

        private bool _isSearching;
        public bool IsSearching
        {
            get => _isSearching;
            set { _isSearching = value; OnPropertyChanged(); }
        }

        public ObservableCollection<EmailResultViewModel> EmailResults { get; set; }

        public OutlookImportWindow(string? pstPath, string? exportPath, DateTime? dateFrom)
        {
            // ? ODCZYTAJ ŚCIEŻKI Z KONFIGURACJI (ignoruj parametry konstruktora)
            // System.Diagnostics.Debug.WriteLine("=== OutlookImportWindow Constructor ===");
            // System.Diagnostics.Debug.WriteLine($"Parametry konstruktora (IGNOROWANE):");
            // System.Diagnostics.Debug.WriteLine($"  pstPath: '{pstPath ?? "(null)"}'");
            // System.Diagnostics.Debug.WriteLine($"  exportPath: '{exportPath ?? "(null)"}'");
            // System.Diagnostics.Debug.WriteLine($"  dateFrom: {dateFrom?.ToString("yyyy-MM-dd") ?? "(null)"}");

            // ? ODCZYTAJ Z OutlookConfiguration
            _pstPath = OutlookConfiguration.PstPath;
            _exportPath = OutlookConfiguration.PdfExportPath;
            _dateFrom = dateFrom;

            // System.Diagnostics.Debug.WriteLine($"Załadowano z OutlookConfiguration:");
            // System.Diagnostics.Debug.WriteLine($"  _pstPath: '{_pstPath ?? "(null)"}'");
            // System.Diagnostics.Debug.WriteLine($"  _exportPath: '{_exportPath ?? "(null)"}'");
            // System.Diagnostics.Debug.WriteLine($"  pstPath exists: {(!string.IsNullOrEmpty(_pstPath) && File.Exists(_pstPath))}");
            // System.Diagnostics.Debug.WriteLine($"  exportPath exists: {(!string.IsNullOrEmpty(_exportPath) && Directory.Exists(_exportPath))}");

            InitializeComponent();
            DataContext = this;

            // UTF-8 encoding
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            EmailResults = new ObservableCollection<EmailResultViewModel>();

            // Update time range info
            UpdateTimeRangeInfo();

            // ? WYŚWIETL ŚCIEŻKI W TYTULE OKNA
            try
            {
                var serverInfo = $"{OutlookConfiguration.EmailServer}:{OutlookConfiguration.EmailPort}";
                this.Title = $"Import z Email - {serverInfo}";

                // System.Diagnostics.Debug.WriteLine($"Window Title: '{this.Title}'");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"Error setting title: {ex.Message}");
                this.Title = "Import załączników PDF z Email";
            }

            // ? WALIDACJA KONFIGURACJI EMAIL
            if (string.IsNullOrEmpty(OutlookConfiguration.EmailServer) ||
                string.IsNullOrEmpty(OutlookConfiguration.EmailUsername))
            {
                // System.Diagnostics.Debug.WriteLine($"?? WALIDACJA FAILED: Brak konfiguracji serwera email");
                MessageBox.Show(
                    $"Brak konfiguracji serwera email!\n\n" +
                    $"Sprawdź plik: A:\\outlookconfig.txt\n\n" +
                    $"Powinien zawierać:\n" +
                    $"- Serwer email (linia 5)\n" +
                    $"- Port (linia 6)\n" +
                    $"- Użytkownik (linia 8)\n" +
                    $"- Hasło (linia 9)",
                    "Uwaga - Konfiguracja",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
            else
            {
                // System.Diagnostics.Debug.WriteLine("? Konfiguracja email OK");
            }
        }

        private void UpdateTimeRangeInfo()
        {
            // ? GUARD: Sprawdź czy TimeRangeInfoText już istnieje
            if (TimeRangeInfoText == null)
                return;

            if (_dateFrom.HasValue)
            {
                TimeRangeInfoText.Text = $"Bedzie przeszukiwany okres od: {_dateFrom.Value:yyyy-MM-dd} do dzis ({DateTime.Now:yyyy-MM-dd})";
            }
            else
            {
                TimeRangeInfoText.Text = "Bedzie przeszukiwany caly okres (wszystkie e-maile)";
            }
        }

        private void TimeRangeComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // ? GUARD: Sprawdź czy wszystkie kontrolki są już zainicjalizowane
            if (TimeRangeComboBox == null || CustomDaysPanel == null)
                return;

            if (TimeRangeComboBox.SelectedIndex == 6) // "Wlasny zakres" - indeks 6!
            {
                CustomDaysPanel.Visibility = Visibility.Visible;
            }
            else
            {
                CustomDaysPanel.Visibility = Visibility.Collapsed;
            }

            UpdateDateFromBasedOnSelection();
        }

        private void CustomDaysTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            UpdateDateFromBasedOnSelection();
        }

        private void UpdateDateFromBasedOnSelection()
        {
            if (TimeRangeComboBox == null) return;

            switch (TimeRangeComboBox.SelectedIndex)
            {
                case 0: // 1 dzień
                    _dateFrom = DateTime.Now.AddDays(-1);
                    break;
                case 1: // 1 tydzień
                    _dateFrom = DateTime.Now.AddDays(-7);
                    break;
                case 2: // 2 tygodnie
                    _dateFrom = DateTime.Now.AddDays(-14);
                    break;
                case 3: // 1 miesiąc
                    _dateFrom = DateTime.Now.AddMonths(-1);
                    break;
                case 4: // 3 miesiące
                    _dateFrom = DateTime.Now.AddMonths(-3);
                    break;
                case 5: // 6 miesięcy
                    _dateFrom = DateTime.Now.AddMonths(-6);
                    break;
                case 6: // Własny zakres
                    if (int.TryParse(CustomDaysTextBox?.Text, out int days) && days > 0)
                    {
                        _dateFrom = DateTime.Now.AddDays(-days);
                    }
                    else
                    {
                        _dateFrom = DateTime.Now.AddDays(-7); // Default
                    }
                    break;
                default:
                    _dateFrom = DateTime.Now.AddDays(-7);
                    break;
            }

            UpdateTimeRangeInfo();
        }

        private async void SearchButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                IsSearching = true;
                StatusText.Text = "Laczenie z serwerem email...";
                EmailResults.Clear();

                await Task.Run(() => SearchOutlook());

                StatusText.Text = $"Znaleziono {EmailResults.Count} e-maili z zalacznikami PDF";
                ResultCountText.Text = EmailResults.Count.ToString();
                SelectedCountText.Text = "0";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Blad przeszukiwania email:\n\n{ex.Message}",
                    "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Blad przeszukiwania";
            }
            finally
            {
                IsSearching = false;
            }
        }

        private void SearchOutlook()
        {
            try
            {
                // ? UŻYJ EmailPopHelper zamiast OutlookInteropHelper
                // System.Diagnostics.Debug.WriteLine("SearchOutlook: Używam EmailPopHelper (IMAP/POP3)");

                using var emailHelper = new EmailPopHelper();

                // Połącz się z serwerem email
                emailHelper.Connect(
                    server: OutlookConfiguration.EmailServer,
                    port: OutlookConfiguration.EmailPort,
                    useSsl: OutlookConfiguration.EmailUseSsl,
                    username: OutlookConfiguration.EmailUsername,
                    password: OutlookConfiguration.EmailPassword
                );

                // Przeszukaj email
                var emails = emailHelper.SearchEmailsWithPdfAttachments(
                    folderName: OutlookConfiguration.EmailFolder,
                    dateFrom: _dateFrom,
                    dateTo: DateTime.Now.AddDays(1) // ? +1 dzień aby uwzględnić dzisiejsze e-maile
                );

                Dispatcher.Invoke(() =>
                {
                    foreach (var email in emails)
                    {
                        EmailResults.Add(new EmailResultViewModel
                        {
                            Subject = email.Subject,
                            From = email.From,
                            ReceivedTime = email.ReceivedTime,
                            FolderPath = email.FolderPath,
                            PdfCount = email.PdfAttachments.Count,
                            PdfAttachmentsPop = email.PdfAttachments, // ? NOWE: Używaj załączników z EmailPopHelper
                            IsSelected = false
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Blad: {ex.Message}\n\n{ex.StackTrace}",
                        "Blad Email", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private async void ImportButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedEmails = EmailResults.Where(x => x.IsSelected).ToList();
                if (!selectedEmails.Any())
                {
                    MessageBox.Show("Zaznacz przynajmniej jeden e-mail do importu.",
                        "Informacja", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                IsSearching = true;
                StatusText.Text = $"Importowanie {selectedEmails.Count} e-maili...";

                int importedCount = 0;
                int duplicatesCount = 0;

                await Task.Run(() =>
                {
                    // ? UŻYJ EmailPopHelper
                    using var emailHelper = new EmailPopHelper();

                    // Połącz się ponownie (jeśli potrzeba)
                    emailHelper.Connect(
                        server: OutlookConfiguration.EmailServer,
                        port: OutlookConfiguration.EmailPort,
                        useSsl: OutlookConfiguration.EmailUseSsl,
                        username: OutlookConfiguration.EmailUsername,
                        password: OutlookConfiguration.EmailPassword
                    );

                    foreach (var email in selectedEmails)
                    {
                        bool importedAnyAttachment = false;

                        // ? NOWE: Używaj PdfAttachmentsPop
                        foreach (var attachment in email.PdfAttachmentsPop)
                        {
                            try
                            {
                                // Generate filename: original_name + email_date
                                var dateStr = email.ReceivedTime.ToString("yyyy-MM-dd_HHmmss");
                                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(attachment.FileName);
                                var extension = Path.GetExtension(attachment.FileName);

                                // ? KONWERSJA WYŁĄCZONE - zapisz oryginalny plik
                                var newFileName = $"{fileNameWithoutExt}_{dateStr}{extension}";

                                // Check for duplicates
                                if (IsDuplicate(newFileName, _exportPath))
                                {
                                    duplicatesCount++;
                                    // System.Diagnostics.Debug.WriteLine($"Duplicate found: {newFileName}");
                                    continue;
                                }

                                // Save to export folder
                                var savePath = Path.Combine(_exportPath, newFileName);
                                emailHelper.SaveAttachment(attachment, savePath);
                                importedCount++;
                                importedAnyAttachment = true;

                                // System.Diagnostics.Debug.WriteLine($"Imported: {savePath}");
                            }
                            catch (Exception)
                            {
                                // System.Diagnostics.Debug.WriteLine($"Error importing attachment: {ex.Message}");
                            }
                        }

                        // ? NOWE: Usuń lub przenieś e-mail po zaimportowaniu załączników
                        if (importedAnyAttachment)
                        {
                            /* ?? ZABLOKOWANE - Do wdrożenia w przyszłości z procedurami i użytkownikami
                            try
                            {
                                // Konwertuj EmailResultViewModel na EmailWithAttachments
                                var emailToProcess = new EmailPopHelper.EmailWithAttachments
                                {
                                    Subject = email.Subject,
                                    From = email.From,
                                    ReceivedTime = email.ReceivedTime,
                                    FolderPath = email.FolderPath,
                                    Body = "",
                                    PdfAttachments = email.PdfAttachmentsPop
                                };

                                // ? NOWE: Zmień temat e-maila (jeśli włączone)
                                if (OutlookConfiguration.EmailRenameSubject)
                                {
                                    emailHelper.RenameEmailSubject(emailToProcess, OutlookConfiguration.EmailSubjectPrefix);
                                    // System.Diagnostics.Debug.WriteLine($"Renamed subject: {email.Subject}");
                                }

                                if (OutlookConfiguration.EmailMoveToArchive && !string.IsNullOrEmpty(OutlookConfiguration.EmailArchiveFolder))
                                {
                                    // Przenieś do archiwum
                                    emailHelper.MoveEmailToArchive(emailToProcess, OutlookConfiguration.EmailArchiveFolder);
                                    // System.Diagnostics.Debug.WriteLine($"Moved to archive: {email.Subject}");
                                }
                                else if (!OutlookConfiguration.EmailRenameSubject)
                                {
                                    // Usuń e-mail (jeśli nie przenoszono do archiwum i nie zmieniono tematu)
                                    emailHelper.DeleteEmail(emailToProcess);
                                    // System.Diagnostics.Debug.WriteLine($"Deleted: {email.Subject}");
                                }
                            }
                            catch (Exception)
                            {
                                // System.Diagnostics.Debug.WriteLine($"Error moving/deleting/renaming email: {ex.Message}");
                                // Nie przerywaj importu jeśli nie udało się usunąć/przenieść/zmienić tematu
                            }
                            */

                            // ? AKTUALNIE: E-maile pozostają in skrzynce bez zmian
                            // System.Diagnostics.Debug.WriteLine($"Import completed for: {email.Subject} (email kept in mailbox)");
                        }
                    }
                });

                // Podsumowanie
                var summary = $"Zaimportowano {importedCount} plików";
                if (duplicatesCount > 0)
                {
                    summary += $"\nPominieto {duplicatesCount} duplikatów";
                }

                StatusText.Text = summary;
                NotificationHelper.ShowNotification("Import z Email", summary);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Blad importu:\n\n{ex.Message}",
                    "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Blad importu";
            }
            finally
            {
                IsSearching = false;
            }
        }

        private bool IsDuplicate(string fileName, string? basePath)
        {
            try
            {
                // ? PROSTSZA WERSJA - Directory.GetFiles już sprawdza WSZYSTKIE katalogi (włącznie z głównym)
                if (!Directory.Exists(basePath))
                    return false;

                // SearchOption.AllDirectories sprawdza:
                // - Katalog główny (A:\Skierowania\)
                // - Wszystkie podkatalogi rekursywnie (A:\Skierowania\Archiwum\, A:\Skierowania\2024\, itd.)
                var allFiles = Directory.GetFiles(basePath, fileName, SearchOption.AllDirectories);

                if (allFiles.Length > 0)
                {
                    // System.Diagnostics.Debug.WriteLine($"? IsDuplicate: Znaleziono duplikat: {allFiles[0]}");
                    return true;
                }

                // System.Diagnostics.Debug.WriteLine($"? IsDuplicate: Brak duplikatu dla: {fileName}");
                return false;
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"IsDuplicate error: {ex.Message}");
                return false;
            }
        }

        private void PreviewButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button &&
                    button.Tag is EmailResultViewModel email)
                {
                    // ? SPRAWDŹ TYPY ZAŁACZNIKÓW
                    var pdfAttachments = email.PdfAttachmentsPop.Where(a =>
                        a.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase)).ToList();

                    var docAttachments = email.PdfAttachmentsPop.Where(a =>
                        a.FileName.EndsWith(".doc", StringComparison.OrdinalIgnoreCase) ||
                        a.FileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase) ||
                        a.FileName.EndsWith(".rtf", StringComparison.OrdinalIgnoreCase)).ToList();

                    var xlsAttachments = email.PdfAttachmentsPop.Where(a =>
                        a.FileName.EndsWith(".xls", StringComparison.OrdinalIgnoreCase) ||
                        a.FileName.EndsWith(".xlsx", StringComparison.OrdinalIgnoreCase) ||
                        a.FileName.EndsWith(".xlsm", StringComparison.OrdinalIgnoreCase)).ToList();

                    // ? INFORMACJA O ZAŁĄCZNIKACH Word/Excel (bez PDF)
                    if (pdfAttachments.Count == 0 && (docAttachments.Count > 0 || xlsAttachments.Count > 0))
                    {
                        var allDocs = docAttachments.Concat(xlsAttachments).ToList();
                        var fileList = string.Join("\n", allDocs.Select(a => $"  • {a.FileName}"));

                        var result = MessageBox.Show(
                            $"?? Ten e-mail zawiera dokumenty ({allDocs.Count}):\n\n{fileList}\n\n" +
                            $"? Podgląd jest dostępny tylko dla plików PDF.\n\n" +
                            $"?? Czy chcesz zapisać te dokumenty teraz?\n" +
                            $"   Zostaną zapisane w: {_exportPath}",
                            "Zapisz dokumenty Word/Excel?",
                            MessageBoxButton.YesNo,
                            MessageBoxImage.Question);

                        if (result == MessageBoxResult.Yes)
                        {
                            // Zapisz dokumenty do folderu docelowego
                            int savedCount = 0;
                            using (var emailHelper = new EmailPopHelper())
                            {
                                emailHelper.Connect(
                                    server: OutlookConfiguration.EmailServer,
                                    port: OutlookConfiguration.EmailPort,
                                    useSsl: OutlookConfiguration.EmailUseSsl,
                                    username: OutlookConfiguration.EmailUsername,
                                    password: OutlookConfiguration.EmailPassword
                                );

                                foreach (var attachment in allDocs)
                                {
                                    try
                                    {
                                        var dateStr = email.ReceivedTime.ToString("yyyy-MM-dd_HHmmss");
                                        var fileNameWithoutExt = Path.GetFileNameWithoutExtension(attachment.FileName);
                                        var extension = Path.GetExtension(attachment.FileName);
                                        var newFileName = $"{fileNameWithoutExt}_{dateStr}{extension}";

                                        // Sprawdź duplikaty
                                        if (IsDuplicate(newFileName, _exportPath))
                                        {
                                            continue;
                                        }

                                        var savePath = Path.Combine(_exportPath, newFileName);
                                        emailHelper.SaveAttachment(attachment, savePath);
                                        savedCount++;
                                    }
                                    catch (Exception)
                                    {
                                        // System.Diagnostics.Debug.WriteLine($"Error saving: {ex.Message}");
                                    }
                                }
                            }

                            NotificationHelper.ShowSuccess($"Zapisano {savedCount} dokumentów do {_exportPath}");
                        }
                        return;
                    }

                    if (pdfAttachments.Count == 0)
                    {
                        MessageBox.Show("Brak załączników PDF do podglądu.", "Informacja",
                            MessageBoxButton.OK, MessageBoxImage.Information);
                        return;
                    }

                    // ? PODGLĄD PDF
                    var tempFolder = Path.Combine(_exportPath ?? string.Empty, "temp");
                    if (!Directory.Exists(tempFolder))
                    {
                        Directory.CreateDirectory(tempFolder);
                    }

                    // Zapisz wszystkie załączniki PDF do temp
                    var tempFiles = new List<string>();
                    using (var emailHelper = new EmailPopHelper())
                    {
                        // Połącz się z serwerem
                        emailHelper.Connect(
                            server: OutlookConfiguration.EmailServer,
                            port: OutlookConfiguration.EmailPort,
                            useSsl: OutlookConfiguration.EmailUseSsl,
                            username: OutlookConfiguration.EmailUsername,
                            password: OutlookConfiguration.EmailPassword
                        );

                        foreach (var attachment in pdfAttachments) // ? Tylko PDF
                        {
                            var tempPath = Path.Combine(tempFolder, attachment.FileName);
                            emailHelper.SaveAttachment(attachment, tempPath);
                            tempFiles.Add(tempPath);
                        }
                    }

                    // Otwórz okno podglądu z plikami z temp
                    if (tempFiles.Count > 0)
                    {
                        // ?? INFORMACJA o dokumentach Word/Excel (jeśli są razem z PDF)
                        if (docAttachments.Count > 0 || xlsAttachments.Count > 0)
                        {
                            var otherCount = docAttachments.Count + xlsAttachments.Count;
                            var otherNames = string.Join(", ",
                                docAttachments.Select(a => a.FileName)
                                .Concat(xlsAttachments.Select(a => a.FileName)));

                            MessageBox.Show(
                                $"? Pokazuję podgląd plików PDF ({pdfAttachments.Count}).\n\n" +
                                $"?? Ten e-mail zawiera również {otherCount} dokumentów:\n{otherNames}\n\n" +
                                $"?? Dokumenty Word/Excel zostaną zapisane podczas importu (przycisk '?? Importuj').",
                                "Informacja o załącznikach",
                                MessageBoxButton.OK,
                                MessageBoxImage.Information);
                        }

                        // Otwórz pierwszy plik w podglądzie
                        var previewWindow = new PdfPreviewWindow();
                        previewWindow.LoadFile(tempFiles[0]);
                        previewWindow.ShowDialog();

                        // Usuń pliki temp po zamknięciu
                        try
                        {
                            foreach (var tempFile in tempFiles)
                            {
                                if (File.Exists(tempFile))
                                {
                                    File.Delete(tempFile);
                                }
                            }
                        }
                        catch (Exception cleanupEx)
                        {
                            // System.Diagnostics.Debug.WriteLine($"Cleanup temp files error: {cleanupEx.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Blad podgladu:\n\n{ex.Message}",
                    "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void DeleteEmailButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button &&
                    button.Tag is EmailResultViewModel email)
                {
                    // Potwierdzenie
                    var action = OutlookConfiguration.EmailMoveToArchive && !string.IsNullOrEmpty(OutlookConfiguration.EmailArchiveFolder)
                        ? $"przeniesc do folderu '{OutlookConfiguration.EmailArchiveFolder}'"
                        : "usunac na zawsze";

                    var result = MessageBox.Show(
                        $"Czy na pewno chcesz {action} e-mail:\n\n" +
                        $"Temat: {email.Subject}\n" +
                        $"Od: {email.From}\n" +
                        $"Data: {email.ReceivedTime:yyyy-MM-dd HH:mm}\n\n" +
                        $"Ta operacja nie moze byc cofnieta!",
                        "Potwierdzenie",
                        MessageBoxButton.YesNo,
                        MessageBoxImage.Warning);

                    if (result != MessageBoxResult.Yes)
                        return;

                    IsSearching = true;
                    StatusText.Text = "Usuwanie/przenoszenie e-maila...";

                    await Task.Run(() =>
                    {
                        using var emailHelper = new EmailPopHelper();

                        // Połącz się z serwerem
                        emailHelper.Connect(
                            server: OutlookConfiguration.EmailServer,
                            port: OutlookConfiguration.EmailPort,
                            useSsl: OutlookConfiguration.EmailUseSsl,
                            username: OutlookConfiguration.EmailUsername,
                            password: OutlookConfiguration.EmailPassword
                        );

                        // Konwertuj EmailResultViewModel na EmailWithAttachments
                        var emailToProcess = new EmailPopHelper.EmailWithAttachments
                        {
                            Subject = email.Subject,
                            From = email.From,
                            ReceivedTime = email.ReceivedTime,
                            FolderPath = email.FolderPath,
                            Body = "",
                            PdfAttachments = email.PdfAttachmentsPop
                        };

                        if (OutlookConfiguration.EmailMoveToArchive && !string.IsNullOrEmpty(OutlookConfiguration.EmailArchiveFolder))
                        {
                            // Przenieś do archiwum
                            emailHelper.MoveEmailToArchive(emailToProcess, OutlookConfiguration.EmailArchiveFolder);
                        }
                        else
                        {
                            // Usuń e-mail
                            emailHelper.DeleteEmail(emailToProcess);
                        }
                    });

                    // Usuń z listy
                    EmailResults.Remove(email);
                    ResultCountText.Text = EmailResults.Count.ToString();

                    StatusText.Text = $"E-mail zostal {(OutlookConfiguration.EmailMoveToArchive ? "przeniesiony" : "usuniety")}";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Blad usuwania e-maila:\n\n{ex.Message}",
                    "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
                StatusText.Text = "Blad usuwania";
            }
            finally
            {
                IsSearching = false;
            }
        }

        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var email in EmailResults)
            {
                email.IsSelected = true;
            }
            UpdateSelectedCount();
        }

        private void DeselectAllButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var email in EmailResults)
            {
                email.IsSelected = false;
            }
            UpdateSelectedCount();
        }

        private void UpdateSelectedCount()
        {
            var count = EmailResults.Count(x => x.IsSelected);
            SelectedCountText.Text = count.ToString();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public class EmailResultViewModel : INotifyPropertyChanged
        {
            public event PropertyChangedEventHandler? PropertyChanged;
            protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }

            private bool _isSelected;
            public bool IsSelected
            {
                get => _isSelected;
                set { _isSelected = value; OnPropertyChanged(); }
            }

            public string?Subject { get; set; }
            public string?From { get; set; }
            public DateTime ReceivedTime { get; set; }
            public string?FolderPath { get; set; }
            public int PdfCount { get; set; }

            // ? Używamy tylko EmailPopHelper (IMAP/POP3)
            public System.Collections.Generic.List<ASMED.WPF.Helpers.EmailPopHelper.AttachmentInfo> PdfAttachmentsPop { get; set; } = new System.Collections.Generic.List<ASMED.WPF.Helpers.EmailPopHelper.AttachmentInfo>();
        }
    }
}
