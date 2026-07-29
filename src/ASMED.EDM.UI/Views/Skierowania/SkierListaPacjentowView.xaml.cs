using ASMED.EDM.UI.ViewModels.Skierowania;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;
using System.Windows.Controls;

namespace ASMED.EDM.UI.Views.Skierowania
{
    public partial class SkierListaPacjentowView : UserControl
    {
        public SkierListaPacjentowView()
        {
            InitializeComponent();
            Loaded += SkierListaPacjentowView_Loaded;
        }

        // Konstruktor dla DI (gdy tworzony programowo z gotowym VM)
        public SkierListaPacjentowView(SkierListaPacjentowViewModel viewModel)
        {
            DataContext = viewModel;
            InitializeComponent();
            Loaded += SkierListaPacjentowView_Loaded;
        }

        private void SkierListaPacjentowView_Loaded(object sender, RoutedEventArgs e)
        {
            // DataContext dziedziczy MainWindowViewModel z rodzica – przypisz właściwy VM.
            // Sprawdzamy typ żeby nie nadpisywać przy każdym Loaded.
            if (DataContext is not SkierListaPacjentowViewModel
                && !System.ComponentModel.DesignerProperties.GetIsInDesignMode(this)
                && Application.Current is App app
                && app.Host != null)
            {
                DataContext = app.Host.Services.GetRequiredService<SkierListaPacjentowViewModel>();
            }

            // Załaduj dane po przypisaniu DataContext
            if (DataContext is SkierListaPacjentowViewModel vm)
                vm.RefreshList();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is SkierListaPacjentowViewModel vm)
                vm.SearchText = string.Empty;
        }

        private void RefreshList_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (DataContext is SkierListaPacjentowViewModel vm)
                {
                    vm.SearchText = string.Empty;

                    if (FindName("txtSearchPacjent") is TextBox searchBox)
                        searchBox.Text = string.Empty;

                    vm.RefreshList();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Błąd odświeżania listy:\n\n{ex.Message}",
                    "Błąd", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void WrocLista_Click(object sender, RoutedEventArgs e)
        {
            // TODO: nawigacja wstecz
        }

        private void Rejestracja_Click(object sender, RoutedEventArgs e)
        {
            // TODO: przełączenie na zakładkę Rejestracja
        }
    }
}
