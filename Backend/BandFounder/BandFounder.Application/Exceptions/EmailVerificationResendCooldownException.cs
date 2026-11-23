namespace BandFounder.Application.Exceptions;

public sealed class EmailVerificationResendCooldownException : Exception
{
    public EmailVerificationResendCooldownException(DateTime resendAvailableAt)
        : base("Please wait before requesting another verification email.")
    {
        ResendAvailableAt = resendAvailableAt;
    }

    public DateTime ResendAvailableAt { get; }
}
