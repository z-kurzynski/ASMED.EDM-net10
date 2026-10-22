using ASMED.EDM.UI.ViewModels.Orzeczenie;
using ASMED.EDM.UI.ViewModels.Skierowania;
using Microsoft.Extensions.DependencyInjection;
//using Syncfusion.DocumentSDK.AI.AgentTools;
using Syncfusion.DocIO;
using Syncfusion.DocIO.DLS;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Orzeczenie;

public partial class OrzeczenieView : UserControl
{
    // Cykliczne, ciche odswiezanie listy "Wizyty" (bez przeladowania calego widoku).
    private readonly System.Windows.Threading.DispatcherTimer _autoRefreshTimer;
    private static readonly System.TimeSpan AutoRefreshInterval = System.TimeSpan.FromSeconds(7);

    public OrzeczenieView()
    {
        InitializeComponent();

        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            if (Application.Current is App app && app.Host != null)
            {
                var vm = app.Host.Services.GetRequiredService<OrzeczenieViewModel>();
                DataContext = vm;
            }
            else
            {
                DataContext = new OrzeczenieViewModel();
            }
        }

        if (DataContext is OrzeczenieViewModel vmodel)
        {
            vmodel.PropertyChanged += OrzeczenieViewModel_PropertyChanged;
        }

        _autoRefreshTimer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = AutoRefreshInterval
        };
        _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;

        this.IsVisibleChanged += OrzeczenieView_IsVisibleChanged;
        this.Unloaded += (_, _) => _autoRefreshTimer.Stop();
    }

    private void OrzeczenieViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(OrzeczenieViewModel.SelectedWizyta) && DataContext is OrzeczenieViewModel vm)
        {
            HistoriaTab.SetPatientId(vm.SelectedWizyta?.P_ID);
        }
    }

    private void AutoRefreshTimer_Tick(object? sender, System.EventArgs e)
    {
        if (!IsVisible)
        {
            return;
        }

        if (DataContext is OrzeczenieViewModel vm)
        {
            vm.RefreshSilent();
        }
    }

    private void OrzeczenieView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            // Odswiez natychmiast po uzyskaniu fokusu/widocznosci, nie czekajac na pierwszy tick timera.
            if (DataContext is OrzeczenieViewModel vm)
            {
                vm.RefreshSilent();
                vm.RefreshOrzecznik();
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
