using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace ASMED.WPF.Helpers
{
    public class ArchiveInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public long SizeInBytes { get; set; }

        public string DisplayName => $"{FileName} ({CreatedDate:yyyy-MM-dd HH:mm})";
        public string SizeFormatted => FormatFileSize(SizeInBytes);

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len = len / 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }
    }

    public static class ArchiveManager
    {
        private const string LOG_FILE = @"A:\dbconfig_log.txt";

        private static void Log(string message)
        {
            try
            {
                File.AppendAllText(LOG_FILE, $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - [ArchiveManager] {message}\n");
            }
            catch
            {
                // Ignoruj błędy logowania
            }
        }

        public static void EnsureArchiveDirectoryExists()
        {
            try
            {
                string archivePath = DatabaseConfiguration.ArchivePath;
                if (!Directory.Exists(archivePath))
                {
                    Directory.CreateDirectory(archivePath);
                    Log($"Utworzono katalog archiwum: {archivePath}");
                }
            }
            catch (Exception ex)
            {
                Log($"Błąd tworzenia katalogu archiwum: {ex.Message}");
                throw;
            }
        }

        public static string CreateBackup()
        {
            try
            {
                EnsureArchiveDirectoryExists();

                string sourceDb = DatabaseConfiguration.UzywanaDbPath;
                if (!File.Exists(sourceDb))
                {
                    throw new FileNotFoundException($"Baza danych nie istnieje: {sourceDb}");
                }

                string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                string dbName = Path.GetFileNameWithoutExtension(sourceDb);
                string extension = Path.GetExtension(sourceDb);
                string backupFileName = $"{dbName}_backup_{timestamp}{extension}";
                string backupPath = Path.Combine(DatabaseConfiguration.ArchivePath, backupFileName);

                Log($"Rozpoczynam kopię zapasową: {sourceDb} -> {backupPath}");

                File.Copy(sourceDb, backupPath, false);

                Log($"Kopia zapasowa utworzona pomyślnie: {backupPath}");
                return backupPath;
            }
            catch (Exception ex)
            {
                Log($"Błąd tworzenia kopii zapasowej: {ex.Message}");
                throw;
            }
        }

        public static List<ArchiveInfo> GetArchivesList(int maxCount = 5)
        {
            try
            {
                string archivePath = DatabaseConfiguration.ArchivePath;

                if (!Directory.Exists(archivePath))
                {
                    Log($"Katalog archiwum nie istnieje: {archivePath}");
                    return new List<ArchiveInfo>();
                }

                var files = Directory.GetFiles(archivePath, "*.accdb")
                    .Select(f => new FileInfo(f))
                    .OrderByDescending(f => f.CreationTime)
                    .Take(maxCount)
                    .Select(f => new ArchiveInfo
                    {
                        FileName = f.Name,
                        FullPath = f.FullName,
                        CreatedDate = f.CreationTime,
                        SizeInBytes = f.Length
                    })
                    .ToList();

                Log($"Pobrano listę archiwów: {files.Count} plików");
                return files;
            }
            catch (Exception ex)
            {
                Log($"Błąd pobierania listy archiwów: {ex.Message}");
                return new List<ArchiveInfo>();
            }
        }

        public static void RestoreBackup(string backupPath)
        {
            try
            {
                if (!File.Exists(backupPath))
                {
                    throw new FileNotFoundException($"Plik kopii zapasowej nie istnieje: {backupPath}");
                }

                string currentDb = DatabaseConfiguration.UzywanaDbPath;

                // Najpierw utwórz kopię zapasową bieżącej bazy
                Log("Tworzę kopię zapasową bieżącej bazy przed przywróceniem...");
                string safetyBackup = CreateBackup();
                Log($"Kopia bezpieczeństwa utworzona: {safetyBackup}");

                // Teraz przywróć wybraną kopię
                Log($"Przywracam bazę danych z: {backupPath} -> {currentDb}");
                File.Copy(backupPath, currentDb, true);

                Log("Baza danych przywrócona pomyślnie");
            }
            catch (Exception ex)
            {
                Log($"Błąd przywracania kopii zapasowej: {ex.Message}");
                throw;
            }
        }
    }
}
