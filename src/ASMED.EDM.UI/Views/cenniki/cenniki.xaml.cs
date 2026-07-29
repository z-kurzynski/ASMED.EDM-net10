using System.Windows.Controls;
using ASMED.EDM.UI.ViewModels.Cenniki;
using Microsoft.Extensions.DependencyInjection;

namespace ASMED.EDM.UI.Views.cenniki
{
    public partial class CennikiView : UserControl
    {
        // Konstruktor dla XAML Designer (bez DI)
        public CennikiView()
        {
            InitializeComponent();

            if (System.ComponentModel.DesignerProperties.GetIsInDesignMode(this))
                return;

            Loaded += CennikiView_Loaded;
        }

        // Konstruktor dla DI (wstrzyknięcie z kontenera)
        public CennikiView(CennikiViewModel viewModel) : this()
        {
            DataContext = viewModel;
        }

        private void CennikiView_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            // Nie ustawiaj ponownie jeśli już mamy właściwy ViewModel
            if (DataContext is CennikiViewModel) return;

            try
            {
                if (System.Windows.Application.Current is App app && app.Host != null)
                {
                    var vm = app.Host.Services.GetRequiredService<CennikiViewModel>();
                    DataContext = vm;
                    Console.WriteLine("✅ CennikiView: DataContext ustawiony na CennikiViewModel");
                }
                else
                {
                    Console.WriteLine("⚠️ CennikiView: app.Host niedostępny – tworzę VM bez DI");
                    DataContext = new CennikiViewModel();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ CennikiView_Loaded error: {ex.Message}");
                DataContext = new CennikiViewModel();
            }
        }
    }
}


