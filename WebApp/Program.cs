using Microsoft.EntityFrameworkCore;
using WebApp.Routes;
using WebApp.Data;
using WebApp.Models;
using Microsoft.AspNetCore.Identity;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.AddDbContext<WebAppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
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
    db.Database.EnsureDeleted();
    db.Database.EnsureCreated();
}

WebApp.UseHttpsRedirection();
WebApp.UseStaticFiles();
WebApp.MapRoutes();
WebApp.UseAuthentication();

WebApp.Run();