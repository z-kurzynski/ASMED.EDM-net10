using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ASMED.WPF
{
    public class FirmaDto
    {
        public int Id { get; set; }
        public bool Activ { get; set; }
        public string? Nazwa { get; set; }
        public string? NIP { get; set; }
        public string? Adres { get; set; }
        public string? Miasto { get; set; }
        public string? KodPocztowy { get; set; }

        // Nowe właściwości zgodne z konwencją (PascalCase)
        public string? Cennik { get; set; }
        public string? FkEmail { get; set; }

        // Legacy wrappers — utrzymują kompatybilność z istniejącymi bindingami (np. "cennik" w XAML)
        [Obsolete("Use Cennik")]
        public string? cennik
        {
            get => Cennik;
            set => Cennik = value;
        }

        [Obsolete("Use FkEmail")]
        public string? fkemail
        {
            get => FkEmail;
            set => FkEmail = value;
        }

        public string Display => string.IsNullOrWhiteSpace(NIP) ? (Nazwa ?? "") : $"{Nazwa} | {NIP}";
        public override string ToString() => Display;
    }
}
