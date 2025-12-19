using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TaskManager.Dto.Auth;
using TaskManager.Models;
using TaskManager.Data;
using System.Security.Cryptography;

namespace TaskManager.Services;

public class AuthService(TasksDbContext db, JwtTokenGenerator jwt) : IAuthService
{
    private readonly TasksDbContext _db = db;
    private readonly PasswordHasher<User> _hasher = new();
    private readonly JwtTokenGenerator _jwt = jwt;

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var emailExists = await _db.Users.AsNoTracking().AnyAsync(u => u.Email == request.Email);
        
        if(emailExists)
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
            ?? throw new UnauthorizedAccessException("Invalid credentials");

        var verificationResult = _hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);

        if (verificationResult != PasswordVerificationResult.Success)
            throw new UnauthorizedAccessException("Invalid credentials");

        var accessToken = _jwt.GenerateToken(user);
        var refreshToken = GenerateRefreshToken(user.Id);

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return new AuthResponse(accessToken, refreshToken.Token);
    }

    public async Task<AuthResponse> RefreshAsync(string refreshToken)
    {
        var existingToken = await _db.RefreshTokens
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Token == refreshToken);

        if(existingToken is null)
            throw new UnauthorizedAccessException("Refresh token not found");

        if(existingToken.IsRevoked)
            throw new UnauthorizedAccessException("Refresh token revoked");

        if (existingToken.ExpiresAt <= DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired");

        // Revoke old token (rotation)
        RevokeRefreshToken(existingToken);

        // Generate new tokens
        var newAccessToken = _jwt.GenerateToken(existingToken.User);
        var newRefreshToken = GenerateRefreshToken(existingToken.UserId);

        _db.RefreshTokens.Add(newRefreshToken);
        await _db.SaveChangesAsync();

        return new AuthResponse(newAccessToken, newRefreshToken.Token); // reuse same refresh token (no rotation)
    }

    public async Task LogoutAsync(string refreshToken)
    {
        var token = await _db.RefreshTokens
            .FirstOrDefaultAsync(r => r.Token == refreshToken);

        if (token is null)
            return; // idempotent logout

        RevokeRefreshToken(token);
        await _db.SaveChangesAsync();
    }

    public async Task<MeResponse> GetCurrentUserAsync(Guid userId)
    {
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId) ?? throw new UnauthorizedAccessException();

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

    private void RevokeRefreshToken(RefreshToken token)
    {
        token.IsRevoked = true;
        token.RevokedAt = DateTime.UtcNow;
    }
}