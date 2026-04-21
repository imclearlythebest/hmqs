using Hmqs.Api.Data;
using Hmqs.Api.Models;
using Hmqs.Api.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;


var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))
    ));
builder.Services.AddIdentity<Listener, IdentityRole<Guid>>()
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<ScrobbleService>();
builder.Services.AddScoped<PlaylistService>();
builder.Services.AddScoped<LocalTrackService>();
builder.Services.AddScoped<EthicalCalculatorService>();
builder.Services.AddHttpClient<GlobalTrackService>(client =>
{
    client.BaseAddress = new Uri("https://itunes.apple.com/");
    client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
});
builder.Services.AddControllers();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
