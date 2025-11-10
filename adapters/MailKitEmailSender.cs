using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using MimeKit.Text;
using StockQuoteAlert.Helpers;

namespace StockQuoteAlert.Adapters;

/// <summary>
/// Email sender implementation backed by MailKit, following Microsoft's recommended guidance.
/// </summary>
public class MailKitEmailSender : INotificationSender
{
    private readonly EmailConfiguration _config;

    public MailKitEmailSender(EmailConfiguration config)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
    }

    public async Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var mimeMessage = BuildMimeMessage(message);

        using var smtpClient = new SmtpClient();

        try
        {
            var secureSocketOption = _config.UseSsl ? SecureSocketOptions.SslOnConnect : SecureSocketOptions.StartTlsWhenAvailable;

            await smtpClient.ConnectAsync(_config.SmtpHost, _config.SmtpPort, secureSocketOption, cancellationToken);

            if (!string.IsNullOrWhiteSpace(_config.Username))
            {
                await smtpClient.AuthenticateAsync(_config.Username, _config.Password, cancellationToken);
            }

            await smtpClient.SendAsync(mimeMessage, cancellationToken);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to send notification via SMTP: {ex.Message}", ex);
        }
        finally
        {
            if (smtpClient.IsConnected)
            {
                await smtpClient.DisconnectAsync(true, cancellationToken);
            }
        }
    }

    private MimeMessage BuildMimeMessage(NotificationMessage message)
    {
        var mimeMessage = new MimeMessage();
        mimeMessage.From.Add(MailboxAddress.Parse(_config.FromEmail));
        mimeMessage.To.Add(MailboxAddress.Parse(_config.ToEmail));
        mimeMessage.Subject = message.Subject;
        mimeMessage.Body = new TextPart(TextFormat.Plain)
        {
            Text = message.Body
        };

        return mimeMessage;
    }
}
