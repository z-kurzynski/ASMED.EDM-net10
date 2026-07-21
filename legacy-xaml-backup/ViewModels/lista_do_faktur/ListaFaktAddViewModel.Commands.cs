using ASMED.WPF.Helpers;
using System.Windows.Input;

namespace ASMED.WPF.ViewModels.ListaDoFaktur
{
    public partial class ListaFaktAddViewModel
    {
        private RelayCommand<object>? _saveListCommand;
        public ICommand SaveListCommand => _saveListCommand ??= new RelayCommand<object>(
            async _ => await SaveListOfInvoiceAsync(),
            _ => CanExecuteSaveList());

        private bool CanExecuteSaveList()
        {
            // Dozwolone gdy s¹ pozycje na liœcie i wybrana firma (id lub tekst)
            return (SelectedLista?.Badania?.Count ?? 0) > 0
                   && (SelectedFirmaId != null || !string.IsNullOrWhiteSpace(WybranaFirma));
        }
    }
}