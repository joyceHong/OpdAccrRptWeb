using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels
{
    /// <summary>
    /// 呈現在 健康中心統計的欄位上
    /// </summary>
    public class HelthCenterCountViewModel
    {      

        [Description("責任中心代碼")]
        public string? CenterCode { get; set; }

        [Description("就診日")]
        public string? VisitDate { get; set; }

        [Description("批價代碼")]
        public string? BillingCode { get; set; }

        [Description("批價碼名稱")]
        public string? BillingName { get; set; }

        [Description("金額")]
        public decimal TotalAmount { get; set; }
    }
}
