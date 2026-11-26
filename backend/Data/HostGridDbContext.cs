using System.Reflection;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HostGrid.Api.Data;

public sealed class HostGridDbContext(
    DbContextOptions<HostGridDbContext> options,
    ITenantContext tenantContext) : IdentityDbContext<ApplicationUser, ApplicationRole, Guid>(options)
{
    private Guid? AuthorizedOrganizationId => tenantContext.AuthorizedOrganizationId;

    protected override void ConfigureConventions(ModelConfigurationBuilder builder)
    {
        builder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("UserClaim", "identity");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("UserLogin", "identity");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("UserRole", "identity");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("UserToken", "identity");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("RoleClaim", "identity");

        builder.ApplyConfigurationsFromAssembly(typeof(HostGridDbContext).Assembly);
        builder.Entity<Organization>().HasQueryFilter(
            organization => organization.Id == AuthorizedOrganizationId);

        var tenantFilter = typeof(HostGridDbContext).GetMethod(
            nameof(ApplyTenantFilter), BindingFlags.Instance | BindingFlags.NonPublic)!;
        foreach (var entity in builder.Model.GetEntityTypes())
        {
            if (typeof(ITenantEntity).IsAssignableFrom(entity.ClrType))
            {
                tenantFilter.MakeGenericMethod(entity.ClrType).Invoke(this, [builder]);
            }
        }
    }

    private void ApplyTenantFilter<TEntity>(ModelBuilder builder) where TEntity : class, ITenantEntity
    {
        builder.Entity<TEntity>().HasQueryFilter(entity => entity.OrganizationId == AuthorizedOrganizationId);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        ValidateTenantWrites();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        ValidateTenantWrites();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private void ValidateTenantWrites()
    {
        foreach (var entry in ChangeTracker.Entries()
            .Where(entry => entry.State is EntityState.Added or EntityState.Modified or EntityState.Deleted))
        {
            var organizationId = entry.Entity switch
            {
                ITenantEntity tenantEntity => tenantEntity.OrganizationId,
                Organization organization => organization.Id,
                _ => (Guid?)null
            };

            if (organizationId.HasValue && (organizationId == Guid.Empty || organizationId != AuthorizedOrganizationId))
            {
                throw new InvalidOperationException("An authorized organization is required for this change.");
            }
        }
    }

    private sealed class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
        value => RequireUtc(value),
        value => DateTime.SpecifyKind(value, DateTimeKind.Utc))
    {
        private static DateTime RequireUtc(DateTime value) => value.Kind == DateTimeKind.Utc
            ? value
            : throw new InvalidOperationException("Persisted timestamps must be UTC.");
    }
}
