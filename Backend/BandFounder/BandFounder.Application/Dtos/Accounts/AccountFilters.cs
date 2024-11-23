namespace BandFounder.Application.Dtos.Accounts;

public class AccountFilters
{
    public int? PageSize { get; set; }
    public int? PageNumber { get; set; }
    public string? Username { get; set; }
    public string? UsernameFragment { get; set; }
}