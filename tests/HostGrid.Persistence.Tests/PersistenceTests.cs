using HostGrid.Api.Data;
using HostGrid.Api.Data.Seed;
using HostGrid.Api.Entities.Booking;
using HostGrid.Api.Entities.Expense;
using HostGrid.Api.Entities.Finance;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Entities.Operation;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Persistence.Tests;

public sealed class PersistenceTests(SqlServerFixture database) : IClassFixture<SqlServerFixture>
{
    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task InitialMigrationCreatesAllModuleSchemasAndSeedsReferenceData()
    {
        await using var context = database.CreateContext();
        await context.Database.MigrateAsync(Token);

        var migrations = await context.Database.GetAppliedMigrationsAsync(Token);
        Assert.Contains(migrations, migration => migration.EndsWith("_InitialHostGrid", StringComparison.Ordinal));
        var schemas = await context.Database.SqlQueryRaw<string>("""
            SELECT DISTINCT s.name AS [Value]
            FROM sys.tables t JOIN sys.schemas s ON t.schema_id = s.schema_id
            WHERE t.name <> '__EFMigrationsHistory'
            """).ToListAsync(Token);
        Assert.Equal(
            new[] { "booking", "communication", "document", "expense", "finance", "identity",
                "operation", "organization", "property", "reporting", "system" },
            schemas.Order());
        Assert.Equal(3, await context.Roles.CountAsync(Token));
        Assert.Equal(6, await context.Set<PropertyType>().CountAsync(Token));
        var tableCount = await context.Database.SqlQueryRaw<int>(
            "SELECT COUNT(*) AS [Value] FROM sys.tables WHERE name <> '__EFMigrationsHistory'")
            .SingleAsync(Token);
        Assert.Equal(67, tableCount);
    }

    [Fact]
    public async Task DemoSeedCanBeRerunWithoutCreatingAccountsOrDuplicateCategories()
    {
        await using var context = database.CreateDemoContext();
        var existingUserCount = await context.Users.CountAsync(Token);
        DemoData.Seed(context);
        await DemoData.SeedAsync(context, Token);

        Assert.Single(await context.Set<Organization>().ToListAsync(Token));
        Assert.Single(await context.Set<OrganizationSettings>().ToListAsync(Token));
        Assert.Equal(12, await context.Set<ExpenseCategory>().CountAsync(Token));
        Assert.Equal(existingUserCount, await context.Users.CountAsync(Token));
    }

    [Fact]
    public async Task MissingTenantSeesNoRowsAndCannotWrite()
    {
        var tenant = await CreateTenantAsync();
        await using var context = database.CreateContext();
        Assert.Empty(await context.Set<Organization>().ToListAsync(Token));
        Assert.Empty(await context.Set<Property>().ToListAsync(Token));

        context.Add(new Guest { OrganizationId = tenant.OrganizationId, Name = "Unscoped" });
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync(Token));
    }

    [Fact]
    public async Task AuthorizedTenantSeesOwnRowsOnlyAndCannotWriteForAnotherTenant()
    {
        var first = await CreateTenantAsync();
        var second = await CreateTenantAsync();
        await using var context = database.CreateContext(first.OrganizationId);
        Assert.Equal(first.PropertyId, (await context.Set<Property>().SingleAsync(Token)).Id);
        Assert.Equal(first.OrganizationId, (await context.Set<Organization>().SingleAsync(Token)).Id);
        context.Add(new Guest { OrganizationId = second.OrganizationId, Name = "Other tenant" });
        await Assert.ThrowsAsync<InvalidOperationException>(() => context.SaveChangesAsync(Token));
    }

    [Fact]
    public async Task CompositeForeignKeyRejectsForeignTenantProperty()
    {
        var first = await CreateTenantAsync();
        var second = await CreateTenantAsync();
        await using var context = database.CreateContext(first.OrganizationId);
        context.Add(new CalendarBlock
        {
            OrganizationId = first.OrganizationId, PropertyId = second.PropertyId, Reason = "Test",
            StartDate = new(2026, 11, 1), EndDate = new(2026, 11, 2)
        });
        await AssertSqlErrorAsync(context, 547);
    }

    [Fact]
    public async Task RequiredFieldIsEnforcedBySqlServer()
    {
        var tenant = await CreateTenantAsync();
        await using var context = database.CreateContext(tenant.OrganizationId);
        context.Add(new Guest { OrganizationId = tenant.OrganizationId });
        await AssertSqlErrorAsync(context, 515);
    }

    [Fact]
    public async Task AmountsAndRatesRoundTripWithSpecifiedPrecision()
    {
        var tenant = await CreateTenantAsync();
        await using var context = database.CreateContext(tenant.OrganizationId);
        var reservation = await AddReservationAsync(context, tenant);
        reservation.NetPlatformRevenue = 14000.37m;
        reservation.CommissionRateSnapshot = 20.01m;
        reservation.CommissionAmount = 2801.47m;
        await context.SaveChangesAsync(Token);
        context.ChangeTracker.Clear();

        var saved = await context.Set<Reservation>().SingleAsync(Token);
        Assert.Equal(14000.37m, saved.NetPlatformRevenue);
        Assert.Equal(20.01m, saved.CommissionRateSnapshot);
        Assert.Equal(2801.47m, saved.CommissionAmount);
        Assert.Null(saved.GrossAmount);
        Assert.Null(saved.PlatformFees);

        var scales = await context.Database.SqlQueryRaw<string>("""
            SELECT CONCAT(c.name, ':', c.precision, ',', c.scale) AS [Value]
            FROM sys.columns c
            WHERE c.object_id = OBJECT_ID(N'booking.Reservation')
                AND c.name IN ('NetPlatformRevenue', 'CommissionRateSnapshot')
            """).ToListAsync(Token);
        Assert.Contains("NetPlatformRevenue:18,2", scales);
        Assert.Contains("CommissionRateSnapshot:5,2", scales);
    }

    [Fact]
    public async Task DirectNetModeRejectsGrossAndFees()
    {
        var tenant = await CreateTenantAsync();
        await using var context = database.CreateContext(tenant.OrganizationId);
        var reservation = await AddReservationAsync(context, tenant);
        reservation.GrossAmount = 16000m;
        reservation.PlatformFees = 2000m;
        await AssertSqlErrorAsync(context, 547);
    }

    [Fact]
    public async Task UniqueMonthlyClosureKeyRejectsDuplicatePeriod()
    {
        var tenant = await CreateTenantAsync();
        await using var context = database.CreateContext(tenant.OrganizationId);
        context.Add(CreateClosure(tenant));
        await context.SaveChangesAsync(Token);
        context.Add(CreateClosure(tenant));
        await AssertSqlErrorAsync(context, 2601, 2627);
    }

    [Fact]
    public async Task ClosedPeriodRejectsUpdatesAndAdditionalLines()
    {
        var tenant = await CreateTenantAsync();
        await using var context = database.CreateContext(tenant.OrganizationId);
        var closure = CreateClosure(tenant);
        context.Add(closure);
        await context.SaveChangesAsync(Token);
        closure.Status = ClosureStatus.Closed;
        closure.ClosedById = tenant.UserId;
        closure.ClosedAt = DateTime.UtcNow;
        await context.SaveChangesAsync(Token);

        closure.PayoutDueAtClosing = 500m;
        await AssertSqlErrorAsync(context, 51001);

        context.ChangeTracker.Clear();
        context.Add(new MonthlyClosureLine
        {
            OrganizationId = tenant.OrganizationId, ClosureId = closure.Id,
            SourceId = Guid.NewGuid(), PeriodStart = new(2026, 11, 1), Amount = 100m
        });
        await AssertSqlErrorAsync(context, 51002);
    }

    [Fact]
    public async Task AutomaticCleaningIsUniquePerReservation()
    {
        var tenant = await CreateTenantAsync();
        await using var context = database.CreateContext(tenant.OrganizationId);
        var reservation = await AddReservationAsync(context, tenant);
        CleaningTask NewTask() => new()
        {
            OrganizationId = tenant.OrganizationId, PropertyId = tenant.PropertyId,
            ReservationId = reservation.Id, IsAutomatic = true
        };
        context.Add(NewTask());
        await context.SaveChangesAsync(Token);
        context.Add(NewTask());
        await AssertSqlErrorAsync(context, 2601, 2627);
    }

    [Fact]
    public async Task RecurringExpenseIsUniquePerTemplateAndOccurrence()
    {
        var tenant = await CreateTenantAsync();
        await using var context = database.CreateContext(tenant.OrganizationId);
        var category = new ExpenseCategory { OrganizationId = tenant.OrganizationId, Name = "Test category" };
        context.Add(category);
        await context.SaveChangesAsync(Token);
        var template = new RecurringExpenseTemplate
        {
            OrganizationId = tenant.OrganizationId, PropertyId = tenant.PropertyId, CategoryId = category.Id,
            Title = "Recurring", NextDueDate = new(2026, 11, 1)
        };
        context.Add(template);
        await context.SaveChangesAsync(Token);
        Expense NewExpense(string reference) => new()
        {
            OrganizationId = tenant.OrganizationId, PropertyId = tenant.PropertyId, CategoryId = category.Id,
            RecurringTemplateId = template.Id, OccurrenceDate = new(2026, 11, 1),
            ExpenseDate = new(2026, 11, 1), Title = "Recurring", Reference = reference, ActorId = tenant.UserId
        };
        context.Add(NewExpense("TEST-1"));
        await context.SaveChangesAsync(Token);
        context.Add(NewExpense("TEST-2"));
        await AssertSqlErrorAsync(context, 2601, 2627);
    }

    [Fact]
    public async Task FinancialTransactionRejectsDuplicateIdempotencyKey()
    {
        var tenant = await CreateTenantAsync();
        await using var context = database.CreateContext(tenant.OrganizationId);
        FinancialTransaction NewTransaction() => new()
        {
            OrganizationId = tenant.OrganizationId, SourceId = Guid.NewGuid(), Amount = 100m,
            OccurredOn = new(2026, 11, 1), IdempotencyKey = "TEST-RECEIPT"
        };
        context.Add(NewTransaction());
        await context.SaveChangesAsync(Token);
        context.Add(NewTransaction());
        await AssertSqlErrorAsync(context, 2601, 2627);
    }

    private async Task<Tenant> CreateTenantAsync()
    {
        var organizationId = Guid.NewGuid();
        await using var context = database.CreateContext(organizationId);
        var user = new ApplicationUser { UserName = organizationId.ToString("N"), NormalizedUserName = organizationId.ToString("N") };
        context.AddRange(new Organization
        {
            Id = organizationId, Name = "Test organization", Reference = organizationId.ToString("N")
        }, user);
        await context.SaveChangesAsync(Token);
        context.Add(new OrganizationMember { OrganizationId = organizationId, UserId = user.Id });
        await context.SaveChangesAsync(Token);
        var owner = new Owner { OrganizationId = organizationId, UserId = user.Id, Name = "Test owner", Reference = "OWNER-1" };
        context.Add(owner);
        await context.SaveChangesAsync(Token);
        var typeId = await context.Set<PropertyType>().Where(type => type.Code == "Studio").Select(type => type.Id).SingleAsync(Token);
        var property = new Property
        {
            OrganizationId = organizationId, OwnerId = owner.Id, TypeId = typeId, Name = "Test property",
            Reference = "PROPERTY-1", Address = "Test address", City = "Test city", MaxGuests = 2
        };
        context.Add(property);
        await context.SaveChangesAsync(Token);
        return new Tenant(organizationId, user.Id, owner.Id, property.Id);
    }

    private static async Task<Reservation> AddReservationAsync(HostGridDbContext context, Tenant tenant)
    {
        var guest = new Guest { OrganizationId = tenant.OrganizationId, Name = "Test guest" };
        context.Add(guest);
        await context.SaveChangesAsync(Token);
        var reservation = new Reservation
        {
            OrganizationId = tenant.OrganizationId, PropertyId = tenant.PropertyId, GuestId = guest.Id,
            Reference = "BOOKING-1", Adults = 1, CheckInDate = new(2026, 11, 1), CheckOutDate = new(2026, 11, 2),
            RevenueInputMode = RevenueInputMode.NetDirect, NetPlatformRevenue = 14000m,
            CommissionRateSnapshot = 20m, CommissionAmount = 2800m
        };
        context.Add(reservation);
        await context.SaveChangesAsync(Token);
        return reservation;
    }

    private static MonthlyClosure CreateClosure(Tenant tenant) => new()
    {
        OrganizationId = tenant.OrganizationId, PropertyId = tenant.PropertyId, OwnerId = tenant.OwnerId,
        Year = 2026, Month = 11, NetPlatformRevenue = 14000m, CommissionAmount = 2800m,
        OwnerExpenses = 100m, OwnerProfit = 11100m
    };

    private static async Task AssertSqlErrorAsync(HostGridDbContext context, params int[] errorNumbers)
    {
        var error = await Assert.ThrowsAsync<DbUpdateException>(() => context.SaveChangesAsync(Token));
        var sqlError = Assert.IsType<SqlException>(error.InnerException);
        Assert.Contains(sqlError.Number, errorNumbers);
    }

    private sealed record Tenant(Guid OrganizationId, Guid UserId, Guid OwnerId, Guid PropertyId);
}
