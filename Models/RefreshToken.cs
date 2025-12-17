namespace TaskManager.Models;

public class  RefreshToken
{
    public int Id { get; set; }
    public string Token { get; set; } = null!;
    public Guid UserId { get; set; }
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; }
    public User User { get; set; } = null!;
}