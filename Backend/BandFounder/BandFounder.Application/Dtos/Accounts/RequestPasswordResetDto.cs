using System.ComponentModel.DataAnnotations;

namespace BandFounder.Application.Dtos.Accounts;

public class RequestPasswordResetDto
{
    [Required]
    [EmailAddress]
    public required string Email { get; set; }
}
