using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.faktura
{
    public partial class FakturaView : UserControl
    {
        public FakturaView()
        {
            InitializeComponent();
        }

        private void RefreshList_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for refresh logic
        }

        private void NewInvoice_Click(object sender, RoutedEventArgs e)
        {
            if (NewInvoice != null)
            {
                NewInvoice.Visibility = NewInvoice.Visibility == Visibility.Visible 
                    ? Visibility.Collapsed 
                    : Visibility.Visible;
            }
        }

        private void BtnSelectFirma_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for firma selection dialog
        }

        private void AddInvoice_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for add invoice logic
        }
    }
}
