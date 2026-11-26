using HostGrid.Api.Entities.Expense;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Entities.Property;

namespace HostGrid.Api.Data.Seed;

internal static class ReferenceData
{
    internal static ApplicationRole[] Roles { get; } =
    [
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000001"), Name = "SuperAdmin", NormalizedName = "SUPERADMIN", ConcurrencyStamp = "SuperAdmin" },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000002"), Name = "Manager", NormalizedName = "MANAGER", ConcurrencyStamp = "Manager" },
        new() { Id = Guid.Parse("10000000-0000-0000-0000-000000000003"), Name = "Owner", NormalizedName = "OWNER", ConcurrencyStamp = "Owner" }
    ];

    internal static PropertyType[] PropertyTypes { get; } =
    [
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000001"), Code = "Studio" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000002"), Code = "Apartment" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000003"), Code = "Villa" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000004"), Code = "House" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000005"), Code = "Riad" },
        new() { Id = Guid.Parse("20000000-0000-0000-0000-000000000006"), Code = "Room" }
    ];

    private static readonly string[] ExpenseCategoryNames =
    [
        "Water", "Electricity", "Internet", "Cleaning", "Linen", "Maintenance",
        "Furniture", "Consumables", "Association fees", "Insurance", "Taxes", "Other"
    ];

    internal static IEnumerable<ExpenseCategory> MissingExpenseCategories(Guid organizationId, IEnumerable<string> existingNames)
    {
        var existing = existingNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return ExpenseCategoryNames.Where(name => !existing.Contains(name))
            .Select(name => new ExpenseCategory { OrganizationId = organizationId, Name = name, IsActive = true });
    }
}
