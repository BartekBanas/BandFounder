using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Resend;

namespace BandFounder.Application.Services.Email;

public sealed class ResendEmailSender : IEmailSender
{
    private readonly IResend _resend;
    private readonly EmailOptions _options;
    private readonly ILogger<ResendEmailSender> _logger;

    public ResendEmailSender(IResend resend, IOptions<EmailOptions> options, ILogger<ResendEmailSender> logger)
    {
        _resend = resend;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default)
    {
        var message = new EmailMessage
        {
            From = _options.FromAddress,
            Subject = email.Subject,
            HtmlBody = email.HtmlBody,
            TextBody = email.TextBody
        };
        message.To.Add(email.To);

        try
        {
            await _resend.EmailSendAsync(message, cancellationToken);
            _logger.LogInformation("Sent email to {Recipient} with subject {Subject}", email.To, email.Subject);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send email to {Recipient} with subject {Subject}", email.To, email.Subject);
            throw;
        }
    }
}
