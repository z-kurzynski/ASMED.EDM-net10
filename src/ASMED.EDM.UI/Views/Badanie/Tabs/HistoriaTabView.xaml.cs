using ASMED.EDM.UI.ViewModels.Orzeczenie;
using Microsoft.Extensions.DependencyInjection;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Badanie.Tabs;

public partial class HistoriaTabView : UserControl
{
    public HistoriaTabView()
    {
        InitializeComponent();

        // Nie ustawiamy DataContext w trybie projektanta - zapobiega bledom w designerze.
        if (!DesignerProperties.GetIsInDesignMode(this))
        {
            if (Application.Current is App app && app.Host != null)
            {
                DataContext = app.Host.Services.GetRequiredService<HistoriaViewModel>();
            }
            else
            {
                DataContext = new HistoriaViewModel();
            }
        }
    }

    /// <summary>
    /// Ustawia P_ID pacjenta, dla ktorego ma byc wyswietlona historia badan.
    /// Wywolywane przez rodzica (BadanieView) po zmianie zaznaczenia na liscie pacjentow.
    /// </summary>
    public void SetPatientId(int? pId)
    {
        if (DataContext is HistoriaViewModel vm)
        {
            vm.PID = pId;
        }
    }
}
