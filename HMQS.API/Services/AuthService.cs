using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using HMQS.API.Data;
using HMQS.API.DTOs;
using HMQS.API.Models;

namespace HMQS.API.Services
{
    public class AuthService
    {
        private readonly AppDbContext _db;
        private readonly IConfiguration _config;

        // These are injected by ASP.NET Core's dependency injection system
        // We do not create these manually - the framework handles it
        public AuthService(AppDbContext db, IConfiguration config)
        {
            _db = db;
            _config = config;
        }

        // ─── REGISTER ────────────────────────────────────────────────────────────

        public async Task<AuthResponseDto?> RegisterAsync(RegisterDto dto)
        {
            // Check if email is already taken
            bool emailExists = await _db.Users.AnyAsync(u => u.Email == dto.Email);
            if (emailExists) return null; // Return null to signal failure

            // Check if username is already taken
            bool usernameExists = await _db.Users.AnyAsync(u => u.Username == dto.Username);
            if (usernameExists) return null;

            // Hash the password using bcrypt
            // BCrypt automatically handles salting - you do not need to do it manually
            // WorkFactor 12 means it runs 2^12 = 4096 rounds - slow enough to resist brute force
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password, workFactor: 12);

            // Create the new user object
            var user = new User
            {
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow
            };

            // Save user to database
            _db.Users.Add(user);
            await _db.SaveChangesAsync();

            // Generate tokens and return response
            return await GenerateAuthResponseAsync(user);
        }

        // ─── LOGIN ───────────────────────────────────────────────────────────────

        public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
        {
            // Find the user by email
            var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);

            // If user not found, return null
            if (user == null) return null;

            // Verify the password against the stored hash
            // BCrypt.Verify handles all the salt comparison internally
            bool passwordValid = BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash);
            if (!passwordValid) return null;

            // Generate tokens and return response
            return await GenerateAuthResponseAsync(user);
        }

        // ─── REFRESH TOKEN ───────────────────────────────────────────────────────

        public async Task<AuthResponseDto?> RefreshAsync(string refreshToken)
        {
            // Find the refresh token in the database
            var storedToken = await _db.RefreshTokens
                .Include(rt => rt.User) // Also load the related User object
                .FirstOrDefaultAsync(rt => rt.Token == refreshToken);

            // Token not found or expired
            if (storedToken == null || storedToken.ExpiresAt < DateTime.UtcNow)
                return null;

            // Delete the old refresh token (one-time use)
            _db.RefreshTokens.Remove(storedToken);
            await _db.SaveChangesAsync();

            // Issue a new pair of tokens
            return await GenerateAuthResponseAsync(storedToken.User);
        }

        // ─── PRIVATE HELPERS ─────────────────────────────────────────────────────

        private async Task<AuthResponseDto> GenerateAuthResponseAsync(User user)
        {
            var accessToken = GenerateJwtToken(user);
            var refreshToken = await GenerateRefreshTokenAsync(user.Id);

            return new AuthResponseDto
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                Username = user.Username,
                Email = user.Email
            };
        }

        private string GenerateJwtToken(User user)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"]!;
            var expiryMinutes = int.Parse(jwtSettings["AccessTokenExpiryMinutes"]!);

            // Claims are pieces of data embedded inside the token
            // Anyone who has the token can read these (but not modify them)
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()), // Subject = user ID
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("username", user.Username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()) // Unique token ID
            };

            // The signing key - this is what makes the token tamper-proof
            // If anyone changes the token payload, the signature will not match
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: jwtSettings["Issuer"],
                audience: jwtSettings["Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(expiryMinutes),
                signingCredentials: credentials
            );

            // Serialize the token to a string like "eyJhbGci..."
            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task<string> GenerateRefreshTokenAsync(int userId)
        {
            var jwtSettings = _config.GetSection("JwtSettings");
            var expiryDays = int.Parse(jwtSettings["RefreshTokenExpiryDays"]!);

            // Generate a cryptographically secure random string
            // This is more secure than a JWT for refresh tokens
            var randomBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomBytes);
            var token = Convert.ToBase64String(randomBytes);

            // Save it to the database so we can validate it later
            var refreshToken = new RefreshToken
            {
                UserId = userId,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
                CreatedAt = DateTime.UtcNow
            };

            _db.RefreshTokens.Add(refreshToken);
            await _db.SaveChangesAsync();

            return token;
        }
    }
}