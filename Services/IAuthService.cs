using TaskManager.Dto.Auth;

namespace TaskManager.Services;

public interface IAuthService
{
    void Register(RegisterRequest request);
    AuthResponse Login(LoginRequest request);
}