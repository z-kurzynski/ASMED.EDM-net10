using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Orzeczenie.Tabs;

public partial class BadanieLaryngologiczneTabView : UserControl
{
    public BadanieLaryngologiczneTabView()
    {
        InitializeComponent();
        IsVisibleChanged += BadanieLaryngologiczneTabView_IsVisibleChanged;
    }

    private void BadanieLaryngologiczneTabView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
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
        rbLarStatus_Wykonano.IsChecked = true;
        rbVHI_Brak.IsChecked = true;
        txtVHIOpis.Text = string.Empty;
        rbKrtan_Prawidlowa.IsChecked = true;
        rbNosogardlo_Prawidlowe.IsChecked = true;
        rbSluch_Prawidlowy.IsChecked = true;
        rbBbebenkowe_Prawidlowe.IsChecked = true;
        rbRownowaga_Prawidlowa.IsChecked = true;
        rbGlos_Brak.IsChecked = true;
        rbWynik_Prawidlowy.IsChecked = true;
        txtUwagi.Text = string.Empty;
    }
}
