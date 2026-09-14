using System.ComponentModel;

namespace OpdAccrRptWeb.ViewModels;

public readonly record struct C10VisitKey(
    string VisitDate,
    string VisitTime,
    string VisitRoom,
    int VisitNumber);

public sealed record C10DebtVisit(
    C10VisitKey VisitKey,
    string AdmissionSequence,
    string RoomType,
    string MedicalRecordNumber,
    string PatientName,
    string DoctorName,
    string DepartmentName,
    string? DischargeDate,
    string? HomePhone,
    string? Address1,
    string? Address2,
    string? ContactName,
    string? ContactRelation,
    string? ContactPhone,
    decimal DebtAmount);

public enum C10ChargeSource
{
    Order,
    Drug
}

public sealed record C10ChargeAggregate(
    C10VisitKey VisitKey,
    C10ChargeSource Source,
    string ChargeItemName,
    decimal? Sub6,
    decimal? Sub3,
    decimal? Sub1,
    decimal? Sub25);

public sealed class C10RepositoryResult
{
    public required IReadOnlyList<C10DebtVisit> Visits { get; init; }

    public required IReadOnlyList<C10ChargeAggregate> Charges { get; init; }
}

public sealed class C10ReceivableDetailRow
{
    [Description("就診日期")]
    public string VisitDate { get; init; } = string.Empty;

    [Description("就診時間")]
    public string VisitTime { get; init; } = string.Empty;

    [Description("診間")]
    public string VisitRoom { get; init; } = string.Empty;

    [Description("序號")]
    public int VisitNumber { get; init; }

    [Description("診別")]
    public string RoomTypeName { get; init; } = string.Empty;

    [Description("出院日期")]
    public string? DischargeDate { get; init; }

    [Description("科別")]
    public string DepartmentName { get; init; } = string.Empty;

    [Description("病歷號")]
    public string MedicalRecordNumber { get; init; } = string.Empty;

    [Description("姓名")]
    public string PatientName { get; init; } = string.Empty;

    [Description("住院序號")]
    public string AdmissionSequence { get; init; } = string.Empty;

    [Description("醫師")]
    public string DoctorName { get; init; } = string.Empty;

    [Description("電話")]
    public string? HomePhone { get; init; }

    [Description("地址一")]
    public string? Address1 { get; init; }

    [Description("地址二")]
    public string? Address2 { get; init; }

    [Description("聯絡人")]
    public string? ContactName { get; init; }

    [Description("關係")]
    public string? ContactRelation { get; init; }

    [Description("聯絡電話")]
    public string? ContactPhone { get; init; }

    [Description("已繳")]
    public long PaidAmount { get; init; }

    [Description("應繳")]
    public long AmountDue { get; init; }

    [Description("優待")]
    public long DiscountAmount { get; init; }

    [Description("欠款")]
    public long DebtAmount { get; init; }

    [Description("科目一")]
    public string? ItemName1 { get; init; }

    [Description("自費一")]
    public long? SelfPayAmount1 { get; init; }

    [Description("健保一")]
    public long? InsuranceAmount1 { get; init; }

    [Description("科目二")]
    public string? ItemName2 { get; init; }

    [Description("自費二")]
    public long? SelfPayAmount2 { get; init; }

    [Description("健保二")]
    public long? InsuranceAmount2 { get; init; }

    [Description("科目三")]
    public string? ItemName3 { get; init; }

    [Description("自費三")]
    public long? SelfPayAmount3 { get; init; }

    [Description("健保三")]
    public long? InsuranceAmount3 { get; init; }
}
