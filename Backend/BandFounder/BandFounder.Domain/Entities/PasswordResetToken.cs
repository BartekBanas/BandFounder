using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BandFounder.Domain.Entities;

[Table("PasswordResetTokens")]
public class PasswordResetToken : Entity
{
    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(Account))]
    public Guid AccountId { get; set; }

    public virtual Account Account { get; set; } = null!;

    [MaxLength(128)]
    public required string TokenHash { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
