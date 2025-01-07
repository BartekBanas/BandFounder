using System.Text.RegularExpressions;
using BandFounder.Domain.Entities;
using BandFounder.Domain.Exceptions;
using BandFounder.Domain.Repositories;
using FluentValidation;

namespace BandFounder.Domain.Validation;

public class AccountValidator : AbstractValidator<Account>
{
    private readonly IRepository<Account> _accountRepository;

    public AccountValidator(IRepository<Account> accountRepository)
    {
        _accountRepository = accountRepository;
        
        RuleFor(account => account.Email).EmailAddress().NotEmpty();
        RuleFor(account => account.Name).Length(3, 32).NotEmpty();
        RuleFor(account => account.PasswordHash).NotEmpty();
        RuleFor(account => account).CustomAsync(ValidateUniqueUsername);
        RuleFor(account => account).CustomAsync(ValidateUsername);
        RuleFor(account => account).CustomAsync(ValidateUniqueEmail);
    }
    
    private async Task ValidateUniqueUsername(Account account, ValidationContext<Account> context, CancellationToken token)
    {
        var matchingAccounts = await _accountRepository.GetAsync
            (existingAccount => existingAccount.Name.ToLower() == account.Name.ToLower());

        if (matchingAccounts.Any())
        {
            throw new ItemDuplicatedException("Account with that username already exists");
        }
    }
    
    private async Task ValidateUsername(Account account, ValidationContext<Account> context, CancellationToken token)
    {
        var regex = new Regex("^[a-zA-Z0-9_-]+$");
        if (!regex.IsMatch(account.Name))
        {
            throw new CustomValidationException("Username can only contain letters, numbers, hyphens, and underscores");
        }

        if (account.Name.StartsWith('-') || account.Name.StartsWith('_'))
        {
            throw new CustomValidationException("Username cannot start with a hyphen or an underscore");
        }

        if (account.Name.EndsWith('-') || account.Name.EndsWith('_'))
        {
            throw new CustomValidationException("Username cannot end with a hyphen or an underscore");
        }
    }
    
    private async Task ValidateUniqueEmail(Account account, ValidationContext<Account> context, CancellationToken token)
    {
        var matchingAccounts = await _accountRepository
            .GetAsync(foundAccount => foundAccount.Email == account.Email);

        if (matchingAccounts.Any())
        {
            throw new ItemDuplicatedException("Account with that email address already exists");
        }
    }
}