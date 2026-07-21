using ASMED.WPF.ViewModels;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace ASMED.WPF.Views
{
    public partial class DuplikatyScalDialog : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        private readonly DuplikatGrupa _grupa;

        public DuplikatyScalDialog(DuplikatGrupa grupa)
        {
            InitializeComponent();
            _grupa = grupa;
            DataContext = this;
        }

        // ── Właściwości wiążące ──

        public string NaglowekText => $"Grupa duplikatów: {_grupa.KluczGrupy}  ({_grupa.Liczba} rekordów, tabela: {_grupa.Tabela})";

        public System.Collections.ObjectModel.ObservableCollection<DuplikatRekord> Rekordy => _grupa.Rekordy;

        private DuplikatRekord? _wybranyRekord;
        public DuplikatRekord? WybranyRekord
        {
            get => _wybranyRekord;
            set
            {
                if (_wybranyRekord == value) return;
                _wybranyRekord = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CzyWybrano));
                OnPropertyChanged(nameof(WybranyIdText));
            }
        }

        public bool CzyWybrano => WybranyRekord != null;
        public string WybranyIdText => WybranyRekord != null ? WybranyRekord.Id.ToString() : "—";

        /// <summary>ID wybranego rekordu głównego (null = anulowano).</summary>
        public int? WybranyGlownyId { get; private set; }

        // ── Obsługa przycisków ──

        private void Scal_Click(object sender, RoutedEventArgs e)
        {
            if (WybranyRekord == null)
            {
                MessageBox.Show("Wybierz rekord główny.", "Walidacja", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var duplikaty = _grupa.Rekordy.Count - 1;
            var result = MessageBox.Show(
                $"Czy na pewno scalić rekordy?\n\n" +
                $"Rekord główny (ID={WybranyRekord.Id}) zostanie zachowany.\n" +
                $"{duplikaty} duplikat(ów) zostanie usuniętych, a ich powiązania przepięte.\n\n" +
                "UWAGA: Ta operacja nie może być cofnięta!",
                "Potwierdzenie scalania",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);

            if (result != MessageBoxResult.Yes) return;

            WybranyGlownyId = WybranyRekord.Id;
            DialogResult = true;
            Close();
        }

        private void Anuluj_Click(object sender, RoutedEventArgs e)
        {
            WybranyGlownyId = null;
            DialogResult = false;
            Close();
        }
    }
}
