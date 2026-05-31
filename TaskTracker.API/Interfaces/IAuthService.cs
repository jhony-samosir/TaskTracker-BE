using TaskTracker.API.DTOs;

namespace TaskTracker.API.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, int userId, CancellationToken cancellationToken);
    Task<UserProfileResponse> GetCurrentUserAsync(int userId, CancellationToken cancellationToken);
    Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken);
}
