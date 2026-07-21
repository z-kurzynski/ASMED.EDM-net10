using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using ASMED.WPF.Helpers;
using static ASMED.WPF.Views.OutlookImportWindow;

namespace ASMED.WPF.Views
{
    public partial class OutlookAttachmentPreviewWindow : Window
    {
        private readonly EmailResultViewModel _email;

        public OutlookAttachmentPreviewWindow(EmailResultViewModel email)
        {
            InitializeComponent();

            _email = email;

            // Display email info
            SubjectText.Text = email.Subject;
            FromText.Text = email.From;
            DateText.Text = email.ReceivedTime.ToString("yyyy-MM-dd HH:mm:ss");

            // Display attachments
            // ? U�ywamy PdfAttachmentsPop zamiast PdfAttachments
            var attachmentViewModels = email.PdfAttachmentsPop.Select(a => new AttachmentViewModel
            {
                FileName = a.FileName,
                Size = a.Size,
                SizeFormatted = FormatFileSize(a.Size),
                Data = a.Data
            }).ToList();

            AttachmentsList.ItemsSource = attachmentViewModels;
        }

        private string ?FormatFileSize(long bytes)
        {
            if (bytes < 1024)
                return $"{bytes} B";
            if (bytes < 1024 * 1024)
                return $"{bytes / 1024.0:F1} KB";
            return $"{bytes / (1024.0 * 1024.0):F1} MB";
        }

        private void PreviewAttachment_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is System.Windows.Controls.Button button &&
                    button.Tag is AttachmentViewModel attachment)
                {
                    // Save to temp folder and open
                    var tempPath = Path.Combine(Path.GetTempPath(), "ASMED_Outlook", attachment.FileName);
                    var tempDir = Path.GetDirectoryName(tempPath);

                    if (!Directory.Exists(tempDir ?? string.Empty))
                        Directory.CreateDirectory(tempDir ?? string.Empty);

                    // Save attachment
                    if (attachment.Data != null)
                    {
                        File.WriteAllBytes(tempPath, attachment.Data);

                        // Open in default PDF viewer
                        var pdfPreview = new PdfPreviewWindow();
                        pdfPreview.LoadFile(tempPath);
                        pdfPreview.Show();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Blad podgladu PDF:\n\n{ex.Message}",
                    "Blad", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        public class AttachmentViewModel
        {
            public string ?FileName { get; set; }
            public long Size { get; set; }
            public string ?SizeFormatted { get; set; }
            public byte[]? Data { get; set; }
        }
    }
}
