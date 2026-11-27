using System.Data.Common;
using System.Net;
using System.Security.Claims;
using HostGrid.Api.DTOs.Auth;
using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.Entities.Booking;
using HostGrid.Api.Entities.Finance;
using HostGrid.Api.Entities.Organization;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Entities.System;
using HostGrid.Api.Enums;
using HostGrid.Api.Security;
using HostGrid.Api.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HostGrid.Persistence.Tests;

public sealed partial class AuthenticationTests
{
    private static readonly DateOnly StayStart = new(2032, 2, 27);

    [Fact]
    public async Task AvailabilityMigrationPreservesExistingReservationAndCommissionValues()
    {
        const string previous = "20261126200005_PropertyInventoryNotes";
        var oldCatalog = new SqlServerFixture(previous);
        try
        {
            await oldCatalog.InitializeAsync();
            using var app = new Application(oldCatalog.ConnectionString);
            var manager = await AddAccountToCatalogAsync(app, oldCatalog, OrganizationRole.Manager);
            var owner = await AddAccountToCatalogAsync(app, oldCatalog, OrganizationRole.Owner, manager.OrganizationId);
            var id = Guid.NewGuid();
            await using var db = oldCatalog.CreateContext(manager.OrganizationId);
            var guest = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [booking].[Guest] ([Id], [OrganizationId], [Name])
                VALUES ({guest}, {manager.OrganizationId}, N'Migration fixture guest');
                """, Token);
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [booking].[Reservation] ([Id], [OrganizationId], [PropertyId], [GuestId], [Reference], [Source],
                    [CheckInDate], [CheckOutDate], [Adults], [Children], [RevenueInputMode], [NetPlatformRevenue],
                    [CommissionRateSnapshot], [CommissionAmount], [Status], [PaymentStatus])
                VALUES ({id}, {manager.OrganizationId}, {owner.PropertyId}, {guest}, N'MIGRATION-BOOKING',
                    2, '2025-12-31', '2026-01-02', 1, 0, 2, 1000, 23.25, 232.50, 1, 0);
                """, Token);
            await db.Database.MigrateAsync("20261127133709_BookingAvailabilityFoundation", Token);
            var reservation = await db.Set<Reservation>().Where(row => row.Id == id)
                .Select(row => new { row.CommissionRateSnapshot, row.CommissionAmount, row.CheckInDate, row.CheckOutDate,
                    row.AvailabilityReleasedFrom, row.AvailabilityReleasedAt, row.AvailabilityReleasedBy }).SingleAsync(Token);
            Assert.Equal(23.25m, reservation.CommissionRateSnapshot);
            Assert.Equal(232.50m, reservation.CommissionAmount);
            Assert.Equal(new DateOnly(2025, 12, 31), reservation.CheckInDate);
            Assert.Equal(new DateOnly(2026, 1, 2), reservation.CheckOutDate);
            Assert.Null(reservation.AvailabilityReleasedFrom);
            Assert.Null(reservation.AvailabilityReleasedAt);
            Assert.Null(reservation.AvailabilityReleasedBy);
            Assert.Equal(6, (await db.Database.GetAppliedMigrationsAsync(Token)).Count());
        }
        finally { await oldCatalog.DisposeAsync(); }
    }

    [Theory]
    [InlineData(0, 3, false)] // identical and crossing February 29
    [InlineData(2, 4, false)]
    [InlineData(1, 2, false)]
    [InlineData(-1, 4, false)]
    [InlineData(-1, 0, true)]
    [InlineData(3, 4, true)]
    public async Task AvailabilityUsesHalfOpenSqlIntervals(int start, int end, bool available)
    {
        using var app = new Application(database.ConnectionString);
        var fixture = await SeedAvailabilityAsync(app);
        await BookingScope(app, fixture.Manager, service => service.HoldAsync(fixture.Property, Pending(fixture), Token));
        await BookingScope(app, fixture.Manager, async service =>
        {
            var value = await service.QueryAsync(fixture.Property, Dates(start, end), Token);
            Assert.Equal(available, value.Available);
            Assert.Equal(available ? "available" : "booking_conflict", value.Status);
            return value;
        });
    }

    [Fact]
    public async Task AvailabilityEndpointScopesReadsAndReturnsOnlyConflictCategory()
    {
        using var app = new Application(database.ConnectionString);
        var fixture = await SeedAvailabilityAsync(app);
        var other = await AddAccountAsync(app, OrganizationRole.Owner, fixture.Manager.OrganizationId);
        var foreign = await AddAccountAsync(app, OrganizationRole.Owner);
        var admin = await AddAccountAsync(app, superAdmin: true);
        var org = fixture.Manager.OrganizationId;

        await BookingScope(app, fixture.Manager, service => service.HoldAsync(fixture.Owner.PropertyId, Pending(fixture), Token));
        using var client = Client(app);
        var (manager, _) = await EnrollAsync(client, fixture.Manager);
        var (owner, _) = await LoginAsync(client, fixture.Owner);
        var (differentOwner, _) = await LoginAsync(client, other);
        var (differentTenant, _) = await LoginAsync(client, foreign);
        var (adminSession, _) = await EnrollAsync(client, admin);
        var path = Portfolio(org, $"properties/{fixture.Owner.PropertyId}/availability");
        var range = "?checkInDate=2032-02-27&checkOutDate=2032-03-01";
        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, path + range, null));
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, path + range, differentOwner.AccessToken));
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, path + range, differentTenant.AccessToken));
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client,
            path.Replace(org.ToString(), Guid.NewGuid().ToString()) + range, adminSession.AccessToken));
        foreach (var bearer in new[] { manager.AccessToken, owner.AccessToken, adminSession.AccessToken })
        {
            using var response = await Send(client, HttpMethod.Get, path + range + "&excludeReservationId=" +
                await ReservationIdAsync(fixture, fixture.Owner.PropertyId), bearer);
            using var payload = System.Text.Json.JsonDocument.Parse(await response.Content.ReadAsStringAsync(Token));
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("booking_conflict", payload.RootElement.GetProperty("status").GetString());
            Assert.Equal(4, payload.RootElement.EnumerateObject().Count());
            Assert.True(response.Headers.CacheControl!.NoStore);
        }

        foreach (var bad in new[] { "", "?checkInDate=2032-02-29&checkOutDate=2032-02-29",
            "?checkInDate=2032-03-01&checkOutDate=2032-02-29" })
        {
            using var response = await Send(client, HttpMethod.Get, path + bad, manager.AccessToken);
            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
            Assert.Contains("invalid_date_range", await response.Content.ReadAsStringAsync(Token));
        }
        using var write = await Send(client, HttpMethod.Post, path, owner.AccessToken, new { });
        Assert.Equal(HttpStatusCode.MethodNotAllowed, write.StatusCode);
        await BookingScope(app, fixture.Owner, async service =>
        {
            var error = await Assert.ThrowsAsync<SecurityRequestException>(
                () => service.HoldAsync(fixture.Owner.PropertyId, Pending(fixture), Token));
            Assert.Equal(403, error.Status);
            return true;
        }, CurrentUserService.Owner, fixture.OwnerId);
    }

    [Fact]
    public async Task BookingAndBlockStatusesProtectHistoricAndUnreleasedNights()
    {
        using var app = new Application(database.ConnectionString);
        var fixture = await SeedAvailabilityAsync(app);
        var past = new StayDates(new(2025, 12, 31), new(2026, 1, 2));
        var completed = await InsertReservationAsync(fixture, past, ReservationStatus.CheckedOut, actual: true);
        var pending = await InsertReservationAsync(fixture, Dates(0, 3), ReservationStatus.Pending);
        var confirmed = await InsertReservationAsync(fixture, Dates(4, 7), ReservationStatus.Confirmed);
        var checkedIn = await InsertReservationAsync(fixture, Dates(8, 11), ReservationStatus.CheckedIn, actual: true);
        var cancelled = await InsertReservationAsync(fixture, Dates(12, 15), ReservationStatus.Cancelled);
        var today = PropertyAvailability.BusinessDate(DateTimeOffset.UtcNow);
        var noShow = await InsertReservationAsync(fixture, new(today.AddDays(-1), today.AddDays(2)),
            ReservationStatus.NoShow, property: fixture.Owner.PropertyId);
        var early = await InsertReservationAsync(fixture, new(today.AddDays(-2), today.AddDays(3)),
            ReservationStatus.CheckedOut, actual: true);
        await BookingScope(app, fixture.Manager, async service =>
        {
            foreach (var row in new[] { completed, pending, confirmed, checkedIn, noShow, early })
                Assert.False((await service.QueryAsync(row.PropertyId, new(row.CheckInDate, row.CheckOutDate), Token)).Available);
            Assert.True((await service.QueryAsync(fixture.Property, new(cancelled.CheckInDate, cancelled.CheckOutDate), Token)).Available);
            Assert.Equal("availability_release_invalid", (await Assert.ThrowsAsync<SecurityRequestException>(
                () => service.ReleaseAsync(early.Id, early.RowVersion, early.CheckInDate, Token))).Code);
            await service.ReleaseAsync(noShow.Id, noShow.RowVersion, today, Token);
            await service.ReleaseAsync(early.Id, early.RowVersion, today.AddDays(1), Token);
            Assert.False((await service.QueryAsync(noShow.PropertyId, new(noShow.CheckInDate, today), Token)).Available);
            Assert.True((await service.QueryAsync(noShow.PropertyId, new(today, noShow.CheckOutDate), Token)).Available);
            Assert.False((await service.QueryAsync(fixture.Property, new(today, today.AddDays(1)), Token)).Available);
            var released = new StayDates(today.AddDays(1), early.CheckOutDate);
            Assert.True((await service.QueryAsync(fixture.Property, released, Token)).Available);
            await service.SaveBlockAsync(fixture.Property, null, null, released, "Manual block", Token);
            Assert.Equal("calendar_block_conflict", (await service.QueryAsync(fixture.Property, released, Token)).Status);
            return true;
        });
        await using var context = database.CreateContext(fixture.Manager.OrganizationId);
        var preserved = await context.Set<Reservation>().SingleAsync(row => row.Id == early.Id, Token);
        Assert.Equal(early.CheckOutDate, preserved.CheckOutDate);
        Assert.Equal(early.ActualCheckOutAt, preserved.ActualCheckOutAt);
        Assert.Equal(early.CommissionRateSnapshot, preserved.CommissionRateSnapshot);
        Assert.Equal(early.CommissionAmount, preserved.CommissionAmount);
        Assert.Equal(fixture.Manager.UserId, preserved.AvailabilityReleasedBy);
        Assert.NotNull(preserved.AvailabilityReleasedAt);
        Assert.Equal(2, await context.Set<AuditLog>().CountAsync(row => row.Action == "reservation_nights_released", Token));
    }

    [Theory]
    [InlineData("reservation", "reservation", "booking_conflict")]
    [InlineData("reservation", "block", "booking_conflict")]
    [InlineData("block", "reservation", "calendar_block_conflict")]
    [InlineData("block", "block", "calendar_block_conflict")]
    [InlineData("edit", "edit", "booking_conflict")]
    public async Task CompetingWritesWaitForTransactionThenOnlyOneIntervalWins(string first, string second, string conflict)
    {
        var gate = new SaveGate();
        var observer = new LockObserver();
        using var app = new Application(database.ConnectionString, bookingInterceptors: [gate, observer]);
        var fixture = await SeedAvailabilityAsync(app);
        var initial = first == "edit" ? new[]
        {
            await InsertReservationAsync(fixture, Dates(5, 6), ReservationStatus.Pending),
            await InsertReservationAsync(fixture, Dates(8, 9), ReservationStatus.Pending)
        } : [];
        Task WriteAsync(string kind, int index)
            => BookingScope(app, fixture.Manager, async service =>
            {
                if (kind == "edit")
                    await service.ReviseAsync(initial[index].Id, fixture.Property, initial[index].RowVersion, Dates(0, 3), 1, 0, Token);
                else if (kind == "reservation")
                    await service.HoldAsync(fixture.Property, Pending(fixture), Token);
                else
                    await service.SaveBlockAsync(fixture.Property, null, null, Dates(0, 3), "Race block", Token);
                return true;
            });

        gate.Arm();
        var winner = Task.Run(() => WriteAsync(first, 0), Token);
        Task? loser = null;
        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
            observer.Arm();
            loser = Task.Run(() => WriteAsync(second, 1), Token);
            await observer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
            Assert.False(loser.IsCompleted);
        }
        finally { gate.Continue.TrySetResult(); }
        await winner;
        var problem = await Assert.ThrowsAsync<SecurityRequestException>(() => loser!);
        Assert.Equal(conflict, problem.Code);
        Assert.Equal(409, problem.Status);
        await using var db = database.CreateContext(fixture.Manager.OrganizationId);
        var reservations = await db.Set<Reservation>().CountAsync(row => row.PropertyId == fixture.Property &&
            row.CheckInDate < StayStart.AddDays(3) && row.CheckOutDate > StayStart, Token);
        var blocks = await db.Set<CalendarBlock>().CountAsync(row => row.PropertyId == fixture.Property, Token);
        Assert.Equal(1, reservations + blocks);
    }

    [Fact]
    public async Task LockTimeoutAndCancellationRollBackAndDifferentPropertiesProceed()
    {
        var gate = new SaveGate();
        var observer = new LockObserver();
        using var app = new Application(database.ConnectionString, availabilityLockTimeout: 800, bookingInterceptors: [gate, observer]);
        var fixture = await SeedAvailabilityAsync(app);
        gate.Arm();
        var first = Task.Run(() => BookingScope(app, fixture.Manager,
            service => service.HoldAsync(fixture.Property, Pending(fixture), Token)), Token);
        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
            await BookingScope(app, fixture.Manager, service => service.HoldAsync(fixture.Owner.PropertyId, Pending(fixture), Token));
            var timeout = await Assert.ThrowsAsync<SecurityRequestException>(() => BookingScope(app, fixture.Manager,
                service => service.HoldAsync(fixture.Property, Pending(fixture), Token)));
            Assert.Equal("availability_lock_timeout", timeout.Code);
            Assert.Equal(503, timeout.Status);

            using var cancelled = CancellationTokenSource.CreateLinkedTokenSource(Token);
            observer.Arm();
            var waiting = Task.Run(() => BookingScope(app, fixture.Manager,
                service => service.HoldAsync(fixture.Property, Pending(fixture, Dates(3, 4)), cancelled.Token)), Token);
            await observer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
            await cancelled.CancelAsync();
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() => waiting);
        }
        finally { gate.Continue.TrySetResult(); }
        await first;
        await BookingScope(app, fixture.Manager,
            service => service.HoldAsync(fixture.Property, Pending(fixture, Dates(3, 4)), Token));
        await using var db = database.CreateContext(fixture.Manager.OrganizationId);
        Assert.Equal(3, await db.Set<Reservation>().CountAsync(Token));
    }

    [Fact]
    public async Task FailedSaveReleasesLockAndRetryDoesNotDuplicateRows()
    {
        using var app = new Application(database.ConnectionString, bookingInterceptors: [new FailSaveOnce()]);
        var fixture = await SeedAvailabilityAsync(app);
        var request = Pending(fixture);
        await Assert.ThrowsAsync<DbUpdateException>(() => BookingScope(app, fixture.Manager,
            service => service.HoldAsync(fixture.Property, request, Token)));
        await using var db = database.CreateContext(fixture.Manager.OrganizationId);
        Assert.Empty(await db.Set<Reservation>().ToArrayAsync(Token));
        Assert.Empty(await db.Set<AuditLog>().Where(row => row.Action == "reservation_hold_created").ToArrayAsync(Token));
        await BookingScope(app, fixture.Manager, service => service.HoldAsync(fixture.Property, request, Token));
        var row = await db.Set<Reservation>().SingleAsync(Token);
        Assert.Equal(request.Reference, row.Reference);
        Assert.Null(row.CommissionRateSnapshot);
        Assert.Null(row.CommissionAmount);
        Assert.Single(await db.Set<AuditLog>().Where(audit => audit.Action == "reservation_hold_created").ToArrayAsync(Token));
    }

    [Fact]
    public async Task PendingHoldWaitsForManagerReleaseThenCanTakeTheReleasedNights()
    {
        var gate = new SaveGate();
        var observer = new LockObserver();
        using var app = new Application(database.ConnectionString, bookingInterceptors: [gate, observer]);
        var fixture = await SeedAvailabilityAsync(app);
        var today = PropertyAvailability.BusinessDate(DateTimeOffset.UtcNow);
        var dates = new StayDates(today, today.AddDays(3));
        var noShow = await InsertReservationAsync(fixture, dates, ReservationStatus.NoShow);
        gate.Arm();
        var release = Task.Run(() => BookingScope(app, fixture.Manager, async service =>
        {
            await service.ReleaseAsync(noShow.Id, noShow.RowVersion, today, Token);
            return true;
        }), Token);
        Task<Guid>? hold = null;
        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
            observer.Arm();
            hold = Task.Run(() => BookingScope(app, fixture.Manager,
                service => service.HoldAsync(fixture.Property, Pending(fixture, dates), Token)), Token);
            await observer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
            Assert.False(hold.IsCompleted);
        }
        finally { gate.Continue.TrySetResult(); }
        await release;
        var holdId = await hold!;
        await using var db = database.CreateContext(fixture.Manager.OrganizationId);
        var rows = await db.Set<Reservation>().ToDictionaryAsync(row => row.Id, Token);
        Assert.Equal(2, rows.Count);
        Assert.Equal(today, rows[noShow.Id].AvailabilityReleasedFrom);
        Assert.Equal(noShow.CheckOutDate, rows[noShow.Id].CheckOutDate);
        Assert.Equal(ReservationStatus.Pending, rows[holdId].Status);
    }

    [Fact]
    public async Task EfExecutionStrategyRepeatsTheCompleteTransactionWithoutDuplicatingHold()
    {
        var observer = new LockObserver();
        using var app = new Application(database.ConnectionString, bookingInterceptors: [observer, new RetrySaveOnce()],
            retryAvailability: true);
        var fixture = await SeedAvailabilityAsync(app);
        var id = await BookingScope(app, fixture.Manager, service => service.HoldAsync(fixture.Property, Pending(fixture), Token));
        await using var db = database.CreateContext(fixture.Manager.OrganizationId);
        Assert.Equal(2, observer.Acquisitions);
        Assert.Equal(id, (await db.Set<Reservation>().SingleAsync(Token)).Id);
        Assert.Single(await db.Set<AuditLog>().Where(row => row.ResourceId == id).ToArrayAsync(Token));
    }

    [Theory]
    [InlineData(false, "guest_capacity_invalid")]
    [InlineData(true, "property_not_bookable")]
    public async Task BookingAdmissionReloadsAfterConcurrentPropertyChange(bool archive, string expected)
    {
        var gate = new SaveGate(beforeWrite: true);
        var observer = new LockObserver();
        using var app = new Application(database.ConnectionString, bookingInterceptors: [gate, observer]);
        var fixture = await SeedAvailabilityAsync(app);
        gate.Arm();
        var update = Task.Run(() => BookingScope(app, fixture.Manager, async service =>
        {
            await service.UpdatePropertyAsync(fixture.Property, property =>
            {
                if (archive) property.Status = PropertyStatus.Archived;
                else property.MaxGuests = 1;
                return Task.CompletedTask;
            }, Token);
            return true;
        }), Token);
        Task? booking = null;
        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
            observer.Arm();
            booking = Task.Run(() => BookingScope(app, fixture.Manager, service =>
            {
                var request = Pending(fixture);
                request.Adults = 2;
                return service.HoldAsync(fixture.Property, request, Token);
            }), Token);
            await observer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
        }
        finally { gate.Continue.TrySetResult(); }

        await update;
        Assert.Equal(expected, (await Assert.ThrowsAsync<SecurityRequestException>(() => booking!)).Code);
        await using var db = database.CreateContext(fixture.Manager.OrganizationId);
        Assert.Empty(await db.Set<Reservation>().ToArrayAsync(Token));
    }

    [Fact]
    public async Task OppositePendingMovesUseOneLockOrderAndFailedMovesKeepOriginalHolds()
    {
        using var app = new Application(database.ConnectionString);
        var fixture = await SeedAvailabilityAsync(app);
        var from = await InsertReservationAsync(fixture, Dates(0, 3), ReservationStatus.Pending);
        var to = await InsertReservationAsync(fixture, Dates(0, 3), ReservationStatus.Pending, property: fixture.Owner.PropertyId);
        var moves = new[] { Move(from, to.PropertyId), Move(to, from.PropertyId) };
        foreach (var error in await Task.WhenAll(moves))
            Assert.Equal("booking_conflict", error.Code);

        await BookingScope(app, fixture.Manager, async service =>
        {
            Assert.False((await service.QueryAsync(from.PropertyId, Dates(0, 3), Token)).Available);
            Assert.False((await service.QueryAsync(to.PropertyId, Dates(0, 3), Token)).Available);
            await service.ReviseAsync(from.Id, to.PropertyId, from.RowVersion, Dates(3, 4), 1, 0, Token);
            Assert.True((await service.QueryAsync(from.PropertyId, Dates(0, 3), Token)).Available);
            Assert.False((await service.QueryAsync(to.PropertyId, Dates(3, 4), Token)).Available);
            var stale = await Assert.ThrowsAsync<SecurityRequestException>(
                () => service.ReviseAsync(from.Id, to.PropertyId, from.RowVersion, Dates(4, 5), 1, 0, Token));
            Assert.Equal("availability_stale", stale.Code);
            return true;
        });

        Task<SecurityRequestException> Move(Reservation row, Guid property) => Task.Run(() => BookingScope(app, fixture.Manager,
            service => Assert.ThrowsAsync<SecurityRequestException>(
                () => service.ReviseAsync(row.Id, property, row.RowVersion, Dates(0, 3), 1, 0, Token))), Token);
    }

    [Fact]
    public async Task AdmissionChecksStatusCapacityClosedPeriodsAndPreservesCommissionOnEdits()
    {
        using var app = new Application(database.ConnectionString);
        var fixture = await SeedAvailabilityAsync(app);
        await using var db = database.CreateContext(fixture.Manager.OrganizationId);
        var property = await db.Set<Property>().SingleAsync(row => row.Id == fixture.Property, Token);
        property.OperationalStatus = OperationalStatus.NeedsCleaning;
        await db.SaveChangesAsync(Token);
        await BookingScope(app, fixture.Manager, async service =>
        {
            Assert.True((await service.QueryAsync(property.Id, Dates(0, 3), Token)).Available);
            var tooMany = Pending(fixture);
            tooMany.Children = property.MaxGuests;
            Assert.Equal("guest_capacity_invalid", (await Assert.ThrowsAsync<SecurityRequestException>(
                () => service.HoldAsync(property.Id, tooMany, Token))).Code);
            var today = PropertyAvailability.BusinessDate(DateTimeOffset.UtcNow);
            await service.HoldAsync(property.Id, Pending(fixture, new(today, today.AddDays(1))), Token);
            return true;
        });
        foreach (var status in new[] { PropertyStatus.Inactive, PropertyStatus.Archived })
        {
            property.Status = status;
            await db.SaveChangesAsync(Token);
            await BookingScope(app, fixture.Manager, async service =>
            {
                Assert.Equal("property_not_bookable", (await service.QueryAsync(property.Id, Dates(0, 3), Token)).Status);
                Assert.Equal("property_not_bookable", (await Assert.ThrowsAsync<SecurityRequestException>(
                    () => service.HoldAsync(property.Id, Pending(fixture), Token))).Code);
                return true;
            });
        }
        property.Status = PropertyStatus.Active;
        db.Add(new MonthlyClosure { OrganizationId = property.OrganizationId, OwnerId = fixture.OwnerId,
            PropertyId = property.Id, Year = 2025, Month = 12, Status = ClosureStatus.Closed,
            ClosedAt = DateTime.UtcNow, ClosedById = fixture.Manager.UserId });
        await db.SaveChangesAsync(Token);
        var confirmed = await InsertReservationAsync(fixture, Dates(0, 3), ReservationStatus.Confirmed);
        await BookingScope(app, fixture.Manager, async service =>
        {
            Assert.Equal("financial_period_closed", (await Assert.ThrowsAsync<SecurityRequestException>(() =>
                service.HoldAsync(property.Id, Pending(fixture, new(new(2025, 12, 31), new(2026, 1, 1))), Token))).Code);
            Assert.Equal("reservation_move_not_allowed", (await Assert.ThrowsAsync<SecurityRequestException>(() =>
                service.ReviseAsync(confirmed.Id, fixture.Owner.PropertyId, confirmed.RowVersion, Dates(0, 3), 1, 0, Token))).Code);
            await service.ReviseAsync(confirmed.Id, property.Id, confirmed.RowVersion, Dates(1, 4), 1, 0, Token);
            return true;
        });
        var updated = await db.Set<Reservation>().AsNoTracking().SingleAsync(row => row.Id == confirmed.Id, Token);
        Assert.Equal(confirmed.CommissionRateSnapshot, updated.CommissionRateSnapshot);
        Assert.Equal(confirmed.CommissionAmount, updated.CommissionAmount);
        Assert.Equal(StayStart.AddDays(1), updated.CheckInDate);
    }

    private async Task<BookingFixture> SeedAvailabilityAsync(Application app)
    {
        var manager = await AddAccountAsync(app, OrganizationRole.Manager);
        var owner = await AddAccountAsync(app, OrganizationRole.Owner, manager.OrganizationId);
        await using var db = database.CreateContext(manager.OrganizationId);
        var own = await db.Set<Property>().SingleAsync(row => row.Id == owner.PropertyId, Token);
        var property = new Property
        {
            OrganizationId = manager.OrganizationId, OwnerId = own.OwnerId, TypeId = own.TypeId, Name = "Availability test",
            Reference = Guid.NewGuid().ToString("N"), Address = "Fixture", City = "Fes", MaxGuests = 4
        };
        var guest = new Guest { OrganizationId = manager.OrganizationId, Name = "Fictitious guest", Email = "guest@example.test" };
        db.AddRange(property, guest);
        await db.SaveChangesAsync(Token);
        return new(manager, owner, own.OwnerId, property.Id, guest.Id);
    }

    private async Task<Guid> ReservationIdAsync(BookingFixture fixture, Guid property)
    {
        await using var db = database.CreateContext(fixture.Manager.OrganizationId);
        return await db.Set<Reservation>().Where(row => row.PropertyId == property).Select(row => row.Id).SingleAsync(Token);
    }

    private async Task<Reservation> InsertReservationAsync(BookingFixture fixture, StayDates dates,
        ReservationStatus status, bool actual = false, Guid? property = null)
    {
        await using var db = database.CreateContext(fixture.Manager.OrganizationId);
        var row = Pending(fixture, dates);
        row.OrganizationId = fixture.Manager.OrganizationId;
        row.PropertyId = property ?? fixture.Property;
        row.Status = status;
        row.CommissionRateSnapshot = 20m;
        row.CommissionAmount = 200m;
        if (actual)
        {
            row.ActualCheckInAt = dates.CheckInDate.ToDateTime(new(14, 0), DateTimeKind.Utc);
            if (status == ReservationStatus.CheckedOut)
                row.ActualCheckOutAt = dates.CheckInDate.AddDays(1).ToDateTime(new(9, 0), DateTimeKind.Utc);
        }
        db.Add(row);
        await db.SaveChangesAsync(Token);
        return row;
    }

    private static StayDates Dates(int start, int end) => new(StayStart.AddDays(start), StayStart.AddDays(end));
    private static Reservation Pending(BookingFixture fixture, StayDates? dates = null)
        => new() { GuestId = fixture.Guest, Reference = Guid.NewGuid().ToString("N"), Adults = 1,
            CheckInDate = (dates ?? Dates(0, 3)).CheckInDate, CheckOutDate = (dates ?? Dates(0, 3)).CheckOutDate,
            Source = BookingSource.Direct, RevenueInputMode = RevenueInputMode.NetDirect, NetPlatformRevenue = 1000m };

    private static async Task<T> BookingScope<T>(Application app, Account account, Func<PropertyAvailability, Task<T>> run,
        string role = CurrentUserService.Manager, Guid? owner = null)
    {
        await using var scope = app.Services.CreateAsyncScope();
        var tenant = scope.ServiceProvider.GetRequiredService<AuthenticatedTenantContext>();
        tenant.Authorize(account.OrganizationId, new MembershipView(account.OrganizationId, role, owner));
        var accessor = scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>();
        accessor.HttpContext = new DefaultHttpContext
        {
            RequestServices = scope.ServiceProvider,
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", account.UserId.ToString())], "Fixture"))
        };
        try { return await run(scope.ServiceProvider.GetRequiredService<PropertyAvailability>()); }
        finally { accessor.HttpContext = null; }
    }

    private sealed record BookingFixture(Account Manager, Account Owner, Guid OwnerId, Guid Property, Guid Guest);

    private sealed class SaveGate(bool beforeWrite = false) : SaveChangesInterceptor
    {
        private int armed;
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal TaskCompletionSource Continue { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal void Arm() => Interlocked.Exchange(ref armed, 1);

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            if (!beforeWrite) await WaitAsync(cancellationToken);
            return result;
        }

        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
            InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (beforeWrite) await WaitAsync(cancellationToken);
            return result;
        }

        private async Task WaitAsync(CancellationToken cancellationToken)
        {
            if (Interlocked.Exchange(ref armed, 0) == 1)
            {
                Entered.TrySetResult();
                await Continue.Task.WaitAsync(cancellationToken);
            }
        }
    }

    private sealed class LockObserver : DbCommandInterceptor
    {
        private int armed;
        private int acquisitions;
        internal int Acquisitions => Volatile.Read(ref acquisitions);
        internal TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        internal void Arm() => Interlocked.Exchange(ref armed, 1);

        public override ValueTask<InterceptionResult<int>> NonQueryExecutingAsync(DbCommand command,
            CommandEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            if (command.CommandText.Contains("sys.sp_getapplock", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref acquisitions);
                if (Interlocked.Exchange(ref armed, 0) == 1)
                    Entered.TrySetResult();
            }
            return ValueTask.FromResult(result);
        }
    }

    private sealed class FailSaveOnce : SaveChangesInterceptor
    {
        private int failed;

        public override ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            if (eventData.Context!.ChangeTracker.Entries<Reservation>().Any() && Interlocked.Exchange(ref failed, 1) == 0)
                throw new DbUpdateException("Fixture failure after SQL writes and before commit.");
            return ValueTask.FromResult(result);
        }
    }

    private sealed class RetrySaveOnce : SaveChangesInterceptor
    {
        private int failed;

        public override async ValueTask<int> SavedChangesAsync(SaveChangesCompletedEventData eventData, int result,
            CancellationToken cancellationToken = default)
        {
            var context = eventData.Context!;
            if (context.ChangeTracker.Entries<Reservation>().Any() && Interlocked.Exchange(ref failed, 1) == 0)
                await context.Database.ExecuteSqlRawAsync("THROW 51005, 'Fixture transient before commit.', 1;", cancellationToken);
            return result;
        }
    }
}
