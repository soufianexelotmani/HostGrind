namespace HostGrid.Api.Security;

public sealed class SecurityRequestException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;

    internal static SecurityRequestException InvalidProof()
        => new(StatusCodes.Status401Unauthorized, "The credentials or verification code could not be verified.");
}
