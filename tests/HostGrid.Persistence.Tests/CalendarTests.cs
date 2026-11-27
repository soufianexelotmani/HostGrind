using System.Net;
using System.Text.Json;
using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.Entities.Booking;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Entities.System;
using HostGrid.Api.Enums;
using HostGrid.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Persistence.Tests;

public sealed partial class AuthenticationTests
{
    [Fact]
    public async Task CalendarSeparatesDisplayFiltersFromScopedAvailabilityAcrossYearBoundary()
    {
        using var app = new Application(database.ConnectionString);
        var fixture = await SeedAvailabilityAsync(app);
        var managerId = fixture.Manager.OrganizationId;
        var pending = await InsertReservationAsync(fixture, new(new(2031, 12, 28), new(2032, 1, 2)), ReservationStatus.Pending);
        var cancelled = await InsertReservationAsync(fixture, new(new(2032, 1, 2), new(2032, 1, 3)), ReservationStatus.Cancelled);
        await BookingScope(app, fixture.Manager, service => service.SaveBlockAsync(fixture.Property, null, null,
            new StayDates(new(2032, 1, 3), new(2032, 1, 8)), "Maintenance", Token));
        using var client = Client(app);
        var (manager, _) = await EnrollAsync(client, fixture.Manager);
        var url = Portfolio(managerId, "calendar") + "?startDate=2031-12-31&endDate=2032-01-04";
        var propertyUrl = url + $"&propertyId={fixture.Property}&ownerId={fixture.OwnerId}&search=Availability";

        var calendar = await GetCalendar(propertyUrl, manager.AccessToken);
        Assert.Equal("2031-12-28", calendar.GetProperty("reservations")[0].GetProperty("checkInDate").GetString());
        Assert.Equal(pending.Id, Assert.Single(calendar.GetProperty("reservations").EnumerateArray()).GetProperty("id").GetGuid());
        Assert.Equal("2032-01-08", calendar.GetProperty("blocks")[0].GetProperty("endDate").GetString());
        Assert.Contains("Fictitious guest", calendar.GetRawText());
        Assert.DoesNotContain("guest@example.test", calendar.GetRawText());

        var hidden = await GetCalendar(propertyUrl + "&bookingSource=Airbnb&blockReason=Other", manager.AccessToken);
        Assert.Empty(hidden.GetProperty("reservations").EnumerateArray());
        Assert.Empty(hidden.GetProperty("blocks").EnumerateArray());
        Assert.Single(hidden.GetProperty("reservationHolds").EnumerateArray());
        Assert.Single(hidden.GetProperty("blockHolds").EnumerateArray());

        var shown = await GetCalendar(propertyUrl + "&includeCancelledReservations=true&reservationStatus=Cancelled", manager.AccessToken);
        var item = Assert.Single(shown.GetProperty("reservations").EnumerateArray());
        Assert.Equal(cancelled.Id, item.GetProperty("id").GetGuid());
        Assert.Equal(JsonValueKind.Null, item.GetProperty("effectiveEnd").ValueKind);
        Assert.Equal("2032-01-02", Assert.Single(shown.GetProperty("reservationHolds").EnumerateArray())
            .GetProperty("endDate").GetString());

        var paged = (await GetCalendar(url + "&size=1", manager.AccessToken)).GetProperty("properties");
        Assert.Equal(2, paged.GetProperty("total").GetInt32());
        Assert.Single(paged.GetProperty("items").EnumerateArray());
        foreach (var query in new[] { "", "?startDate=2032-01-02&endDate=2032-01-02", "?startDate=2032-01-01&endDate=2033-01-01" })
            Assert.Equal(HttpStatusCode.BadRequest, await Status(client, Portfolio(managerId, "calendar") + query, manager.AccessToken));

        async Task<JsonElement> GetCalendar(string path, string? bearer)
        {
            using var response = await Send(client, HttpMethod.Get, path, bearer);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.True(response.Headers.CacheControl!.NoStore);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Token));
            return payload.RootElement.Clone();
        }
    }

    [Fact]
    public async Task CalendarFeedRestrictsOwnerAndExplicitAdminScopes()
    {
        using var app = new Application(database.ConnectionString);
        var fixture = await SeedAvailabilityAsync(app);
        var differentOwner = await AddAccountAsync(app, OrganizationRole.Owner, fixture.Manager.OrganizationId);
        var foreign = await AddAccountAsync(app, OrganizationRole.Owner);
        var admin = await AddAccountAsync(app, superAdmin: true);
        await InsertReservationAsync(fixture, new(new(2032, 1, 1), new(2032, 1, 3)), ReservationStatus.Confirmed);
        using var client = Client(app);
        var (owner, _) = await LoginAsync(client, fixture.Owner);
        var (other, _) = await LoginAsync(client, differentOwner);
        var (outside, _) = await LoginAsync(client, foreign);
        var (administrator, _) = await EnrollAsync(client, admin);
        var (manager, _) = await EnrollAsync(client, fixture.Manager);
        var url = Portfolio(fixture.Manager.OrganizationId, "calendar") + "?startDate=2032-01-01&endDate=2032-01-02";

        Assert.Equal(HttpStatusCode.Unauthorized, await Status(client, url, null));
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, url + $"&propertyId={fixture.Property}", other.AccessToken));
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, url, outside.AccessToken));
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, url + $"&ownerId={fixture.OwnerId}", owner.AccessToken));
        foreach (var bearer in new[] { owner.AccessToken, administrator.AccessToken })
        {
            using var response = await Send(client, HttpMethod.Get, url + $"&propertyId={fixture.Property}", bearer);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Token));
            var reservation = Assert.Single(payload.RootElement.GetProperty("reservations").EnumerateArray());
            Assert.Equal(JsonValueKind.Null, reservation.GetProperty("guestName").ValueKind);
            Assert.Equal(JsonValueKind.Null, reservation.GetProperty("paymentStatus").ValueKind);
            Assert.False(reservation.GetProperty("canViewDetails").GetBoolean());
        }
        using var managerResponse = await Send(client, HttpMethod.Get, url + $"&propertyId={fixture.Property}", manager.AccessToken);
        Assert.Equal(HttpStatusCode.OK, managerResponse.StatusCode);
        using var managerPayload = JsonDocument.Parse(await managerResponse.Content.ReadAsStringAsync(Token));
        var legacy = Assert.Single(managerPayload.RootElement.GetProperty("reservations").EnumerateArray());
        Assert.True(legacy.GetProperty("canViewDetails").GetBoolean());
        Assert.False(legacy.GetProperty("canEdit").GetBoolean());
    }

    [Fact]
    public async Task CalendarSummaryCountsTurnoverAndDistinguishesHeldBlockedAvailableAndRealizedNights()
    {
        using var app = new Application(database.ConnectionString);
        var fixture = await SeedAvailabilityAsync(app);
        var day = new DateOnly(2032, 2, 29);
        var date = new StayDates(day, day.AddDays(1));
        await InsertReservationAsync(fixture, new(day.AddDays(-1), day), ReservationStatus.CheckedOut, actual: true);
        await InsertReservationAsync(fixture, date, ReservationStatus.Confirmed);
        await InsertReservationAsync(fixture, date, ReservationStatus.Pending, property: fixture.Owner.PropertyId);

        var otherOwner = await AddAccountAsync(app, OrganizationRole.Owner, fixture.Manager.OrganizationId);
        var inactive = await AddAccountAsync(app, OrganizationRole.Owner, fixture.Manager.OrganizationId);
        var archived = await AddAccountAsync(app, OrganizationRole.Owner, fixture.Manager.OrganizationId);
        var available = await AddAccountAsync(app, OrganizationRole.Owner, fixture.Manager.OrganizationId);
        await using (var context = database.CreateContext(fixture.Manager.OrganizationId))
        {
            (await context.Set<Property>().SingleAsync(row => row.Id == inactive.PropertyId, Token)).Status = PropertyStatus.Inactive;
            (await context.Set<Property>().SingleAsync(row => row.Id == archived.PropertyId, Token)).Status = PropertyStatus.Archived;
            (await context.Set<Property>().SingleAsync(row => row.Id == available.PropertyId, Token)).OperationalStatus = OperationalStatus.NeedsCleaning;
            await context.SaveChangesAsync(Token);
        }
        await BookingScope(app, fixture.Manager, service => service.SaveBlockAsync(otherOwner.PropertyId, null, null,
            date, "Maintenance", Token));

        using var client = Client(app);
        var (manager, _) = await EnrollAsync(client, fixture.Manager);
        var (outside, _) = await LoginAsync(client, await AddAccountAsync(app, OrganizationRole.Owner));
        var url = Portfolio(fixture.Manager.OrganizationId, "calendar/summary") + "?date=2032-02-29";
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, url, outside.AccessToken));
        var summary = await Summary(url);
        Assert.Equal(new CalendarSummary(day, 1, 1, 1, 1, 1, 1), summary);
        Assert.Equal(new CalendarSummary(day, 1, 1, 1, 0, 0, 0), await Summary(url + $"&propertyId={fixture.Property}"));
        Assert.Equal(new CalendarSummary(day, 1, 1, 1, 0, 0, 1), await Summary(url + $"&ownerId={fixture.OwnerId}"));
        var realized = await Summary(Portfolio(fixture.Manager.OrganizationId, "calendar/summary") + "?date=2032-02-28");
        Assert.Equal(1, realized.OccupiedProperties);
        Assert.Equal(0, realized.PendingHeldProperties);

        async Task<CalendarSummary> Summary(string path)
        {
            using var response = await Send(client, HttpMethod.Get, path, manager.AccessToken);
            var body = await response.Content.ReadAsStringAsync(Token);
            Assert.True(response.IsSuccessStatusCode, body);
            return Assert.IsType<CalendarSummary>(JsonSerializer.Deserialize<CalendarSummary>(body,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)));
        }
    }

    [Fact]
    public async Task CalendarBlockWritesGuardOverlapVersionAndElapsedHistory()
    {
        using var app = new Application(database.ConnectionString);
        var fixture = await SeedAvailabilityAsync(app);
        var today = PropertyAvailability.BusinessDate(DateTimeOffset.UtcNow);
        var ongoing = new CalendarBlock { OrganizationId = fixture.Manager.OrganizationId, PropertyId = fixture.Property,
            StartDate = today.AddDays(-2), EndDate = today.AddDays(2), Reason = "Legacy fixture", Status = CalendarBlockStatus.Active };
        await using (var context = database.CreateContext(fixture.Manager.OrganizationId))
        {
            context.Add(ongoing);
            await context.SaveChangesAsync(Token);
        }
        using var client = Client(app);
        var (manager, _) = await EnrollAsync(client, fixture.Manager);
        var (owner, _) = await LoginAsync(client, fixture.Owner);
        var url = Portfolio(fixture.Manager.OrganizationId, "calendar-blocks");
        var existingUrl = $"{url}/{ongoing.Id}";
        var revision = new CalendarBlockWrite(ongoing.StartDate, today.AddDays(4), CalendarBlockReason.Renovation,
            "Synthetic fixture", ongoing.RowVersion);
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, existingUrl, owner.AccessToken));
        using var foreignEdit = await Send(client, HttpMethod.Put, existingUrl, owner.AccessToken, revision);
        Assert.Equal(HttpStatusCode.Forbidden, foreignEdit.StatusCode);
        using var overlap = await Send(client, HttpMethod.Post, url, manager.AccessToken,
            new CalendarBlockCreate(fixture.Property, revision with { StartDate = today }));
        Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);
        Assert.Contains("calendar_block_conflict", await overlap.Content.ReadAsStringAsync(Token));
        using var retroactive = await Send(client, HttpMethod.Post, url, manager.AccessToken,
            new CalendarBlockCreate(fixture.Property, revision));
        Assert.Equal(HttpStatusCode.BadRequest, retroactive.StatusCode);

        using var changed = await Send(client, HttpMethod.Put, existingUrl, manager.AccessToken, revision);
        var changedBody = await changed.Content.ReadAsStringAsync(Token);
        Assert.True(changed.IsSuccessStatusCode, changedBody);
        using var change = JsonDocument.Parse(changedBody);
        var replacementId = change.RootElement.GetProperty("id").GetGuid();
        Assert.NotEqual(ongoing.Id, replacementId);
        using var stale = await Send(client, HttpMethod.Put, existingUrl, manager.AccessToken, revision);
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        Assert.Contains("availability_stale", await stale.Content.ReadAsStringAsync(Token));

        byte[] version;
        await using (var context = database.CreateContext(fixture.Manager.OrganizationId))
        {
            var historical = await context.Set<CalendarBlock>().SingleAsync(row => row.Id == ongoing.Id, Token);
            Assert.Equal(today, historical.AvailabilityReleasedFrom);
            Assert.Equal(ongoing.StartDate, historical.StartDate);
            Assert.Equal(ongoing.EndDate, historical.EndDate);
            Assert.Equal("Legacy fixture", historical.Reason);
            var replacement = await context.Set<CalendarBlock>().SingleAsync(row => row.Id == replacementId, Token);
            Assert.Equal(today, replacement.StartDate);
            Assert.Equal(CalendarBlockReason.Renovation, replacement.Category);
            Assert.NotNull(replacement.CreatedByUserId);
            version = replacement.RowVersion;
        }
        using var cancelled = await Send(client, HttpMethod.Post, $"{url}/{replacementId}/cancel", manager.AccessToken,
            new CalendarBlockCancel(version, "Fixture cancellation"));
        Assert.Equal(HttpStatusCode.NoContent, cancelled.StatusCode);

        var availability = Portfolio(fixture.Manager.OrganizationId, $"properties/{fixture.Property}/availability");
        await CheckNight(today.AddDays(-1), "calendar_block_conflict");
        await CheckNight(today, "available");
        await using var verified = database.CreateContext(fixture.Manager.OrganizationId);
        var row = await verified.Set<CalendarBlock>().SingleAsync(item => item.Id == replacementId, Token);
        Assert.Equal(CalendarBlockStatus.Cancelled, row.Status);
        Assert.Equal("Fixture cancellation", row.CancellationReason);
        Assert.NotNull(row.CancelledByUserId);
        Assert.True(await verified.Set<AuditLog>().AnyAsync(entry => entry.ResourceId == replacementId &&
            entry.Action == "calendar_block_cancelled", Token));

        async Task CheckNight(DateOnly night, string expected)
        {
            using var response = await Send(client, HttpMethod.Get,
                $"{availability}?checkInDate={night:yyyy-MM-dd}&checkOutDate={night.AddDays(1):yyyy-MM-dd}", manager.AccessToken);
            using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Token));
            Assert.Equal(expected, payload.RootElement.GetProperty("status").GetString());
        }
    }

    [Fact]
    public async Task CalendarBlockMigrationKeepsLegacyIntervalsReasonsAndReservations()
    {
        await using var previous = new SqlServerFixture("20261127142833_GuestReservationManagement");
        await previous.InitializeAsync();
        using var app = new Application(previous.ConnectionString);
        var manager = await AddAccountToCatalogAsync(app, previous, OrganizationRole.Manager);
        var owner = await AddAccountToCatalogAsync(app, previous, OrganizationRole.Owner, manager.OrganizationId);
        var blockId = Guid.NewGuid();
        var start = new DateOnly(2032, 1, 3);
        var end = start.AddDays(4);
        const string original = "Legacy apartment owner requested a private hold";
        await using var context = previous.CreateContext(manager.OrganizationId);
        var guest = new Guest { Name = "Fictitious historical fixture", OrganizationId = manager.OrganizationId };
        context.Add(guest);
        await context.SaveChangesAsync(Token);
        var reservation = new Reservation { OrganizationId = manager.OrganizationId, PropertyId = owner.PropertyId,
            GuestId = guest.Id, Reference = "CALENDAR-REPLAY", CheckInDate = start.AddDays(-2), CheckOutDate = start,
            Adults = 1, Source = BookingSource.Direct, RevenueInputMode = RevenueInputMode.NetDirect, NetPlatformRevenue = 20 };
        context.Add(reservation);
        await context.SaveChangesAsync(Token);
        await context.Database.ExecuteSqlInterpolatedAsync($"""
            INSERT INTO booking.CalendarBlock (Id, OrganizationId, PropertyId, StartDate, EndDate, Reason, Status)
            VALUES ({blockId}, {manager.OrganizationId}, {owner.PropertyId}, {start}, {end}, {original}, 0)
            """, Token);

        await context.Database.MigrateAsync(Token);
        var block = await context.Set<CalendarBlock>().AsNoTracking().SingleAsync(row => row.Id == blockId, Token);
        Assert.Equal(original, block.Reason);
        Assert.Equal(start, block.StartDate);
        Assert.Equal(end, block.EndDate);
        Assert.Equal(CalendarBlockStatus.Active, block.Status);
        Assert.Null(block.Category);
        Assert.Null(block.CreatedAt);
        Assert.Null(block.CancelledAt);
        Assert.Null(block.AvailabilityReleasedFrom);
        Assert.Equal(20m, await context.Set<Reservation>().Where(row => row.Id == reservation.Id)
            .Select(row => row.NetPlatformRevenue).SingleAsync(Token));
    }

    [Fact]
    public async Task CalendarBlockAndReservationEndpointsSerializeCompetingWrites()
    {
        using var app = new Application(database.ConnectionString);
        var fixture = await SeedAvailabilityAsync(app);
        using var client = Client(app);
        var (manager, _) = await EnrollAsync(client, fixture.Manager);
        var blocks = Portfolio(fixture.Manager.OrganizationId, "calendar-blocks");
        var bookings = Portfolio(fixture.Manager.OrganizationId, "reservations");
        var day = new DateOnly(2032, 4, 5);
        var block = new CalendarBlockCreate(fixture.Property, new(day, day.AddDays(2), CalendarBlockReason.OwnerUse, null, null));
        var booking = Booking(fixture, day, day.AddDays(1));
        var first = await Race(CreateBlock(block), Send(client, HttpMethod.Post, bookings, manager.AccessToken, booking));
        Assert.Contains(first, new[] { "booking_conflict", "calendar_block_conflict" });

        var next = block with { Block = block.Block with { StartDate = day.AddDays(2), EndDate = day.AddDays(3) } };
        Assert.Equal("calendar_block_conflict", await Race(CreateBlock(next), CreateBlock(next)));

        Task<HttpResponseMessage> CreateBlock(CalendarBlockCreate body) => Send(client, HttpMethod.Post, blocks, manager.AccessToken, body);

        async Task<string> Race(Task<HttpResponseMessage> left, Task<HttpResponseMessage> right)
        {
            var responses = await Task.WhenAll(left, right);
            using var first = responses[0];
            using var second = responses[1];
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
            var conflict = Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Conflict);
            using var problem = JsonDocument.Parse(await conflict.Content.ReadAsStringAsync(Token));
            return problem.RootElement.GetProperty("code").GetString()!;
        }
    }

    [Fact]
    public async Task CalendarBlockValidationAcceptsEveryCategoryOnInactivePropertyAndDeniesArchivedProperties()
    {
        using var app = new Application(database.ConnectionString);
        var fixture = await SeedAvailabilityAsync(app);
        var start = new DateOnly(2032, 5, 1);
        await using (var context = database.CreateContext(fixture.Manager.OrganizationId))
        {
            (await context.Set<Property>().SingleAsync(row => row.Id == fixture.Property, Token)).Status = PropertyStatus.Inactive;
            (await context.Set<Property>().SingleAsync(row => row.Id == fixture.Owner.PropertyId, Token)).Status = PropertyStatus.Archived;
            await context.SaveChangesAsync(Token);
        }
        using var client = Client(app);
        var (manager, _) = await EnrollAsync(client, fixture.Manager);
        var path = Portfolio(fixture.Manager.OrganizationId, "calendar-blocks");
        foreach (var reason in Enum.GetValues<CalendarBlockReason>())
        {
            var day = start.AddDays((int)reason);
            using var result = await PostBlock(fixture.Property, new(day, day.AddDays(1), reason,
                reason == CalendarBlockReason.Other ? "Test purpose" : null, null));
            Assert.Equal(HttpStatusCode.Created, result.StatusCode);
        }
        var valid = new CalendarBlockWrite(start.AddDays(6), start.AddDays(7), CalendarBlockReason.Maintenance, null, null);
        using var archived = await PostBlock(fixture.Owner.PropertyId, valid);
        Assert.Equal(HttpStatusCode.Conflict, archived.StatusCode);
        Assert.Contains("calendar_block_property_archived", await archived.Content.ReadAsStringAsync(Token));

        foreach (var invalid in new[] { valid with { EndDate = default }, valid with { EndDate = valid.StartDate },
            valid with { Reason = (CalendarBlockReason)99 }, valid with { Reason = CalendarBlockReason.Other } })
        {
            using var result = await PostBlock(fixture.Property, invalid);
            Assert.Equal(HttpStatusCode.BadRequest, result.StatusCode);
        }

        var maintenance = Portfolio(fixture.Manager.OrganizationId, "calendar") +
            $"?startDate=2032-05-01&endDate=2032-05-10&propertyId={fixture.Property}&blockReason=Maintenance";
        using var feed = await Send(client, HttpMethod.Get, maintenance, manager.AccessToken);
        using var payload = JsonDocument.Parse(await feed.Content.ReadAsStringAsync(Token));
        Assert.Single(payload.RootElement.GetProperty("blocks").EnumerateArray());
        Assert.Equal(5, payload.RootElement.GetProperty("blockHolds").GetArrayLength());

        Task<HttpResponseMessage> PostBlock(Guid property, CalendarBlockWrite input) =>
            Send(client, HttpMethod.Post, path, manager.AccessToken, new CalendarBlockCreate(property, input));
    }

    [Fact]
    public async Task CalendarCancellationCommitsBeforeWaitingReservationUsesReleasedNight()
    {
        var gate = new SaveGate();
        var observer = new LockObserver();
        using var app = new Application(database.ConnectionString, bookingInterceptors: [gate, observer]);
        var fixture = await SeedAvailabilityAsync(app);
        using var client = Client(app);
        var (manager, _) = await EnrollAsync(client, fixture.Manager);
        var start = new DateOnly(2032, 5, 1);
        var end = start.AddDays(1);
        var blockId = await BookingScope(app, fixture.Manager, service =>
            service.SaveBlockAsync(fixture.Property, null, null, new(start, end), "Waiting race", Token));
        await using var context = database.CreateContext(fixture.Manager.OrganizationId);
        var block = await context.Set<CalendarBlock>().AsNoTracking().SingleAsync(row => row.Id == blockId, Token);
        var route = Portfolio(fixture.Manager.OrganizationId, $"calendar-blocks/{blockId}/cancel");

        gate.Arm();
        var cancellation = Send(client, HttpMethod.Post, route, manager.AccessToken,
            new CalendarBlockCancel(block.RowVersion, "Release for race test"));
        Task<HttpResponseMessage>? pending = null;
        try
        {
            await gate.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
            observer.Arm();
            pending = Send(client, HttpMethod.Post, Portfolio(fixture.Manager.OrganizationId, "reservations"),
                manager.AccessToken, Booking(fixture, start, end));
            await observer.Entered.Task.WaitAsync(TimeSpan.FromSeconds(10), Token);
            Assert.False(pending.IsCompleted);
        }
        finally { gate.Continue.TrySetResult(); }
        using var released = await cancellation;
        using var created = await pending!;
        Assert.Equal(HttpStatusCode.NoContent, released.StatusCode);
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        Assert.Equal(start, await context.Set<CalendarBlock>().Where(row => row.Id == blockId)
            .Select(row => row.AvailabilityReleasedFrom).SingleAsync(Token));
    }
}
