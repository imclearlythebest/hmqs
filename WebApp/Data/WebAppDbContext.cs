using Microsoft.EntityFrameworkCore;
namespace WebApp.Data;

public class WebAppDbContext : DbContext
{
    public WebAppDbContext(DbContextOptions<WebAppDbContext> options)
        : base(options)
    {
    }
}