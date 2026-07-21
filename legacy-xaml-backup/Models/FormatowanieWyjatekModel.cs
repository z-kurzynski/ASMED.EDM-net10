using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ASMED.WPF.Models
{
    /// <summary>
    /// Model reprezentujący wyjątek formatowania tekstu z tabeli FormatowanieTekstu
    /// </summary>
    public class FormatowanieWyjatekModel : INotifyPropertyChanged
    {
        private int _id;
        private string _slowo = string.Empty;
        private string _formatTyp = string.Empty;
        private string _kategoria = string.Empty;

        public int ID
        {
            get => _id;
            set
            {
                _id = value;
                OnPropertyChanged();
            }
        }

        public string Slowo
        {
            get => _slowo;
            set
            {
                _slowo = value;
                OnPropertyChanged();
            }
        }

        public string FormatTyp
        {
            get => _formatTyp;
            set
            {
                _formatTyp = value;
                OnPropertyChanged();
            }
        }

        public string Kategoria
        {
            get => _kategoria;
            set
            {
                _kategoria = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
