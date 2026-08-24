using Microsoft.Extensions.Options;

namespace OpdAccrRptWeb.Infrastructure;

public sealed class DatabaseConnectionOptions
{
    public const string SectionName = "DatabaseConnections";

    public string SelectedDatabase { get; init; } = string.Empty;

    public DatabaseEndpointOptions DbTest3 { get; init; } = new();

    public DatabaseEndpointOptions DbGen { get; init; } = new();

    public DatabaseEndpointOptions GetSelectedEndpoint() => SelectedDatabase switch
    {
        var value when value.Equals(DatabaseNames.DbTest3, StringComparison.OrdinalIgnoreCase) => DbTest3,
        var value when value.Equals(DatabaseNames.DbGen, StringComparison.OrdinalIgnoreCase) => DbGen,
        _ => throw new InvalidOperationException(
            $"不支援的資料庫選擇 [{SelectedDatabase}]，僅允許 {DatabaseNames.DbTest3} 或 {DatabaseNames.DbGen}。")
    };
}

public static class DatabaseNames
{
    public const string DbTest3 = "DbTest3";
    public const string DbGen = "DbGen";

    public static bool IsSupported(string? value) =>
        value is not null
        && (value.Equals(DbTest3, StringComparison.OrdinalIgnoreCase)
            || value.Equals(DbGen, StringComparison.OrdinalIgnoreCase));
}

public sealed class DatabaseConnectionOptionsValidator : IValidateOptions<DatabaseConnectionOptions>
{
    public ValidateOptionsResult Validate(string? name, DatabaseConnectionOptions options)
    {
        var failures = new List<string>();

        if (!DatabaseNames.IsSupported(options.SelectedDatabase))
        {
            failures.Add(
                $"{DatabaseConnectionOptions.SectionName}:SelectedDatabase 必須為 {DatabaseNames.DbTest3} 或 {DatabaseNames.DbGen}。");
        }

        ValidateEndpoint(DatabaseNames.DbTest3, options.DbTest3, failures);
        ValidateEndpoint(DatabaseNames.DbGen, options.DbGen, failures);

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }

    private static void ValidateEndpoint(string endpointName, DatabaseEndpointOptions endpoint, ICollection<string> failures)
    {
        if (string.IsNullOrWhiteSpace(endpoint.DatabaseName))
        {
            failures.Add($"{DatabaseConnectionOptions.SectionName}:{endpointName}:DatabaseName 不得為空白。");
        }

        if (string.IsNullOrWhiteSpace(endpoint.ApplicationName))
        {
            failures.Add($"{DatabaseConnectionOptions.SectionName}:{endpointName}:ApplicationName 不得為空白。");
        }
    }
}

public sealed class DatabaseEndpointOptions
{
    public string DatabaseName { get; init; } = string.Empty;

    public string ApplicationName { get; init; } = string.Empty;
}
