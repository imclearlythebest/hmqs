using Microsoft.AspNetCore.Identity;

namespace WebApp.Models;
public class WebAppUser : IdentityUser
{
    public virtual ICollection<Friendship> SentFriendRequests { get; set; }
    public virtual ICollection<Friendship> ReceivedFriendRequests { get; set; }
    public string? AvatarUrl { get; set; }
    public string? DiscordWebhookUrl { get; set; }
    public virtual ICollection<BlendMember> BlendMemberships { get; set; }

    public WebAppUser()
    {
        SentFriendRequests = new HashSet<Friendship>();
        ReceivedFriendRequests = new HashSet<Friendship>();
        BlendMemberships = new HashSet<BlendMember>();
    }
}