using System.Windows;
using ASMED.WPF.Models;
using ASMED.WPF.ViewModels;

namespace ASMED.WPF.Views
{
    public partial class FirmaEditWindow : Window
    {
        public FirmaEditWindow()
        {
            InitializeComponent();

            var viewModel = new FirmaEditViewModel();
            viewModel.RequestClose += () => this.Close();

            DataContext = viewModel;
        }

        public FirmaEditWindow(Firma firma) : this()
        {
            var viewModel = new FirmaEditViewModel(firma);
            viewModel.RequestClose += () => this.Close();

            DataContext = viewModel;
        }
    }
}
