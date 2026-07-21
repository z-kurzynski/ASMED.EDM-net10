using System.Windows;
using System.Windows.Controls;
using ASMED.WPF.Helpers;
using ASMED.WPF.ViewModels;

namespace ASMED.WPF.Views
{
    public partial class KonfiguracjaView : UserControl
    {
        public KonfiguracjaView()
        {
            InitializeComponent();
            DataContext = new KonfiguracjaViewModel();
        }
    }
}
