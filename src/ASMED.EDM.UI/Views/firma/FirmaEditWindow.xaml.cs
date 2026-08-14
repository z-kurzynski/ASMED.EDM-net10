using System.Windows;
using ASMED.EDM.UI.ViewModels.Firmy;

namespace ASMED.EDM.UI.Views.Firma;

public partial class FirmaEditWindow : Window
{
    public FirmaEditWindow()
    {
        InitializeComponent();
    }

    public FirmaEditWindow(FirmaEditWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
        viewModel.RequestClose += () =>
        {
            DialogResult = viewModel.DialogResultValue;
            Close();
        };
    }
}
