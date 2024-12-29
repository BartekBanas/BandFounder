using BandFounder.Domain.Entities;
using FluentValidation;

namespace BandFounder.Domain.Validation;

public class ListingValidator : AbstractValidator<Listing>
{
    private const int MaxLinesInDescription = 5;
    private const int MaxDescriptionLength = 220;
    private const int MinMusicianSlots = 2;
    private const int MaxMusicianSlots = 10;
    
    public ListingValidator()
    {
        RuleFor(listing => listing.Name).NotEmpty()
            .WithMessage("Listing must have a name");
        
        RuleFor(listing => listing.MusicianSlots.Count).GreaterThanOrEqualTo(MinMusicianSlots)
            .WithMessage($"Listing must have at least {MinMusicianSlots} musician slots");
        
        RuleFor(listing => listing.MusicianSlots.Count).LessThanOrEqualTo(MaxMusicianSlots)
            .WithMessage($"Listing must have at most {MaxMusicianSlots} musician slots");
        
        RuleFor(listing => listing.Description)
            .Must(NotExceedMaxNewLines)
            .WithMessage($"Description contains too many new lines. Maximum allowed is {MaxLinesInDescription}");
        
        RuleFor(listing => listing.Description).MaximumLength(MaxDescriptionLength)
            .WithMessage($"Description must be at most {MaxDescriptionLength} characters long");
        
        RuleForEach(listing => listing.MusicianSlots)
            .ChildRules(musicianSlot =>
            {
                musicianSlot.RuleFor(slot => slot.Role.Name).NotEmpty()
                    .WithMessage("Musician role name must not be empty");
            });
    }
    
    private bool NotExceedMaxNewLines(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return true;

        var newLineCount = text.Count(c => c == '\n');
        return newLineCount <= MaxLinesInDescription - 1;
    }
}