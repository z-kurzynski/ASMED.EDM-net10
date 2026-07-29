using System.Windows;
using System.Windows.Controls;
using ASMED.EDM.UI.ViewModels.ustawienia;

namespace ASMED.EDM.UI.Views.Settings;

public partial class FacilityDataView : UserControl
{
    public FacilityDataView()
    {
        InitializeComponent();

        // Ustaw DataContext natychmiast przez Dispatcher — po zainicjowaniu visual tree
        // ale przed renderowaniem (Loaded może nie odpałać w zagnieżdżonych tabach Syncfusion)
        Dispatcher.InvokeAsync(ApplyViewModel,
            System.Windows.Threading.DispatcherPriority.DataBind);
    }

    private void ApplyViewModel()
    {
        if (DataContext is FacilityDataViewModel) return;
        var vm = ((App)Application.Current).GetService<FacilityDataViewModel>();
        DataContext = vm;
        // PasswordBox musi być ustawiony po przypisaniu DataContext
        if (TlsPwdBox    is not null)  TlsPwdBox.Password    = vm.TlsCertificatePassword;
        if (WssPwdBox    is not null)  WssPwdBox.Password    = vm.WssCertificatePassword;
    }

    private void TlsPwdBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is FacilityDataViewModel vm)
            vm.TlsCertificatePassword = ((PasswordBox)sender).Password;
    }

    private void WssPwdBox_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (DataContext is FacilityDataViewModel vm)
            vm.WssCertificatePassword = ((PasswordBox)sender).Password;
    }
}

