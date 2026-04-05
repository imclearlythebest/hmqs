using Microsoft.EntityFrameworkCore;
using WebApp.Routes;
using WebApp.Data;

var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddControllersWithViews()
    .AddRazorRuntimeCompilation();

builder.Services.AddDbContext<WebAppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));

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

WebApp.UseHttpsRedirection();
WebApp.UseStaticFiles();
WebApp.MapRoutes();

WebApp.Run();