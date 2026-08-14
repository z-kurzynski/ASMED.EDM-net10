using ASMED.EDM.UI.ViewModels.Orzeczenie;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Badanie;

public partial class BadanieView : UserControl
{
    // ✅ Cykliczne, ciche odświeżanie listy "Wizyty" (bez przeładowania całego widoku).
    // Program jest wielostanowiskowy — inne stanowiska mogą w tym samym czasie rejestrować/zmieniać dane,
    // dlatego lista jest odświeżana co określony czas, ale tylko gdy ten widok ma fokus/jest widoczny.
    private readonly System.Windows.Threading.DispatcherTimer _autoRefreshTimer;
    private static readonly System.TimeSpan AutoRefreshInterval = System.TimeSpan.FromSeconds(7);

    public BadanieView()
    {
        InitializeComponent();

        // Nie ustawiamy DataContext w trybie projektanta - zapobiega błędom w designerze.
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            if (Application.Current is App app && app.Host != null)
            {
                var vm = app.Host.Services.GetRequiredService<ViewModels.BadanieView.BadanieViewModel>();
                DataContext = vm;
            }
        }

        if (DataContext is ViewModels.BadanieView.BadanieViewModel vmodel)
        {
            vmodel.PropertyChanged += BadanieViewModel_PropertyChanged;
        }

        _autoRefreshTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = AutoRefreshInterval
        };
        _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;

        this.IsVisibleChanged += BadanieView_IsVisibleChanged;
        this.Unloaded += (_, _) => _autoRefreshTimer.Stop();
    }

    private void BadanieViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(ViewModels.BadanieView.BadanieViewModel.SelectedWizyta) && DataContext is ViewModels.BadanieView.BadanieViewModel vm)
        {
            HistoriaTab.SetPatientId(vm.SelectedWizyta?.P_ID);
        }
    }

    private void Data_Skierowania_SelectedDateChanged(object? sender, System.EventArgs e)
    {
        if (DataContext is ViewModels.BadanieView.BadanieViewModel vm)
        {
            vm.SaveDataSkierowaniaToDb();
        }
    }

    private void AutoRefreshTimer_Tick(object? sender, System.EventArgs e)
    {
        // Odświeżaj gdy zakładka "Badanie" jest aktualnie wyświetlana (aktywna).
        if (!IsVisible)
        {
            return;
        }

        if (DataContext is ViewModels.BadanieView.BadanieViewModel vm)
        {
            vm.RefreshSilent();
        }
    }

    private void BadanieView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            // Odswiez natychmiast po uzyskaniu fokusu/widocznosci, nie czekajac na pierwszy tick timera.
            if (DataContext is ViewModels.BadanieView.BadanieViewModel vm)
            {
                vm.RefreshSilent();
            }

            _autoRefreshTimer.Start();
        }
        else
        {
            _autoRefreshTimer.Stop();
        }
    }
    private void RefreshList_Click(object sender, RoutedEventArgs e)
    {

    }
}
