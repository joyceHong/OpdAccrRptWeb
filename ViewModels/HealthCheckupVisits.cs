using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels
{
    public class HealthCheckupVisits
    {
        /// <summary>
        /// 就診月份
        /// </summary>
        [Description("就診月份")]
        public string Chop1date { get; set; }

        /// <summary>
        /// 科別
        /// </summary>
        [Description("科別")]
        public string Chop1sec { get; set; }

        /// <summary>
        /// 人次
        /// </summary>
        [Description("人次")]
        public int Visits { get; set; }

    }
}
