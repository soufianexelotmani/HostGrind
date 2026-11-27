using HostGrid.Api.Data;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Entities.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace HostGrid.Api.Repositories;

public sealed class AuthenticationRepository(HostGridDbContext database)
{
    internal HostGridDbContext Database => database;
    internal DbSet<RefreshToken> Sessions => database.Set<RefreshToken>();
    internal DbSet<LoginChallenge> Challenges => database.Set<LoginChallenge>();

    internal async Task<IDbContextTransaction> LockAsync(string resource, CancellationToken token)
    {
        var transaction = await database.Database.BeginTransactionAsync(token);
        try
        {
            await LockResourceAsync(resource, token);
            return transaction;
        }
        catch
        {
            await transaction.DisposeAsync();
            throw;
        }
    }

    internal Task LockResourceAsync(string resource, CancellationToken token)
    {
        var name = "hostgrid:auth:" + resource;
        return database.Database.ExecuteSqlInterpolatedAsync($"""
            DECLARE @result int;
            EXEC @result = sp_getapplock @Resource = {name}, @LockMode = 'Exclusive',
                @LockOwner = 'Transaction', @LockTimeout = 10000;
            IF @result < 0 THROW 51003, 'Could not acquire authentication lock.', 1;
            """, token);
    }

    internal async Task RevokeAsync(Guid userId, DateTime now, CancellationToken token, Guid? family = null)
    {
        await Sessions.Where(row => row.UserId == userId && row.RevokedAt == null &&
                (family == null || row.FamilyId == family))
            .ExecuteUpdateAsync(update => update.SetProperty(row => row.RevokedAt, now), token);
    }

    // Identity resolution precedes tenant selection. This lookup is constrained to the verified account.
    internal IQueryable<OrganizationMember> ActiveMemberships(Guid userId)
        => database.Set<OrganizationMember>().IgnoreQueryFilters().AsNoTracking()
            .Where(member => member.UserId == userId && member.IsActive &&
                database.Set<Organization>().IgnoreQueryFilters()
                    .Any(organization => organization.Id == member.OrganizationId && organization.IsActive));

    internal IQueryable<Owner> ActiveOwners(Guid userId)
        => database.Set<Owner>().IgnoreQueryFilters().AsNoTracking()
            .Where(owner => owner.UserId == userId && owner.IsActive);
}
