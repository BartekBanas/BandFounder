using BandFounder.Domain.Entities;
using BandFounder.Domain.Validation;
using FluentValidation.TestHelper;

namespace Domain.Tests;

[TestFixture]
public class ListingValidatorTests
{
    private ListingValidator _validator;

    [SetUp]
    public void SetUp()
    {
        _validator = new ListingValidator();
    }

    [Test]
    public void Should_Have_Error_When_Name_Is_Empty()
    {
        var listing = new Listing
        {
            Name = string.Empty,
            MusicianSlots = new List<MusicianSlot>
            {
                new() { Role = new MusicianRole { Name = "Test Role" } },
                new() { Role = new MusicianRole { Name = "Test Role" } },
            },
            Description = "Valid description",
            OwnerId = default,
            Type = ListingType.Band
        };

        var result = _validator.TestValidate(listing);
        result.ShouldHaveValidationErrorFor(l => l.Name);
    }

    [Test]
    public void Should_Have_Error_When_MusicianSlots_Are_Less_Than_Minimum()
    {
        var listing = new Listing
        {
            Name = "Valid Name",
            MusicianSlots = new List<MusicianSlot>
            {
                new() { Role = new MusicianRole { Name = "Test Role" } },
            },
            Description = "Valid description",
            OwnerId = default,
            Type = ListingType.Band
        };

        var result = _validator.TestValidate(listing);
        result.ShouldHaveValidationErrorFor(l => l.MusicianSlots.Count);
    }

    [Test]
    public void Should_Have_Error_When_MusicianSlots_Are_More_Than_Maximum()
    {
        var listing = new Listing
        {
            Name = "Valid Name",
            MusicianSlots = new List<MusicianSlot>(new MusicianSlot[11]),
            Description = "Valid description",
            OwnerId = default,
            Type = ListingType.Band
        };

        var result = _validator.TestValidate(listing);
        result.ShouldHaveValidationErrorFor(l => l.MusicianSlots.Count);
    }

    [Test]
    public void Should_Have_Error_When_Description_Exceeds_Max_New_Lines()
    {
        var listing = new Listing
        {
            Name = "Valid Name",
            MusicianSlots = new List<MusicianSlot>
            {
                new() { Role = new MusicianRole { Name = "Test Role" } },
                new() { Role = new MusicianRole { Name = "Test Role" } },
            },
            Description = "Line1\nLine2\nLine3\nLine4\nLine5\nLine6",
            OwnerId = default,
            Type = ListingType.Band
        };

        var result = _validator.TestValidate(listing);
        result.ShouldHaveValidationErrorFor(l => l.Description)
            .WithErrorMessage("Description contains too many new lines. Maximum allowed is 5");
    }

    [Test]
    public void Should_Have_Error_When_Description_Exceeds_Max_Length()
    {
        var listing = new Listing
        {
            Name = "Valid Name",
            MusicianSlots = new List<MusicianSlot>
            {
                new() { Role = new MusicianRole { Name = "Test Role" } },
                new() { Role = new MusicianRole { Name = "Test Role" } },
            },
            Description = new string('a',
                221),
            OwnerId = default,
            Type = ListingType.Band
        };

        var result = _validator.TestValidate(listing);
        result.ShouldHaveValidationErrorFor(l => l.Description)
            .WithErrorMessage("Description must be at most 220 characters long");
    }

    [Test]
    public void Should_Not_Have_Error_When_Listing_Is_Valid()
    {
        var listing = new Listing
        {
            Name = "Valid Name",
            MusicianSlots = new List<MusicianSlot>
            {
                new() { Role = new MusicianRole { Name = "Test Role" } },
                new() { Role = new MusicianRole { Name = "Test Role" } },
            },
            Description = "Valid description",
            OwnerId = default,
            Type = ListingType.Band
        };

        var result = _validator.TestValidate(listing);
        result.ShouldNotHaveAnyValidationErrors();
    }
    
    [Test]
    public void Should_Have_Error_When_MusicianRole_Name_Is_Empty()
    {
        var listing = new Listing
        {
            Name = "Valid Name",
            MusicianSlots = new List<MusicianSlot>
            {
                new MusicianSlot { Role = new MusicianRole { Name = string.Empty } },
                new MusicianSlot { Role = new MusicianRole { Name = "Valid Role" } }
            },
            Description = "Valid description",
            OwnerId = default,
            Type = ListingType.Band
        };

        var result = _validator.TestValidate(listing);
        result.ShouldHaveValidationErrorFor("MusicianSlots[0].Role.Name");
    }

    [Test]
    public void Should_Not_Have_Error_When_MusicianRole_Name_Is_Valid()
    {
        var listing = new Listing
        {
            Name = "Valid Name",
            MusicianSlots = new List<MusicianSlot>
            {
                new MusicianSlot { Role = new MusicianRole { Name = "Valid Role 1" } },
                new MusicianSlot { Role = new MusicianRole { Name = "Valid Role 2" } }
            },
            Description = "Valid description",
            OwnerId = default,
            Type = ListingType.Band
        };

        var result = _validator.TestValidate(listing);
        result.ShouldNotHaveValidationErrorFor("MusicianSlots[0].Role.Name");
        result.ShouldNotHaveValidationErrorFor("MusicianSlots[1].Role.Name");
    }
}