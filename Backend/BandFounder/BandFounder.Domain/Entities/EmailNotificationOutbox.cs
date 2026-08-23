using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BandFounder.Domain.Entities;

[Table("EmailNotificationOutbox")]
public class EmailNotificationOutbox : Entity
{
    [Key]
    public Guid Id { get; set; }

    [ForeignKey(nameof(RecipientAccount))]
    public Guid RecipientAccountId { get; set; }

    public virtual Account RecipientAccount { get; set; } = null!;

    [ForeignKey(nameof(Chatroom))]
    public Guid ChatRoomId { get; set; }

    public virtual Chatroom Chatroom { get; set; } = null!;

    [Column(TypeName = "text")]
    public EmailNotificationStatus Status { get; set; } = EmailNotificationStatus.Pending;

    public DateTime NotBeforeUtc { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? ProcessedAt { get; set; }

    public DateTime? LastAttemptAt { get; set; }

    public int AttemptCount { get; set; }

    public int UnreadCountHint { get; set; } = 1;

    public string LatestSenderName { get; set; } = string.Empty;

    public string ChatroomName { get; set; } = string.Empty;

    public string Snippet { get; set; } = string.Empty;

    public string? LastError { get; set; }
}

public enum EmailNotificationStatus
{
    Pending,
    Processing,
    Sent,
    Cancelled,
    Failed
}
