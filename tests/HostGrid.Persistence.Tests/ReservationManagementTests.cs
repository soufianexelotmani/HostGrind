using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.DTOs.Properties;
using HostGrid.Api.Entities.Booking;
using HostGrid.Api.Entities.Finance;
using HostGrid.Api.Entities.Property;
using HostGrid.Api.Entities.System;
using HostGrid.Api.Enums;
using HostGrid.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Persistence.Tests;

public sealed partial class AuthenticationTests
{
    [Fact]
    public async Task GuestsCanBeSelectedAndUpdatedWithoutMergingAndLookupIsTenantPrivate()
    {
        using var app = new Application(database.ConnectionString);
        var fixture = await SeedAvailabilityAsync(app);
        var foreign = await AddAccountAsync(app, OrganizationRole.Manager);
        using var client = Client(app);
        var (manager, _) = await EnrollAsync(client, fixture.Manager);
        var (owner, _) = await LoginAsync(client, fixture.Owner);
        var (other, _) = await EnrollAsync(client, foreign);
        var route = Portfolio(fixture.Manager.OrganizationId, "guests");
        var auth = manager.AccessToken!;

        var first = await CreateGuest(new("ليلى مثال", "06 12 34 56 78", "Guest.Match@example.test", null));
        var second = await CreateGuest(new("Aya Example", "+212 612 345 678", null, null));
        var optional = await CreateGuest(new("Guest without contact", null, null, null));
        using var suggestion = await Send(client, HttpMethod.Post, route + "/suggestions", auth,
            new GuestLookup("guest.match@example.test", "00212 (612) 345 678"));
        var matches = await Read<List<GuestView>>(suggestion);
        Assert.Contains(matches, row => row.Id == first);
        Assert.Contains(matches, row => row.Id == second);
        Assert.Equal(2, matches.Count);
        var original = matches.Single(row => row.Id == first);
        using var edit = await Send(client, HttpMethod.Put, route + "/" + first, auth,
            new GuestWrite(original.Name, "+33 1 23 45 67 89", "changed@example.test", original.Version));
        Assert.Equal(HttpStatusCode.OK, edit.StatusCode);
        using var stale = await Send(client, HttpMethod.Put, route + "/" + first, auth,
            new GuestWrite(original.Name, null, null, original.Version));
        Assert.Equal(HttpStatusCode.Conflict, stale.StatusCode);
        using var list = await Send(client, HttpMethod.Get, route + "?search=Guest&hasContact=false&sort=-name&size=1", auth);
        var page = await Read<Page<GuestView>>(list);
        Assert.Equal(optional, Assert.Single(page.Items).Id);
        Assert.Equal(1, page.Total);
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, route, owner.AccessToken));
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, route + "/" + first, other.AccessToken));
        Assert.Equal(HttpStatusCode.NotFound, await Status(client, Portfolio(foreign.OrganizationId, $"guests/{first}"), other.AccessToken));

        async Task<Guid> CreateGuest(GuestWrite input)
        {
            using var response = await Send(client, HttpMethod.Post, route, auth, input);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return (await Read<Created>(response)).Id;
        }
    }

    [Fact]
    public async Task LiveReservationWritesKeepSnapshotsAndEnforceExternalReferenceAcrossProperties()
    {
        using var app = new Application(database.ConnectionString);
        var fixture = await SeedAvailabilityAsync(app);
        using var client = Client(app);
        var (manager, _) = await EnrollAsync(client, fixture.Manager);
        var (owner, _) = await LoginAsync(client, fixture.Owner);
        var route = Portfolio(fixture.Manager.OrganizationId, "reservations");
        var bearer = manager.AccessToken!;
        var today = PropertyAvailability.BusinessDate(DateTimeOffset.UtcNow);
        var start = today.AddDays(10);
        var direct = Booking(fixture, start, start.AddDays(2)) with { Source = BookingSource.Direct, NetAmount = null };
        using var missingRate = await Send(client, HttpMethod.Post, route, bearer, direct with { Confirm = true });
        Assert.Equal("commission_rate_unavailable", await ErrorCode(missingRate, HttpStatusCode.Conflict));

        await using var db = database.CreateContext(fixture.Manager.OrganizationId);
        Assert.Empty(await db.Set<Reservation>().ToArrayAsync(Token));
        Assert.Empty(await db.Set<ReservationNightPrice>().ToArrayAsync(Token));
        var property = await db.Set<Property>().SingleAsync(row => row.Id == fixture.Property, Token);
        property.BaseNightlyRate = 100m;
        db.Add(new CommissionRateHistory { OrganizationId = property.OrganizationId, PropertyId = property.Id,
            EffectiveFrom = today, Rate = 17.25m });
        db.Add(new SeasonalRate { OrganizationId = property.OrganizationId, PropertyId = property.Id,
            Priority = 3, DaysOfWeek = 127, StartDate = start, EndDate = start.AddDays(1), NightlyRate = 180m });
        await db.SaveChangesAsync(Token);
        using var confirm = await Send(client, HttpMethod.Post, route, bearer, direct with
        {
            Confirm = true, Mode = RevenueInputMode.GrossWithFees, PlatformFees = 20m,
            NightlyPrices = [new(start.AddDays(1), 120m)]
        });
        Assert.Equal(HttpStatusCode.Created, confirm.StatusCode);
        var id = (await Read<Created>(confirm)).Id;
        var view = await Detail(id);
        Assert.Equal(today, view.ConfirmationDate);
        Assert.Equal(17.25m, view.CommissionRate);
        Assert.Equal(48.30m, view.CommissionAmount);
        Assert.Equal(280m, view.Summary.NetAmount);
        Assert.Equal(fixture.Manager.UserId, view.ConfirmedBy);
        Assert.Equal(["custom", "manual"], view.Pricing!.Nights.Select(night => night.Source).ToArray());
        Assert.NotNull(view.Pricing.Nights[0].RuleId);
        Assert.Equal(2, view.NightCount);

        property.BaseNightlyRate = 250m;
        await db.SaveChangesAsync(Token);
        Assert.Equal(180m, (await Detail(id)).Pricing!.Nights[0].Amount);
        var revision = direct with { CheckInDate = start.AddDays(1), CheckOutDate = start.AddDays(3), Version = view.Version,
            Mode = RevenueInputMode.GrossWithFees, PlatformFees = 20m };
        using var needsAcceptance = await Send(client, HttpMethod.Put, route + "/" + id, bearer, revision);
        Assert.Equal("price_revision_acceptance_required", await ErrorCode(needsAcceptance, HttpStatusCode.Conflict));
        using var update = await Send(client, HttpMethod.Put, route + "/" + id, bearer, revision with { AcceptPriceChanges = true });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await Detail(id);
        Assert.Equal(17.25m, updated.CommissionRate);
        Assert.Equal(60.38m, updated.CommissionAmount);
        Assert.Equal([120m, 250m], updated.Pricing!.Nights.Select(night => night.Amount).ToArray());
        using var history = await Send(client, HttpMethod.Get, route + $"/{id}/history", bearer);
        var priceHistory = await Read<Page<PriceRevisionView>>(history);
        Assert.Equal(180m, priceHistory.Items[1].Nights[0].Amount);
        using var stale = await Send(client, HttpMethod.Put, route + "/" + id, bearer, revision);
        Assert.Equal("availability_stale", await ErrorCode(stale, HttpStatusCode.Conflict));

        var pending = Booking(fixture, start.AddDays(3), start.AddDays(6)) with { ExternalBookingId = "  AB-103  ", NetAmount = 10.01m };
        using var hold = await Send(client, HttpMethod.Post, route, bearer, pending);
        Assert.Equal(HttpStatusCode.Created, hold.StatusCode);
        var holdId = (await Read<Created>(hold)).Id;
        var held = await Detail(holdId);
        Assert.Null(held.CommissionRate);
        Assert.Null(held.GrossAmount);
        Assert.Null(held.PlatformFees);
        Assert.Equal([3.34m, 3.34m, 3.33m], held.Pricing!.Nights.Select(night => night.Amount).ToArray());
        Assert.All(held.Pricing.Nights, row => Assert.Equal("allocated", row.Source));
        using var duplicate = await Send(client, HttpMethod.Post, route, bearer, pending with
        { PropertyId = fixture.Owner.PropertyId, ExternalBookingId = "AB-103" });
        Assert.Equal("external_booking_reference_duplicate", await ErrorCode(duplicate, HttpStatusCode.Conflict));
        using var overlap = await Send(client, HttpMethod.Post, route, bearer, pending with { ExternalBookingId = null });
        Assert.Equal("booking_conflict", await ErrorCode(overlap, HttpStatusCode.Conflict));
        using var list = await Send(client, HttpMethod.Get, route + "?status=Pending&property=" + fixture.Property + "&size=1", bearer);
        Assert.Equal(holdId, Assert.Single((await Read<Page<ReservationSummary>>(list)).Items).Id);
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, route + "/" + id, owner.AccessToken));
        using var ownerWrite = await Send(client, HttpMethod.Post, route, owner.AccessToken, direct);
        Assert.Equal(HttpStatusCode.Forbidden, ownerWrite.StatusCode);

        async Task<ReservationView> Detail(Guid key)
        {
            using var response = await Send(client, HttpMethod.Get, route + "/" + key, bearer);
            return await Read<ReservationView>(response);
        }
    }

    [Fact]
    public async Task HistoricalReservationRequiresPrivateEvidenceAndUsesProvidedConfirmationDate()
    {
        using var app = new Application(database.ConnectionString);
        var fixture = await SeedAvailabilityAsync(app);
        using var client = Client(app);
        var (manager, _) = await EnrollAsync(client, fixture.Manager);
        var (owner, _) = await LoginAsync(client, fixture.Owner);
        var org = fixture.Manager.OrganizationId;
        var route = Portfolio(org, "reservations");
        var start = new DateOnly(2025, 10, 20);
        var confirmed = new DateOnly(2025, 9, 30);
        var model = new HistoricalReservation(Booking(fixture, start, start.AddDays(2)), confirmed,
            start, start.AddDays(2), "Fictitious historical statement");
        await using var db = database.CreateContext(org);
        db.AddRange(new CommissionRateHistory { OrganizationId = org, PropertyId = fixture.Property,
                EffectiveFrom = confirmed.AddDays(-10), EffectiveTo = confirmed.AddDays(1), Rate = 13m },
            new CommissionRateHistory { OrganizationId = org, PropertyId = fixture.Property,
                EffectiveFrom = confirmed.AddDays(1), Rate = 23m });
        await db.SaveChangesAsync(Token);
        using var missing = await Historical(model, false);
        Assert.Equal("historical_evidence_required", await ErrorCode(missing, HttpStatusCode.BadRequest));
        using var noDate = await Historical(model with { ConfirmationDate = default }, true);
        Assert.Equal("historical_dates_invalid", await ErrorCode(noDate, HttpStatusCode.BadRequest));
        db.Add(new MonthlyClosure { OrganizationId = org, PropertyId = fixture.Property, OwnerId = fixture.OwnerId,
            Year = 2025, Month = 11, Status = ClosureStatus.Closed, ClosedAt = DateTime.UtcNow,
            ClosedById = fixture.Manager.UserId });
        await db.SaveChangesAsync(Token);
        using var closed = await Historical(model with
        {
            Booking = model.Booking with { CheckInDate = new(2025, 11, 2), CheckOutDate = new(2025, 11, 4) },
            ActualCheckInDate = null, ActualCheckOutDate = null
        }, true);
        Assert.Equal("financial_period_closed", await ErrorCode(closed, HttpStatusCode.Conflict));
        using var save = await Historical(model, true);
        Assert.Equal(HttpStatusCode.Created, save.StatusCode);
        var id = (await Read<Created>(save)).Id;
        var row = await db.Set<Reservation>().SingleAsync(item => item.Id == id, Token);
        Assert.Equal(13m, row.CommissionRateSnapshot);
        Assert.Equal(confirmed, row.ConfirmationDate);
        Assert.Null(row.ConfirmedAt);
        Assert.Null(row.ActualCheckInAt);
        Assert.Null(row.ActualCheckOutAt);
        Assert.Equal(start, row.HistoricalActualCheckIn);
        Assert.Equal(fixture.Manager.UserId, row.HistoricalEnteredBy);
        Assert.Single(await db.Set<AuditLog>().Where(item => item.ResourceId == id && item.Action == "reservation_history_entered").ToArrayAsync(Token));
        using var clash = await Historical(model, true);
        Assert.Equal("booking_conflict", await ErrorCode(clash, HttpStatusCode.Conflict));
        Assert.Equal(HttpStatusCode.OK, await Status(client, route + $"/{id}/evidence", manager.AccessToken));
        Assert.Equal(HttpStatusCode.Forbidden, await Status(client, route + $"/{id}/evidence", owner.AccessToken));

        async Task<HttpResponseMessage> Historical(HistoricalReservation input, bool evidence)
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, route + "/historical");
            request.Headers.Authorization = new("Bearer", manager.AccessToken);
            request.Headers.Add("X-HostGrid-CSRF", "1");
            using var form = new MultipartFormDataContent();
            form.Add(new StringContent(JsonSerializer.Serialize(input, new JsonSerializerOptions(JsonSerializerDefaults.Web))), "request");
            if (evidence) form.Add(new ByteArrayContent("%PDF-1.4\n%%EOF\n"u8.ToArray()), "file", "Fictitious statement.pdf");
            request.Content = form;
            return await client.SendAsync(request, Token);
        }
    }

    private static ReservationWrite Booking(BookingFixture fixture, DateOnly start, DateOnly end) =>
        new(fixture.Property, fixture.Guest, BookingSource.Airbnb, null, start, end, 1, 0, RevenueInputMode.NetDirect,
            null, null, 100m, null, false, false, null, null);

    [Fact]
    public async Task ReservationMigrationRejectsDuplicateReferencesThenPreservesLegacyRows()
    {
        const string prior = "20261127133709_BookingAvailabilityFoundation";
        var legacy = new SqlServerFixture(prior);
        try
        {
            await legacy.InitializeAsync();
            using var app = new Application(legacy.ConnectionString);
            var manager = await AddAccountToCatalogAsync(app, legacy, OrganizationRole.Manager);
            var one = await AddAccountToCatalogAsync(app, legacy, OrganizationRole.Owner, manager.OrganizationId);
            var two = await AddAccountToCatalogAsync(app, legacy, OrganizationRole.Owner, manager.OrganizationId);
            await using var db = legacy.CreateContext(manager.OrganizationId);
            var guest = Guid.NewGuid();
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                INSERT INTO [booking].[Guest] ([Id], [OrganizationId], [Name], [Email], [Phone])
                VALUES ({guest}, {manager.OrganizationId}, N'Legacy guest', N'Legacy@example.test', N'06 12 34 56 78');
                """, Token);
            var first = Guid.NewGuid();
            var second = Guid.NewGuid();
            await Insert(first, one.PropertyId, "  LEGACY-1 ");
            await Insert(second, two.PropertyId, "LEGACY-1");
            await Assert.ThrowsAsync<Microsoft.Data.SqlClient.SqlException>(() => db.Database.MigrateAsync(Token));
            Assert.Equal(6, (await db.Database.GetAppliedMigrationsAsync(Token)).Count());
            await db.Database.ExecuteSqlInterpolatedAsync($"""
                UPDATE [booking].[Reservation] SET [ExternalBookingId] = N'legacy-1' WHERE [Id] = {second};
                """, Token);
            await db.Database.MigrateAsync(Token);
            var saved = await db.Set<Reservation>().SingleAsync(row => row.Id == first, Token);
            Assert.Equal("LEGACY-1", saved.ExternalBookingId);
            Assert.Equal(21.5m, saved.CommissionRateSnapshot);
            Assert.Equal(21.5m, saved.CommissionAmount);
            Assert.Null(saved.ConfirmationDate);
            Assert.Null(saved.ConfirmedAt);
            Assert.Empty(await db.Set<ReservationPriceRevision>().ToListAsync(Token));
            var retained = await db.Set<Guest>().SingleAsync(row => row.Id == guest, Token);
            Assert.Equal("LEGACY@EXAMPLE.TEST", retained.EmailMatch);
            Assert.Equal("212612345678", retained.PhoneMatch);
            Assert.Null(retained.CreatedAt);

            async Task Insert(Guid id, Guid property, string platformReference)
            {
                await db.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO [booking].[Reservation] ([Id], [OrganizationId], [PropertyId], [GuestId], [Reference], [Source],
                        [ExternalBookingId], [CheckInDate], [CheckOutDate], [Adults], [Children], [RevenueInputMode], [NetPlatformRevenue],
                        [CommissionRateSnapshot], [CommissionAmount], [Status], [PaymentStatus])
                    VALUES ({id}, {manager.OrganizationId}, {property}, {guest}, {id.ToString("N")}, 0,
                        {platformReference}, '2025-05-01', '2025-05-02', 1, 0, 2, 100, 21.50, 21.50, 1, 0);
                    """, Token);
            }
        }
        finally { await legacy.DisposeAsync(); }
    }

    [Fact]
    public async Task ReservationEndpointsSerializeConcurrentOverlapsAndDuplicateReferences()
    {
        using var app = new Application(database.ConnectionString);
        var fixture = await SeedAvailabilityAsync(app);
        using var client = Client(app);
        var (manager, _) = await EnrollAsync(client, fixture.Manager);
        var route = Portfolio(fixture.Manager.OrganizationId, "reservations");
        var start = PropertyAvailability.BusinessDate(DateTimeOffset.UtcNow).AddDays(20);
        var input = Booking(fixture, start, start.AddDays(1));
        await OneConflict(input, input, "booking_conflict");
        var duplicate = input with { CheckInDate = start.AddDays(1), CheckOutDate = start.AddDays(2), ExternalBookingId = "RACE-AB" };
        await OneConflict(duplicate, duplicate with { PropertyId = fixture.Owner.PropertyId }, "external_booking_reference_duplicate");

        await BookingScope(app, fixture.Manager, service => service.SaveBlockAsync(fixture.Property, null, null,
            new(start.AddDays(4), start.AddDays(5)), "Fictitious maintenance", Token));
        using var block = await Create(input with { CheckInDate = start.AddDays(4), CheckOutDate = start.AddDays(5) });
        Assert.Equal("calendar_block_conflict", await ErrorCode(block, HttpStatusCode.Conflict));
        await using var db = database.CreateContext(fixture.Manager.OrganizationId);
        Assert.Equal(2, await db.Set<Reservation>().CountAsync(Token));
        Assert.Equal(2, await db.Set<ReservationPriceRevision>().CountAsync(Token));

        Task<HttpResponseMessage> Create(ReservationWrite value) => Send(client, HttpMethod.Post, route, manager.AccessToken, value);

        async Task OneConflict(ReservationWrite left, ReservationWrite right, string code)
        {
            var responses = await Task.WhenAll(Create(left), Create(right));
            using var first = responses[0];
            using var second = responses[1];
            Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
            Assert.Equal(code, await ErrorCode(responses.Single(response => !response.IsSuccessStatusCode), HttpStatusCode.Conflict));
        }
    }

    [Fact]
    public async Task ReservationRevisionAuthorizesTargetsAndProtectsLifecycleAndExistingHolds()
    {
        using var app = new Application(database.ConnectionString);
        var fixture = await SeedAvailabilityAsync(app);
        var foreign = await SeedAvailabilityAsync(app);
        using var client = Client(app);
        var (manager, _) = await EnrollAsync(client, fixture.Manager);
        var org = fixture.Manager.OrganizationId;
        var bearer = manager.AccessToken;
        var route = Portfolio(org, "reservations");
        var today = PropertyAvailability.BusinessDate(DateTimeOffset.UtcNow);
        var input = Booking(fixture, today, today.AddDays(2)) with { NightlyPrices = [new(today, 65m)] };
        using var wrongGuest = await Send(client, HttpMethod.Post, route, bearer, input with { GuestId = foreign.Guest });
        Assert.Equal(HttpStatusCode.NotFound, wrongGuest.StatusCode);
        using var wrongProperty = await Send(client, HttpMethod.Post, route, bearer, input with { PropertyId = foreign.Property });
        Assert.Equal(HttpStatusCode.NotFound, wrongProperty.StatusCode);
        using var capacity = await Send(client, HttpMethod.Post, route, bearer, input with { Adults = 3, Children = 2 });
        Assert.Equal("guest_capacity_invalid", await ErrorCode(capacity, HttpStatusCode.BadRequest));
        using var zeroNight = await Send(client, HttpMethod.Post, route, bearer, input with { CheckOutDate = today });
        Assert.Equal("invalid_date_range", await ErrorCode(zeroNight, HttpStatusCode.BadRequest));

        await using var db = database.CreateContext(org);
        var destination = await db.Set<Property>().SingleAsync(row => row.Id == fixture.Owner.PropertyId, Token);
        foreach (var status in new[] { PropertyStatus.Inactive, PropertyStatus.Archived })
        {
            destination.Status = status;
            await db.SaveChangesAsync(Token);
            using var closed = await Send(client, HttpMethod.Post, route, bearer, input with { PropertyId = destination.Id });
            Assert.Equal("property_not_bookable", await ErrorCode(closed, HttpStatusCode.Conflict));
        }
        destination.Status = PropertyStatus.Active;
        db.Add(new CommissionRateHistory { OrganizationId = org, PropertyId = destination.Id, EffectiveFrom = today, Rate = 20m });
        await db.SaveChangesAsync(Token);
        var pending = await Create(input);
        Assert.Equal(["external", "allocated"], pending.Pricing!.Nights.Select(night => night.Source).ToArray());
        var full = await Create(input with { PropertyId = destination.Id, Confirm = true });
        using var prematureMove = await Send(client, HttpMethod.Put, route + "/" + pending.Summary.Id, bearer,
            input with { PropertyId = destination.Id, Version = pending.Version });
        Assert.Equal("booking_conflict", await ErrorCode(prematureMove, HttpStatusCode.Conflict));
        Assert.Equal(fixture.Property, (await Detail(pending.Summary.Id)).Summary.PropertyId);
        using var confirmedMove = await Send(client, HttpMethod.Put, route + "/" + full.Summary.Id, bearer,
            input with { Version = full.Version });
        Assert.Equal("reservation_move_not_allowed", await ErrorCode(confirmedMove, HttpStatusCode.Conflict));

        using var successfulMove = await Send(client, HttpMethod.Put, route + "/" + pending.Summary.Id, bearer,
            input with { PropertyId = destination.Id, CheckInDate = today.AddDays(2), CheckOutDate = today.AddDays(3),
                NightlyPrices = null, Version = pending.Version });
        Assert.Equal(HttpStatusCode.OK, successfulMove.StatusCode);
        Assert.Equal(destination.Id, (await Detail(pending.Summary.Id)).Summary.PropertyId);
        var checkedIn = await InsertReservationAsync(fixture, new(today.AddDays(4), today.AddDays(5)), ReservationStatus.CheckedIn, actual: true);
        using var checkedInEdit = await Send(client, HttpMethod.Put, route + "/" + checkedIn.Id, bearer,
            input with { Version = checkedIn.RowVersion });
        Assert.Equal("reservation_edit_not_allowed", await ErrorCode(checkedInEdit, HttpStatusCode.Conflict));

        async Task<ReservationView> Create(ReservationWrite body)
        {
            using var response = await Send(client, HttpMethod.Post, route, bearer, body);
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await Detail((await Read<Created>(response)).Id);
        }

        async Task<ReservationView> Detail(Guid id)
        {
            using var response = await Send(client, HttpMethod.Get, route + "/" + id, bearer);
            return await Read<ReservationView>(response);
        }
    }

    private static async Task<string?> ErrorCode(HttpResponseMessage response, HttpStatusCode status)
    {
        using var payload = await response.Content.ReadFromJsonAsync<JsonDocument>(Token);
        Assert.Equal(status, response.StatusCode);
        return payload!.RootElement.GetProperty("code").GetString();
    }
}
