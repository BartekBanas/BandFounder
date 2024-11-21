using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BandFounder.Domain.Entities;

public class ProfilePicture : Entity
{
    [Key]
    public Guid Id { get; set; }

    public required Guid AccountId { get; set; }
    
    [ForeignKey(nameof(AccountId))]
    public virtual Account Account { get; set; }
    
    public required string MimeType { get; set; }
    
    public required byte[] ImageData { get; set; }
}