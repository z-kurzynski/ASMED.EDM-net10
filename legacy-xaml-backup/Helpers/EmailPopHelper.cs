using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using MailKit;
using MailKit.Net.Pop3;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;

namespace ASMED.WPF.Helpers
{
    /// <summary>
    /// Klasa pomocnicza do przeszukiwania skrzynki email przez POP3/IMAP
    /// (bez Outlook - bezpo�rednio z serwera email)
    /// </summary>
    public class EmailPopHelper : IDisposable
    {
        private ImapClient? _imapClient;
        private Pop3Client? _pop3Client;
        private bool _disposed = false;
        private bool _usePop3 = false; // Flaga okre�laj�ca u�ywany protok�

        /// <summary>
        /// Model danych dla e-maila z za��cznikami
        /// </summary>
        public class EmailWithAttachments
        {
            public string? Subject { get; set; }
            public string? From { get; set; }
            public DateTime ReceivedTime { get; set; }
            public List<AttachmentInfo> PdfAttachments { get; set; } = new List<AttachmentInfo>();
            public string Body { get; set; } = string.Empty;
            public string? FolderPath { get; set; }
        }

        /// <summary>
        /// Informacje o za��czniku
        /// </summary>
        public class AttachmentInfo
        {
            public string FileName { get; set; } = string.Empty;
            public long Size { get; set; }
            public byte[]? Data { get; set; }
        }

        /// <summary>
        /// ��czy si� z serwerem email (automatycznie wykrywa POP3 lub IMAP)
        /// </summary>
        public void Connect(string server, int port, bool useSsl, string username, string password)
        {
            try
            {
                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: Connecting to {server}:{port} (SSL: {useSsl})");

                // ? WYKRYJ PROTOKӣ PO PORCIE
                // Port 995 = POP3 SSL, Port 110 = POP3
                // Port 993 = IMAP SSL, Port 143 = IMAP
                _usePop3 = (port == 995 || port == 110);

                if (_usePop3)
                {
                    // System.Diagnostics.Debug.WriteLine("EmailPopHelper: Detected POP3 protocol");

                    _pop3Client = new Pop3Client();

                    // Callback dla walidacji certyfikatu SSL
                    _pop3Client.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: SSL Certificate validation:");
                        // System.Diagnostics.Debug.WriteLine($"  Subject: {certificate.Subject}");
                        // System.Diagnostics.Debug.WriteLine($"  Issuer: {certificate.Issuer}");
                        // System.Diagnostics.Debug.WriteLine($"  Errors: {sslPolicyErrors}");

                        if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                            return true;

                        // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: ?? Akceptuj� certyfikat pomimo b��d�w: {sslPolicyErrors}");
                        return true;
                    };

                    _pop3Client.Connect(server, port, useSsl);
                    _pop3Client.Authenticate(username, password);
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine("EmailPopHelper: Detected IMAP protocol");

                    _imapClient = new ImapClient();

                    // Callback dla walidacji certyfikatu SSL
                    _imapClient.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                    {
                        // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: SSL Certificate validation:");
                        // System.Diagnostics.Debug.WriteLine($"  Subject: {certificate.Subject}");
                        // System.Diagnostics.Debug.WriteLine($"  Issuer: {certificate.Issuer}");
                        // System.Diagnostics.Debug.WriteLine($"  Errors: {sslPolicyErrors}");

                        if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                            return true;

                        // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: ?? Akceptuj� certyfikat pomimo b��d�w: {sslPolicyErrors}");
                        return true;
                    };

                    _imapClient.Connect(server, port, useSsl);
                    _imapClient.Authenticate(username, password);
                }

                // System.Diagnostics.Debug.WriteLine("EmailPopHelper: Successfully connected and authenticated");
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: Connection error: {ex.Message}");
                throw new Exception($"Nie mo�na po��czy� si� z serwerem email:\n\n{ex.Message}", ex);
            }
        }

        /// <summary>
        /// Przeszukuje skrzynk? email w poszukiwaniu e-maili z za??cznikami (PDF, Word, Excel)
        /// </summary>
        public List<EmailWithAttachments> SearchEmailsWithPdfAttachments(
            string? folderName = null,
            DateTime? dateFrom = null,
            DateTime? dateTo = null)
        {
            var results = new List<EmailWithAttachments>();

            try
            {
                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: === ROZPOCZYNAM WYSZUKIWANIE ===");
                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: Szukam za��cznik�w: PDF, Word (.doc/.docx/.rtf), Excel (.xls/.xlsx/.xlsm)");
                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: Zakres dat: {dateFrom?.ToString("yyyy-MM-dd") ?? "(brak)"} do {dateTo?.ToString("yyyy-MM-dd") ?? "(brak)"}");

                if (_usePop3)
                {
                    // ? POP3: Pobierz wszystkie wiadomo�ci (POP3 nie ma filtrowania po dacie)
                    if (_pop3Client == null || !_pop3Client.IsConnected)
                        throw new Exception("Nie po��czono z serwerem POP3");

                    int messageCount = _pop3Client.Count;
                    // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: POP3 - {messageCount} wiadomo�ci na serwerze");

                    int checkedCount = 0;
                    int foundWithPdf = 0;
                    int noPdfAttachments = 0;
                    int dateFilteredOut = 0;

                    // Pobierz wiadomo�ci od najnowszych
                    for (int i = messageCount - 1; i >= 0; i--)
                    {
                        try
                        {
                            var message = _pop3Client.GetMessage(i);
                            var messageDate = message.Date.DateTime;

                            // Filtruj po dacie (r�cznie, bo POP3 nie obs�uguje filtrowania)
                            if (dateFrom.HasValue && messageDate < dateFrom.Value)
                            {
                                dateFilteredOut++;
                                continue;
                            }
                            if (dateTo.HasValue && messageDate > dateTo.Value)
                            {
                                dateFilteredOut++;
                                continue;
                            }

                            checkedCount++;

                            // Sprawd� za��czniki PDF
                            var pdfAttachments = GetPdfAttachments(message);

                            if (pdfAttachments.Any())
                            {
                                foundWithPdf++;
                                results.Add(new EmailWithAttachments
                                {
                                    Subject = message.Subject ?? "(brak tematu)",
                                    From = message.From.ToString(),
                                    ReceivedTime = messageDate,
                                    Body = message.TextBody ?? message.HtmlBody ?? string.Empty,
                                    FolderPath = "INBOX",
                                    PdfAttachments = pdfAttachments
                                });

                                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: ? Znaleziono e-mail z za��cznikami #{foundWithPdf}: '{message.Subject}' ({pdfAttachments.Count} plik�w) - Data: {message.Date:yyyy-MM-dd}");
                            }
                            else
                            {
                                noPdfAttachments++;
                            }

                            // Progress log co 100 wiadomo�ci
                            if (checkedCount % 100 == 0)
                            {
                                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: Post�p - sprawdzono {checkedCount}/{messageCount} wiadomo�ci");
                            }
                        }
                        catch (Exception)
                        {
                            // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: B��d przetwarzania wiadomo�ci #{i}: {ex.Message}");
                        }
                    }

                    // Podsumowanie
                    // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: ===== PODSUMOWANIE (POP3) =====");
                    // System.Diagnostics.Debug.WriteLine($"  - Wiadomo�ci na serwerze: {messageCount}");
                    // System.Diagnostics.Debug.WriteLine($"  - E-maili w zakresie dat: {checkedCount}");
                    // System.Diagnostics.Debug.WriteLine($"  - E-maili poza zakresem dat: {dateFilteredOut}");
                    // System.Diagnostics.Debug.WriteLine($"  - E-maili z za��cznikami (PDF/Word/Excel): {foundWithPdf}");
                    // System.Diagnostics.Debug.WriteLine($"  - E-maili bez za��cznik�w: {noPdfAttachments}");
                }
                else
                {
                    // ? IMAP: U�yj filtrowania serwera
                    if (_imapClient == null || !_imapClient.IsConnected)
                        throw new Exception("Nie po��czono z serwerem IMAP");

                    // Wybierz folder (domy�lnie INBOX)
                    var folder = _imapClient.Inbox;
                    if (!string.IsNullOrEmpty(folderName))
                    {
                        folder = _imapClient.GetFolder(folderName);
                    }

                    folder?.Open(FolderAccess.ReadOnly);
                    // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: Folder: {folder.Name} - {folder.Count} wiadomo�ci");

                    // Przygotuj zapytanie wyszukiwania
                    SearchQuery query = SearchQuery.All;

                    if (dateFrom.HasValue)
                    {
                        query = query.And(SearchQuery.SentSince(dateFrom.Value));
                    }

                    if (dateTo.HasValue)
                    {
                        query = query.And(SearchQuery.SentBefore(dateTo.Value.AddDays(1)));
                    }

                    // Wyszukaj wiadomo�ci
                    var uids = folder.Search(query);
                    // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: Znaleziono {uids?.Count} wiadomo�ci w zakresie dat");

                    int checkedCount = 0;
                    int foundWithPdf = 0;
                    int noPdfAttachments = 0;

                    // Pobierz wiadomo�ci
                    foreach (var uid in uids)
                    {
                        try
                        {
                            var message = folder.GetMessage(uid);
                            checkedCount++;

                            // Sprawd� za��czniki PDF
                            var pdfAttachments = GetPdfAttachments(message);

                            if (pdfAttachments.Any())
                            {
                                foundWithPdf++;
                                results.Add(new EmailWithAttachments
                                {
                                    Subject = message.Subject ?? "(brak tematu)",
                                    From = message.From.ToString(),
                                    ReceivedTime = message.Date.DateTime,
                                    Body = message.TextBody ?? message.HtmlBody ?? string.Empty,
                                    FolderPath = folder.Name,
                                    PdfAttachments = pdfAttachments
                                });

                                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: ? Znaleziono e-mail z za��cznikami #{foundWithPdf}: '{message.Subject}' ({pdfAttachments.Count} plik�w) - Data: {message.Date:yyyy-MM-dd}");
                            }
                            else
                            {
                                noPdfAttachments++;
                            }

                            // Progress log co 100 wiadomo�ci
                            if (checkedCount % 100 == 0)
                            {
                                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: Post�p - sprawdzono {checkedCount}/{uids?.Count} wiadomo�ci");
                            }
                        }
                        catch (Exception)
                        {
                            // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: B��d przetwarzania wiadomo�ci: {ex.Message}");
                        }
                    }

                    folder.Close();

                    // Podsumowanie
                    // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: ===== PODSUMOWANIE (IMAP) =====");
                    // System.Diagnostics.Debug.WriteLine($"  - E-maili w zakresie dat: {checkedCount}");
                    // System.Diagnostics.Debug.WriteLine($"  - E-maili z za��cznikami (PDF/Word/Excel): {foundWithPdf}");
                    // System.Diagnostics.Debug.WriteLine($"  - E-maili bez za��cznik�w: {noPdfAttachments}");
                }

                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: === ZAKO�CZONO WYSZUKIWANIE ===");
                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: Znaleziono {results.Count} e-maili z za��cznikami (PDF/Word/Excel)");
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: B��d wyszukiwania: {ex.Message}");
                throw;
            }

            return results;
        }

        /// <summary>
        /// Pobiera za��czniki PDF oraz dokumenty Word/Excel z wiadomo�ci
        /// </summary>
        private List<AttachmentInfo> GetPdfAttachments(MimeMessage message)
        {
            var pdfAttachments = new List<AttachmentInfo>();

            try
            {
                foreach (var attachment in message.Attachments)
                {
                    if (attachment is MimePart part && !string.IsNullOrEmpty(part.FileName))
                    {
                        string fileName = part.FileName.ToLowerInvariant();

                        // ? Sprawd� czy to PDF, Word lub Excel
                        bool isSupported = fileName.EndsWith(".pdf") ||
                                          fileName.EndsWith(".doc") ||
                                          fileName.EndsWith(".docx") ||
                                          fileName.EndsWith(".xls") ||
                                          fileName.EndsWith(".xlsx") ||
                                          fileName.EndsWith(".xlsm") ||
                                          fileName.EndsWith(".rtf");

                        if (isSupported)
                        {
                            using var memory = new MemoryStream();
                            part.Content.DecodeTo(memory);
                            var data = memory.ToArray();

                            pdfAttachments.Add(new AttachmentInfo
                            {
                                FileName = part.FileName,
                                Size = data.Length,
                                Data = data
                            });

                            // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: ? Znaleziono za��cznik: {part.FileName} ({data.Length} bytes)");
                        }
                    }
                }
            }
            catch (Exception)
            {
                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: B??d w GetPdfAttachments: {ex.Message}");
            }

            return pdfAttachments;
        }

        /// <summary>
        /// Zapisuje za��cznik do pliku
        /// </summary>
        public void SaveAttachment(AttachmentInfo attachment, string destinationPath)
        {
            try
            {
                if (attachment.Data != null && attachment.Data.Length > 0)
                {
                    var directory = Path.GetDirectoryName(destinationPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                    {
                        Directory.CreateDirectory(directory);
                    }

                    File.WriteAllBytes(destinationPath, attachment.Data);
                    // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: Zapisano za��cznik: {destinationPath}");
                }
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: B��d zapisywania za��cznika: {ex.Message}");
                throw new Exception($"Nie mo�na zapisa� za��cznika: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Przenosi e-mail do folderu archiwum (tylko IMAP)
        /// </summary>
        public void MoveEmailToArchive(EmailWithAttachments email, string archiveFolderName)
        {
            try
            {
                if (_usePop3)
                {
                    // System.Diagnostics.Debug.WriteLine("EmailPopHelper: ?? POP3 nie obs�uguje przenoszenia wiadomo�ci");
                    return; // POP3 nie obs�uguje przenoszenia e-maili
                }

                if (_imapClient == null || !_imapClient.IsConnected)
                    throw new Exception("Nie po��czono z serwerem IMAP");

                // Otw�rz folder �r�d�owy
                var sourceFolder = _imapClient.Inbox;
                if (!string.IsNullOrEmpty(email.FolderPath) && email.FolderPath != "INBOX")
                {
                    sourceFolder = _imapClient.GetFolder(email.FolderPath);
                }

                sourceFolder?.Open(FolderAccess.ReadWrite);

                // Znajd� folder archiwum (lub utw�rz je�li nie istnieje)
                var archiveFolder = GetOrCreateFolder(archiveFolderName);

                // Znajd� UID wiadomo�ci po dacie i temacie
                var query = SearchQuery.SubjectContains(email.Subject)
                    .And(SearchQuery.SentOn(email.ReceivedTime.Date));

                var uids = sourceFolder?.Search(query);

                if (uids?.Count > 0)
                {
                    // Przenie� pierwsz� znalezion� wiadomo��
                    sourceFolder?.MoveTo(uids[0], archiveFolder);
                    // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: ? Przeniesiono e-mail '{email.Subject}' do folderu '{archiveFolderName}'");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: ?? Nie znaleziono e-maila '{email.Subject}' do przeniesienia");
                }

                sourceFolder?.Close();
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: B��d przenoszenia e-maila: {ex.Message}");
                throw new Exception($"Nie mo�na przenie�� e-maila: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Usuwa e-mail (tylko IMAP)
        /// </summary>
        public void DeleteEmail(EmailWithAttachments email)
        {
            try
            {
                if (_usePop3)
                {
                    // System.Diagnostics.Debug.WriteLine("EmailPopHelper: ?? POP3 nie obs�uguje usuwania wiadomo�ci");
                    return; // POP3 nie obs�uguje usuwania e-maili
                }

                if (_imapClient == null || !_imapClient.IsConnected)
                    throw new Exception("Nie po��czono z serwerem IMAP");

                // Otw�rz folder �r�d�owy
                var sourceFolder = _imapClient.Inbox;
                if (!string.IsNullOrEmpty(email.FolderPath) && email.FolderPath != "INBOX")
                {
                    sourceFolder = _imapClient.GetFolder(email.FolderPath);
                }

                sourceFolder?.Open(FolderAccess.ReadWrite);

                // Znajd� UID wiadomo�ci po dacie i temacie
                var query = SearchQuery.SubjectContains(email.Subject)
                    .And(SearchQuery.SentOn(email.ReceivedTime.Date));

                var uids = sourceFolder?.Search(query);

                if (uids?.Count > 0)
                {
                    // Oznacz jako usuni�te
                    sourceFolder?.AddFlags(uids[0], MessageFlags.Deleted, true);

                    // Trwale usu�
                    sourceFolder?.Expunge();

                    // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: ? Usuni�to e-mail '{email.Subject}'");
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: ?? Nie znaleziono e-maila '{email.Subject}' do usuni�cia");
                }

                sourceFolder?.Close();
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: B��d usuwania e-maila: {ex.Message}");
                throw new Exception($"Nie mo�na usun�� e-maila: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Zmienia temat e-maila (dodaje prefix) - tylko IMAP
        /// </summary>
        public void RenameEmailSubject(EmailWithAttachments email, string prefix = "POBRANA__ ")
        {
            try
            {
                if (_usePop3)
                {
                    // System.Diagnostics.Debug.WriteLine("EmailPopHelper: ?? POP3 nie obs�uguje zmiany tematu wiadomo�ci");
                    return;
                }

                if (_imapClient == null || !_imapClient.IsConnected)
                    throw new Exception("Nie po��czono z serwerem IMAP");

                // Otw�rz folder �r�d�owy
                var sourceFolder = _imapClient.Inbox;
                if (!string.IsNullOrEmpty(email.FolderPath) && email.FolderPath != "INBOX")
                {
                    sourceFolder = _imapClient.GetFolder(email.FolderPath);
                }

                sourceFolder?.Open(FolderAccess.ReadWrite);

                // Znajd� UID wiadomo�ci po dacie i temacie
                var query = SearchQuery.SubjectContains(email.Subject)
                    .And(SearchQuery.SentOn(email.ReceivedTime.Date));

                var uids = sourceFolder?.Search(query);

                if (uids?.Count > 0)
                {
                    // Pobierz oryginaln� wiadomo��
                    var message = sourceFolder.GetMessage(uids[0]);

                    // Pobierz flagi
                    var items = sourceFolder.Fetch(new[] { uids[0] }, MessageSummaryItems.Flags);
                    var messageFlags = items.FirstOrDefault()?.Flags ?? MessageFlags.None;

                    // Sprawd� czy temat ju� nie zawiera prefixu
                    if (!message.Subject.StartsWith(prefix))
                    {
                        // Stw�rz now� wiadomo�� z zmienionym tematem
                        var builder = new BodyBuilder();

                        // Kopiuj tre��
                        if (message.TextBody != null)
                            builder.TextBody = message.TextBody;
                        if (message.HtmlBody != null)
                            builder.HtmlBody = message.HtmlBody;

                        // Kopiuj za��czniki
                        foreach (var attachment in message.Attachments)
                        {
                            builder.Attachments.Add(attachment);
                        }

                        // Stw�rz now� wiadomo��
                        var newMessage = new MimeMessage();
                        newMessage.From.AddRange(message.From);
                        newMessage.To.AddRange(message.To);
                        newMessage.Subject = $"{prefix}{message.Subject}";
                        newMessage.Body = builder.ToMessageBody();
                        newMessage.Date = message.Date;

                        // Kopiuj inne w�a�ciwo�ci
                        if (!string.IsNullOrEmpty(message.MessageId))
                            newMessage.MessageId = message.MessageId;
                        if (!string.IsNullOrEmpty(message.InReplyTo))
                            newMessage.InReplyTo = message.InReplyTo;
                        if (message.References.Count > 0)
                            newMessage.References.AddRange(message.References);

                        // Dodaj now� wiadomo�� z flagami
                        sourceFolder.Append(newMessage, messageFlags);

                        // Usu� star� wiadomo��
                        sourceFolder?.AddFlags(uids[0], MessageFlags.Deleted, true);
                        sourceFolder?.Expunge();

                        // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: ? Zmieniono temat e-maila: '{email.Subject}' ? '{prefix}{email.Subject}'");
                    }
                    else
                    {
                        // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: ?? E-mail ju� ma prefix: '{message.Subject}'");
                    }
                }
                else
                {
                    // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: ?? Nie znaleziono e-maila '{email.Subject}' do zmiany tematu");
                }

                sourceFolder?.Close();
            }
            catch (Exception ex)
            {
                // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: B��d zmiany tematu e-maila: {ex.Message}");
                throw new Exception($"Nie mo�na zmieni� tematu e-maila: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Znajduje lub tworzy folder IMAP
        /// </summary>
        private IMailFolder GetOrCreateFolder(string folderName)
        {
            if (_imapClient == null)
                throw new Exception("IMAP client not initialized");

            try
            {
                // Szukaj folderu
                var folder = _imapClient.GetFolder(folderName);
                if (folder == null || !folder.Exists)
                {
                    // Utw�rz folder je�li nie istnieje
                    folder = _imapClient.Inbox.Create(folderName, true);
                    // System.Diagnostics.Debug.WriteLine($"EmailPopHelper: Utworzono nowy folder: {folderName}");
                }
                return folder;
            }
            catch
            {
                // Je�li nie uda�o si� znale��, spr�buj utworzy� w Inbox
                return _imapClient.Inbox.Create(folderName, true);
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // ? Zwolnij IMAP client
                    if (_imapClient != null)
                    {
                        if (_imapClient.IsConnected)
                        {
                            _imapClient.Disconnect(true);
                        }
                        _imapClient.Dispose();
                        _imapClient = null;
                    }

                    // ? Zwolnij POP3 client
                    if (_pop3Client != null)
                    {
                        if (_pop3Client.IsConnected)
                        {
                            _pop3Client.Disconnect(true);
                        }
                        _pop3Client.Dispose();
                        _pop3Client = null;
                    }
                }

                _disposed = true;
            }
        }

        ~EmailPopHelper()
        {
            Dispose(false);
        }
    }
}
