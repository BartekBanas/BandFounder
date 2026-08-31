using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Exceptions;
using BandFounder.Application.Services.Email;
using BandFounder.Application.Services.Jwt;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BandFounder.Application.Services;

public interface IEmailVerificationService
{
    Task<EmailVerificationIssuance> IssueForAccountAsync(
        Account account, DateTime utcNow, bool cancelExisting);
    Task<DateTime?> GetResendAvailableAtAsync(Guid accountId);
    Task ConfirmAsync(ConfirmEmailVerificationDto dto);
    Task<EmailVerificationResendDto> ResendAsync(Guid accountId);
    Task<bool> ProcessDueAsync(CancellationToken cancellationToken = default);
    Task ProcessDeliveryAsync(
        Guid tokenId,
        bool throwOnSendFailure = false,
        CancellationToken cancellationToken = default);
}

public sealed record EmailVerificationIssuance(Guid TokenId, DateTime ResendAvailableAt);

public sealed class EmailVerificationService : IEmailVerificationService
{
    private readonly IEmailVerificationStore _store;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;
    private readonly MessageEmailNotificationOptions _deliveryOptions;
    private readonly JwtConfiguration _jwtConfiguration;
    private readonly ILogger<EmailVerificationService> _logger;

    public EmailVerificationService(
        IEmailVerificationStore store,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions,
        IOptions<MessageEmailNotificationOptions> deliveryOptions,
        IOptions<JwtConfiguration> jwtConfiguration,
        ILogger<EmailVerificationService> logger)
    {
        _store = store;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
        _deliveryOptions = deliveryOptions.Value;
        _jwtConfiguration = jwtConfiguration.Value;
        _logger = logger;
    }

    public async Task<EmailVerificationIssuance> IssueForAccountAsync(
        Account account, DateTime utcNow, bool cancelExisting)
    {
        var token = CreateToken(account, utcNow);
        await _store.IssueForAccountAsync(token, utcNow, cancelExisting);

        return new EmailVerificationIssuance(
            token.Id,
            utcNow.Add(GetResendCooldown()));
    }

    public async Task<DateTime?> GetResendAvailableAtAsync(Guid accountId)
    {
        var latestCreatedAt = await _store.GetLatestActiveCreatedAtAsync(accountId);
        return latestCreatedAt?.Add(GetResendCooldown());
    }

    public async Task ConfirmAsync(ConfirmEmailVerificationDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Token))
        {
            throw InvalidToken();
        }

        var status = await _store.ConfirmAsync(
            SecureTokenHelper.HashToken(dto.Token.Trim()),
            DateTime.UtcNow);
        if (status == EmailVerificationConfirmationStatus.Invalid)
        {
            throw InvalidToken();
        }
    }

    public async Task<EmailVerificationResendDto> ResendAsync(Guid accountId)
    {
        var utcNow = DateTime.UtcNow;
        var tokenId = Guid.NewGuid();
        var rawToken = DeriveRawToken(tokenId);
        var issuance = await _store.IssueResendAsync(
            accountId,
            tokenId,
            SecureTokenHelper.HashToken(rawToken),
            utcNow,
            GetTokenTtl(),
            GetResendCooldown());

        if (issuance.Status == EmailVerificationTokenIssuanceStatus.AlreadyVerified)
        {
            return new EmailVerificationResendDto();
        }

        if (issuance.Status == EmailVerificationTokenIssuanceStatus.CooldownActive)
        {
            throw new EmailVerificationResendCooldownException(
                issuance.ResendAvailableAt!.Value);
        }

        await ProcessDeliveryAsync(issuance.Token!.Id);
        return new EmailVerificationResendDto
        {
            ResendAvailableAt = issuance.ResendAvailableAt
        };
    }

    public async Task<bool> ProcessDueAsync(CancellationToken cancellationToken = default)
    {
        var utcNow = DateTime.UtcNow;
        await _store.RecoverStaleDeliveriesAsync(
            utcNow.AddMinutes(-_deliveryOptions.StaleClaimMinutes),
            utcNow,
            _deliveryOptions.MaxAttempts);

        var candidateId = await _store.GetNextDueDeliveryIdAsync(utcNow);
        if (candidateId is null)
        {
            return false;
        }

        await ProcessDeliveryAsync(candidateId.Value, cancellationToken: cancellationToken);
        return true;
    }

    public async Task ProcessDeliveryAsync(
        Guid tokenId,
        bool throwOnSendFailure = false,
        CancellationToken cancellationToken = default)
    {
        var claimedAt = DateTime.UtcNow;
        if (!await _store.TryClaimDeliveryAsync(tokenId, claimedAt))
        {
            return;
        }

        var token = await _store.GetDeliveryAsync(tokenId);
        if (token is null)
        {
            return;
        }

        var attemptCount = token.DeliveryAttemptCount;
        if (token.ConsumedAt is not null ||
            token.ExpiresAt <= DateTime.UtcNow ||
            token.Account.EmailVerifiedAt is not null ||
            !string.Equals(token.Account.Email, token.IntendedEmail, StringComparison.Ordinal))
        {
            await _store.TryMarkDeliveryCancelledAsync(
                token.Id, attemptCount, DateTime.UtcNow);
            return;
        }

        var rawToken = DeriveRawToken(token.Id);
        if (!string.Equals(
                SecureTokenHelper.HashToken(rawToken),
                token.TokenHash,
                StringComparison.Ordinal))
        {
            await _store.TryTransitionDeliveryAfterFailureAsync(
                token.Id,
                attemptCount,
                maxAttempts: 1,
                DateTime.UtcNow,
                DateTime.UtcNow,
                "Email verification signing key no longer matches the queued token.");
            _logger.LogError(
                "Cannot deliver email verification token {TokenId} because its signing key changed",
                token.Id);
            return;
        }

        try
        {
            await _emailSender.SendAsync(BuildEmail(token, rawToken), cancellationToken);
            await _store.TryMarkDeliverySentAsync(
                token.Id, attemptCount, DateTime.UtcNow);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            var failedAt = DateTime.UtcNow;
            await _store.TryTransitionDeliveryAfterFailureAsync(
                token.Id,
                attemptCount,
                _deliveryOptions.MaxAttempts,
                failedAt.AddMinutes(5 * attemptCount),
                failedAt,
                exception.Message);
            _logger.LogError(
                exception,
                "Failed to deliver email verification token {TokenId}; attempt {AttemptCount}",
                token.Id,
                attemptCount);

            if (throwOnSendFailure)
            {
                throw;
            }
        }
    }

    private EmailVerificationToken CreateToken(Account account, DateTime utcNow)
    {
        var tokenId = Guid.NewGuid();
        var rawToken = DeriveRawToken(tokenId);

        return new EmailVerificationToken
        {
            Id = tokenId,
            AccountId = account.Id,
            TokenHash = SecureTokenHelper.HashToken(rawToken),
            IntendedEmail = NormalizeEmail(account.Email),
            CreatedAt = utcNow,
            ExpiresAt = utcNow.Add(GetTokenTtl()),
            DeliveryNotBeforeUtc = utcNow
        };
    }

    private string DeriveRawToken(Guid tokenId)
    {
        return SecureTokenHelper.DeriveRawToken(tokenId, _jwtConfiguration.SecretKey);
    }

    private OutgoingEmail BuildEmail(EmailVerificationToken token, string rawToken)
    {
        var frontendBaseUrl = _emailOptions.FrontendBaseUrl.TrimEnd('/');
        var verificationUrl =
            $"{frontendBaseUrl}/verify-email?token={Uri.EscapeDataString(rawToken)}";
        var ttlMinutes = Math.Max(1, (int)Math.Ceiling(
            (token.ExpiresAt - DateTime.UtcNow).TotalMinutes));
        var ttlDescription = FormatTokenTtl(ttlMinutes);

        return new OutgoingEmail
        {
            To = token.IntendedEmail,
            Subject = "Verify your BandFounder email",
            TextBody =
                $"Verify your email using this link (valid for {ttlDescription}):\n\n{verificationUrl}\n\nIf you did not create this account, you can ignore this email.",
            HtmlBody =
                $"<p>Verify your email using the link below (valid for {ttlDescription}):</p>" +
                $"<p><a href=\"{verificationUrl}\">Verify email</a></p>" +
                "<p>If you did not create this account, you can ignore this email.</p>"
        };
    }

    private TimeSpan GetTokenTtl() => TimeSpan.FromMinutes(
        _emailOptions.EmailVerificationTokenTtlMinutes > 0
            ? _emailOptions.EmailVerificationTokenTtlMinutes
            : 1440);

    private TimeSpan GetResendCooldown() => TimeSpan.FromSeconds(
        _emailOptions.EmailVerificationResendCooldownSeconds > 0
            ? _emailOptions.EmailVerificationResendCooldownSeconds
            : 60);

    private static string FormatTokenTtl(int ttlMinutes)
    {
        if (ttlMinutes >= 60 && ttlMinutes % 60 == 0)
        {
            var hours = ttlMinutes / 60;
            return hours == 1 ? "1 hour" : $"{hours} hours";
        }

        return ttlMinutes == 1 ? "1 minute" : $"{ttlMinutes} minutes";
    }

    private static BadRequestException InvalidToken()
    {
        return new BadRequestException("Invalid or expired email verification token");
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
