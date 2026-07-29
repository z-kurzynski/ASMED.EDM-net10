namespace ASMED.WPF.Helpers
{
    internal class RejestracjaRecord : AccessDbContext.RejestracjaRecord
    {
        public new string R_Data { get; set; } = string.Empty;
        public string R_Status { get; set; } = string.Empty;
        public new int R_S_ID { get; set; }
        public new string R_GG_MM { get; set; } = string.Empty;
        public new string R_Uwagi { get; set; } = string.Empty;
        public new int R_P_ID { get; set; }
    }
}