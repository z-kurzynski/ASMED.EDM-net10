using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ASMED.WPF.ViewModels
{
    public class RaportyViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private string _testText = "TEST: ViewModel Raporty działa";
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
