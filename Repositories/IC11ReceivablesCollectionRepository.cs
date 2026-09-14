namespace OpdAccrRptWeb.Repositories;

public sealed record C11AggregateRow(
    string Year,
    string RoomType,
    string RoomTypeName,
    decimal OutstandingAmount);

public sealed record C11PatientCountRow(string RoomTypeName, decimal PatientCount);

public interface IC11ReceivablesCollectionRepository
{
    Task<IReadOnlyList<C11AggregateRow>> QueryOutstandingToEndAsync(
        string source, string endDate, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<C11AggregateRow>> QueryPeriodOutstandingAsync(
        string source, string startDate, string endDate, CancellationToken cancellationToken = default);

    Task<C11PatientCountRow?> QueryPeriodPatientCountAsync(
        string source, string startDate, string endDate, string roomTypeName,
        CancellationToken cancellationToken = default);
}
