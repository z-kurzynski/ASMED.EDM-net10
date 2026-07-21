using ASMED.WPF.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ASMED.WPF.Views
{
    public partial class PacjentDodajView : UserControl
    {
        public PacjentDodajView()
        {
            InitializeComponent();
            // DataContext = new PacjentDodajViewModel();
            this.PreviewKeyDown += SkierowaniaView_PreviewKeyDown;
        }


        private void SkierowaniaView_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Przechodzi do nastêpnego kontrolki jak Tab
                e.Handled = true;
                var request = new TraversalRequest(FocusNavigationDirection.Next);
                var focused = Keyboard.FocusedElement as UIElement;
                focused?.MoveFocus(request);
            }
        }
    }
}

