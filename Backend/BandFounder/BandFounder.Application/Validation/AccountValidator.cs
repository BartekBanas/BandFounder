using BandFounder.Domain.Entities;
using BandFounder.Infrastructure;
using BandFounder.Infrastructure.Errors;
using FluentValidation;

namespace BandFounder.Application.Validation;

public class AccountValidator : AbstractValidator<Account>
{
    private readonly IRepository<Account> _accountRepository;

    public AccountValidator(IRepository<Account> accountRepository)
    {
        _accountRepository = accountRepository;
        
        RuleFor(account => account.Email).EmailAddress().NotEmpty();
        RuleFor(account => account.Name).Length(3, 32).NotEmpty();
        RuleFor(account => account.PasswordHash).NotEmpty();
        RuleFor(account => account).CustomAsync(ValidateUniqueName);
        RuleFor(account => account).CustomAsync(ValidateUniqueEmail);
    }
    
    private async Task ValidateUniqueName(Account account, ValidationContext<Account> context, CancellationToken token)
    {
        var matchingAccounts = await _accountRepository
            .GetAsync(foundAccount => foundAccount.Name == account.Name);

        if (matchingAccounts.Any())
        {
            throw new ItemDuplicatedErrorException("Account with that username already exists");
        }
    }
    
    private async Task ValidateUniqueEmail(Account account, ValidationContext<Account> context, CancellationToken token)
    {
        var matchingAccounts = await _accountRepository
            .GetAsync(foundAccount => foundAccount.Email == account.Email);

        if (matchingAccounts.Any())
        {
            throw new ItemDuplicatedErrorException("Account with that email address already exists");
        }
    }
}