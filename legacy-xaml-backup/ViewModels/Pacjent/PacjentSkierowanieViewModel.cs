using System;
using System.Windows.Input;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using ASMED.WPF.ViewModels.Skierowania;

namespace ASMED.WPF.ViewModels
{
    public class PacjentSkierowanieViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private object _prawyWidokVM;
        public object PrawyWidokVM
        {
            get => _prawyWidokVM;
            set { _prawyWidokVM = value; OnPropertyChanged(); }
        }

        public ICommand ?NoweSkierowanieCommand { get; }

        // --- MOCK DANE PACJENTA I FIRMY ---
        public string ?PatientFirstName { get; set; } = "Jan";
        public string ?PatientLastName { get; set; } = "Kowalski";
        public string ?PatientPesel { get; set; } = "80010112345";
        public string ?PatientGender { get; set; } = "M";
        public DateTime PatientBirthDate { get; set; } = new DateTime(1980, 1, 1);
        public string ?PatientJobTitle { get; set; } = "Kierowca";
        public string ?PatientPostalCode { get; set; } = "00-001";
        public string ?PatientCity { get; set; } = "Warszawa";
        public string ?PatientStreet { get; set; } = "ul. Przykładowa 1";
        public int PatientId { get; set; } = 2663;
        public string ?Uwagi { get; set; } = "Brak uwag";
        public int CompanyId { get; set; } = 101;
        public string ?CompanyName { get; set; } = "Firma Testowa";
        public string ?CompanyPostalCode { get; set; } = "00-002";
        public string ?CompanyCity { get; set; } = "Warszawa";
        public string ?CompanyStreet { get; set; } = "ul. Firmowa 2";

        public PacjentSkierowanieViewModel()
        {
            NoweSkierowanieCommand = new RelayCommand(_ => PrawyWidokVM = new SkierowaniaPacjentDodajViewModel());
            // Domyślnie lista skierowań
            PrawyWidokVM = new SkierowaniaPacjentDodajViewModel();
        }
    }
}
