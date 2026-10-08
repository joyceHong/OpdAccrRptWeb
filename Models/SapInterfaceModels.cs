namespace OpdAccrRptWeb.Models;

public sealed class SapInterfaceRequest
{
    public string BusinessDate { get; init; } = string.Empty;
    public bool RunCash { get; init; }
    public bool RunContract { get; init; }
    public bool RunAcc { get; init; }
    public bool RunRev { get; init; }
    public IReadOnlyList<string> ConfirmRerun { get; init; } = [];
}

public sealed record SapInterfaceEventStatus(string Event, string Name, bool Completed);

public sealed record SapInterfaceRunResult(
    string BusinessDate,
    IReadOnlyList<SapInterfaceEventResult> Events,
    IReadOnlyList<SapInterfaceEventStatus> ConfirmationRequired);

public sealed record SapInterfaceEventResult(string Event, string Name, string Status, string? Message = null);

public enum SapInterfaceRepositoryRunStatus
{
    Completed,
    NoData,
    ConfirmationRequired
}

public sealed record SapInterfaceRepositoryRunResult(
    SapInterfaceRepositoryRunStatus Status,
    int InsertedRows);
