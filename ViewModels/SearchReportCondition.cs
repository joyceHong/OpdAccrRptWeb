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

        /// <summary>C21 帳務範圍；依來源接受 0-3 或 4/5/8。</summary>
        public int? AccountingScope { get; set; }

        /// <summary>C21、C23 或 C24 是否強制重新計算／重建。</summary>
        public bool ForceRebuild { get; set; }

        /// <summary>C23 日期模式：General 或 EncounterDate。</summary>
        public string? DateMode { get; set; }

        /// <summary>C23 住院別：Inpatient 或 Discharged；就診日模式必須為空。</summary>
        public string? InpatientType { get; set; }

        /// <summary>C23、C211 選填合約代碼。</summary>
        public string? ContractCode { get; set; }

        /// <summary>C24、C10、C11、C143、C144 資料來源：OpdEr 或 Inpatient。</summary>
        public string? Source { get; set; }

        /// <summary>C24 作業模式：Accounting 或 Billing。</summary>
        public string? Mode { get; set; }

        /// <summary>C143 報表類型：Difference 或 All；C16：All、Child 或 NewHope。</summary>
        public string? ReportType { get; set; }

        /// <summary>C3 彙總 0 或明細 1。</summary>
        public int? DetailType { get; set; }

        /// <summary>C3 全部 0、物流 1 或非物流 2。</summary>
        public int? LogisticsType { get; set; }

        public string? DepartmentCode { get; set; }
        public string? RoomCodes { get; set; }
        public string? ChargeCodes { get; set; }

        /// <summary>C24 房別範圍：All、Emergency 或 NonEmergency；C10 使用 All、Emergency 或 Outpatient。</summary>
        public string? RoomScope { get; set; }

        /// <summary>C24、C10 選填病歷號；C12 為必填病歷號或身分證號。</summary>
        public string? MedicalRecordNo { get; set; }

        /// <summary>C12 選填新科別碼。</summary>
        public string? NewSectionCode { get; set; }

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

    public static class C21EncounterSources
    {
        public const string Outpatient = "Outpatient";
        public const string Inpatient = "Inpatient";

        public static bool IsSupported(string? value) => value is Outpatient or Inpatient;

        public static bool IsScopeSupported(string source, int scope) =>
            source == Outpatient ? scope is 0 or 1 or 2 or 3 : scope is 4 or 5 or 8;
    }

    public static class C23EncounterSources
    {
        public const string Outpatient = "Outpatient";
        public const string Inpatient = "Inpatient";
        public static bool IsSupported(string? value) => value is Outpatient or Inpatient;
    }

    public static class C23DateModes
    {
        public const string General = "General";
        public const string EncounterDate = "EncounterDate";
        public static bool IsSupported(string? value) => value is General or EncounterDate;
    }

    public static class C23InpatientTypes
    {
        public const string Inpatient = "Inpatient";
        public const string Discharged = "Discharged";
        public static bool IsSupported(string? value) => value is Inpatient or Discharged;
    }

    public static class ReceivableBalanceTypes
    {
        public const string SelfPay = "SelfPay";

        public const string Insurance = "Insurance";

        public static bool IsSupported(string? value) =>
            value is SelfPay or Insurance;
    }

    public static class C24Sources
    {
        public const string OpdEr = "OpdEr";
        public const string Inpatient = "Inpatient";
        public static bool IsSupported(string? value) => value is OpdEr or Inpatient;
    }

    public static class C24Modes
    {
        public const string Accounting = "Accounting";
        public const string Billing = "Billing";
        public static bool IsSupported(string? value) => value is Accounting or Billing;
    }

    public static class C24RoomScopes
    {
        public const string All = "All";
        public const string Emergency = "Emergency";
        public const string NonEmergency = "NonEmergency";
        public static bool IsSupported(string? value) => value is All or Emergency or NonEmergency;
    }

    public static class C10Sources
    {
        public const string OpdEr = "OpdEr";
        public const string Inpatient = "Inpatient";
        public static bool IsSupported(string? value) => value is OpdEr or Inpatient;
    }

    public static class C10RoomScopes
    {
        public const string All = "All";
        public const string Emergency = "Emergency";
        public const string Outpatient = "Outpatient";
        public static bool IsSupported(string? value) => value is All or Emergency or Outpatient;
    }

    public static class C12Sources
    {
        public const string OpdEr = "OpdEr";
        public const string Inpatient = "Inpatient";
        public static bool IsSupported(string? value) => value is OpdEr or Inpatient;
    }

    public static class C12RoomScopes
    {
        public const string All = "All";
        public const string Emergency = "Emergency";
        public const string Outpatient = "Outpatient";
        public static bool IsSupported(string? value) => value is All or Emergency or Outpatient;
    }

    public static class C143Sources
    {
        public const string OpdEr = "OpdEr";
        public const string Inpatient = "Inpatient";
        public static bool IsSupported(string? value) => value is OpdEr or Inpatient;
    }

    public static class C143ReportTypes
    {
        public const string Difference = "Difference";
        public const string All = "All";
        public static bool IsSupported(string? value) => value is Difference or All;
        public static string ToLegacyValue(string value) => value switch
        {
            Difference => "1",
            All => "2",
            _ => throw new ArgumentException("C143 報表類型不正確。", nameof(value))
        };
    }
}
