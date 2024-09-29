using System.ComponentModel.DataAnnotations;

namespace BandFounder.Application.Dtos;

public class AccountDto
{
    [Required] 
    public required string Id { get; init; }
    
    [Required]
    public required string Name { get; init; }

    [Required]
    public required string Email { get; init; }
}