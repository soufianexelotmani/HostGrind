using HostGrid.Api.Data.Seed;
using HostGrid.Api.Security;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HostGrid.Api.Data;

public sealed class HostGridDbContextFactory : IDesignTimeDbContextFactory<HostGridDbContext>
{
    public HostGridDbContext CreateDbContext(string[] args)
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            Args = args,
            ApplicationName = typeof(HostGridDbContext).Assembly.GetName().Name
        });
        var connection = builder.Configuration.GetConnectionString("HostGrid");
        var options = new DbContextOptionsBuilder<HostGridDbContext>();
        options.UseSqlServer(connection);

        if (!string.Equals(builder.Configuration["HostGrid:SeedDemo"], "true", StringComparison.OrdinalIgnoreCase))
        {
            return new HostGridDbContext(options.Options, new UnresolvedTenantContext());
        }

        if (!builder.Environment.IsDevelopment())
        {
            throw new InvalidOperationException("Demo data requires the Development environment.");
        }

        ValidateDemoTarget(connection);
        options.UseSeeding((context, _) =>
        {
            ValidateDemoTarget(context.Database.GetConnectionString());
            DemoData.Seed((HostGridDbContext)context);
        });
        options.UseAsyncSeeding((context, _, token) =>
        {
            ValidateDemoTarget(context.Database.GetConnectionString());
            return DemoData.SeedAsync((HostGridDbContext)context, token);
        });
        return new HostGridDbContext(options.Options, new DemoData.DemoTenantContext());
    }

    private static void ValidateDemoTarget(string? connection)
    {
        if (string.IsNullOrWhiteSpace(connection))
        {
            throw new InvalidOperationException("Demo data requires an explicit LocalDB connection.");
        }

        var target = new SqlConnectionStringBuilder(connection);
        if (!target.DataSource.Equals(@"(localdb)\MSSQLLocalDB", StringComparison.OrdinalIgnoreCase)
            || !target.InitialCatalog.Equals("HostGridDb", StringComparison.OrdinalIgnoreCase)
            || !target.IntegratedSecurity || !string.IsNullOrEmpty(target.AttachDBFilename))
        {
            throw new InvalidOperationException("Demo data can only target the local HostGridDb development database.");
        }

    }
}
