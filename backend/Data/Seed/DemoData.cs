using HostGrid.Api.Entities.Expense;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Security;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Data.Seed;

internal static class DemoData
{
    private static readonly Guid OrganizationId = Guid.Parse("30000000-0000-0000-0000-000000000001");

    internal sealed class DemoTenantContext : ITenantContext
    {
        public Guid? AuthorizedOrganizationId => OrganizationId;
    }

    internal static void Seed(HostGridDbContext context)
    {
        AddMissingRows(context,
            context.Set<Organization>().Any(),
            context.Set<OrganizationSettings>().Any(),
            context.Set<ExpenseCategory>().Select(category => category.Name).ToList());
        context.SaveChanges();
    }

    internal static async Task SeedAsync(HostGridDbContext context, CancellationToken cancellationToken)
    {
        AddMissingRows(context,
            await context.Set<Organization>().AnyAsync(cancellationToken),
            await context.Set<OrganizationSettings>().AnyAsync(cancellationToken),
            await context.Set<ExpenseCategory>().Select(category => category.Name).ToListAsync(cancellationToken));
        await context.SaveChangesAsync(cancellationToken);
    }

    private static void AddMissingRows(HostGridDbContext context, bool hasOrganization, bool hasSettings, IEnumerable<string> categoryNames)
    {
        if (!hasOrganization)
        {
            context.Add(new Organization { Id = OrganizationId, Reference = "HOSTGRID-DEMO", Name = "HostGrid Demo", IsActive = true });
        }

        if (!hasSettings)
        {
            context.Add(new OrganizationSettings { OrganizationId = OrganizationId, Language = "fr" });
        }

        context.AddRange(ReferenceData.MissingExpenseCategories(OrganizationId, categoryNames));
    }
}
