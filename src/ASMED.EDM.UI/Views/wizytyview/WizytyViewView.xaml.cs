using System.ComponentModel;
using System.Windows.Input;
using System.Windows.Controls;
using System.Windows;
using ASMED.EDM.UI.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using Syncfusion.UI.Xaml.Scheduler;

namespace ASMED.EDM.UI.Views.wizytyview;

public partial class WizytyViewView : UserControl
{
    private bool _isUpdatingSelectedDate = false;

    // ✅ Cykliczne, ciche odświeżanie listy "lista_rejestracja" (bez przeładowania całej zakładki).
    // Program jest wielostanowiskowy — inne stanowiska mogą w tym samym czasie rejestrować/zmieniać wizyty,
    // dlatego lista jest odświeżana co określony czas, ale tylko gdy ten widok ma fokus.
    private readonly System.Windows.Threading.DispatcherTimer _autoRefreshTimer;
    private static readonly TimeSpan AutoRefreshInterval = TimeSpan.FromSeconds(7);

    public WizytyViewView()
    {
        InitializeComponent();

        // Nie ustawiamy DataContext w trybie projektanta — zapobiega błędom w designerze.
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            if (Application.Current is App app && app.Host != null)
            {
                var vm = app.Host.Services.GetRequiredService<VisitsViewModel>();
                vm.NavigateToZakonczBadanie += OnNavigateToZakonczBadanie;
                DataContext = vm;
            }
        }

        this.Loaded += WizytyViewView_Loaded;
        this.IsVisibleChanged += WizytyViewView_IsVisibleChanged;

        _autoRefreshTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = AutoRefreshInterval
        };
        _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;
        this.Unloaded += (_, _) => _autoRefreshTimer.Stop();
    }

    private void AutoRefreshTimer_Tick(object? sender, EventArgs e)
    {
        // Odświeżaj gdy zakładka "Wizyty" jest aktualnie wyświetlana (aktywna).
        // Uwaga: nie wymagamy dosłownego fokusu klawiatury (IsKeyboardFocusWithin) — użytkownik
        // często tylko obserwuje listę bez klikania w nią, a mimo to dane mają się aktualizować.
        if (!IsVisible)
        {
            return;
        }

        if (DataContext is VisitsViewModel vm)
        {
            vm.RefreshSilent();
        }
    }

    private void OnNavigateToZakonczBadanie()
    {
        if (Window.GetWindow(this) is MainWindow mainWindow)
            mainWindow.SelectZakonczBadanieTab();
    }

    private void WizytyViewView_Loaded(object sender, RoutedEventArgs e)
    {
        // Ustaw początkową datę w kalendarzu
        if (FindName("WizytyScheduler") is SfScheduler scheduler)
        {
            scheduler.PreviewMouseDoubleClick -= Scheduler_PreviewMouseDoubleClick;
            scheduler.PreviewMouseDoubleClick += Scheduler_PreviewMouseDoubleClick;
        }
        // Nie wymuszamy daty tutaj; domyślną datę ustawia się podczas zmiany widoczności widoku (IsVisibleChanged)
    }

    private void Scheduler_PreviewMouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (sender is not SfScheduler scheduler)
        {
            return;
        }

        if (scheduler.ViewType == SchedulerViewType.Month)
        {
            var selectedDate = scheduler.SelectedDate;
            if (selectedDate.HasValue)
            {
                scheduler.ViewType = SchedulerViewType.Day;
                scheduler.SelectedDate = selectedDate;
                e.Handled = true;
            }
        }
    }

    private void WizytyViewView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            _autoRefreshTimer.Start();
        }
        else
        {
            _autoRefreshTimer.Stop();
        }

        try
        {
            if (IsVisible && DataContext is VisitsViewModel vm)
            {
                // Legacy behavior: sprawdź checkbox domyślnej daty PRZED odświeżeniem
                if (vm.UseCustomDefaultDateCalendar)
                {
                    vm.SelectedDate = vm.CustomDefaultDateCalendar;
                    if (FindName("WizytyScheduler") is SfScheduler sch)
                    {
                        sch.DisplayDate = vm.CustomDefaultDateCalendar;
                        sch.SelectedDate = vm.CustomDefaultDateCalendar;
                    }
                }
                else
                {
                    vm.SelectedDate = DateTime.Now;
                    if (FindName("WizytyScheduler") is SfScheduler sch)
                    {
                        sch.DisplayDate = vm.SelectedDate ?? DateTime.Today;
                        sch.SelectedDate = vm.SelectedDate;
                    }
                }

                // Odśwież dane po ustawieniu daty
                try { vm.RefreshCommand.Execute(null); } catch { }
            }
        }
        catch { }
    }

    private void WizytyScheduler_SelectionChanged(object sender, object e)
    {
        System.Diagnostics.Debug.WriteLine($"🔔 WizytyScheduler_SelectionChanged EVENT fired!");
        System.Diagnostics.Debug.WriteLine($"   sender type: {sender?.GetType().Name}");

        if (_isUpdatingSelectedDate)
        {
            System.Diagnostics.Debug.WriteLine($"   ⚠️ _isUpdatingSelectedDate is TRUE - returning early");
            return;
        }

        if (sender is SfScheduler scheduler && DataContext is VisitsViewModel vm)
        {
            System.Diagnostics.Debug.WriteLine($"   scheduler.SelectedDate: {scheduler.SelectedDate}");
            System.Diagnostics.Debug.WriteLine($"   vm.SelectedDate BEFORE: {vm.SelectedDate}");

            // Pobierz wybraną datę z kalendarza i zaktualizuj ViewModel
            if (scheduler.SelectedDate != null)
            {
                _isUpdatingSelectedDate = true;
                try
                {
                    vm.SelectedDate = scheduler.SelectedDate;
                    System.Diagnostics.Debug.WriteLine($"   ✅ vm.SelectedDate AFTER: {vm.SelectedDate}");
                }
                finally
                {
                    _isUpdatingSelectedDate = false;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"   ⚠️ scheduler.SelectedDate is NULL");
            }
        }
        else
        {
            System.Diagnostics.Debug.WriteLine($"   ⚠️ sender is not SfScheduler or DataContext is not VisitsViewModel");
        }
    }
}
