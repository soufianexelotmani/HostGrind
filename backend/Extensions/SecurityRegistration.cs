using System.Security.Claims;
using System.Security.Cryptography;
using System.Threading.RateLimiting;
using HostGrid.Api.Data;
using HostGrid.Api.DTOs.Auth;
using HostGrid.Api.Entities.Identity;
using HostGrid.Api.Integrations.Email;
using HostGrid.Api.Options;
using HostGrid.Api.Repositories;
using HostGrid.Api.Security;
using HostGrid.Api.Services;
using HostGrid.Api.Validators;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.IdentityModel.Tokens;

namespace HostGrid.Api.Extensions;

internal static class SecurityRegistration
{
    internal static void AddHostGridSecurity(this WebApplicationBuilder builder)
    {
        var options = builder.Configuration.GetSection("Security").Get<SecurityOptions>() ?? new();
        if (!Uri.TryCreate(options.PublicOrigin, UriKind.Absolute, out var origin) ||
            (origin.Scheme != "https" && !(builder.Environment.IsDevelopment() && origin.Scheme == "http" && origin.IsLoopback)) ||
            !string.Equals(origin.GetLeftPart(UriPartial.Authority), options.PublicOrigin, StringComparison.Ordinal))
        {
            throw new InvalidOperationException("Security:PublicOrigin must be an explicit origin.");
        }
        var material = options.SigningKey is not null ? Convert.FromBase64String(options.SigningKey)
            : builder.Environment.IsDevelopment() ? RandomNumberGenerator.GetBytes(32)
            : throw new InvalidOperationException("Security:SigningKey is required.");
        if (material.Length < 32)
        {
            throw new InvalidOperationException("Security:SigningKey must contain at least 32 random bytes.");
        }
        var key = new SymmetricSecurityKey(material);
        var services = builder.Services;
        services.AddSingleton(options);
        services.AddSingleton(key);
        services.AddSingleton(TimeProvider.System);
        services.AddDataProtection().SetApplicationName("HostGrid:" + builder.Environment.EnvironmentName);
        services.AddHttpContextAccessor();
        services.AddScoped<AuthenticatedTenantContext>();
        services.AddScoped<ITenantContext>(provider => provider.GetRequiredService<AuthenticatedTenantContext>());
        services.AddScoped<AuthenticationRepository>();
        services.AddScoped<AuthenticationService>();
        services.AddScoped<TokenService>();
        services.AddScoped<CurrentUserService>();
        services.AddScoped<SecurityAuditService>();
        services.AddScoped<AccountSecurityService>();
        services.AddScoped<AccountManagementService>();
        services.AddScoped<ISecurityEmail, SmtpSecurityEmail>();
        services.AddScoped<IAuthorizationHandler, PropertyAccessHandler>();
        SecurityValidationFilter.Register(services);

        services.AddIdentityCore<ApplicationUser>(identity =>
        {
            identity.User.RequireUniqueEmail = true;
            identity.SignIn.RequireConfirmedEmail = true;
            identity.Password.RequiredLength = 12;
            identity.Password.RequireDigit = false;
            identity.Password.RequireLowercase = false;
            identity.Password.RequireUppercase = false;
            identity.Password.RequireNonAlphanumeric = false;
            identity.Lockout.AllowedForNewUsers = true;
            identity.Lockout.MaxFailedAccessAttempts = 5;
            identity.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        }).AddRoles<ApplicationRole>().AddEntityFrameworkStores<HostGridDbContext>().AddDefaultTokenProviders();
        services.Configure<DataProtectionTokenProviderOptions>(provider => provider.TokenLifespan = TimeSpan.FromMinutes(30));

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(jwt =>
        {
            jwt.MapInboundClaims = false;
            jwt.IncludeErrorDetails = false;
            jwt.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true, ValidIssuer = options.Issuer,
                ValidateAudience = true, ValidAudience = options.Audience,
                ValidateIssuerSigningKey = true, IssuerSigningKey = key, ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero, ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                RoleClaimType = ClaimTypes.Role
            };
            jwt.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var service = context.HttpContext.RequestServices.GetRequiredService<TokenService>();
                    var account = await service.ValidateAsync(context.Principal!, context.HttpContext.RequestAborted);
                    if (account is null)
                    {
                        context.Fail("Session is unavailable.");
                        return;
                    }
                    context.HttpContext.Items[typeof(CurrentUserView)] = account;
                    if (account.SuperAdmin)
                    {
                        ((ClaimsIdentity)context.Principal!.Identity!).AddClaim(new Claim(ClaimTypes.Role, CurrentUserService.SuperAdmin));
                    }
                }
            };
        });
        services.AddAuthorizationBuilder()
            .AddPolicy(SecurityPolicies.GlobalAdmin, policy => policy.RequireRole(CurrentUserService.SuperAdmin))
            .AddPolicy(SecurityPolicies.TenantManager, policy => policy.RequireAuthenticatedUser().RequireAssertion(context =>
                context.Resource is HttpContext http &&
                http.RequestServices.GetRequiredService<AuthenticatedTenantContext>() is { AuthorizedOrganizationId: not null } tenant &&
                (context.User.IsInRole(CurrentUserService.SuperAdmin) || tenant.Membership?.Role == CurrentUserService.Manager)));
        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.AddPolicy("authentication", http => RateLimitPartition.GetFixedWindowLimiter(
                http.Connection.RemoteIpAddress?.ToString() ?? "local",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 60, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
        });
        services.AddExceptionHandler<SecurityExceptionHandler>();
    }
}
