namespace WebApp.Models;

public class Friendship
{
    public int Id { get; set; }
    public string RequesterId { get; set; }
    public virtual WebAppUser Requester { get; set; }
    public string ReceiverId { get; set; }
    public virtual WebAppUser Receiver { get; set; }
    public FriendshipStatus Status { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

public enum FriendshipStatus
{
    Pending,
    Accepted,
    Rejected
}
