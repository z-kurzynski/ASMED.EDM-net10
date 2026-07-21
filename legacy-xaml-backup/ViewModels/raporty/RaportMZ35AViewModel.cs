using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ASMED.WPF.ViewModels
{
    public class RaportMZ35AViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public RaportMZ35AViewModel()
        {
        }
    }
}
