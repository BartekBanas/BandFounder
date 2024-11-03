namespace BandFounder.Application.Dtos.Listings;

public class ListingsFeedDto
{
    public List<ListingWithScore> Listings { get; set; } = [];
}

public class ListingWithScore
{
    public required ListingDto Listing { get; set; }
    public required int SimilarityScore { get; set; }
}