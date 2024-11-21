using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Error;
using BandFounder.Application.Services.Jwt;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;
using FluentValidation;
using Microsoft.AspNetCore.Http;

namespace BandFounder.Application.Services;

public interface IAccountService
{
    Task<Account> GetAccountAsync(Guid? accountId = null);
    Task<Account> GetAccountAsync(string username);
    Task<Account> GetDetailedAccount(Guid? accountId = null);
    Task<IEnumerable<AccountDto>> GetAccountsAsync();
    Task<IEnumerable<AccountDto>> GetAccountsAsync(int pageSize, int pageNumber);
    Task<string> RegisterAccountAsync(RegisterAccountDto registerDto);
    Task<string> AuthenticateAsync(LoginDto loginDto);
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

    private readonly IValidator<Account> _validator;
    private readonly IAuthenticationService _authenticationService;
    private readonly IHashingService _hashingService;
    private readonly IJwtService _jwtService;

    public AccountService(
        IRepository<Account> accountRepository, 
        IRepository<Artist> artistRepository,
        IRepository<MusicianRole> musicianRoleRepository,
        IRepository<SpotifyTokens> spotifyTokensRepository,
        IValidator<Account> validator,
        IAuthenticationService authenticationService,
        IHashingService hashingService,
        IJwtService jwtService)
    {
        _accountRepository = accountRepository;
        _artistRepository = artistRepository;
        _musicianRoleRepository = musicianRoleRepository;
        _spotifyTokensRepository = spotifyTokensRepository;
        _validator = validator;
        _authenticationService = authenticationService;
        _hashingService = hashingService;
        _jwtService = jwtService;
    }

    public async Task<Account> GetAccountAsync(Guid? accountId = null)
    {
        accountId ??= _authenticationService.GetUserId();
        var account = await _accountRepository.GetOneRequiredAsync(accountId);

        return account;
    }

    public async Task<Account> GetAccountAsync(string username)
    {
        var account = await _accountRepository.GetOneRequiredAsync(account => account.Name.ToLower() == username.ToLower());

        return account;
    }

    public async Task<Account> GetDetailedAccount(Guid? accountId = null)
    {
        accountId ??= _authenticationService.GetUserId();

        return await _accountRepository.GetOneRequiredAsync(key: accountId, keyPropertyName: "Id",
            includeProperties:
            [
                nameof(Account.Artists), "Artists.Genres",
                nameof(Account.Chatrooms), nameof(Account.MusicianRoles), nameof(Account.SpotifyTokens)
            ]);
    }

    public async Task<IEnumerable<AccountDto>> GetAccountsAsync()
    {
        var accounts = await _accountRepository.GetAsync();

        var dtos = accounts.ToDto();
        
        return dtos;
    }

    public async Task<IEnumerable<AccountDto>> GetAccountsAsync(int pageSize, int pageNumber)
    {
        var pagedAccounts = await _accountRepository.GetAsync(pageSize, pageNumber);
        var dtos = pagedAccounts.ToDto();
        
        return dtos;
    }

    public async Task<string> RegisterAccountAsync(RegisterAccountDto registerDto)
    {
        var passwordHash = _hashingService.HashPassword(registerDto.Password);

        var newAccount = new Account()
        {
            Id = new Guid(),
            Email = registerDto.Email,
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
        var foundAccount = await _accountRepository
            .GetOneAsync(account =>
                account.Name == loginDto.UsernameOrEmail || account.Email == loginDto.UsernameOrEmail);

        if (foundAccount is null)
            throw new ForbiddenError("Username of email is incorrect");

        if (!_hashingService.VerifyPassword(foundAccount, loginDto.Password))
            throw new ForbiddenError("Password is incorrect");

        var claims = _authenticationService.GenerateClaimsIdentity(foundAccount);

        var token = _jwtService.GenerateSymmetricJwtToken(claims);
        return token;
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

        var testAccount = new Account
        {
            Id = new Guid(),
            DateCreated = originalAccount.DateCreated,

            Name = updateDto.Name ?? "testAccountName",
            Email = updateDto.Email ?? "testAccountEmail@email.com",
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
            Email = updateDto.Email ?? originalAccount.Email,
            PasswordHash = passwordHash ?? originalAccount.PasswordHash
        };

        if (originalAccount.Equals(updatedAccount))
        {
            throw new BadRequestError("Updated account is indifferent to the original");
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
            throw new BadRequestError(exception.Message);
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
            throw new NotFoundError("Role not found.");
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
            throw new ForbiddenError("You cannot add artists to this account");
        }

        if (account.Artists.Select(artist => artist.Name).Contains(artistName))
        {
            throw new BadRequestError($"Artist {artistName} is already linked to this account");
        }
        
        Artist artist;
        try
        {
            artist = await _artistRepository.GetOrCreateAsync(artistName);
        }
        catch (ArgumentException exception)
        {
            throw new BadRequestError(exception.Message);
        }
        
        account.Artists.Add(artist);

        await _accountRepository.SaveChangesAsync();
    }
    
    public async Task UpdateProfilePicture(Guid accountId, IFormFile file)
    {
        if (accountId != _authenticationService.GetUserId())
        {
            throw new ForbiddenError("You cannot update profile picture of another user");
        }
        
        var account = await _accountRepository.GetOneRequiredAsync(
            key: accountId, includeProperties: nameof(Account.ProfilePicture));
        
        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var pictureBytes = memoryStream.ToArray();
        
        if (account.ProfilePicture is not null)
        {
            account.ProfilePicture.ImageData = pictureBytes;
        }
        else
        {
            account.ProfilePicture = new ProfilePicture()
            {
                AccountId = accountId,
                ImageData = pictureBytes
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
            throw new NotFoundError("Profile picture not found");
        }

        return account.ProfilePicture;
    }
}