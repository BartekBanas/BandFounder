namespace BandFounder.Application.Dtos.Accounts;

public class UpdateAccountDto
{
    public string? Name { get; set; }
    
    public string? Password { get; set; }
    
    public string? Email { get; set; }

    public bool? EmailOnNewMessage { get; set; }

    public int? EmailUnreadDelayMinutes { get; set; }
}