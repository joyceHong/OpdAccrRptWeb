namespace OpdAccrRptWeb.Infrastructure;

public sealed class DatabaseConnectionOptions
{
    public const string SectionName = "DatabaseConnections";

    public DatabaseEndpointOptions GuidAp01 { get; init; } = new();

    public DatabaseEndpointOptions DbTest3 { get; init; } = new();
}

public sealed class DatabaseEndpointOptions
{
    public string DatabaseName { get; init; } = string.Empty;

    public string ApplicationName { get; init; } = string.Empty;
}
