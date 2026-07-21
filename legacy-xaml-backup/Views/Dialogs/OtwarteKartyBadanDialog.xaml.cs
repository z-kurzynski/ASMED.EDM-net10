using ASMED.WPF.ViewModels.Dialogs;
using System.Windows;

namespace ASMED.WPF.Views.Dialogs
{
    public partial class OtwarteKartyBadanDialog : Window
    {
        public OtwarteKartyBadanDialog(OtwarteKartyBadanDialogViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;

            // Subskrybuj zdarzenie zamkniêcia dialogu
            viewModel.RequestClose += (s, e) => this.DialogResult = e;
        }
    }
}
