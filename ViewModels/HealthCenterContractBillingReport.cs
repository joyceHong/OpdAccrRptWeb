using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels
{
    public class HealthCenterContractBillingReport
    {
        [Description("記帳代碼")]
        public string? BillingCode { get; set; }

        [Description("記帳名稱")]
        public string? BillingName { get; set; }

        [Description("就診日")]
        public string? VisitDate { get; set; }

        [Description("病歷號")]
        public string? Chop1mrno { get; set; }

        [Description("病患姓名")]
        public string? PatientName { get; set; }

        [Description("科別名稱")]
        public string? DepartmentName { get; set; }

        [Description("醫師名稱")]
        public string? DoctorName { get; set; }

        [Description("會計科目代碼")]
        public string? AccountSubjectCode { get; set; }

        [Description("會計科目名稱")]
        public string? AccountSubjectName { get; set; }

        [Description("優待金額")]
        public decimal DiscountAmount { get; set; }

        [Description("記帳金額")]
        public decimal BillingAmount { get; set; }

        [Description("總金額")]
        public decimal TotalAmount { get; set; }

        [Description("記帳員")]
        public string? BillingUser { get; set; }
    }
}
