using System.Collections.Concurrent;
using Microsoft.AspNetCore.Identity;
using TaskManager.Dto.Auth;
using TaskManager.Models;

namespace TaskManager.Services;

public class AuthService : IAuthService
{
    private static readonly ConcurrentDictionary<string, User> _users = new();
    private readonly PasswordHasher<User> _hasher = new();
    private readonly JwtTokenGenerator _jwt;

    public AuthService(JwtTokenGenerator jwt)
    {
        _jwt = jwt;
    }

    public void Register(RegisterRequest request)
    {
        if (_users.ContainsKey(request.Email))
            throw new InvalidOperationException("User already exists");

        var user = new User { Email = request.Email };
        user.PasswordHash = _hasher.HashPassword(user, request.Password);

        _users[request.Email] = user;
    }

    public AuthResponse Login(LoginRequest request)
    {
        if (!_users.TryGetValue(request.Email, out var user))
            throw new InvalidOperationException("Invalid credentials");

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