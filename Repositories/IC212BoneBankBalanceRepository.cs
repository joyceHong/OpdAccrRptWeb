namespace OpdAccrRptWeb.Repositories;

public interface IC212BoneBankBalanceRepository
{
    Task<IReadOnlyList<C212RawRow>> GetRowsAsync(
        string endDateRoc,
        string monthFirstDayRoc,
        CancellationToken cancellationToken = default);

    Task<DateTime> GetOracleNowAsync(CancellationToken cancellationToken = default);
}

public enum C212RowKind
{
    OpeningBalance = 0,
    Movement = 1
}

public sealed record C212RawRow(
    C212RowKind Kind,
    string AccountingDateRoc,
    string MedicalRecordNo,
    string PatientName,
    decimal RawOracleAmount);
