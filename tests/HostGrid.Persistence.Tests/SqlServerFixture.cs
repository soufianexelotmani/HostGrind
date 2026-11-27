using HostGrid.Api.Data;
using HostGrid.Api.Security;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Persistence.Tests;

public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly string? targetMigration;
    private readonly string databaseName = $"HostGrid_Phase2_Test_{Guid.NewGuid():N}";
    private string serverConnection = null!;
    private string databaseConnection = null!;
    private bool ownsDatabase;

    public SqlServerFixture() { }

    internal SqlServerFixture(string targetMigration) => this.targetMigration = targetMigration;

    public async ValueTask InitializeAsync()
    {
        var builder = new SqlConnectionStringBuilder(
            Environment.GetEnvironmentVariable("HOSTGRID_TEST_SQLSERVER")
            ?? @"Server=(localdb)\MSSQLLocalDB;Integrated Security=True;Connect Timeout=10");
        if (!string.IsNullOrEmpty(builder.AttachDBFilename))
        {
            throw new InvalidOperationException("Use a SQL Server instance connection without AttachDBFilename.");
        }

        builder.Pooling = false;
        builder.InitialCatalog = "master";
        serverConnection = builder.ConnectionString;
        builder.InitialCatalog = databaseName;
        databaseConnection = builder.ConnectionString;

        await ExecuteServerCommandAsync($"CREATE DATABASE [{databaseName}]");
        ownsDatabase = true;
        await using var context = CreateContext();
        await context.Database.MigrateAsync(targetMigration, TestContext.Current.CancellationToken);
    }

    internal HostGridDbContext CreateContext(Guid? organizationId = null)
        => new(ContextOptions(), new TestTenant(organizationId));

    internal string ConnectionString => databaseConnection;

    internal HostGridDbContext CreateDemoContext()
        => new(ContextOptions(), new HostGrid.Api.Data.Seed.DemoData.DemoTenantContext());

    private DbContextOptions<HostGridDbContext> ContextOptions()
        => new DbContextOptionsBuilder<HostGridDbContext>().UseSqlServer(databaseConnection).Options;

    public async ValueTask DisposeAsync()
    {
        if (ownsDatabase)
        {
            await ExecuteServerCommandAsync($"DROP DATABASE [{databaseName}]");
        }
    }

    private async Task ExecuteServerCommandAsync(string sql)
    {
        await using var connection = new SqlConnection(serverConnection);
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = sql;
        await command.ExecuteNonQueryAsync();
    }

    private sealed record TestTenant(Guid? AuthorizedOrganizationId) : ITenantContext;
}
