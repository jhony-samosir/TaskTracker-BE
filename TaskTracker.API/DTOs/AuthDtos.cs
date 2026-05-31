namespace TaskTracker.API.DTOs;

public sealed record RegisterRequest(string FullName, string Email, string Password, string Role = "User");
public sealed record LoginRequest(string Email, string Password);
public sealed record RefreshTokenRequest(string RefreshToken);
public sealed record LogoutRequest(string RefreshToken);
public sealed record ChangePasswordRequest(string CurrentPassword, string NewPassword);
public sealed record AuthResponse(string AccessToken, string RefreshToken, DateTime AccessTokenExpiresAt, UserProfileResponse User);
public sealed record UserProfileResponse(int UserId, string FullName, string Email, string Role);
public sealed record ApiResponse<T>(bool Succeeded, string Message, T? Data = default, IReadOnlyList<string>? Errors = null)
{
    public static ApiResponse<T> Success(T data, string message = "Success") => new(true, message, data);
    public static ApiResponse<T> Failure(string message, IReadOnlyList<string>? errors = null) => new(false, message, default, errors);
}
