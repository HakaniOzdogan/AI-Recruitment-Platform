using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using BCrypt.Net;
using IkOtomasyon.Api.Contracts;
using IkOtomasyon.Api.Data;
using IkOtomasyon.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace IkOtomasyon.Api.Services;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly JwtOptions _jwtOptions;

    public AuthService(AppDbContext db, IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<AuthResponse?> LoginAsync(string email, string password, CancellationToken ct = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();
        var user = await _db.Users
            .Include(u => u.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, ct);

        if (user is null || user.Status != UserStatus.Active)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
        {
            return null;
        }

        var roles = user.UserRoles.Select(ur => ur.Role!.Name).ToList();
        var accessToken = CreateAccessToken(user, roles);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, ct);

        return new AuthResponse(accessToken, refreshToken);
    }

    public async Task<AuthResponse> RegisterAsync(AuthRegisterRequest request, CancellationToken ct = default)
    {
        var fullName = request.FullName.Trim();
        var email = request.Email.Trim().ToLowerInvariant();
        var password = request.Password;

        var exists = await _db.Users.AnyAsync(u => u.Email.ToLower() == email, ct);
        if (exists)
        {
            throw new ConflictException("Bu e-posta ile kayitli kullanici zaten var.");
        }

        var role = await _db.Roles.FirstOrDefaultAsync(r => r.Name == "User", ct)
            ?? await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Applicant", ct)
            ?? await _db.Roles.FirstOrDefaultAsync(r => r.Name == "HR", ct)
            ?? await _db.Roles.FirstOrDefaultAsync(r => r.Name == "Recruiter", ct)
            ?? await _db.Roles.FirstOrDefaultAsync(r => r.Name == "HiringManager", ct);

        if (role is null)
        {
            throw new ConflictException("Sistem rol tanimi bulunamadi. Once migration/seed calistirin.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            Status = UserStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        _db.Users.Add(user);
        _db.UserRoles.Add(new UserRole
        {
            UserId = user.Id,
            RoleId = role.Id
        });
        await _db.SaveChangesAsync(ct);

        var roles = new List<string> { role.Name };
        var accessToken = CreateAccessToken(user, roles);
        var refreshToken = await CreateRefreshTokenAsync(user.Id, ct);
        return new AuthResponse(accessToken, refreshToken);
    }

    public async Task<AuthResponse?> RefreshAsync(string refreshToken, CancellationToken ct = default)
    {
        if (!TryParseRefreshToken(refreshToken, out var tokenId))
        {
            return null;
        }

        var stored = await _db.RefreshTokens
            .Include(rt => rt.User)
            .ThenInclude(u => u!.UserRoles)
            .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(rt => rt.Id == tokenId, ct);

        if (stored is null || stored.User is null)
        {
            return null;
        }

        if (stored.User.Status != UserStatus.Active)
        {
            return null;
        }

        if (stored.RevokedAt is not null || stored.ExpiresAt <= DateTime.UtcNow)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(refreshToken, stored.TokenHash))
        {
            return null;
        }

        stored.RevokedAt = DateTime.UtcNow;
        var roles = stored.User.UserRoles.Select(ur => ur.Role!.Name).ToList();
        var accessToken = CreateAccessToken(stored.User, roles);
        var newRefreshToken = await CreateRefreshTokenAsync(stored.User.Id, ct);

        return new AuthResponse(accessToken, newRefreshToken);
    }

    private string CreateAccessToken(User user, IReadOnlyCollection<string> roles)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email),
            new(JwtRegisteredClaimNames.Name, user.FullName)
        };

        if (user.TenantId is Guid tenantId)
        {
            claims.Add(new Claim("tenantId", tenantId.ToString()));
        }

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtOptions.AccessTokenMinutes),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private async Task<string> CreateRefreshTokenAsync(Guid userId, CancellationToken ct)
    {
        var tokenId = Guid.NewGuid();
        var tokenSecret = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));
        var rawToken = $"{tokenId}.{tokenSecret}";

        var entity = new RefreshToken
        {
            Id = tokenId,
            UserId = userId,
            TokenHash = BCrypt.Net.BCrypt.HashPassword(rawToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow
        };

        _db.RefreshTokens.Add(entity);
        await _db.SaveChangesAsync(ct);

        return rawToken;
    }

    private static bool TryParseRefreshToken(string token, out Guid tokenId)
    {
        tokenId = Guid.Empty;
        var parts = token.Split('.', 2);
        return parts.Length == 2 && Guid.TryParse(parts[0], out tokenId);
    }
}

