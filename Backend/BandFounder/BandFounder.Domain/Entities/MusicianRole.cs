using System.ComponentModel.DataAnnotations;

namespace BandFounder.Domain.Entities;

public class MusicianRole : Entity
{
    [Key]
    public Guid Id { get; set; }
    
    public required string RoleName { get; set; }

    public virtual List<Account> Accounts { get; set; } = [];
}