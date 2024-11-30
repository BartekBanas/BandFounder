using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Error;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;

namespace BandFounder.Application.Services;

public interface IPasswordResetService
{
    Task SendResetPasswordEmailAsync(RequestPasswordResetDto dto);
    Task ResetPasswordAsync(PasswordResetDto resetDto);
}

public class PasswordResetService : IPasswordResetService
{
    private readonly IAccountService _accountService;
    private readonly IAuthenticationService _authenticationService;
    private readonly IEmailService _emailService;
    private readonly IHashingService _hashingService;
    private readonly IRepository<PasswordResetToken> _repository;

    public PasswordResetService(IAccountService accountService, IAuthenticationService authenticationService, 
        IEmailService emailService, IHashingService hashingService, IRepository<PasswordResetToken> repository)
    {
        _accountService = accountService;
        _authenticationService = authenticationService;
        _emailService = emailService;
        _hashingService = hashingService;
        _repository = repository;
    }

    public async Task SendResetPasswordEmailAsync(RequestPasswordResetDto dto)
    {
        var userId = _authenticationService.GetUserId();
        var account = await _accountService.GetAccountAsync(userId);

        var token = await CreatePasswordResetTokenAsync(account);

        var resetLink = dto.ResetPasswordPageUrl + $"?token={token}";
        await _emailService.SendEmailAsync(account.Email, "Password Reset", 
            $"Reset your password using this link: {resetLink}");
    }

    public async Task ResetPasswordAsync(PasswordResetDto resetDto)
    {
        var userId = _authenticationService.GetUserId();
        var existingToken = await _repository.GetOneAsync(resetToken => resetToken.AccountId == userId);
        if (existingToken is null || existingToken.ExpirationDate < DateTime.UtcNow)
        {
            throw new BadRequestError("Provided token is expired");
        }
        
        if (resetDto.Token != existingToken.TokenHash)
        {
            throw new BadRequestError("Provided token is invalid");
        }

        await _repository.DeleteOneAsync(existingToken.Id);
        
        var hashedPassword = _hashingService.HashPassword(resetDto.NewPassword);
        var account = await _accountService.GetAccountAsync(userId);
        account.PasswordHash = hashedPassword;
        
        await _repository.SaveChangesAsync();
    }
    
    private async Task<string> CreatePasswordResetTokenAsync(Account account)
    {
        var token = Guid.NewGuid().ToString();
        var hashedToken = _hashingService.HashPassword(token);
        
        var existingToken = await _repository.GetOneAsync(resetToken => resetToken.AccountId == account.Id);
        if (existingToken is not null)
        {
            await _repository.DeleteOneAsync(existingToken.Id);
        }
        
        var passwordResetToken = new PasswordResetToken
        {
            TokenHash = hashedToken,
            AccountId = account.Id,
            ExpirationDate = DateTime.UtcNow.AddMinutes(10)
        };
        
        await _repository.CreateAsync(passwordResetToken);
        await _repository.SaveChangesAsync();
        
        return hashedToken;
    }
}