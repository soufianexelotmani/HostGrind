using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace HostGrid.Api.Integrations.Email;

public interface ISecurityEmail
{
    Task SendAsync(string recipient, string subject, string body, CancellationToken token);
}

internal sealed class SmtpSecurityEmail(IConfiguration configuration, IHostEnvironment environment) : ISecurityEmail
{
    public async Task SendAsync(string recipient, string subject, string body, CancellationToken token)
    {
        var host = configuration["Email:Host"] ?? (environment.IsDevelopment() ? "127.0.0.1" : null)
            ?? throw new InvalidOperationException("Email:Host is required.");
        var port = configuration.GetValue("Email:Port", environment.IsDevelopment() ? 1025 : 587);
        var from = configuration["Email:From"] ?? (environment.IsDevelopment() ? "hostgrid@example.test" : null)
            ?? throw new InvalidOperationException("Email:From is required.");
        var allowPlainLocal = environment.IsDevelopment() && host is "127.0.0.1" or "localhost";
        using var smtp = new SmtpClient { Timeout = 10000 };
        using var message = new MimeMessage { Subject = subject, Body = new TextPart("plain") { Text = body } };
        message.From.Add(MailboxAddress.Parse(from));
        message.To.Add(MailboxAddress.Parse(recipient));
        await smtp.ConnectAsync(host, port, allowPlainLocal ? SecureSocketOptions.None : SecureSocketOptions.StartTls, token);
        if (configuration["Email:Username"] is { Length: > 0 } username)
        {
            await smtp.AuthenticateAsync(username, configuration["Email:Password"]
                ?? throw new InvalidOperationException("Email:Password is required with a username."), token);
        }
        await smtp.SendAsync(new SendRequest(message), token);
        await smtp.DisconnectAsync(true, token);
    }
}
