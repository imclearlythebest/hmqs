using Microsoft.EntityFrameworkCore;
using WebApp.Routes;
using WebApp.Data;
using WebApp.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews()
    .AddRazorRuntimeCompilation();
builder.Services.AddHttpClient();
builder.Services.AddScoped<WebApp.Services.OdesliService>();
builder.Services.AddScoped<WebApp.Services.ItunesService>();
builder.Services.AddScoped<WebApp.Services.DiscordService>();

builder.Services.AddDbContext<WebAppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MySqlServerVersion(new Version(8, 0, 21)) // Hardcoded to avoid AutoDetect timeout
    ));
builder.Services.AddIdentity<WebAppUser, IdentityRole>()
    .AddEntityFrameworkStores<WebAppDbContext>();
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Auth/Login";
    options.LogoutPath = "/Auth/Logout";
    options.AccessDeniedPath = "/Auth/Denied";
});

var WebApp = builder.Build();

if (WebApp.Environment.IsDevelopment())
{
    WebApp.UseDeveloperExceptionPage();
}
else
{
    WebApp.UseExceptionHandler("/Home/Error");
    WebApp.UseHsts();
}

using (var scope = WebApp.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<WebAppDbContext>();
    // db.Database.EnsureDeleted(); // Removed for production safety
    db.Database.EnsureCreated();
}

WebApp.UseHttpsRedirection();
WebApp.UseStaticFiles();
WebApp.UseAuthentication();
WebApp.UseAuthorization();
WebApp.MapRoutes();

WebApp.Run();