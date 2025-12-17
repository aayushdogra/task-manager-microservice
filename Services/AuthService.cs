using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TaskManager.Dto.Auth;
using TaskManager.Models;
using TaskManager.Data;
using System.Security.Cryptography;

namespace TaskManager.Services;

public class AuthService : IAuthService
{
    private readonly TasksDbContext _db;
    private readonly PasswordHasher<User> _hasher = new();
    private readonly JwtTokenGenerator _jwt;

    public AuthService(TasksDbContext db, JwtTokenGenerator jwt)
    {
        _db = db;
        _jwt = jwt;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var exists = await _db.Users.AnyAsync(u => u.Email == request.Email);
        if(exists)
            throw new InvalidOperationException("User already exists");

        var user = new User { Email = request.Email };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var accessToken = _jwt.GenerateToken(user);
        var refreshToken = GenerateRefreshToken(user.Id);

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return new AuthResponse(accessToken, refreshToken.Token);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email) 
            ?? throw new InvalidOperationException("Invalid credentials");

        var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (result == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Invalid credentials");

        var accessToken = _jwt.GenerateToken(user);
        var refreshToken = GenerateRefreshToken(user.Id);

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return new AuthResponse(accessToken, refreshToken.Token);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == refreshToken);

        if (token is null || token.IsRevoked || token.ExpiresAt < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Invalid refresh token");

        var newAccessToken = _jwt.GenerateToken(token.User);

        return new AuthResponse(newAccessToken,refreshToken); // reuse same refresh token (no rotation)
    }

    public async Task<MeResponse> GetCurrentUserAsync(Guid userId)
    {
        var user = await _db.Users.FindAsync(userId) ?? throw new UnauthorizedAccessException();

        return new MeResponse(user.Id, user.Email, user.CreatedAt);
    }

    private static RefreshToken GenerateRefreshToken(Guid userId)
    {
        return new RefreshToken
        {
            Token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64)),
            UserId = userId,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            IsRevoked = false
        };
    }
}