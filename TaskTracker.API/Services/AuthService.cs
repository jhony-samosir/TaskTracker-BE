using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskTracker.API.Data;
using TaskTracker.API.DTOs;
using TaskTracker.API.Config;
using TaskTracker.API.Interfaces;
using TaskTracker.API.Models;

namespace TaskTracker.API.Services;

public sealed class AuthService(
    AppDbContext dbContext,
    IJwtTokenService jwtTokenService,
    IPasswordHasher<MstUser> passwordHasher,
    IOptions<JwtOptions> jwtOptions) : IAuthService
{
    private readonly JwtOptions _jwtOptions = jwtOptions.Value;
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var existingUser = await dbContext.MstUsers
            .AnyAsync(u => u.Email == request.Email && u.DeletedDate == null, cancellationToken);

        if (existingUser)
            throw new InvalidOperationException("User with this email already exists.");

        var role = await dbContext.MstRoles
            .FirstOrDefaultAsync(r => r.RoleName.ToLower() == request.Role.ToLower() && r.DeletedDate == null, cancellationToken);

        if (role is null)
        {
            role = new MstRole
            {
                RoleName = request.Role,
                CreatedDate = DateTime.UtcNow,
                CreatedBy = "System"
            };
            dbContext.MstRoles.Add(role);
            await dbContext.SaveChangesAsync(cancellationToken);
        }

        var user = new MstUser
        {
            FullName = request.FullName,
            Email = request.Email,
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System"
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
        dbContext.MstUsers.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        var userRole = new TrxUserRole
        {
            UserId = user.UserId,
            RoleId = role.RoleId, //Default employee
            CreatedDate = DateTime.UtcNow,
            CreatedBy = "System"
        };
        dbContext.TrxUserRoles.Add(userRole);
        await dbContext.SaveChangesAsync(cancellationToken);

        var (accessToken, expiresAt) = jwtTokenService.CreateAccessToken(user, role.RoleName);
        var refreshToken = jwtTokenService.CreateRefreshToken();

        var rtEntity = new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = jwtTokenService.HashRefreshToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Set<RefreshToken>().Add(rtEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, refreshToken, expiresAt, new UserProfileResponse(user.UserId, user.FullName, user.Email, role.RoleName));
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.MstUsers
            .FirstOrDefaultAsync(u => u.Email == request.Email && u.DeletedDate == null, cancellationToken);

        if (user is null)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.Password);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid email or password.");

        var userRole = await dbContext.TrxUserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.UserId && ur.DeletedDate == null, cancellationToken);

        var roleName = "User";
        if (userRole != null)
        {
            var role = await dbContext.MstRoles.FindAsync(new object[] { userRole.RoleId }, cancellationToken);
            if (role != null) roleName = role.RoleName;
        }

        var (accessToken, expiresAt) = jwtTokenService.CreateAccessToken(user, roleName);
        var refreshToken = jwtTokenService.CreateRefreshToken();

        var rtEntity = new RefreshToken
        {
            UserId = user.UserId,
            TokenHash = jwtTokenService.HashRefreshToken(refreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow
        };
        dbContext.Set<RefreshToken>().Add(rtEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, refreshToken, expiresAt, new UserProfileResponse(user.UserId, user.FullName, user.Email, roleName));
    }

    public async Task<AuthResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var tokenHash = jwtTokenService.HashRefreshToken(request.RefreshToken);

        var rtEntity = await dbContext.Set<RefreshToken>()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        if (rtEntity is null || !rtEntity.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        rtEntity.RevokedAt = DateTime.UtcNow;

        var userRole = await dbContext.TrxUserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == rtEntity.UserId && ur.DeletedDate == null, cancellationToken);

        var roleName = "User";
        if (userRole != null)
        {
            var role = await dbContext.MstRoles.FindAsync(new object[] { userRole.RoleId }, cancellationToken);
            if (role != null) roleName = role.RoleName;
        }

        var (accessToken, expiresAt) = jwtTokenService.CreateAccessToken(rtEntity.User, roleName);
        var newRefreshToken = jwtTokenService.CreateRefreshToken();

        var newRtEntity = new RefreshToken
        {
            UserId = rtEntity.UserId,
            TokenHash = jwtTokenService.HashRefreshToken(newRefreshToken),
            ExpiresAt = DateTime.UtcNow.AddDays(_jwtOptions.RefreshTokenDays),
            CreatedAt = DateTime.UtcNow
        };
        rtEntity.ReplacedByTokenHash = newRtEntity.TokenHash;

        dbContext.Set<RefreshToken>().Add(newRtEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new AuthResponse(accessToken, newRefreshToken, expiresAt, new UserProfileResponse(rtEntity.User.UserId, rtEntity.User.FullName, rtEntity.User.Email, roleName));
    }

    public async Task LogoutAsync(LogoutRequest request, int userId, CancellationToken cancellationToken)
    {
        var tokenHash = jwtTokenService.HashRefreshToken(request.RefreshToken);

        var rtEntity = await dbContext.Set<RefreshToken>()
            .FirstOrDefaultAsync(r => r.TokenHash == tokenHash && r.UserId == userId, cancellationToken);

        if (rtEntity is not null && rtEntity.IsActive)
        {
            rtEntity.RevokedAt = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<UserProfileResponse> GetCurrentUserAsync(int userId, CancellationToken cancellationToken)
    {
        var user = await dbContext.MstUsers
            .FirstOrDefaultAsync(u => u.UserId == userId && u.DeletedDate == null, cancellationToken);

        if (user is null)
            throw new KeyNotFoundException("User not found.");

        var userRole = await dbContext.TrxUserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == user.UserId && ur.DeletedDate == null, cancellationToken);

        var roleName = "User";
        if (userRole != null)
        {
            var role = await dbContext.MstRoles.FindAsync(new object[] { userRole.RoleId }, cancellationToken);
            if (role != null) roleName = role.RoleName;
        }

        return new UserProfileResponse(user.UserId, user.FullName, user.Email, roleName);
    }

    public async Task<IEnumerable<UserDropdownResponse>> GetUsersAsync(CancellationToken cancellationToken)
    {
        var users = await (
            from user in dbContext.MstUsers
            where user.DeletedDate == null
            join userRole in dbContext.TrxUserRoles.Where(ur => ur.DeletedDate == null)
                on user.UserId equals userRole.UserId into userRoles
            from userRole in userRoles.DefaultIfEmpty()
            join role in dbContext.MstRoles.Where(r => r.DeletedDate == null)
                on userRole.RoleId equals role.RoleId into roles
            from role in roles.DefaultIfEmpty()
            orderby user.FullName
            select new UserDropdownResponse(
                user.UserId,
                user.FullName,
                user.Email,
                role != null ? role.RoleName : "User"))
            .ToListAsync(cancellationToken);

        return users;
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request, CancellationToken cancellationToken)
    {
        var user = await dbContext.MstUsers
            .FirstOrDefaultAsync(u => u.UserId == userId && u.DeletedDate == null, cancellationToken);

        if (user is null)
            throw new KeyNotFoundException("User not found.");

        var result = passwordHasher.VerifyHashedPassword(user, user.PasswordHash, request.CurrentPassword);
        if (result == PasswordVerificationResult.Failed)
            throw new UnauthorizedAccessException("Invalid current password.");

        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedDate = DateTime.UtcNow;
        user.UpdatedBy = user.UserId.ToString();

        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
