using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Orzeczenie.Tabs;

public partial class BadaniaSanitarneTabView : UserControl
{
    public BadaniaSanitarneTabView()
    {
        InitializeComponent();
        IsVisibleChanged += BadaniaSanitarneTabView_IsVisibleChanged;
    }

    private void BadaniaSanitarneTabView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
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
        rbSanit_Pozytywny.IsChecked = true;
        tbSanitUwagi.Text = string.Empty;
    }
}
