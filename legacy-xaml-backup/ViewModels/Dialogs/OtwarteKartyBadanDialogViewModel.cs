using ASMED.WPF.Helpers;
using ASMED.WPF.Models;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace ASMED.WPF.ViewModels.Dialogs
{
    public class OtwarteKartyBadanDialogViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        // ? Event do zamykania dialogu
        public event EventHandler<bool>? RequestClose;

        // ? Dane pacjenta
        public string ?ImieNazwisko { get; set; }
        public string ?Pesel { get; set; }
        public string ?Firma { get; set; }

        // ? Lista otwartych kart
        public ObservableCollection<OtwartaKartaBadanDto> OtwarteKarty { get; set; }

        private OtwartaKartaBadanDto _wybranaKarta;
        public OtwartaKartaBadanDto WybranaKarta
        {
            get => _wybranaKarta;
            set
            {
                if (_wybranaKarta != value)
                {
                    _wybranaKarta = value;
                    OnPropertyChanged();
                }
            }
        }

        // ? Wynik wyboru użytkownika
        public enum DialogResult
        {
            None,
            EdytujKarte,
            NowaKarta,
            Anulowano
        }

        public DialogResult Result { get; private set; } = DialogResult.None;
        public int? WybraneB_ID { get; private set; }

        // ? Komendy
        public ICommand ?EdytujKarteCommand { get; }
        public ICommand ?NowaKartaCommand { get; }
        public ICommand ?AnulujCommand { get; }

        public OtwarteKartyBadanDialogViewModel(
            string imieNazwisko,
            string pesel,
            string firma,
            ObservableCollection<OtwartaKartaBadanDto> otwarteKarty)
        {
            ImieNazwisko = imieNazwisko;
            Pesel = pesel;
            Firma = firma;
            OtwarteKarty = otwarteKarty;

            // Domyślnie zaznacz pierwszą kartę
            if (OtwarteKarty.Count > 0)
                WybranaKarta = OtwarteKarty[0];

            EdytujKarteCommand = new RelayCommand(EdytujKarte);
            NowaKartaCommand = new RelayCommand(NowaKarta);
            AnulujCommand = new RelayCommand(Anuluj);
        }

        private void EdytujKarte(object? parameter)
        {
            if (WybranaKarta == null)
            {
                NotificationHelper.ShowWarning("Wybierz kartę z listy.");
                return;
            }

            Result = DialogResult.EdytujKarte;
            WybraneB_ID = WybranaKarta.B_ID;

            // System.Diagnostics.Debug.WriteLine($"? Dialog: Wybrano edycję karty B_ID={WybraneB_ID}");
            RequestClose?.Invoke(this, true);
        }

        private void NowaKarta(object? parameter)
        {
            Result = DialogResult.NowaKarta;
            WybraneB_ID = null;

            // System.Diagnostics.Debug.WriteLine("? Dialog: Wybrano utworzenie nowej karty");
            RequestClose?.Invoke(this, true);
        }

        private void Anuluj(object? parameter)
        {
            Result = DialogResult.Anulowano;
            WybraneB_ID = null;

            // System.Diagnostics.Debug.WriteLine("? Dialog: Anulowano");
            RequestClose?.Invoke(this, false);
        }
    }
}
