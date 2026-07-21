namespace ASMED.WPF.Models
{
    /// <summary>
    /// Role u¿ytkowników w systemie ASMED
    /// </summary>
    public enum UserRole
    {
        /// <summary>
        /// ?? Super Administrator - pe³ny dostêp do wszystkich funkcji (w tym zarz¹dzania u¿ytkownikami)
        /// </summary>
        SuperAdmin = 0,

        /// <summary>
        /// ?? Administrator - dostêp do wiêkszoœci funkcji (bez zarz¹dzania u¿ytkownikami)
        /// </summary>
        Admin = 1,

        /// <summary>
        /// ?? Recepcja - rejestracja wizyt, wizyty, podstawowe operacje
        /// </summary>
        Recepcja = 2,

        /// <summary>
        /// ????? Lekarz - badania, dokumentacja medyczna, wydruków formularzy
        /// </summary>
        Lekarz = 3,

        /// <summary>
        /// ?? Biuro - faktury, listy do faktur, raporty finansowe
        /// </summary>
        Biuro = 4
    }
}
