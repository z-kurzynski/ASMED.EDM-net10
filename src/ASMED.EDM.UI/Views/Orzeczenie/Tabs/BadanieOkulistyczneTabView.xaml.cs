using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Orzeczenie.Tabs;

public partial class BadanieOkulistyczneTabView : UserControl
{
    public BadanieOkulistyczneTabView()
    {
        InitializeComponent();
        IsVisibleChanged += BadanieOkulistyczneTabView_IsVisibleChanged;
    }

    private void BadanieOkulistyczneTabView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
    {
        if (IsVisible)
        {
            ResetView();
        }
    }

    private void WyczyscButton_Click(object sender, RoutedEventArgs e)
    {
        ResetView();
    }

    private void AnulujButton_Click(object sender, RoutedEventArgs e)
    {
        ResetView();
    }

    private void ResetView()
    {
        rbOkuStatus_Wykonano.IsChecked = true;
        rbOkuBarwy_Prawidlowe.IsChecked = true;
        rbOkuPoleWidz_Prawidlowe.IsChecked = true;
        rbOkuObuoczne_Prawidlowe.IsChecked = true;
        rbOkuWynik_Prawidlowy.IsChecked = true;
        rbMonitor_Brak.IsChecked = true;
        rbOkulary_Bez.IsChecked = true;
        tbOkuUwagi.Text = string.Empty;
    }
}
