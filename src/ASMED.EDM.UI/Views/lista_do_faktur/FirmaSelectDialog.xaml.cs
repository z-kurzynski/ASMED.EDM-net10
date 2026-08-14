using ASMED.EDM.Data.Services;
using ASMED.EDM.UI.Models.ListaDoFaktur;
using ASMED.EDM.UI.Services;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Data;

namespace ASMED.EDM.UI.Views.lista_do_faktur
{
    public partial class FirmaSelectDialog : Window
    {
        private readonly FirmaSelectVm _vm;

        /// <summary>Wybrana firma — do odczytu po zamknięciu dialogu.</summary>
        public FirmaDto? SelectedFirma =>
            _vm.SelectedFirma != null
                ? new FirmaDto
                {
                    Id      = _vm.SelectedFirma.Id,
                    Nazwa   = _vm.SelectedFirma.Nazwa,
                    NIP     = _vm.SelectedFirma.NIP,
                    Activ   = true,
                    Cennik  = _vm.SelectedFirma.Cennik,
                    FkEmail = _vm.SelectedFirma.FkEmail
                }
                : null;

        public FirmaSelectDialog()
        {
            InitializeComponent();
            var dbFactory = ((App)Application.Current).Host.Services
                                .GetService<DbConnectionFactory>();
            _vm = new FirmaSelectVm(dbFactory);
            DataContext = _vm;
            _vm.Load();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
            => _vm.SearchText = string.Empty;

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void RowChoose_Click(object sender, RoutedEventArgs e)
        {
            // Wiersz z przyciskiem "Wybierz" — DataContext wiersza to FirmaSelectItemDto
            if (sender is System.Windows.Controls.Button btn &&
                btn.DataContext is FirmaSelectItemDto item)
            {
                _vm.SelectedFirma = item;
            }
            DialogResult = true;
            Close();
        }
    }

    // ===== Prosty ViewModel dialogu =====
    public class FirmaSelectItemDto : INotifyPropertyChanged
    {
        public int     Id      { get; init; }
        public string? Nazwa   { get; init; }
        public string? NIP     { get; init; }
        public string? Cennik  { get; init; }
        public string? FkEmail { get; init; }

        private bool _isSelected;
        public bool IsSelected
        {
            get => _isSelected;
            set { _isSelected = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }

    public class FirmaSelectVm : INotifyPropertyChanged
    {
        private readonly ListaFaktAddService _svc;
        private readonly ObservableCollection<FirmaSelectItemDto> _all = new();

        public ICollectionView FirmsView { get; }

        private FirmaSelectItemDto? _selectedFirma;
        public FirmaSelectItemDto? SelectedFirma
        {
            get => _selectedFirma;
            set { _selectedFirma = value; OnPropertyChanged(); }
        }

        private string _searchText = string.Empty;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                FirmsView.Refresh();
            }
        }

        public FirmaSelectVm(DbConnectionFactory? dbFactory)
        {
            _svc = new ListaFaktAddService(dbFactory);
            FirmsView = CollectionViewSource.GetDefaultView(_all);
            FirmsView.Filter = item =>
            {
                if (string.IsNullOrWhiteSpace(_searchText)) return true;
                var f = item as FirmaSelectItemDto;
                return f?.Nazwa?.Contains(_searchText, StringComparison.OrdinalIgnoreCase) == true;
            };
        }

        public void Load()
        {
            try
            {
                var firmy = _svc.GetFirmy();
                _all.Clear();
                foreach (var f in firmy)
                    _all.Add(new FirmaSelectItemDto
                    {
                        Id      = f.Id,
                        Nazwa   = f.Nazwa,
                        NIP     = f.NIP,
                        Cennik  = f.Cennik,
                        FkEmail = f.FkEmail
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FirmaSelectVm] Load ERROR: {ex.Message}");
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? n = null)
            => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(n));
    }
}
