using System.ComponentModel.DataAnnotations;

namespace BandFounder.Application.Dtos.Accounts;

public class CompletePasswordResetDto
{
    [Required]
    public required string Token { get; set; }

    [Required]
    public required string NewPassword { get; set; }
}
