    <PackageReference Include="iTextSharp" Version="5.5.13.4" />
    <PackageReference Include="Notifications.Wpf" Version="0.1.1" />
    <PackageReference Include="PdfiumViewer" Version="2.13.0" />
    <PackageReference Include="PdfiumViewer.Native.x86_64.v8-xfa" Version="2018.4.8.256" />
    <PackageReference Include="Syncfusion.SfScheduler.WPF" Version="30.2.6" />


    Z noweo cs

           private void ZapiszNowegoPacjenta()
        {
            var db = new Helpers.AccessDbHelper();
            using (var conn = db.GetConnection())
            {
                conn.Open();
                using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO P_Pacjent (
                        P_pesel, P_brak, P_płeć, P_imie, P_nazwisko, P_Ades_kod, P_Adres_ulica_numer,
                        P_Ades_miasto, P_zawód, P_Adres_kraj, P_data_urodzenia, P_obywatelstwo,
                        P_telefon, P_email, P_dwie, P_activ
                    ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)";
                    cmd.Parameters.AddWithValue("P_pesel", P_pesel ?? "");
                    cmd.Parameters.AddWithValue("P_brak", P_brak ?? "");
                    cmd.Parameters.AddWithValue("P_płeć", P_płeć ?? "");
                    cmd.Parameters.AddWithValue("P_imie", P_imie ?? "");
                    cmd.Parameters.AddWithValue("P_nazwisko", P_nazwisko ?? "");
                    cmd.Parameters.AddWithValue("P_Ades_kod", P_Ades_kod ?? "");
                    cmd.Parameters.AddWithValue("P_Adres_ulica_numer", P_Adres_ulica_numer ?? "");
                    cmd.Parameters.AddWithValue("P_Ades_miasto", P_Ades_miasto ?? "");
                    cmd.Parameters.AddWithValue("P_zawód", P_zawód ?? "");
                    //cmd.Parameters.AddWithValue("P_firma", WybranaFirma?.Id ?? 0);
                    cmd.Parameters.AddWithValue("P_Adres_kraj", P_Adres_kraj ?? "");
                    cmd.Parameters.AddWithValue("P_data_urodzenia", P_data_urodzenia ?? DateTime.Now);
                    cmd.Parameters.AddWithValue("P_obywatelstwo", P_obywatelstwo ?? "");
                    cmd.Parameters.AddWithValue("P_telefon", P_telefon ?? "");
                    cmd.Parameters.AddWithValue("P_email", P_email ?? "");
                    cmd.Parameters.AddWithValue("P_dwie", P_dwie ?? "");
                    cmd.Parameters.AddWithValue("P_activ", P_activ ?? true);
                    cmd.ExecuteNonQuery();
                }
            }
        }





1. Nadrzędny ViewModel dla zakładki (np. PacjentTabViewModel)

public class PacjentTabViewModel : BaseViewModel
{
    private readonly Stack<object> _history = new();
    private object _currentViewModel;
    public object CurrentViewModel
    {
        get => _currentViewModel;
        set { _currentViewModel = value; OnPropertyChanged(); }
    }

    public ICommand BackCommand { get; }

    public PacjentTabViewModel()
    {
        BackCommand = new RelayCommand(_ => GoBack(), _ => _history.Count > 0);
        NavigateTo(new ListaPacjentowViewModel(this));
    }

    public void NavigateTo(object vm)
    {
        if (CurrentViewModel != null)
            _history.Push(CurrentViewModel);
        CurrentViewModel = vm;
    }

    public void GoBack()
    {
        if (_history.Count > 0)
            CurrentViewModel = _history.Pop();
    }
}

2. Przekazywanie referencji do nadrzędnego ViewModelu

public class ListaPacjentowViewModel
{
    private readonly PacjentTabViewModel _tabVm;
    public ListaPacjentowViewModel(PacjentTabViewModel tabVm)
    {
        _tabVm = tabVm;
    }

    public ICommand DodajPacjentaCommand => new RelayCommand(_ => _tabVm.NavigateTo(new PacjentDodajViewModel(_tabVm)));
}

3. MainWindowViewModel

public class MainWindowViewModel : BaseViewModel
{
    public object PacjentWidok { get; }
    public object SkierowaniaWidok { get; }

    public MainWindowViewModel()
    {
        PacjentWidok = new PacjentTabViewModel();
        SkierowaniaWidok = new SkierowaniaTabViewModel();
    }
}

4. XAML zakładki

 <syncfusion:TabItemExt Header="📝 Pacjent">
    <ContentControl Content="{Binding PacjentWidok.CurrentViewModel}" />
    <Button Content="Wróć" Command="{Binding PacjentWidok.BackCommand}" />
</syncfusion:TabItemExt>

5. DataTemplates w MainWindow.xaml

<DataTemplate DataType="{x:Type vm:ListaPacjentowViewModel}">
    <views:ListaPacjentowView />
</DataTemplate>
<DataTemplate DataType="{x:Type vm:PacjentDodajViewModel}">
    <views:PacjentDodajView />
</DataTemplate>

6. Przykład użycia w widoku
1. ListaPacjentowView.xaml
1. <Button Content="Dodaj Pacjenta" Command="{Binding DodajPacjentaCommand}" />
1. PacjentDodajView.xaml
1. <Button Content="Zapisz" Command="{Binding ZapiszNowegoPacjentaCommand}" />
1. Podsumowanie
1. Utworzyliśmy nadrzędny ViewModel dla zakładki, który zarządza nawigacją i historią.
1. Przekazujemy referencję do nadrzędnego ViewModelu do podwidoków, aby mogły inicjować nawigację.
1. W MainWindowViewModel tworzymy instancje zakładek.
1. W XAML zakładki używamy ContentControl do dynamicznego ładowania widoków na podstawie CurrentViewModel.
1. Definiujemy DataTemplates dla każdego ViewModelu, aby WPF wiedział, jaki widok załadować.
1. W widokach podłączamy przyciski do odpowiednich komend w ViewModelach.
1. Teraz masz kompletny system nawigacji z historią w zakładkach aplikacji WPF, zgodny z wzorcem MVVM.
1. Dzięki temu użytkownik może przechodzić do podwidoków i wracać do poprzednich widoków bez utraty kontekstu.
1. Każdy podwidok może inicjować nawigację, a nadrzędny ViewModel zarządza stanem i historią.
1. To podejście jest skalowalne i łatwe do utrzymania, idealne dla aplikacji z wieloma zakładkami i podwidokami.
1. ożesz rozszerzać funkcjonalność, dodając więcej podwidoków i komend nawigacyjnych według potrzeb.
1. Dzięki temu masz solidną podstawę do budowy złożonych interfejsów użytkownika w WPF z użyciem MVVM.
1. Powodzenia w dalszym rozwoju aplikacji!
1. 

realizacja zapisu nowego pacjenta w PacjentDodajViewModel

    private ICommand _zapiszNowegoPacjentaCommand;
    public ICommand ZapiszNowegoPacjentaCommand => _zapiszNowegoPacjentaCommand ??= new RelayCommand(_ => ZapiszNowegoPacjenta());
    private void ZapiszNowegoPacjenta()
    {
        var db = new Helpers.AccessDbHelper();
        using (var conn = db.GetConnection())
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"INSERT INTO P_Pacjent (
                    P_pesel, P_brak, P_płeć, P_imie, P_nazwisko, P_Ades_kod, P_Adres_ulica_numer,
                    P_Ades_miasto, P_zawód, P_Adres_kraj, P_data_urodzenia, P_obywatelstwo,
                    P_telefon, P_email, P_dwie, P_activ
                ) VALUES (?,
        }

        readonly PacjentTabViewModel _tabVm;
        public PacjentDodajViewModel(PacjentTabViewModel tabVm)
        {
            _tabVm = tabVm;
        }


