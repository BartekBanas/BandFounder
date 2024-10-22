using System.Security.Claims;
using BandFounder.Application.Dtos;
using BandFounder.Application.Services.Jwt;
using BandFounder.Domain;
using BandFounder.Domain.Entities;
using BandFounder.Infrastructure.Errors.Api;
using FluentValidation;

namespace BandFounder.Application.Services;

public interface IAccountService
{
    Task<AccountDto> GetAccountAsync(Guid accountId);
    Task<Account> GetDetailedAccount(Guid accountId);
    Task<IEnumerable<AccountDto>> GetAccountsAsync();
    Task<IEnumerable<AccountDto>> GetAccountsAsync(int pageSize, int pageNumber);
    Task<string> RegisterAccountAsync(RegisterAccountDto registerDto);
    Task<string> AuthenticateAsync(LoginDto loginDto);
    Task<AccountDto> UpdateAccountAsync(Guid accountId, UpdateAccountDto updateDto);
    Task DeleteAccountAsync(Guid accountId);
}

public class AccountService : IAccountService
{
    private readonly IRepository<Account> _accountRepository;

    private readonly IValidator<Account> _validator;
    private readonly IHashingService _hashingService;
    private readonly IJwtService _jwtService;

    public AccountService(
        IRepository<Account> accountRepository, 
        IValidator<Account> validator,
        IHashingService hashingService,
        IJwtService jwtService)
    {
        _accountRepository = accountRepository;
        _hashingService = hashingService;
        _validator = validator;
        _jwtService = jwtService;
    }

    public async Task<AccountDto> GetAccountAsync(Guid accountId)
    {
        var account = await _accountRepository.GetOneRequiredAsync(accountId);

        var dtos = account.ToDto();

        return dtos;
    }

    public async Task<Account> GetDetailedAccount(Guid accountId)
    {
        return await _accountRepository.GetOneRequiredAsync(
            accountId, "Id", "Artists", "Artists.Genres");
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
        
        await _validator.ValidateAsync(newAccount);

        await _accountRepository.CreateAsync(newAccount);
        
        await _accountRepository.SaveChangesAsync();
        var claims = GenerateClaimsIdentity(newAccount);

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

        var claims = GenerateClaimsIdentity(foundAccount);

        var token = _jwtService.GenerateSymmetricJwtToken(claims);
        return token;
    }
    
    public async Task<AccountDto> UpdateAccountAsync(Guid accountId, UpdateAccountDto updateDto)
    {
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

        await _validator.ValidateAsync(testAccount);
        
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
    
    public async Task DeleteAccountAsync(Guid accountId)
    {
        await _accountRepository.DeleteOneAsync(accountId);

        await _accountRepository.SaveChangesAsync();
    }

    private static ClaimsIdentity GenerateClaimsIdentity(Account account)
    {
        return new ClaimsIdentity(new Claim[]
        {
            new(ClaimTypes.Sid, account.Id.ToString()),
            new(ClaimTypes.PrimarySid, account.Id.ToString()),
            new(ClaimTypes.Name, account.Name),
            new(ClaimTypes.Email, account.Email),
        });
    }
}