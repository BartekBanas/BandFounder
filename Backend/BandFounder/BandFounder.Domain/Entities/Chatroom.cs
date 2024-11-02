using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BandFounder.Domain.Entities;

public class Chatroom : Entity
{
    [Key]
    public Guid Id { get; set; }
    
    public required string Name { get; set; }

    [ForeignKey(nameof(Owner))] public Guid? OwnerId { get; set; }
    public virtual Account? Owner { get; set; }
    
    [Column(TypeName = "varchar(24)")]
    public ChatRoomType ChatRoomType { get; set; }

    // Many-to-Many relationship with Account
    public virtual List<Account?> Members { get; set; } = [];
    
    // One-to-Many relationship with Message
    public virtual List<Message> Messages { get; set; } = [];
}

public enum ChatRoomType
{
    General,
    Direct
}