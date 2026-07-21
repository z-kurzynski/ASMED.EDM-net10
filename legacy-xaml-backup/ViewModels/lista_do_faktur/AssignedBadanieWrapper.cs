using ASMED.WPF.Helpers;

namespace ASMED.WPF.ViewModels.ListaDoFaktur
{
    public class AssignedBadanieWrapper
    {
        public AssignedBadanieWrapper(AccessDbContext.AssignedBadanieDto dto)
        {
            Dto = dto;
        }

        public bool IsMarked { get; internal set; }
        public object Dto { get; internal set; }
    }
}