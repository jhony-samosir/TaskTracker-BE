using TaskTracker.API.Models;

namespace TaskTracker.API.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt) CreateAccessToken(MstUser user, string role);
    string CreateRefreshToken();
    string HashRefreshToken(string refreshToken);
}
