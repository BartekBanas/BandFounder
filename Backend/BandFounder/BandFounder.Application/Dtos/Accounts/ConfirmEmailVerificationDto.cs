using System.ComponentModel.DataAnnotations;

namespace BandFounder.Application.Dtos.Accounts;

public class ConfirmEmailVerificationDto
{
    [Required]
    public required string Token { get; set; }
}
