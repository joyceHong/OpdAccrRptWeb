using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using OpdAccrRptWeb.Infrastructure;

namespace OpdAccrRptWeb.Tests;

public sealed class ConnectionStringProviderTests
{
    [Fact]
    public void Configuration_BindsExplicitDatabaseEndpoints()
    {
        var values = new Dictionary<string, string?>
        {
            ["DatabaseConnections:SelectedDatabase"] = "DbTest3",
            ["DatabaseConnections:DbTest3:DatabaseName"] = "DBTEST3",
            ["DatabaseConnections:DbTest3:ApplicationName"] = "GUID_AP01",
            ["DatabaseConnections:DbGen:DatabaseName"] = "DBGEN",
            ["DatabaseConnections:DbGen:ApplicationName"] = "GUID_AP01"
        };
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(values)
            .Build();
        var options = new DatabaseConnectionOptions();

        configuration.GetSection(DatabaseConnectionOptions.SectionName).Bind(options);

        Assert.Equal("DbTest3", options.SelectedDatabase);
        Assert.Equal("DBTEST3", options.DbTest3.DatabaseName);
        Assert.Equal("DBGEN", options.DbGen.DatabaseName);
        Assert.Equal("GUID_AP01", options.DbTest3.ApplicationName);
        Assert.Equal("GUID_AP01", options.DbGen.ApplicationName);
    }

    [Theory]
    [InlineData("DbTest3", "TEST_CONNECTION")]
    [InlineData("dbtest3", "TEST_CONNECTION")]
    [InlineData("DbGen", "GEN_CONNECTION")]
    [InlineData("DBGEN", "GEN_CONNECTION")]
    public void GetConnectionString_SelectsConfiguredEndpointCaseInsensitively(
        string selection,
        string expectedConnectionString)
    {
        DatabaseEndpointOptions? capturedEndpoint = null;
        var provider = CreateProvider(selection, endpoint =>
        {
            capturedEndpoint = endpoint;
            return endpoint.DatabaseName == "DBTEST3" ? "TEST_CONNECTION" : "GEN_CONNECTION";
        });

        string result = provider.GetConnectionString();

        Assert.Equal(expectedConnectionString, result);
        Assert.NotNull(capturedEndpoint);
        Assert.Equal(expectedConnectionString == "TEST_CONNECTION" ? "DBTEST3" : "DBGEN", capturedEndpoint.DatabaseName);
        Assert.Equal("GUID_AP01", capturedEndpoint.ApplicationName);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("DbUnknown")]
    public void Validator_RejectsMissingOrUnsupportedSelection(string? selection)
    {
        ValidateOptionsResult result = Validate(CreateOptions(selection ?? string.Empty));

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains("SelectedDatabase", StringComparison.Ordinal));
    }

    [Theory]
    [InlineData("DbTest3", "", "GUID_AP01", "DbTest3:DatabaseName")]
    [InlineData("DbTest3", "DBTEST3", "", "DbTest3:ApplicationName")]
    [InlineData("DbGen", "", "GUID_AP01", "DbGen:DatabaseName")]
    [InlineData("DbGen", "DBGEN", "", "DbGen:ApplicationName")]
    public void Validator_RejectsIncompleteEndpoint(
        string endpointName,
        string databaseName,
        string applicationName,
        string expectedFailure)
    {
        DatabaseConnectionOptions options = endpointName == DatabaseNames.DbTest3
            ? CreateOptions(dbTest3DatabaseName: databaseName, dbTest3ApplicationName: applicationName)
            : CreateOptions(dbGenDatabaseName: databaseName, dbGenApplicationName: applicationName);

        ValidateOptionsResult result = Validate(options);

        Assert.False(result.Succeeded);
        Assert.Contains(result.Failures!, failure => failure.Contains(expectedFailure, StringComparison.Ordinal));
    }

    [Fact]
    public void Validator_AcceptsCompleteSupportedConfiguration()
    {
        Assert.True(Validate(CreateOptions("dbgen")).Succeeded);
    }

    [Fact]
    public void GetConnectionString_ResolutionFailureLogsDatabaseNameWithoutSensitiveValues()
    {
        var logger = new CapturingLogger<ConnectionStringProvider>();
        var provider = CreateProvider(
            DatabaseNames.DbGen,
            _ => throw new Exception("resolver failed"),
            logger,
            dbGenDatabaseName: "DBGEN_SAFE_NAME");

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(provider.GetConnectionString);

        Assert.Contains("DBGEN_SAFE_NAME", exception.Message);
        CapturedLog entry = Assert.Single(logger.Entries);
        Assert.Null(entry.Exception);
        Assert.Contains("DBGEN_SAFE_NAME", entry.Message);
        Assert.DoesNotContain("GUID_AP01", entry.Message);
        Assert.DoesNotContain("Password", entry.Message, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("Data Source", entry.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static ConnectionStringProvider CreateProvider(
        string selection,
        Func<DatabaseEndpointOptions, string> resolver,
        CapturingLogger<ConnectionStringProvider>? logger = null,
        string dbGenDatabaseName = "DBGEN") =>
        new(
            Options.Create(CreateOptions(selection, dbGenDatabaseName: dbGenDatabaseName)),
            logger ?? new CapturingLogger<ConnectionStringProvider>(),
            resolver);

    private static ValidateOptionsResult Validate(DatabaseConnectionOptions options) =>
        new DatabaseConnectionOptionsValidator().Validate(Options.DefaultName, options);

    private static DatabaseConnectionOptions CreateOptions(
        string selectedDatabase = DatabaseNames.DbTest3,
        string dbTest3DatabaseName = "DBTEST3",
        string dbTest3ApplicationName = "GUID_AP01",
        string dbGenDatabaseName = "DBGEN",
        string dbGenApplicationName = "GUID_AP01") =>
        new()
        {
            SelectedDatabase = selectedDatabase,
            DbTest3 = new DatabaseEndpointOptions
            {
                DatabaseName = dbTest3DatabaseName,
                ApplicationName = dbTest3ApplicationName
            },
            DbGen = new DatabaseEndpointOptions
            {
                DatabaseName = dbGenDatabaseName,
                ApplicationName = dbGenApplicationName
            }
        };
}
