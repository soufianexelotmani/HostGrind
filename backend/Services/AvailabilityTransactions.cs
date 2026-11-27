using System.Data;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Options;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Options;

namespace HostGrid.Api.Services;

internal sealed class AvailabilityTransactions(AvailabilityRepository repository, PortfolioAccess access,
    IOptions<AvailabilityOptions> options, ILogger<AvailabilityTransactions> logger)
{
    private HostGrid.Api.Data.HostGridDbContext Db => repository.Database;

    internal async Task ExecuteAsync(IEnumerable<Guid> properties, Func<IReadOnlyDictionary<Guid, Property>, Task> write,
        CancellationToken token)
    {
        var organization = access.OrganizationId;
        var ordered = properties.Distinct().Order().ToArray();
        if (ordered.Length == 0) throw new ArgumentException("A property is required.", nameof(properties));
        foreach (var property in ordered)
            await access.PropertyAsync(property, true, token, tracking: false);
        access.EnsureManager();

        try
        {
            await Db.Database.CreateExecutionStrategy().ExecuteAsync(async cancellation =>
            {
                var commitStarted = false;
                try
                {
                    await using var transaction = await Db.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, cancellation);
                    foreach (var property in ordered)
                        await LockAsync(organization, property, cancellation);

                    var current = new Dictionary<Guid, Property>();
                    foreach (var property in ordered)
                    {
                        var tracked = Db.ChangeTracker.Entries<Property>()
                            .SingleOrDefault(entry => entry.Entity.Id == property);
                        if (tracked is not null)
                            await tracked.ReloadAsync(cancellation);
                        current.Add(property, await access.PropertyAsync(property, true, cancellation));
                    }

                    await write(current);
                    await Db.SaveChangesAsync(cancellation);
                    cancellation.ThrowIfCancellationRequested();
                    commitStarted = true;
                    await transaction.CommitAsync(cancellation);
                }
                catch
                {
                    Db.ChangeTracker.Clear();
                    // Disposing the uncommitted transaction rolls it back and releases its locks.
                    if (commitStarted)
                        throw new SecurityRequestException(503, "The write outcome is unknown. Reload before retrying.",
                            "availability_commit_unknown");
                    cancellation.ThrowIfCancellationRequested();
                    throw;
                }
            }, token);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw Stale();
        }
        catch (Exception error) when (error is RetryLimitExceededException ||
            error.GetBaseException() is SqlException sql && IsTransient(sql))
        {
            token.ThrowIfCancellationRequested();
            logger.LogWarning("An availability transaction failed transiently for organization {OrganizationId}", organization);
            throw new SecurityRequestException(503, "Availability could not be updated. Try again.", "availability_database_busy");
        }
    }

    internal static SecurityRequestException Stale()
        => new(409, "This record changed. Reload it and try again.", "availability_stale");

    private async Task LockAsync(Guid organization, Guid property, CancellationToken token)
    {
        var result = await repository.AcquireAsync(organization, property, options.Value.LockTimeoutMilliseconds, token);
        if (result is 0 or 1) return;

        logger.LogWarning("Availability lock failed for organization {OrganizationId} and property {PropertyId} with {Result}",
            organization, property, result);
        token.ThrowIfCancellationRequested();
        throw result switch
        {
            -1 => new SecurityRequestException(503, "Availability is busy. Try again.", "availability_lock_timeout"),
            -2 => new SecurityRequestException(503, "Availability lock was cancelled. Try again.", "availability_lock_cancelled"),
            -3 => new SecurityRequestException(503, "Availability is busy. Try again.", "availability_lock_deadlock"),
            _ => new SecurityRequestException(503, "Availability could not be locked.", "availability_lock_failed")
        };
    }

    private static bool IsTransient(SqlException exception)
        => exception.IsTransient || exception.Number is -2 or 1205 or 1222 or 51003;
}
