namespace OpdAccrRptWeb.ViewModels
{
    public class SearchReportCondition
    {
        /// <summary>
        /// 報表代碼
        /// </summary>
        public string?  ReportCode { get; set; }

        /// <summary>
        /// 查報報表的區間_起
        /// </summary>
        public string?  StartDate { get; set; }

        /// <summary>
        /// 查詢報表的區間_迄
        /// </summary>

        public string? EndDate { get; set; }

        /// <summary>
        /// C18 就醫來源，僅接受 Emergency 或 Inpatient。
        /// </summary>
        public string? EncounterSource { get; set; }

        /// <summary>
        /// C19 選填的護理站／床號前綴。
        /// </summary>
        public string? StationOrBedPrefix { get; set; }

        public string? CashierUserId { get; set; }

        public string? CashierCashSortType { get; set; }

        /// <summary>
        /// C29 選填的合約代碼；空白代表全部合約。
        /// </summary>
        public string? BillingCode { get; set; }

        /// <summary>
        /// C214 應收餘額類型，僅接受 SelfPay 或 Insurance。
        /// </summary>
        public string? ReceivableBalanceType { get; set; }

        /// <summary>
        /// 科別
        /// </summary>
        public string? Chop1sec { get; set; }

        /// <summary>
        /// 從 1 開始的頁碼。C171 未提供時預設為 1。
        /// </summary>
        public int? PageNumber { get; set; }

        /// <summary>
        /// 每頁筆數。C171 僅接受 10、30 或 50。
        /// </summary>
        public int? PageSize { get; set; }

    }

    public static class EncounterSources
    {
        public const string Emergency = "Emergency";

        public const string Inpatient = "Inpatient";

        public static bool IsSupported(string? value) =>
            value is Emergency or Inpatient;
    }

    public static class ReceivableBalanceTypes
    {
        public const string SelfPay = "SelfPay";

        public const string Insurance = "Insurance";

        public static bool IsSupported(string? value) =>
            value is SelfPay or Insurance;
    }
}
