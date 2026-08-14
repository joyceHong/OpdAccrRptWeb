using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels
{
    /// <summary>
    ///  顯示健保中心明細的欄位，包含記帳代碼、責任中心代碼、醫師代碼、就診日、病歷號、病患名稱、數量、單價、總金額等屬性。
    ///  Description  ==> 用在UI 上面的column header 顯示    
    /// </summary>
    public class HelthCenterDetailViewModel
    {       
        [Description("記帳代碼")]
        public string? PostingCode { get; set; }

        [Description("記帳名稱")]
        public string? PostingName { get; set; }

        [Description("責任中心代碼")]
        public string? CenterCode { get; set; }

        [Description("開單醫師代碼")]
        public string? OrderingDoctorId { get; set; }

        [Description("執行醫師代碼")]
        public string? PerformingDoctorId { get; set; }

        [Description("就診日")]
        public string? VisitDate { get; set; }

        [Description("診間")]
        public string? ClinicRoom { get; set; }

        [Description("病歷號")]
        public string? CHMRNO { get; set; }

        [Description("病患名稱")]
        public string? PatientName { get; set; }

        [Description("數量")]
        public int Qty { get; set; }

        [Description("單價")]
        public decimal UnitPrice { get; set; }

        [Description("總金額")]
        public decimal TotalAmount { get; set; }

        [Description("批價代碼")]
        public string? BillingCode { get; set; }

        [Description("批價碼名稱")]
        public string? BillingName { get; set; }

        [Description("開單時間")]
        public string? OrderTime { get; set; }
    }
}
