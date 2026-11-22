using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace SwiftDrop.Notifications.Business.Email;

public sealed class NotificationEmailOptions
{
    public const string SectionName = "Notifications:Email";
    public bool Enabled { get; set; }
    public string Host { get; set; } = "";
    public int Port { get; set; } = 587;
    public bool EnableSsl { get; set; } = true;
    public string? Username { get; set; }
    public string? Password { get; set; }
    public string FromAddress { get; set; } = "";
    public string TrackingBaseUrl { get; set; } = "";

    internal bool IsValid() => !Enabled ||
        (!string.IsNullOrWhiteSpace(Host) && Port is > 0 and <= 65535 &&
         MailAddress.TryCreate(FromAddress, out _) &&
         Uri.TryCreate(TrackingBaseUrl, UriKind.Absolute, out var url) &&
         url.Scheme is "http" or "https" && string.IsNullOrEmpty(url.Query) && string.IsNullOrEmpty(url.Fragment) &&
         string.IsNullOrEmpty(Username) == string.IsNullOrEmpty(Password) &&
         (EnableSsl || string.IsNullOrEmpty(Username)));
}

internal interface INotificationEmailSender
{
    Task SendAsync(MailAddress recipient, string subject, string body, string messageId, CancellationToken ct);
}

internal sealed class SmtpNotificationEmailSender(IOptions<NotificationEmailOptions> options) : INotificationEmailSender
{
    public async Task SendAsync(MailAddress recipient, string subject, string body, string messageId, CancellationToken ct)
    {
        var settings = options.Value;
        using var message = new MailMessage(new MailAddress(settings.FromAddress), recipient)
        {
            Subject = subject,
            Body = body
        };
        message.Headers.Add("Message-ID", messageId);
        using var smtp = new SmtpClient(settings.Host, settings.Port)
        {
            EnableSsl = settings.EnableSsl,
            UseDefaultCredentials = false
        };
        if (!string.IsNullOrEmpty(settings.Username))
            smtp.Credentials = new NetworkCredential(settings.Username, settings.Password);
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
        timeout.CancelAfter(TimeSpan.FromSeconds(15));
        try
        {
            await smtp.SendMailAsync(message, timeout.Token);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException("SMTP delivery timed out.");
        }
    }
}
