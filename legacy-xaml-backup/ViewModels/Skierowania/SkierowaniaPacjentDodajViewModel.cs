using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ASMED.WPF.ViewModels.Skierowania
{
    public class SkierowaniaPacjentDodajViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public SkierowaniaPacjentDodajViewModel()
        {
            // Initialization code can go here
        }

        public DateTime ReferralDate { get; set; } = DateTime.Now;
        private string _testText = "SkierowaniaPacjentDodajViewModel działa";
        public string TestText
        {
            get => _testText;
            set
            {
                if (_testText != value)
                {
                    _testText = value;
                    OnPropertyChanged();
                }
            }
        }
    }
}
