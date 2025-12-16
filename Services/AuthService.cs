using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TaskManager.Dto.Auth;
using TaskManager.Models;
using TaskManager.Data;
using System.Threading.Tasks;

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

        var token = _jwt.GenerateToken(user);
        return new AuthResponse(token);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email == request.Email) 
            ?? throw new InvalidOperationException("Invalid credentials");

        var result = _hasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password
        );

        if (result == PasswordVerificationResult.Failed)
            throw new InvalidOperationException("Invalid credentials");

        var token = _jwt.GenerateToken(user);
        return new AuthResponse(token);
    }
}