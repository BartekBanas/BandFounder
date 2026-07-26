namespace BandFounder.Application.Services.Email;

public interface IEmailSender
{
    Task SendAsync(OutgoingEmail email, CancellationToken cancellationToken = default);
}
