using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using Google.Apis.Auth;
using CatalogOrderApi.Data;
using CatalogOrderApi.Models;
using Microsoft.EntityFrameworkCore;

namespace CatalogOrderApi.Services;

public interface IAuthService
{
    Task<(string token, User user)> AuthenticateWithGoogleAsync(string idToken);
    string GenerateJwtToken(User user);
    Task<User?> GetUserByIdAsync(int userId);
}

public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;

    public AuthService(AppDbContext context, IConfiguration configuration, ILogger<AuthService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<(string token, User user)> AuthenticateWithGoogleAsync(string idToken)
    {
        try
        {
            // Verify Google ID token
            var googleClientId = _configuration["GoogleAuth:ClientId"];
            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { googleClientId }
            });

            // Find or create user
            var user = await _context.Users.FirstOrDefaultAsync(u => u.GoogleSub == payload.Subject);
            
            if (user == null)
            {
                user = new User
                {
                    GoogleSub = payload.Subject,
                    Email = payload.Email,
                    Name = payload.Name,
                    AvatarUrl = payload.Picture,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                
                _logger.LogInformation("New user created: {Email}", user.Email);
            }
            else
            {
                // Update user info if changed
                user.Name = payload.Name;
                user.AvatarUrl = payload.Picture;
                await _context.SaveChangesAsync();
            }

            var token = GenerateJwtToken(user);
            return (token, user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error authenticating with Google");
            throw;
        }
    }

    public string GenerateJwtToken(User user)
    {
        var secretKey = _configuration["JwtSettings:SecretKey"] 
            ?? throw new InvalidOperationException("JWT SecretKey not configured");
        var issuer = _configuration["JwtSettings:Issuer"] ?? "CatalogOrderApi";
        var audience = _configuration["JwtSettings:Audience"] ?? "CatalogOrderApiClients";
        var expirationMinutes = int.Parse(_configuration["JwtSettings:ExpirationMinutes"] ?? "1440");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Name, user.Name),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(expirationMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await _context.Users.FindAsync(userId);
    }
}
