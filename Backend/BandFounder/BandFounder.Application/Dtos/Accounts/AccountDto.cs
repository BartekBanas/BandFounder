namespace BandFounder.Application.Dtos.Accounts;

public class AccountDto
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public required string Email { get; init; }
}

public sealed class AccountSettingsDto : AccountDto
{
    public required bool EmailOnNewMessage { get; init; }

    public required int EmailUnreadDelayMinutes { get; init; }

    public required bool EmailVerified { get; init; }

    public DateTime? ResendAvailableAt { get; init; }
}

public sealed class EmailVerificationResendDto
{
    public DateTime? ResendAvailableAt { get; init; }
}