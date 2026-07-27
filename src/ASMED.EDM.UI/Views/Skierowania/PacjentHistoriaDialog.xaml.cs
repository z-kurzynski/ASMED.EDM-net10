using System.Windows;

namespace ASMED.EDM.UI.Views.Skierowania
{
    public partial class PacjentHistoriaDialog : Window
    {
        public PacjentHistoriaDialog()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
