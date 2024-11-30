namespace BandFounder.Domain.Entities;

public class PasswordResetToken : Entity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid AccountId { get; set; }
    public Account Account { get; set; }

    public string TokenHash { get; set; }
    public bool IsUsed { get; set; }
    public DateTime ExpirationDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}