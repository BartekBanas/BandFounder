using Microsoft.Extensions.Logging;

namespace BandFounder.Application.Services.Email;

public sealed class LoggingEmailSender : IEmailSender
{
    private readonly ILogger<LoggingEmailSender> _logger;

    public LoggingEmailSender(ILogger<LoggingEmailSender> logger)
    {
        _logger = logger;
    }

    public Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Email (dev/logging sender) To={Recipient} Subject={Subject}\nTextBody:\n{TextBody}\nHtmlBody:\n{HtmlBody}",
            email.To,
            email.Subject,
            email.TextBody,
            email.HtmlBody);

        return Task.CompletedTask;
    }
}
