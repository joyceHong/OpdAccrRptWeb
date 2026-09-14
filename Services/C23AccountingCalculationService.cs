namespace OpdAccrRptWeb.Services;

public sealed record C23ClassificationInput(
    string AccountingDate, string Room, string ContractCode, string OrderCode,
    string? SelfPayCode, string? IrbNumber, bool IsInpatient,
    decimal Sub3, decimal Sub5, bool IsCancellation);

public sealed record C23ClassificationResult(
    string ContractCode, string ContractName, decimal GrossAmount,
    decimal ContractAmount, decimal DiscountAmount);

public static class C23AccountingCalculationService
{
    private static readonly HashSet<string> DisabledCodes = ["64-001", "64-002", "64-003", "64-004", "64-005"];

    public static C23ClassificationResult Calculate(C23ClassificationInput input)
    {
        var (code, name) = Classify(input);
        var sign = input.IsCancellation ? -1m : 1m;
        var contractAmount = input.Sub5 * sign;
        var discountAmount = input.Sub3 * sign;
        return new(code, name, contractAmount + discountAmount, contractAmount, discountAmount);
    }

    private static (string Code, string Name) Classify(C23ClassificationInput input)
    {
        if (input.Room.Trim() == "EEEE" && input.ContractCode.Trim() != "118") return ("ZZ", "聯盟代檢");
        if (input.OrderCode.Trim() == "00-039") return ("XX", "老人照護鑑定");
        if (DisabledCodes.Contains(input.OrderCode.Trim())) return ("YY", "殘障鑑定");
        if (input.OrderCode.Trim() is "102-23" or "102-24") return ("WW", "北縣65歲以上老人健檢");
        if (input.OrderCode.Trim() is "S001A" or "S001B") return ("42", "移植用組織捐贈專戶");

        var modern = string.CompareOrdinal(input.AccountingDate, "0930601") >= 0;
        if (modern && input.SelfPayCode?.Trim() == "6") return ("VV", "維康記帳");
        if (modern && input.SelfPayCode?.Trim() == "7")
            return input.IsInpatient && !string.IsNullOrWhiteSpace(input.IrbNumber)
                ? (input.IrbNumber.Trim(), "研究經費")
                : ("TT", "研究經費");
        if (input.ContractCode.Trim() == "00") return modern ? ("UU", "其他") : ("VV", "維康記帳");
        return (input.ContractCode.Trim(), string.Empty);
    }
}
