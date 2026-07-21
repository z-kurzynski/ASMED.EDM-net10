using System.Windows;

namespace ASMED.WPF
{
    public partial class PatientAdd : Window
    {
        public PatientAdd(ASMED.WPF.ViewModels.PatientAddViewModel? viewModel = null)
        {
            InitializeComponent();
            if (viewModel == null)
                viewModel = new ASMED.WPF.ViewModels.PatientAddViewModel();
            DataContext = viewModel;
        }
    }
}
