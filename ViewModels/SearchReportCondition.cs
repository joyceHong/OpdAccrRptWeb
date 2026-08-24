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
}
