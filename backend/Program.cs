using HostGrid.Api.Data;
using HostGrid.Api.Extensions;
using HostGrid.Api.Health;
using HostGrid.Api.Middlewares;
using HostGrid.Api.Services;
using HostGrid.Api.Validators;
using HostGrid.Api.Integrations.Storage;
using HostGrid.Api.Repositories;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers(options => options.Filters.Add<SecurityValidationFilter>());
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.AddHostGridSecurity();
PortfolioValidation.Register(builder.Services);
builder.Services.AddScoped<PortfolioRepository>();
builder.Services.AddScoped<PortfolioAccess>();
builder.Services.AddScoped<PortfolioAudit>();
builder.Services.AddScoped<OwnerPortfolioService>();
builder.Services.AddScoped<PropertyPortfolioService>();
builder.Services.AddScoped<PropertyConfigurationService>();
builder.Services.AddScoped<PropertyPricing>();
builder.Services.AddScoped<PropertyFiles>();
builder.Services.AddOptions<PropertyStorageOptions>().BindConfiguration("PropertyStorage")
    .Validate(options => options.PhotoBytes is > 0 and <= 60000000 && options.DocumentBytes is > 0 and <= 60000000);
builder.Services.AddSingleton<IFileStorageService, LocalFileStorage>();
builder.Services.AddSingleton<IFileScanner, QuarantineFileScanner>();
builder.Services.AddDbContext<HostGridDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("HostGrid")));
builder.Services.AddHealthChecks().AddCheck<SqlServerHealthCheck>(
    "sql-server", timeout: TimeSpan.FromSeconds(5));

var app = builder.Build();

if (args.Contains("--bootstrap-super-admin", StringComparer.Ordinal))
{
    var address = app.Configuration["Security:BootstrapEmail"];
    if (string.IsNullOrWhiteSpace(address) || !System.Net.Mail.MailAddress.TryCreate(address, out var mailbox) ||
        mailbox.Address != address)
    {
        throw new InvalidOperationException("Supply Security__BootstrapEmail with the administrator email address.");
    }
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<AccountManagementService>().BootstrapAsync(address, CancellationToken.None);
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();
app.Use(async (http, next) =>
{
    http.Response.Headers["X-Content-Type-Options"] = "nosniff";
    http.Response.Headers["X-Frame-Options"] = "DENY";
    http.Response.Headers["Referrer-Policy"] = "no-referrer";
    http.Response.Headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
    if (!app.Environment.IsDevelopment() || http.Request.Path.StartsWithSegments("/api"))
    {
        http.Response.Headers["Content-Security-Policy"] = "default-src 'self'; frame-ancestors 'none'; base-uri 'self'; object-src 'none'; form-action 'self'";
    }
    await next(http);
});

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}
else
{
    app.UseHsts();
    app.UseHttpsRedirection();
}

app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseMiddleware<TenantSecurityMiddleware>();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/api/v1/health/live", new HealthCheckOptions
{
    Predicate = _ => false
});
app.MapHealthChecks("/api/v1/health/ready");

app.Run();

public partial class Program;
