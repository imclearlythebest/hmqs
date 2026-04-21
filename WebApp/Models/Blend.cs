namespace WebApp.Models;

public class Blend
{
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public virtual ICollection<BlendMember> Members { get; set; }
    
    public Blend()
    {
        Members = new HashSet<BlendMember>();
    }
}

public class BlendMember
{
    public int BlendId { get; set; }
    public virtual Blend Blend { get; set; }
    public string UserId { get; set; }
    public virtual WebAppUser User { get; set; }
}
