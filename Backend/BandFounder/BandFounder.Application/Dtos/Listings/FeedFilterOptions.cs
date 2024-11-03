using BandFounder.Domain.Entities;

namespace BandFounder.Application.Dtos.Listings;

public class FeedFilterOptions
{
    public bool ExcludeOwn { get; init; } = true;
    public bool MatchRole { get; init; } = true;
    public ListingType? ListingType { get; init; }
    public string? Genre { get; init; }
}