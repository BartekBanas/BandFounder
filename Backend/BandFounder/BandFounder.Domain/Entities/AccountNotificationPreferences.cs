using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BandFounder.Domain.Entities;

[Table("AccountNotificationPreferences")]
public class AccountNotificationPreferences : Entity
{
    [Key]
    [ForeignKey(nameof(Account))]
    public Guid AccountId { get; set; }

    public virtual Account Account { get; set; } = null!;

    public bool EmailOnNewMessage { get; set; } = true;

    public int EmailUnreadDelayMinutes { get; set; } = 1440;
}
