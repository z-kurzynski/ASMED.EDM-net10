using System.ComponentModel;
using System.Runtime.CompilerServices;
namespace ASMED.WPF.Models
{
    public class Firma : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private int _id;
        public int id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        private string ?_cennik;
        public string? Cennik
        {
            get => _cennik;
            set { _cennik = value; OnPropertyChanged(); }
        }

        private string ?_nazwa;
        public string? Nazwa
        {
            get => _nazwa;
            set { _nazwa = value; OnPropertyChanged(); }
        }

        private string ?_miejscowosc;
        public string? Miejscowosc
        {
            get => _miejscowosc;
            set { _miejscowosc = value; OnPropertyChanged(); }
        }

        private string ?_ulica;
        public string? Ulica
        {
            get => _ulica;
            set { _ulica = value; OnPropertyChanged(); }
        }

        private string ?_nip;
        public string? NIP
        {
            get => _nip;
            set { _nip = value; OnPropertyChanged(); }
        }

        private string ?_osoba_kontaktowa;
        public string? Osoba_kontaktowa
        {
            get => _osoba_kontaktowa;
            set { _osoba_kontaktowa = value; OnPropertyChanged(); }
        }

        private string ?_telefon;
        public string? Telefon
        {
            get => _telefon;
            set { _telefon = value; OnPropertyChanged(); }
        }

        private string ?_email;
        public string? Email
        {
            get => _email;
            set { _email = value; OnPropertyChanged(); }
        }

        private string ?_fkemail;
        public string? FKemail
        {
            get => _fkemail;
            set { _fkemail = value; OnPropertyChanged(); }
        }

        // ✅ Nowe pola związane z umowami
        private DateTime? _umowaDo;
        public DateTime? UmowaDo
        {
            get => _umowaDo;
            set { _umowaDo = value; OnPropertyChanged(); }
        }

        private bool _czasNieokreslon;
        public bool CzasNieokreslon
        {
            get => _czasNieokreslon;
            set { _czasNieokreslon = value; OnPropertyChanged(); }
        }
    }
}
