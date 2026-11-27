using System.Data;
using HostGrid.Api.Data;
using HostGrid.Api.DTOs.Booking;
using HostGrid.Api.Entities.Booking;
using HostGrid.Api.Enums;
using HostGrid.Api.Security;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Repositories;

internal sealed class AvailabilityRepository(HostGridDbContext database, ITenantContext tenant)
{
    internal HostGridDbContext Database => database;
    internal DbSet<Reservation> Reservations => database.Set<Reservation>();
    internal DbSet<CalendarBlock> Blocks => database.Set<CalendarBlock>();

    internal async Task<int> AcquireAsync(Guid organization, Guid property, int timeout, CancellationToken token)
    {
        var resource = new SqlParameter("@resource", SqlDbType.NVarChar, 255)
        {
            Value = $"hostgrid:availability:{organization:N}:{property:N}"
        };
        var wait = new SqlParameter("@timeout", SqlDbType.Int) { Value = timeout };
        var result = new SqlParameter("@result", SqlDbType.Int) { Direction = ParameterDirection.Output };
        await database.Database.ExecuteSqlRawAsync("""
            EXEC @result = sys.sp_getapplock @Resource = @resource, @LockMode = 'Exclusive',
                @LockOwner = 'Transaction', @LockTimeout = @timeout, @DbPrincipal = 'public';
            """, [resource, wait, result], token);
        return (int)result.Value;
    }

    internal Task<bool> HasReservationAsync(Guid property, StayDates dates, Guid? reservation, CancellationToken token)
        // Seek by tenant and property even on a sparse table so another property's uncommitted rows
        // cannot turn a disjoint booking into a table-scan wait.
        => Reservations.FromSqlInterpolated($"""
            SELECT * FROM [booking].[Reservation]
            WITH (FORCESEEK ([IX_Reservation_OrganizationId_PropertyId_CheckInDate_CheckOutDate] ([OrganizationId], [PropertyId])))
            WHERE [OrganizationId] = {tenant.AuthorizedOrganizationId} AND [PropertyId] = {property}
            """).AsNoTracking().AnyAsync(row => row.Id != reservation &&
            (row.Status != ReservationStatus.Cancelled || row.ActualCheckInAt != null) &&
            row.CheckInDate < dates.CheckOutDate && row.CheckOutDate > dates.CheckInDate &&
            (row.AvailabilityReleasedFrom == null || row.AvailabilityReleasedFrom > dates.CheckInDate &&
                row.AvailabilityReleasedFrom > row.CheckInDate), token);

    internal Task<bool> HasBlockAsync(Guid property, StayDates dates, Guid? block, CancellationToken token)
        => Blocks.FromSqlInterpolated($"""
            SELECT * FROM [booking].[CalendarBlock]
            WITH (FORCESEEK ([IX_CalendarBlock_OrganizationId_PropertyId_StartDate_EndDate] ([OrganizationId], [PropertyId])))
            WHERE [OrganizationId] = {tenant.AuthorizedOrganizationId} AND [PropertyId] = {property}
            """).AsNoTracking().AnyAsync(row => row.Id != block &&
            row.Status == CalendarBlockStatus.Active && row.StartDate < dates.CheckOutDate &&
            row.EndDate > dates.CheckInDate, token);
}
