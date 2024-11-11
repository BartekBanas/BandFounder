using System.ComponentModel.DataAnnotations;

namespace BandFounder.Domain.Entities;

public class MusicianRole : Entity
{
    [Key] public required string Name { get; set; }
    public virtual List<Account> Accounts { get; set; } = [];
}