using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ASMED.WPF.Models
{
    public class Umowa : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = "")
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private int _id;
        public int Id
        {
            get => _id;
            set { _id = value; OnPropertyChanged(); }
        }

        private int _firmaId;
        public int FirmaId
        {
            get => _firmaId;
            set { _firmaId = value; OnPropertyChanged(); }
        }

        private string _firmaNazwa = "";
        public string FirmaNazwa
        {
            get => _firmaNazwa;
            set { _firmaNazwa = value; OnPropertyChanged(); }
        }

        // ✅ NOWE: Numer umowy
        private string _nrUmowy = "";
        public string NrUmowy
        {
            get => _nrUmowy;
            set { _nrUmowy = value; OnPropertyChanged(); }
        }

        private DateTime _dataUmowy;
        public DateTime DataUmowy
        {
            get => _dataUmowy;
            set 
            { 
                _dataUmowy = value; 
                OnPropertyChanged();
                WyliczDataKoncowa();
            }
        }

        private int _iloscMiesiecy;
        public int IloscMiesiecy
        {
            get => _iloscMiesiecy;
            set 
            { 
                _iloscMiesiecy = value; 
                OnPropertyChanged();
                WyliczDataKoncowa();
            }
        }

        private DateTime? _dataKoncowa;
        public DateTime? DataKoncowa
        {
            get => _dataKoncowa;
            set { _dataKoncowa = value; OnPropertyChanged(); }
        }

        private bool _czyTerminowa = true;
        public bool CzyTerminowa
        {
            get => _czyTerminowa;
            set 
            { 
                _czyTerminowa = value; 
                OnPropertyChanged();
                WyliczDataKoncowa();
            }
        }

        private string _status = "Aktywna";
        public string Status
        {
            get => _status;
            set { _status = value; OnPropertyChanged(); }
        }

        private decimal _budzet;
        public decimal Budzet
        {
            get => _budzet;
            set { _budzet = value; OnPropertyChanged(); WyliczPozostalaKwota(); }
        }

        private decimal _wartoscWykonanychBadan;
        public decimal WartoscWykonanychBadan
        {
            get => _wartoscWykonanychBadan;
            set { _wartoscWykonanychBadan = value; OnPropertyChanged(); WyliczPozostalaKwota(); }
        }

        private decimal _pozostalaKwota;
        public decimal PozostalaKwota
        {
            get => _pozostalaKwota;
            set { _pozostalaKwota = value; OnPropertyChanged(); }
        }

        private void WyliczDataKoncowa()
        {
            if (CzyTerminowa && IloscMiesiecy > 0)
            {
                DataKoncowa = DataUmowy.AddMonths(IloscMiesiecy);
            }
            else
            {
                DataKoncowa = null;
            }
        }

        private void WyliczPozostalaKwota()
        {
            PozostalaKwota = Budzet - WartoscWykonanychBadan;
        }
    }
}
