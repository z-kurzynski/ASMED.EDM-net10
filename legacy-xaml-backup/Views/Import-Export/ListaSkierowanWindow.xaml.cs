using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows;
using ASMED.WPF.ViewModels;
using ASMED.WPF.Helpers;
using System.Collections.Generic;
using System;

namespace ASMED.WPF.Views
{
    public partial class ListaSkierowanWindow : Window
    {
        private ListaSkierowanViewModel _viewModel;
        private ObservableCollection<EmailResultItem> _emailResults;
        private DateTime? _emailDateFrom;

        public ListaSkierowanWindow()
        {
            InitializeComponent();

            _viewModel = new ListaSkierowanViewModel();
            _viewModel.CloseAction = () => this.Close();
            _viewModel.PropertyChanged += ViewModel_PropertyChanged;

            _emailResults = new ObservableCollection<EmailResultItem>();
            EmailsListBox.ItemsSource = _emailResults;

            DataContext = _viewModel;

            // Domyślny zakres czasu dla emaili
            _emailDateFrom = DateTime.Now.AddDays(-7);
        }

        private void ViewModel_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(ListaSkierowanViewModel.WybranyPlik))
            {
                // ✅ WYCZYŚĆ ZAZNACZENIE EMAILA gdy wybrano plik lokalny
                if (_viewModel.WybranyPlik != null)
                {
                    // Tymczasowo odepnij event aby uniknąć pętli
                    EmailsListBox.SelectionChanged -= EmailsListBox_SelectionChanged;
                    EmailsListBox.SelectedItem = null; // Odznacz email
                    EmailsListBox.SelectionChanged += EmailsListBox_SelectionChanged;

                    // System.Diagnostics.Debug.WriteLine($"📁 Wybrano plik lokalny: {_viewModel.WybranyPlik.FileName}");
                }

                LoadFileToViewer();

                // ✅ Aktualizuj tekst źródła
                if (_viewModel.WybranyPlik != null)
                {
                    var folderType = _viewModel.ShowActiveFiles ? "Aktywne" : "Archiwum";
                    PreviewSourceText.Text = $"📁 Plik lokalny ({folderType}): {_viewModel.WybranyPlik.FileName}";
                }
                else
                {
                    PreviewSourceText.Text = "Wybierz plik lub email aby wyświetlić podgląd";
                }
            }
        }

        private void LoadFileToViewer()
        {
            try
            {
                if (_viewModel.WybranyPlik != null && File.Exists(_viewModel.WybranyPlik.FullPath))
                {
                    var extension = Path.GetExtension(_viewModel.WybranyPlik.FullPath).ToLowerInvariant();

                    // ✅ Dla PDF - załaduj do PdfViewer
                    if (extension == ".pdf")
                    {
                        // System.Diagnostics.Debug.WriteLine($"Ładowanie PDF do podglądu: {_viewModel.WybranyPlik.FileName}");

                        PdfViewer.Visibility = Visibility.Visible;
                        DocInfoPanel.Visibility = Visibility.Collapsed;
                        EmptyMessage.Visibility = Visibility.Collapsed;

                        PdfViewer.Load(_viewModel.WybranyPlik.FullPath);
                    }
                    else
                    {
                        // ✅ Dla Word/Excel - pokaż info panel
                        // System.Diagnostics.Debug.WriteLine($"Plik Word/Excel: {_viewModel.WybranyPlik.FileName} - wyświetlam info panel");

                        PdfViewer.Visibility = Visibility.Collapsed;
                        DocInfoPanel.Visibility = Visibility.Visible;
                        EmptyMessage.Visibility = Visibility.Collapsed;

                        PdfViewer.Unload();
                    }
                }
                else
                {
                    PdfViewer.Visibility = Visibility.Collapsed;
                    DocInfoPanel.Visibility = Visibility.Collapsed;
                    EmptyMessage.Visibility = Visibility.Visible;

                    PdfViewer.Unload();
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Błąd ładowania pliku do podglądu:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);

                PdfViewer.Visibility = Visibility.Collapsed;
                DocInfoPanel.Visibility = Visibility.Collapsed;
                EmptyMessage.Visibility = Visibility.Visible;

                PdfViewer.Unload();
            }
        }

        private void OtworzPlik_Click(object? sender, RoutedEventArgs e)
        {
            if (_viewModel?.WybranyPlik != null)
            {
                try
                {
                    var filePath = _viewModel.WybranyPlik.FullPath;
                    var extension = Path.GetExtension(filePath).ToLowerInvariant();

                    // System.Diagnostics.Debug.WriteLine($"Otwieranie pliku w zewnętrznym programie: {filePath} (rozszerzenie: {extension})");

                    // ✅ Otwórz plik w domyślnej aplikacji Windows
                    // - PDF → Adobe/Edge/Chrome
                    // - Word → Microsoft Word
                    // - Excel → Microsoft Excel
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });

                    // System.Diagnostics.Debug.WriteLine($"✅ Plik otwarty pomyślnie: {Path.GetFileName(filePath)}");
                }
                catch (System.Exception ex)
                {
                    var extension = Path.GetExtension(_viewModel.WybranyPlik.FullPath).ToLowerInvariant();
                    var programName = extension switch
                    {
                        ".pdf" => "Adobe Reader, Edge lub Chrome",
                        ".doc" or ".docx" or ".rtf" => "Microsoft Word",
                        ".xls" or ".xlsx" or ".xlsm" => "Microsoft Excel",
                        _ => "odpowiedni program"
                    };

                    MessageBox.Show($"Nie można otworzyć pliku:\n\n{ex.Message}\n\nUpewnij się, że masz zainstalowany: {programName}",
                        "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);

                    // System.Diagnostics.Debug.WriteLine($"❌ Błąd otwierania pliku: {ex.Message}");
                }
            }
        }

        #region ✅ NOWE: Obsługa zarządzania oknami

        /// <summary>
        /// Przesuwa okno na lewy monitor (Win+Shift+←)
        /// </summary>
        private void MoveToLeftScreen_Click(object? sender, RoutedEventArgs e)
        {
            WindowPositionHelper.MoveToLeftMonitor(this);
        }

        /// <summary>
        /// Przesuwa okno na prawy monitor (Win+Shift+→)
        /// </summary>
        private void MoveToRightScreen_Click(object? sender, RoutedEventArgs e)
        {
            WindowPositionHelper.MoveToRightMonitor(this);
        }

        /// <summary>
        /// Przełącza tryb pełnoekranowy
        /// </summary>
        private void ToggleMaximize_Click(object? sender, RoutedEventArgs e)
        {
            WindowPositionHelper.MaximizeWindow(this);
        }

        #endregion

        #region ✅ NOWE: Obsługa emaili

        private void EmailTimeRangeComboBox_SelectionChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            if (EmailTimeRangeComboBox == null) return;

            switch (EmailTimeRangeComboBox.SelectedIndex)
            {
                case 0: _emailDateFrom = DateTime.Now.AddDays(-1); break;
                case 1: _emailDateFrom = DateTime.Now.AddDays(-7); break;
                case 2: _emailDateFrom = DateTime.Now.AddDays(-14); break;
                case 3: _emailDateFrom = DateTime.Now.AddMonths(-1); break;
                default: _emailDateFrom = DateTime.Now.AddDays(-7); break;
            }
        }

        private async void SearchEmailsButton_Click(object? sender, RoutedEventArgs e)
        {
            try
            {
                EmailStatusText.Text = "Łączenie z serwerem email...";
                _emailResults.Clear();

                await Task.Run(() => SearchEmailsAsync());

                EmailStatusText.Text = $"Znaleziono {_emailResults.Count} email(i) z załącznikami";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd przeszukiwania email:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                EmailStatusText.Text = "Błąd przeszukiwania";
            }
        }

        private void SearchEmailsAsync()
        {
            try
            {
                using var emailHelper = new EmailPopHelper();

                emailHelper.Connect(
                    server: OutlookConfiguration.EmailServer,
                    port: OutlookConfiguration.EmailPort,
                    useSsl: OutlookConfiguration.EmailUseSsl,
                    username: OutlookConfiguration.EmailUsername,
                    password: OutlookConfiguration.EmailPassword
                );

                var emails = emailHelper.SearchEmailsWithPdfAttachments(
                    folderName: OutlookConfiguration.EmailFolder,
                    dateFrom: _emailDateFrom,
                    dateTo: DateTime.Now.AddDays(1)
                );

                Dispatcher.Invoke(() =>
                {
                    foreach (var email in emails)
                    {
                        _emailResults.Add(new EmailResultItem
                        {
                            Subject = email.Subject,
                            From = email.From,
                            ReceivedTime = email.ReceivedTime,
                            FolderPath = email.FolderPath,
                            PdfCount = email.PdfAttachments.Count,
                            Attachments = email.PdfAttachments
                        });
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    MessageBox.Show($"Błąd: {ex.Message}",
                        "Błąd Email", MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
        }

        private void EmailsListBox_SelectionChanged(object? sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // System.Diagnostics.Debug.WriteLine($"📧 EmailsListBox_SelectionChanged called. SelectedItem: {EmailsListBox.SelectedItem?.GetType().Name ?? "null"}");

            if (EmailsListBox.SelectedItem is EmailResultItem email)
            {
                // ✅ WYCZYŚĆ ZAZNACZENIE PLIKU LOKALNEGO gdy wybrano email
                _viewModel.WybranyPlik = null;

                EmailStatusText.Text = $"Wybrano: {email.Subject} ({email.PdfCount} załącznik(ów))";

                // System.Diagnostics.Debug.WriteLine($"✅ Wywołuję LoadEmailAttachmentPreview dla: {email.Subject}");

                // ✅ Załaduj podgląd pierwszego załącznika PDF
                LoadEmailAttachmentPreview(email);
            }
            else if (EmailsListBox.SelectedItem == null)
            {
                // System.Diagnostics.Debug.WriteLine("⚠️ Email odznaczony");

                // ✅ ODZNACZONO email - przywróć podgląd pliku lokalnego
                if (_viewModel.WybranyPlik != null)
                {
                    // System.Diagnostics.Debug.WriteLine($"🔄 Przywracam podgląd pliku lokalnego: {_viewModel.WybranyPlik.FileName}");
                    LoadFileToViewer();
                }
                else
                {
                    PreviewSourceText.Text = "Wybierz plik lub email aby wyświetlić podgląd";

                    PdfViewer.Visibility = Visibility.Collapsed;
                    DocInfoPanel.Visibility = Visibility.Collapsed;
                    EmptyMessage.Visibility = Visibility.Visible;

                    PdfViewer.Unload();
                }
            }
        }

        private async void LoadEmailAttachmentPreview(EmailResultItem email)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine($"🔍 LoadEmailAttachmentPreview START dla: {email.Subject}");
                // System.Diagnostics.Debug.WriteLine($"   Liczba załączników: {email.Attachments?.Count ?? 0}");

                // Znajdź pierwszy załącznik PDF
                var pdfAttachment = email.Attachments?.FirstOrDefault(a =>
                    a.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase));

                if (pdfAttachment == null)
                {
                    // System.Diagnostics.Debug.WriteLine("⚠️ Brak załączników PDF");
                    PreviewSourceText.Text = "⚠️ Ten email nie zawiera załączników PDF";

                    // ✅ Ukryj wszystkie panele
                    PdfViewer.Visibility = Visibility.Collapsed;
                    DocInfoPanel.Visibility = Visibility.Collapsed;
                    EmptyMessage.Visibility = Visibility.Visible;

                    PdfViewer.Unload();
                    return;
                }

                // System.Diagnostics.Debug.WriteLine($"✅ Znaleziono załącznik PDF: {pdfAttachment.FileName}");
                PreviewSourceText.Text = $"📧 Ładowanie z emaila: {pdfAttachment.FileName}...";

                // Zapisz do folderu temp
                var tempFolder = Path.Combine(Path.GetTempPath(), "ASMED_EmailPreview");
                if (!Directory.Exists(tempFolder))
                {
                    Directory.CreateDirectory(tempFolder);
                    // System.Diagnostics.Debug.WriteLine($"✅ Utworzono folder temp: {tempFolder}");
                }

                var tempFilePath = Path.Combine(tempFolder, pdfAttachment.FileName);
                // System.Diagnostics.Debug.WriteLine($"🔄 Pobieranie do: {tempFilePath}");

                // Pobierz załącznik z serwera email
                await Task.Run(() =>
                {
                    using var emailHelper = new EmailPopHelper();

                    // System.Diagnostics.Debug.WriteLine("🔌 Łączenie z serwerem email...");
                    emailHelper.Connect(
                        server: OutlookConfiguration.EmailServer,
                        port: OutlookConfiguration.EmailPort,
                        useSsl: OutlookConfiguration.EmailUseSsl,
                        username: OutlookConfiguration.EmailUsername,
                        password: OutlookConfiguration.EmailPassword
                    );
                    // System.Diagnostics.Debug.WriteLine("✅ Połączono z serwerem");

                    // System.Diagnostics.Debug.WriteLine($"💾 Zapisywanie załącznika...");
                    emailHelper.SaveAttachment(pdfAttachment, tempFilePath);
                    // System.Diagnostics.Debug.WriteLine($"✅ Załącznik zapisany");
                });

                // Załaduj do podglądu
                if (File.Exists(tempFilePath))
                {
                    // System.Diagnostics.Debug.WriteLine($"📄 Ładowanie do PdfViewer: {tempFilePath}");

                    // ✅ KLUCZOWE: Pokaż PdfViewer i ukryj resztę
                    PdfViewer.Visibility = Visibility.Visible;
                    DocInfoPanel.Visibility = Visibility.Collapsed;
                    EmptyMessage.Visibility = Visibility.Collapsed;

                    PdfViewer.Load(tempFilePath);
                    PreviewSourceText.Text = $"📧 Podgląd z emaila: {pdfAttachment.FileName}";
                    // System.Diagnostics.Debug.WriteLine($"✅ PDF załadowany do podglądu");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"❌ Plik nie istnieje: {tempFilePath}");
                    PreviewSourceText.Text = "⚠️ Nie udało się pobrać załącznika";

                    PdfViewer.Visibility = Visibility.Collapsed;
                    DocInfoPanel.Visibility = Visibility.Collapsed;
                    EmptyMessage.Visibility = Visibility.Visible;

                    PdfViewer.Unload();
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"❌ BŁĄD LoadEmailAttachmentPreview: {ex.Message}");
                // System.Diagnostics.Debug.WriteLine($"   StackTrace: {ex.StackTrace}");
                PreviewSourceText.Text = $"⚠️ Błąd ładowania podglądu: {ex.Message}";

                PdfViewer.Visibility = Visibility.Collapsed;
                DocInfoPanel.Visibility = Visibility.Collapsed;
                EmptyMessage.Visibility = Visibility.Visible;

                PdfViewer.Unload();

                MessageBox.Show($"Błąd ładowania podglądu z emaila:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private async void ImportEmailAttachmentsButton_Click(object? sender, RoutedEventArgs e)
        {
            if (EmailsListBox.SelectedItem is not EmailResultItem selectedEmail)
            {
                MessageBox.Show("Wybierz email z listy.", "Informacja",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                EmailStatusText.Text = "Importowanie załączników...";

                int importedCount = 0;
                await Task.Run(() =>
                {
                    using var emailHelper = new EmailPopHelper();

                    emailHelper.Connect(
                        server: OutlookConfiguration.EmailServer,
                        port: OutlookConfiguration.EmailPort,
                        useSsl: OutlookConfiguration.EmailUseSsl,
                        username: OutlookConfiguration.EmailUsername,
                        password: OutlookConfiguration.EmailPassword
                    );

                    foreach (var attachment in selectedEmail.Attachments)
                    {
                        try
                        {
                            var dateStr = selectedEmail.ReceivedTime.ToString("yyyy-MM-dd_HHmmss");
                            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(attachment.FileName);
                            var extension = Path.GetExtension(attachment.FileName);
                            var newFileName = $"{fileNameWithoutExt}_{dateStr}{extension}";

                            var savePath = Path.Combine(OutlookConfiguration.PdfExportPath, newFileName);

                            if (!File.Exists(savePath))
                            {
                                emailHelper.SaveAttachment(attachment, savePath);
                                importedCount++;
                            }
                        }
                        catch (Exception)
                        {
                            // System.Diagnostics.Debug.WriteLine($"Error importing: {ex.Message}");
                        }
                    }
                });

                EmailStatusText.Text = $"Zaimportowano {importedCount} plik(ów)";
                NotificationHelper.ShowSuccess($"Zaimportowano {importedCount} plik(ów) z emaila");

                // Odśwież listę plików
                _viewModel?.OdswiezListeCommand?.Execute(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd importu:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                EmailStatusText.Text = "Błąd importu";
            }
        }

        private async void DeleteEmailButton_Click(object? sender, RoutedEventArgs e)
        {
            if (EmailsListBox.SelectedItem is not EmailResultItem selectedEmail)
            {
                MessageBox.Show("Wybierz email z listy.", "Informacja",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var result = MessageBox.Show(
                $"Czy na pewno chcesz usunąć email:\n\n{selectedEmail.Subject}\n\nTa operacja nie może być cofnięta!",
                "Potwierdzenie",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                EmailStatusText.Text = "Usuwanie emaila...";

                await Task.Run(() =>
                {
                    using var emailHelper = new EmailPopHelper();

                    emailHelper.Connect(
                        server: OutlookConfiguration.EmailServer,
                        port: OutlookConfiguration.EmailPort,
                        useSsl: OutlookConfiguration.EmailUseSsl,
                        username: OutlookConfiguration.EmailUsername,
                        password: OutlookConfiguration.EmailPassword
                    );

                    var emailToDelete = new EmailPopHelper.EmailWithAttachments
                    {
                        Subject = selectedEmail.Subject,
                        From = selectedEmail.From,
                        ReceivedTime = selectedEmail.ReceivedTime,
                        FolderPath = selectedEmail.FolderPath,
                        Body = "",
                        PdfAttachments = selectedEmail.Attachments
                    };

                    if (OutlookConfiguration.EmailMoveToArchive &&
                        !string.IsNullOrEmpty(OutlookConfiguration.EmailArchiveFolder))
                    {
                        emailHelper.MoveEmailToArchive(emailToDelete, OutlookConfiguration.EmailArchiveFolder);
                    }
                    else
                    {
                        emailHelper.DeleteEmail(emailToDelete);
                    }
                });

                _emailResults.Remove(selectedEmail);
                EmailStatusText.Text = "Email usunięty";
                NotificationHelper.ShowSuccess("Email został usunięty");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd usuwania:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
                EmailStatusText.Text = "Błąd usuwania";
            }
        }

        #endregion

        protected override void OnClosing(CancelEventArgs e)
        {
            try
            {
                PdfViewer?.Unload();

                if (_viewModel != null)
                {
                    _viewModel.PropertyChanged -= ViewModel_PropertyChanged;
                }

                // ✅ NOWE: Wyczyść folder temp z podglądami emaili
                CleanupEmailPreviewTemp();
            }
            catch { }

            base.OnClosing(e);
        }

        private void CleanupEmailPreviewTemp()
        {
            try
            {
                var tempFolder = Path.Combine(Path.GetTempPath(), "ASMED_EmailPreview");
                if (Directory.Exists(tempFolder))
                {
                    Directory.Delete(tempFolder, true);
                    // System.Diagnostics.Debug.WriteLine("✅ Wyczyszczono folder temp z podglądami emaili");
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"⚠️ Nie udało się wyczyścić folderu temp: {ex.Message}");
            }
        }

        #region Helper Classes

        public class EmailResultItem
        {
            public string ?Subject { get; set; }
            public string ?From { get; set; }
            public DateTime ReceivedTime { get; set; }
            public string ?FolderPath { get; set; }
            public int PdfCount { get; set; }
            public List<EmailPopHelper.AttachmentInfo> Attachments { get; set; }
        }

        #endregion
    }
}
