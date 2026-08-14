using System.Globalization;

namespace OpdAccrRptWeb.Help
{
    public static class DateTimeExtensions
    {
        private static readonly TaiwanCalendar TaiwanCal = new TaiwanCalendar();

        /// <summary>
        /// 轉為民國年格式字串 (例: 115/08/11)
        /// </summary>
        public static string ToRocDateString(this DateTime date, string format = "yyyMMdd")
        {
            int year = TaiwanCal.GetYear(date);
            return format
                .Replace("yyy", year.ToString("D3"))
                .Replace("yy", (year % 100).ToString("D2"))
                .Replace("MM", date.ToString("MM"))
                .Replace("dd", date.ToString("dd"));
        }
    }
}
