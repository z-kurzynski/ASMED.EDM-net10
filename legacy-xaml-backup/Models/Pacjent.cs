using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ASMED.WPF.Models
{
    public class Pacjent : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public int LineNumber { get; set; }
        public int P_ID { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? PESEL { get; set; }
        public string? Company { get; set; }

        private int _liczbaKartBadan;
        public int LiczbaKartBadan
        {
            get => _liczbaKartBadan;
            set
            {
                if (_liczbaKartBadan != value)
                {
                    _liczbaKartBadan = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}

