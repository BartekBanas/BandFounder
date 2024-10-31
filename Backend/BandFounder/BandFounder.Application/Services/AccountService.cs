using BandFounder.Application.Dtos;
using BandFounder.Application.Dtos.Accounts;
using BandFounder.Application.Error;
using BandFounder.Application.Services.Jwt;
using BandFounder.Domain;
using BandFounder.Domain.Entities;
using FluentValidation;

namespace BandFounder.Application.Services;

public interface IAccountService
{
    Task<AccountDto> GetAccountAsync(Guid? accountId = null);
    Task<Account> GetDetailedAccount(Guid accountId);
    Task<IEnumerable<AccountDto>> GetAccountsAsync();
    Task<IEnumerable<AccountDto>> GetAccountsAsync(int pageSize, int pageNumber);
    Task<string> RegisterAccountAsync(RegisterAccountDto registerDto);
    Task<string> AuthenticateAsync(LoginDto loginDto);
    Task<AccountDto> UpdateAccountAsync(UpdateAccountDto updateDto, Guid? accountId = null);
    Task DeleteAccountAsync(Guid? accountId = null);
    Task AddMusicianRole(string role, Guid? accountId = null);
    Task RemoveMusicianRole(string role, Guid? accountId = null);
}

public class AccountService : IAccountService
{
    private readonly IRepository<Account> _accountRepository;
    private readonly IRepository<MusicianRole> _musicianRoleRepository;

    private readonly IValidator<Account> _validator;
    private readonly IAuthenticationService _authenticationService;
    private readonly IHashingService _hashingService;
    private readonly IJwtService _jwtService;

    public AccountService(
        IRepository<Account> accountRepository,
        IRepository<MusicianRole> musicianRoleRepository,
        IValidator<Account> validator,
        IAuthenticationService authenticationService,
        IHashingService hashingService,
        IJwtService jwtService)
    {
        _accountRepository = accountRepository;
        _musicianRoleRepository = musicianRoleRepository;
        _validator = validator;
        _authenticationService = authenticationService;
        _hashingService = hashingService;
        _jwtService = jwtService;
    }

    public async Task<AccountDto> GetAccountAsync(Guid? accountId = null)
    {
        accountId ??= _authenticationService.GetUserId();
        var account = await _accountRepository.GetOneRequiredAsync(accountId);

        var dtos = account.ToDto();

        return dtos;
    }

    public async Task<Account> GetDetailedAccount(Guid accountId)
    {
        return await _accountRepository.GetOneRequiredAsync(
            accountId, "Id", "Artists", "Artists.Genres", "Chatrooms", "MusicianRoles");
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

        if (foundAccount == default)
            throw new ForbiddenError();

        if (!_hashingService.VerifyPassword(foundAccount, loginDto.Password))
            throw new ForbiddenError();

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
    
    public async Task DeleteAccountAsync(Guid? accountId = null)
    {
        accountId ??= _authenticationService.GetUserId();
        
        await _accountRepository.DeleteOneAsync(accountId);

        await _accountRepository.SaveChangesAsync();
    }

    public async Task AddMusicianRole(string role, Guid? accountId = null)
    {
        accountId ??= _authenticationService.GetUserId();
        
        var account = await _accountRepository.GetOneRequiredAsync(accountId);
        
        var musicianRole = await _musicianRoleRepository.GetOrCreateAsync(role);
        
        account.MusicianRoles.Add(musicianRole);
        
        await _accountRepository.SaveChangesAsync();
    }
    
    public async Task RemoveMusicianRole(string role, Guid? accountId = null)
    {
        accountId ??= _authenticationService.GetUserId();
        
        var account = await GetDetailedAccount((Guid)accountId);
        
        role = role.NormalizeName();
        var musicianRole = await _musicianRoleRepository.GetOneAsync(r => r.RoleName == role);
        
        if (musicianRole == null)
        {
            throw new NotFoundError("Role not found.");
        }
        
        account.MusicianRoles.Remove(musicianRole);
        
        await _accountRepository.SaveChangesAsync();
    }
}