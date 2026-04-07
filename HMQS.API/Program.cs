using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using HMQS.API.Data;
using HMQS.API.Services;

var builder = WebApplication.CreateBuilder(args);

// --- 1. Database ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        new MariaDbServerVersion(new Version(10, 4, 32))
    )
);

// --- 2. Services ---
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<SongService>();
builder.Services.AddScoped<SpotifyImportService>();

// --- 3. HttpClient for iTunes ---
builder.Services.AddHttpClient<ItunesService>(client =>
{
    client.BaseAddress = new Uri(
        builder.Configuration["ExternalApis:ItunesBaseUrl"]!
    );
});

// --- 4. JWT Authentication ---
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"]!;

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };

    // Allow JWT from query string for audio streaming
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var token = context.Request.Query["token"];
            if (!string.IsNullOrEmpty(token))
            {
                context.Token = token;
            }
            return Task.CompletedTask;
        }
    };
});

// --- 5. CORS ---
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// --- 6. Controllers ---
builder.Services.AddControllers();

var app = builder.Build();

// --- 7. Middleware pipeline ---
app.UseCors("AllowFrontend");

// Serve static files from wwwroot folder
// This makes dashboard.html accessible at http://localhost:5196/dashboard.html
app.UseDefaultFiles(); // Serves index.html by default if it exists
app.UseStaticFiles();  // Serves all files in wwwroot

app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();