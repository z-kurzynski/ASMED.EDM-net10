using ASMED.EDM.UI.Models;
using ASMED.EDM.UI.ViewModels.Skierowania;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Skierowania
{
    public partial class SkierowaniaView : UserControl
    {
        public SkierowaniaView()
        {
            InitializeComponent();
            Loaded += SkierowaniaView_Loaded;
            IsVisibleChanged += SkierowaniaView_IsVisibleChanged;
        }

        private void SkierowaniaView_Loaded(object sender, RoutedEventArgs e)
        {
            if (DataContext is not SkierowaniaViewModel
                && !System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)
                && Application.Current is App app
                && app.Host != null)
            {
                DataContext = app.Host.Services.GetRequiredService<SkierowaniaViewModel>();
            }

            if (DataContext is SkierowaniaViewModel vm)
            {
                vm.RefreshFromDb();
                vm.NavigateToEdit = OpenSkierPacjentaCard;
            }
        }

        private void SkierowaniaView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (IsVisible && DataContext is SkierowaniaViewModel vm)
            {
                try { vm.RefreshFromDb(); } catch { }
            }
        }

        private void Rejestracja_Click(object sender, RoutedEventArgs e)
        {
            // Placeholder for registration logic
        }

        private void OpenSkierPacjentaCard(SkierowanieListItem item)
        {
            var mainWindow = Window.GetWindow(this) as MainWindow;
            if (mainWindow == null) return;

            if (Application.Current is not App app || app.Host == null)
            {
                MessageBox.Show("Brak konfiguracji DI.", "Błąd", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var cardVm = app.Host.Services.GetRequiredService<SkierPacjentaViewModel>();
            cardVm.LoadFromSkierowanie(item);

            var cardView = new SkierPacjentaView { DataContext = cardVm };

            mainWindow.SetKartyBadanContent(cardView);
            mainWindow.SelectKartyBadanTab();
        }
    }
}
