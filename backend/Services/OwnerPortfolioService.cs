using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Enums;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Services;

public sealed class OwnerPortfolioService(PortfolioRepository repository, PortfolioAccess access, PortfolioAudit audit,
    AuthenticationRepository locks, AccountManagementService accounts, UserManager<ApplicationUser> users)
{
    internal Task<Page<OwnerView>> ListAsync(string? search, bool? archived, string? sort, int page, int size, CancellationToken token)
    {
        access.EnsureManager();
        var query = repository.OwnerRows();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(row => row.Name.Contains(term) || row.Reference.Contains(term) ||
                row.Email != null && row.Email.Contains(term));
        }
        if (archived.HasValue) query = query.Where(row => row.IsArchived == archived);
        var ordered = sort switch
        {
            "-name" => query.OrderByDescending(row => row.Name),
            "reference" => query.OrderBy(row => row.Reference),
            "-reference" => query.OrderByDescending(row => row.Reference),
            "email" => query.OrderBy(row => row.Email),
            "-email" => query.OrderByDescending(row => row.Email),
            _ => query.OrderBy(row => row.Name)
        };
        return PortfolioRepository.PageAsync(ordered.ThenBy(row => row.Id), page, size, token);
    }

    internal async Task<OwnerPortfolio> GetAsync(Guid id, CancellationToken token)
    {
        access.EnsureManager();
        var owner = await repository.OwnerRows().SingleOrDefaultAsync(row => row.Id == id, token) ?? throw Missing();
        var properties = await repository.PropertyRows(repository.Properties.Where(row => row.OwnerId == id))
            .OrderBy(row => row.Name).ThenBy(row => row.Id).Take(100).ToListAsync(token);
        var contracts = await repository.Database.Set<ManagementContract>().AsNoTracking().Where(row => row.OwnerId == id)
            .OrderByDescending(row => row.StartDate).Take(100).ToListAsync(token);
        return new(owner, properties, contracts.Select(row => PropertyPortfolioService.DescribeContract(row, null)).ToArray());
    }

    internal async Task<Guid> CreateAsync(OwnerWrite input, CancellationToken token)
    {
        access.EnsureManager();
        var owner = new Owner { Id = Guid.NewGuid(), OrganizationId = access.OrganizationId, IsActive = true,
            Reference = "OWN-" + Guid.NewGuid().ToString("N").ToUpperInvariant() };
        Apply(owner, input);
        repository.Owners.Add(owner);
        await audit.SaveAsync("owner_created", "Owner", owner.Id, token);
        return owner.Id;
    }

    internal async Task UpdateAsync(Guid id, OwnerWrite input, CancellationToken token)
    {
        var owner = await LoadAsync(id, token);
        PortfolioVersion.Match(owner.RowVersion, input.Version);
        Apply(owner, input);
        await audit.SaveAsync("owner_updated", "Owner", id, token);
    }

    internal async Task InviteAsync(Guid id, CancellationToken token)
    {
        var owner = await LoadAsync(id, token);
        if (owner.IsArchived || string.IsNullOrWhiteSpace(owner.Email))
            throw new SecurityRequestException(409, "An active profile with an email is required.");
        await accounts.InviteAsync(new(owner.Email, owner.Name, CurrentUserService.Owner),
            access.User.IsInRole(CurrentUserService.SuperAdmin), token, owner.Id);
    }

    internal async Task LinkAsync(Guid id, OwnerLinkWrite input, CancellationToken token)
    {
        access.EnsureManager();
        await using var transaction = await locks.LockAsync(input.UserId.ToString("N"), token);
        var owner = await LoadAsync(id, token);
        PortfolioVersion.Match(owner.RowVersion, input.Version);
        if (owner.UserId.HasValue || owner.IsArchived || await repository.Owners.AnyAsync(row => row.UserId == input.UserId, token))
            throw new SecurityRequestException(409, "An eligible unlinked profile and account are required.");
        var account = await users.FindByIdAsync(input.UserId.ToString());
        var eligible = account is not null && !await users.IsInRoleAsync(account, CurrentUserService.SuperAdmin) &&
            await repository.Database.Set<OrganizationMember>().AnyAsync(row => row.UserId == input.UserId &&
                row.Role == OrganizationRole.Owner && row.IsActive, token);
        if (!eligible) throw new SecurityRequestException(404, "An eligible Owner account was not found.");
        owner.UserId = input.UserId;
        owner.IsActive = true;
        await audit.SaveAsync("owner_account_linked", "Owner", id, token);
        await transaction.CommitAsync(token);
    }

    internal async Task DisableAsync(Guid id, CancellationToken token)
    {
        var owner = await LoadAsync(id, token);
        if (owner.UserId is not Guid userId) throw new SecurityRequestException(409, "The profile has no linked account.");
        await accounts.ChangeMembershipAsync(userId, new(false, CurrentUserService.Owner),
            access.User.IsInRole(CurrentUserService.SuperAdmin), token);
    }

    internal async Task ArchiveAsync(Guid id, byte[] version, CancellationToken token)
    {
        access.EnsureManager();
        await using var transaction = await locks.LockAsync("owner:" + id, token);
        var owner = await LoadAsync(id, token);
        PortfolioVersion.Match(owner.RowVersion, version);
        if (await repository.Properties.AnyAsync(row => row.OwnerId == id && row.Status != PropertyStatus.Archived, token))
            throw new SecurityRequestException(409, "Archive the owner's properties first.");
        owner.IsArchived = true;
        owner.IsActive = false;
        await audit.SaveAsync("owner_archived", "Owner", id, token);
        await transaction.CommitAsync(token);
    }

    private async Task<Owner> LoadAsync(Guid id, CancellationToken token)
    {
        access.EnsureManager();
        return await repository.Owners.SingleOrDefaultAsync(row => row.Id == id, token) ?? throw Missing();
    }

    private static void Apply(Owner owner, OwnerWrite input)
    {
        owner.Name = input.Name.Trim();
        owner.Email = string.IsNullOrWhiteSpace(input.Email) ? null : input.Email.Trim();
        owner.Phone = string.IsNullOrWhiteSpace(input.Phone) ? null : input.Phone.Trim();
    }

    private static SecurityRequestException Missing() => new(404, "Owner profile not found.");
}

internal static class PortfolioVersion
{
    internal static void Match(byte[] current, byte[]? expected)
    {
        if (expected is not { Length: 8 } || !current.AsSpan().SequenceEqual(expected))
            throw new SecurityRequestException(409, "This record changed. Reload it and try again.");
    }
}
