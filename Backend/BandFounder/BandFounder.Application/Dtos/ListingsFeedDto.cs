namespace BandFounder.Application.Dtos;

public class ListingsFeedDto
{
    public List<ListingWithScore> Listings { get; set; } = [];
}

public class ListingWithScore
{
    public required MusicProjectListingDto Listing { get; set; }
    public required int SimilarityScore { get; set; }
}