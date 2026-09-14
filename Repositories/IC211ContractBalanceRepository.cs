namespace OpdAccrRptWeb.Repositories;

public interface IC211ContractBalanceRepository
{
    Task<IReadOnlyList<C211Row>> GetRowsAsync(
        string source, DateOnly asOfDate, string? contractCode, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<C211ContractChoice>> GetContractChoicesAsync(
        CancellationToken cancellationToken = default);
}

public sealed record C211Row(
    string ContractCode,
    string MedicalRecordNo,
    string VisitDateRoc,
    string VisitTime,
    string RoomCode,
    decimal SequenceNo,
    decimal SelfAmount,
    decimal ClaimAmount);

public sealed record C211ContractChoice(string Code, string Name);

public static class C211Sources
{
    public const string Outpatient = "O";
    public const string Inpatient = "I";
    public static bool IsSupported(string? value) => value is Outpatient or Inpatient;
}
