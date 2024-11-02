using BandFounder.Domain.Entities;

namespace BandFounder.Application.Dtos.Listings;

public class FeedFilterOptions
{
    public bool MatchRole { get; set; }
    public MusicProjectType? ListingType { get; set; }
}