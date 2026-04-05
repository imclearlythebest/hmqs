namespace WebApp.Routes;
public static class Routes
{
    public static void MapRoutes(this WebApplication WebApp)
    {
        WebApp.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}");
    }
}