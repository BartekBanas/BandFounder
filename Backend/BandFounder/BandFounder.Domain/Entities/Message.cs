using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BandFounder.Domain.Entities;

public class Message : Entity
{
    [Key]
    public Guid Id { get; set; }

    public string Content { get; set; } = null!;

    [DatabaseGenerated(DatabaseGeneratedOption.Computed)]
    public DateTime SentDate { get; set; }

    [ForeignKey(nameof(Sender))]
    public Guid SenderId { get; set; }
    public virtual Account Sender { get; set; } = null!;

    [ForeignKey(nameof(Chatroom))]
    public Guid ChatRoomId { get; set; }
    public virtual Chatroom Chatroom { get; set; } = null!;
}