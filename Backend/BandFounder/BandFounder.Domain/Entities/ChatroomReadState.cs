using System.ComponentModel.DataAnnotations.Schema;

namespace BandFounder.Domain.Entities;

public class ChatroomReadState : Entity
{
    [ForeignKey(nameof(Account))]
    public Guid AccountId { get; set; }

    public virtual Account Account { get; set; } = null!;

    [ForeignKey(nameof(Chatroom))]
    public Guid ChatRoomId { get; set; }

    public virtual Chatroom Chatroom { get; set; } = null!;

    public DateTime? LastReadAt { get; set; }
}