using System.Linq.Expressions;
using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Exceptions;
using BandFounder.Application.Services.Email;
using BandFounder.Application.Services.Jwt;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Repositories;
using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BandFounder.Application.Services;

public interface IAccountService
{
    Task<Account> GetAccountAsync(Guid? accountId = null);
    Task<Account> GetDetailedAccount(Guid? accountId = null, params string[] includeProperties);
    Task<IEnumerable<Account>> GetAccountsAsync();
    Task<IEnumerable<Account>> GetAccountsAsync(AccountFilters filters);
    Task<string> RegisterAccountAsync(RegisterAccountDto registerDto);
    Task<string> AuthenticateAsync(LoginDto loginDto);
    Task RequestPasswordResetAsync(RequestPasswordResetDto dto);
    Task CompletePasswordResetAsync(CompletePasswordResetDto dto);
    Task<AccountDto> UpdateAccountAsync(UpdateAccountDto updateDto, Guid? accountId = null);
    Task<IEnumerable<MusicianRole>> GetUserMusicianRoles(Guid? accountId = null);
    Task AddMusicianRole(string role, Guid? accountId = null);
    Task DeleteAccountAsync(Guid? accountId = null);
    Task RemoveMusicianRole(string role, Guid? accountId = null);
    Task ClearUserMusicProfile(Guid? accountId = null);
    Task AddArtist(Guid accountId, string artistName);
    Task UpdateProfilePicture(Guid accountId, IFormFile file);
    Task<ProfilePicture> GetProfilePictureAsync(Guid accountId);
}

public class AccountService : IAccountService
{
    private readonly IRepository<Account> _accountRepository;
    private readonly IRepository<Artist> _artistRepository;
    private readonly IRepository<MusicianRole> _musicianRoleRepository;
    private readonly IRepository<SpotifyTokens> _spotifyTokensRepository;
    private readonly IRepository<PasswordResetToken> _passwordResetTokenRepository;
    private readonly IPasswordResetTokenStore _passwordResetTokenStore;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IChatroomService _chatroomService;

    private readonly IValidator<Account> _validator;
    private readonly IAuthenticationService _authenticationService;
    private readonly IHashingService _hashingService;
    private readonly IJwtService _jwtService;
    private readonly IEmailSender _emailSender;
    private readonly EmailOptions _emailOptions;
    private readonly ILogger<AccountService> _logger;

    public AccountService(
        IRepository<Account> accountRepository, 
        IRepository<Artist> artistRepository,
        IRepository<MusicianRole> musicianRoleRepository,
        IRepository<SpotifyTokens> spotifyTokensRepository,
        IRepository<PasswordResetToken> passwordResetTokenRepository,
        IPasswordResetTokenStore passwordResetTokenStore,
        IUnitOfWork unitOfWork,
        IChatroomService chatroomService,
        IValidator<Account> validator,
        IAuthenticationService authenticationService,
        IHashingService hashingService,
        IJwtService jwtService,
        IEmailSender emailSender,
        IOptions<EmailOptions> emailOptions,
        ILogger<AccountService> logger)
    {
        _accountRepository = accountRepository;
        _artistRepository = artistRepository;
        _musicianRoleRepository = musicianRoleRepository;
        _spotifyTokensRepository = spotifyTokensRepository;
        _passwordResetTokenRepository = passwordResetTokenRepository;
        _passwordResetTokenStore = passwordResetTokenStore;
        _unitOfWork = unitOfWork;
        _chatroomService = chatroomService;
        _validator = validator;
        _authenticationService = authenticationService;
        _hashingService = hashingService;
        _jwtService = jwtService;
        _emailSender = emailSender;
        _emailOptions = emailOptions.Value;
        _logger = logger;
    }

    public async Task<Account> GetAccountAsync(Guid? accountId = null)
    {
        accountId ??= _authenticationService.GetUserId();
        var account = await _accountRepository.GetOneRequiredAsync(accountId);

        return account;
    }

    public async Task<Account> GetDetailedAccount(Guid? accountId = null, params string[] includeProperties)
    {
        accountId ??= _authenticationService.GetUserId();

        if (includeProperties is { Length: > 0 })
        {
            return await _accountRepository.GetOneRequiredAsync(key: accountId, keyPropertyName: "Id",
                includeProperties: includeProperties);
        }

        return await _accountRepository.GetOneRequiredAsync(key: accountId, keyPropertyName: "Id",
            includeProperties:
            [
                nameof(Account.Artists), "Artists.Genres",
                nameof(Account.Chatrooms), nameof(Account.MusicianRoles), nameof(Account.SpotifyTokens)
            ]);
    }

    public async Task<IEnumerable<Account>> GetAccountsAsync()
    {
        var accounts = await _accountRepository.GetAsync();

        return accounts;
    }

    public async Task<IEnumerable<Account>> GetAccountsAsync(AccountFilters filters)
    {
        Expression<Func<Account, bool>>? filter = null;
        
        if (!string.IsNullOrEmpty(filters.Username))
        {
            var account = await _accountRepository.GetOneAsync
                (account => account.Name.ToLower() == filters.Username.ToLower());
            
            return account is not null ? new[] { account } : [];
        }
        
        if (!string.IsNullOrEmpty(filters.UsernameFragment))
        {
            filter = account => account.Name.Contains(filters.UsernameFragment);
        }
        
        if (filters is { PageSize: not null, PageNumber: not null })
        {
            return await _accountRepository.GetAsync(filters.PageSize.Value, filters.PageNumber.Value, filter);
        }
        
        return await _accountRepository.GetAsync(filter);
    }

    public async Task<string> RegisterAccountAsync(RegisterAccountDto registerDto)
    {
        var passwordHash = _hashingService.HashPassword(registerDto.Password);
        var normalizedEmail = NormalizeEmail(registerDto.Email);

        var newAccount = new Account()
        {
            Id = new Guid(),
            Email = normalizedEmail,
            Name = registerDto.Name,
            PasswordHash = passwordHash,
            DateCreated = DateTime.UtcNow,
        };
        
        var validationResult = await _validator.ValidateAsync(newAccount);
        if (validationResult.IsValid is false)
        {
            throw new ValidationException(validationResult.Errors);
        }

        await _accountRepository.CreateAsync(newAccount);
        
        await _accountRepository.SaveChangesAsync();
        var claims = _authenticationService.GenerateClaimsIdentity(newAccount);

        var token = _jwtService.GenerateSymmetricJwtToken(claims);
        return token;
    }

    public async Task<string> AuthenticateAsync(LoginDto loginDto)
    {
        var usernameOrEmail = loginDto.UsernameOrEmail.Trim();
        var normalizedEmail = NormalizeEmail(usernameOrEmail);

        var foundAccount = await _accountRepository
            .GetOneAsync(account =>
                account.Name == usernameOrEmail || account.Email.ToLower() == normalizedEmail);

        if (foundAccount is null)
            throw new ForbiddenException("Incorrect login details");

        if (!_hashingService.VerifyPassword(foundAccount, loginDto.Password))
            throw new ForbiddenException("Incorrect login details");

        var claims = _authenticationService.GenerateClaimsIdentity(foundAccount);

        var token = _jwtService.GenerateSymmetricJwtToken(claims);
        return token;
    }

    public async Task RequestPasswordResetAsync(RequestPasswordResetDto dto)
    {
        var normalizedEmail = NormalizeEmail(dto.Email);
        var account = await _accountRepository.GetOneAsync(a => a.Email.ToLower() == normalizedEmail);

        if (account is null)
        {
            return;
        }

        var rawToken = PasswordResetTokenHelper.GenerateRawToken();
        var tokenHash = PasswordResetTokenHelper.HashToken(rawToken);
        var ttlMinutes = _emailOptions.PasswordResetTokenTtlMinutes > 0
            ? _emailOptions.PasswordResetTokenTtlMinutes
            : 15;
        var utcNow = DateTime.UtcNow;

        var resetToken = new PasswordResetToken
        {
            Id = Guid.NewGuid(),
            AccountId = account.Id,
            TokenHash = tokenHash,
            CreatedAt = utcNow,
            ExpiresAt = utcNow.AddMinutes(ttlMinutes)
        };

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _passwordResetTokenStore.ConsumeAllForAccountAsync(account.Id, utcNow);
            await _passwordResetTokenRepository.CreateAsync(resetToken);
            await _passwordResetTokenRepository.SaveChangesAsync();
        });

        var frontendBaseUrl = _emailOptions.FrontendBaseUrl.TrimEnd('/');
        var resetUrl = $"{frontendBaseUrl}/reset-password?token={Uri.EscapeDataString(rawToken)}";

        try
        {
            await _emailSender.SendAsync(new OutgoingEmail
            {
                To = account.Email,
                Subject = "Reset your BandFounder password",
                TextBody = $"Reset your password using this link (valid for {ttlMinutes} minutes):\n\n{resetUrl}\n\nIf you did not request this, you can ignore this email.",
                HtmlBody =
                    $"<p>Reset your password using the link below (valid for {ttlMinutes} minutes):</p>" +
                    $"<p><a href=\"{resetUrl}\">Reset password</a></p>" +
                    "<p>If you did not request this, you can ignore this email.</p>"
            });
        }
        catch (Exception ex)
        {
            await _passwordResetTokenRepository.DeleteOneAsync(resetToken.Id);
            await _passwordResetTokenRepository.SaveChangesAsync();
            _logger.LogError(ex, "Failed to send password reset email to {Email}", account.Email);
            throw;
        }
    }

    public async Task CompletePasswordResetAsync(CompletePasswordResetDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Token) || string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            throw new BadRequestException("Invalid password reset request");
        }

        if (dto.NewPassword.Length < 8)
        {
            throw new BadRequestException("Password must be at least 8 characters long");
        }

        var tokenHash = PasswordResetTokenHelper.HashToken(dto.Token.Trim());
        var utcNow = DateTime.UtcNow;

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var accountId = await _passwordResetTokenStore.TryConsumeAsync(tokenHash, utcNow);

            if (accountId is null)
            {
                throw new BadRequestException("Invalid or expired password reset token");
            }

            await _passwordResetTokenStore.ConsumeAllForAccountAsync(accountId.Value, utcNow);

            var account = await _accountRepository.GetOneRequiredAsync(accountId.Value);
            account.PasswordHash = _hashingService.HashPassword(dto.NewPassword);
            account.PasswordVersion++;

            await _accountRepository.SaveChangesAsync();
        });
    }
    
    public async Task<AccountDto> UpdateAccountAsync(UpdateAccountDto updateDto, Guid? accountId = null)
    {
        accountId ??= _authenticationService.GetUserId();
        
        var originalAccount = _accountRepository.GetOneRequiredAsync(accountId).Result;

        string? passwordHash = null;
        
        if (updateDto.Password != null)
        {
            passwordHash = _hashingService.HashPassword(updateDto.Password);
        }

        var normalizedUpdateEmail = updateDto.Email is null ? null : NormalizeEmail(updateDto.Email);

        var testAccount = new Account
        {
            Id = new Guid(),
            DateCreated = originalAccount.DateCreated,

            Name = updateDto.Name ?? "testAccountName",
            Email = normalizedUpdateEmail ?? "testAccountEmail@email.com",
            PasswordHash = passwordHash ?? "testAccountPassword"
        };

        var validationResult = await _validator.ValidateAsync(testAccount);
        if (validationResult.IsValid is false)
        {
            throw new ValidationException(validationResult.Errors);
        }
        
        var updatedAccount = new Account
        {
            Id = originalAccount.Id,
            DateCreated = originalAccount.DateCreated,

            Name = updateDto.Name ?? originalAccount.Name,
            Email = normalizedUpdateEmail ?? originalAccount.Email,
            PasswordHash = passwordHash ?? originalAccount.PasswordHash,
            PasswordVersion = passwordHash is null
                ? originalAccount.PasswordVersion
                : originalAccount.PasswordVersion + 1
        };

        if (originalAccount.Equals(updatedAccount))
        {
            throw new BadRequestException("Updated account is indifferent to the original");
        }

        await _accountRepository.UpdateAsync(updatedAccount, accountId);
        await _accountRepository.SaveChangesAsync();

        var dto = updatedAccount.ToDto();

        return dto;
    }

    public async Task AddMusicianRole(string role, Guid? accountId = null)
    {
        accountId ??= _authenticationService.GetUserId();
        
        var account = await _accountRepository.GetOneRequiredAsync(
            key: accountId, keyPropertyName:nameof(Account.Id), includeProperties: nameof(Account.MusicianRoles));

        MusicianRole musicianRole;
        try
        {
            musicianRole = await _musicianRoleRepository.GetOrCreateAsync(role);
        }
        catch (ArgumentException exception)
        {
            throw new BadRequestException(exception.Message);
        }
        
        if (account.MusicianRoles.Any(currentRole => currentRole.Name == musicianRole.Name))
        {
            throw new RedundantRequestException("Role is already added to the account");
        }
        
        account.MusicianRoles.Add(musicianRole);
        
        await _accountRepository.SaveChangesAsync();
    }

    public async Task<IEnumerable<MusicianRole>> GetUserMusicianRoles(Guid? accountId = null)
    {
        accountId ??= _authenticationService.GetUserId();

        var account = await _accountRepository.GetOneRequiredAsync(
            key: accountId, keyPropertyName:nameof(Account.Id), includeProperties: nameof(Account.MusicianRoles));
        
        return account.MusicianRoles;
    }

    public async Task DeleteAccountAsync(Guid? accountId = null)
    {
        accountId ??= _authenticationService.GetUserId();

        var account = await GetDetailedAccount(accountId: accountId, includeProperties: nameof(Account.Chatrooms));
        
        await _chatroomService.LeaveAllChatrooms(account);
        await _accountRepository.DeleteOneAsync(accountId);

        await _accountRepository.SaveChangesAsync();
    }
    
    public async Task RemoveMusicianRole(string role, Guid? accountId = null)
    {
        accountId ??= _authenticationService.GetUserId();
        
        var account = await GetDetailedAccount(accountId);
        
        role = role.NormalizeName();
        var musicianRole = await _musicianRoleRepository.GetOneAsync(r => r.Name == role);
        
        if (musicianRole == null)
        {
            throw new NotFoundException("Role not found.");
        }
        
        account.MusicianRoles.Remove(musicianRole);
        
        await _accountRepository.SaveChangesAsync();
    }

    public async Task ClearUserMusicProfile(Guid? accountId = null)
    {
        accountId ??= _authenticationService.GetUserId();
        var account = await GetDetailedAccount(accountId);

        if (account.SpotifyTokens is not null)
        {
            await _spotifyTokensRepository.DeleteOneAsync(account.SpotifyTokens.AccountId);
        }
        
        account.Artists.Clear();
        
        await _accountRepository.SaveChangesAsync();
    }

    public async Task AddArtist(Guid accountId, string artistName)
    {
        var account = await GetDetailedAccount(accountId);
        if (account.Id != _authenticationService.GetUserId())
        {
            throw new ForbiddenException("You cannot add artists to this account");
        }

        if (account.Artists.Select(artist => artist.Name).Contains(artistName))
        {
            throw new BadRequestException($"Artist {artistName} is already linked to this account");
        }
        
        Artist artist;
        try
        {
            artist = await _artistRepository.GetOrCreateAsync(artistName);
        }
        catch (ArgumentException exception)
        {
            throw new BadRequestException(exception.Message);
        }
        
        account.Artists.Add(artist);

        await _accountRepository.SaveChangesAsync();
    }
    
    public async Task UpdateProfilePicture(Guid accountId, IFormFile file)
    {
        if (accountId != _authenticationService.GetUserId())
        {
            throw new ForbiddenException("You cannot update profile picture of another user");
        }
        
        var account = await _accountRepository.GetOneRequiredAsync(
            key: accountId, includeProperties: nameof(Account.ProfilePicture));
        
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var pictureBytes = memoryStream.ToArray();
        
        var mimeType = file.ContentType;
        
        if (account.ProfilePicture is not null)
        {
            account.ProfilePicture.ImageData = pictureBytes;
        }
        else
        {
            account.ProfilePicture = new ProfilePicture()
            {
                AccountId = accountId,
                ImageData = pictureBytes,
                MimeType = mimeType
            };
        }
        
        await _accountRepository.SaveChangesAsync();
    }
    
    public async Task<ProfilePicture> GetProfilePictureAsync(Guid accountId)
    {
        var account = await _accountRepository.GetOneRequiredAsync(
            key: accountId, includeProperties: nameof(Account.ProfilePicture));
        
        if (account.ProfilePicture == null || account.ProfilePicture.ImageData.Length == 0)
        {
            throw new NotFoundException("Profile picture not found");
        }

        return account.ProfilePicture;
    }

    private static string NormalizeEmail(string email) => email.Trim().ToLowerInvariant();
}
