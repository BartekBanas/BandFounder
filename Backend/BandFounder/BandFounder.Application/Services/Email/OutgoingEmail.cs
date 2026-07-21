namespace BandFounder.Application.Services.Email;

public sealed class OutgoingEmail
{
    public required string To { get; init; }
    public required string Subject { get; init; }
    public required string HtmlBody { get; init; }
    public required string TextBody { get; init; }
}
