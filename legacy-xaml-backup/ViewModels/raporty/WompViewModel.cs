using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ASMED.WPF.ViewModels
{
    public class WompViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public WompViewModel()
        {
        }
    }
}
