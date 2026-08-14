using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Orzeczenie.Tabs;

public partial class UrlopTabView : UserControl
{
    public UrlopTabView()
    {
        InitializeComponent();
        IsVisibleChanged += UrlopTabView_IsVisibleChanged;
    }

    private void UrlopTabView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
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
        tbUrlopUzasadnienie.Text = string.Empty;
    }
}
