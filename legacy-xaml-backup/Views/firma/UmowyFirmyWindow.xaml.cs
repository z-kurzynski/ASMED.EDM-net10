using System.Windows;
using ASMED.WPF.Models;
using ASMED.WPF.ViewModels;

namespace ASMED.WPF.Views.firma
{
    public partial class UmowyFirmyWindow : Window
    {
        public UmowyFirmyWindow(Firma firma)
        {
            InitializeComponent();
            DataContext = new UmowyFirmyViewModel(firma);
        }
    }
}
