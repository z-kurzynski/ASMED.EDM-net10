using System;
using System.IO;

namespace ASMED.WPF.Models
{
    public class SkierowanieFileInfo
    {
        public string FileName { get; set; } = string.Empty;
        public string FullPath { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public long SizeInBytes { get; set; }

        // ? NOWE: Rozszerzenie pliku (dla bindingu w XAML)
        public string FileExtension => Path.GetExtension(FullPath).ToLowerInvariant();

        public string DisplayName => FileName;
        public string DateDisplay => ModifiedDate.ToString("yyyy-MM-dd HH:mm");
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

        public static SkierowanieFileInfo FromFileInfo(FileInfo fileInfo)
        {
            return new SkierowanieFileInfo
            {
                FileName = fileInfo.Name,
                FullPath = fileInfo.FullName,
                CreatedDate = fileInfo.CreationTime,
                ModifiedDate = fileInfo.LastWriteTime,
                SizeInBytes = fileInfo.Length
            };
        }
    }
}
