using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using BandFounder.Domain;

namespace BandFounder.Domain.Entities;

[Table("EmailVerificationTokens")]
public class EmailVerificationToken : Entity
{
    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(Account))]
    public Guid AccountId { get; set; }

    public virtual Account Account { get; set; } = null!;

    [MaxLength(128)]
    public required string TokenHash { get; set; }

    public required string IntendedEmail { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime? ConsumedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Column(TypeName = "text")]
    public EmailNotificationStatus DeliveryStatus { get; set; } = EmailNotificationStatus.Pending;

    public DateTime DeliveryNotBeforeUtc { get; set; } = DateTime.UtcNow;

    public DateTime? DeliveryProcessedAt { get; set; }

    public DateTime? DeliveryLastAttemptAt { get; set; }

    public int DeliveryAttemptCount { get; set; }

    public string? DeliveryLastError { get; set; }
}
