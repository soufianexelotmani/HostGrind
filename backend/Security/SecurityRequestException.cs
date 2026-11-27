namespace HostGrid.Api.Security;

public sealed class SecurityRequestException(int status, string message, string? code = null) : Exception(message)
{
    public int Status { get; } = status;
    public string? Code { get; } = code;

    internal static SecurityRequestException InvalidProof()
        => new(StatusCodes.Status401Unauthorized, "The credentials or verification code could not be verified.");
}
