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
        /// 科別
        /// </summary>
        public string? Chop1sec { get; set; }


    }
}
