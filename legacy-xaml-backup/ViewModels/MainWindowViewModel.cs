using System.Windows;
using System.Windows.Input;
using System.ComponentModel;
using ASMED.WPF.ViewModels.Skierowania;
using ASMED.WPF.Helpers;
using ASMED.WPF.Views;

namespace ASMED.WPF.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private Timer _timer;

        public ICommand ?CloseAppCommand { get; }

        // Constructor
        public MainWindowViewModel()
        {
            CloseAppCommand = new RelayCommand(CloseApp);
            SkierowaniaWidok = new SkierowaniaViewModel();
            PacjentWidok = new ListaPacjentowViewModel();
            NowaKartaBadanWidok = new SkierListaPacjentowViewModel(); // ✅ DODANE

            UpdateDatabaseInfo();

            DatabaseConfiguration.DatabaseChanged += OnDatabaseChanged;
        }

        private void OnDatabaseChanged(object? sender, System.EventArgs e)
        {
            UpdateDatabaseInfo();
        }

        private void CloseApp(object? obj)
        {
            Application.Current.Shutdown();
        }

        private object _skierowaniaWidok;
        public object SkierowaniaWidok
        {
            get => _skierowaniaWidok;
            set
            {
                if (_skierowaniaWidok != value)
                {
                    _skierowaniaWidok = value;
                    OnPropertyChanged(nameof(SkierowaniaWidok));
                }
            }
        }

        private object _pacjentWidok;
        public object PacjentWidok
        {
            get => _pacjentWidok;
            set
            {
                if (_pacjentWidok != value)
                {
                    _pacjentWidok = value;
                    OnPropertyChanged(nameof(PacjentWidok));
                }
            }
        }

        private string?_databaseInfo;
        public string?DatabaseInfo
        {
            get => _databaseInfo;
            set
            {
                if (_databaseInfo != value)
                {
                    _databaseInfo = value;
                    OnPropertyChanged(nameof(DatabaseInfo));
                }
            }
        }

        public BadaniaEditNewView CurrentView { get; internal set; }

        private void UpdateDatabaseInfo()
        {
            string dbType = DatabaseConfiguration.AktywnaDbTyp;
            string dbPath = DatabaseConfiguration.UzywanaDbPath;
            string dbName = System.IO.Path.GetFileName(dbPath);

            DatabaseInfo = $"⚙️ Baza: {dbType} ⚙️  ";
            // DatabaseInfo = $"⚙️ Baza: {dbType} ({dbName}) ⚙️  ";
        }

        private object _nowaKartaBadanWidok;
        public object NowaKartaBadanWidok
        {
            get => _nowaKartaBadanWidok;
            set
            {
                if (_nowaKartaBadanWidok != value)
                {
                    _nowaKartaBadanWidok = value;
                    OnPropertyChanged(nameof(NowaKartaBadanWidok));
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
