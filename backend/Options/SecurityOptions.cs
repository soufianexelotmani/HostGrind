namespace HostGrid.Api.Options;

public sealed class SecurityOptions
{
    public string? SigningKey { get; init; }
    public string Issuer { get; init; } = "HostGrid";
    public string Audience { get; init; } = "HostGrid.Web";
    public string PublicOrigin { get; init; } = "http://localhost:4200";
}
