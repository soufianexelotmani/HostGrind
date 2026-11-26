using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace HostGrid.Api.Security;

internal sealed class SecurityExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext http, Exception exception, CancellationToken cancellationToken)
    {
        var security = exception switch
        {
            SecurityRequestException known => known,
            DbUpdateConcurrencyException => new SecurityRequestException(409, "This record changed. Reload it and try again."),
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } }
                => new SecurityRequestException(409, "A record with these details already exists."),
            _ => null
        };
        if (security is null)
        {
            return false;
        }
        http.Response.StatusCode = security.Status;
        await problems.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = http,
            ProblemDetails = new ProblemDetails { Status = security.Status, Title = security.Message }
        });
        return true;
    }
}
