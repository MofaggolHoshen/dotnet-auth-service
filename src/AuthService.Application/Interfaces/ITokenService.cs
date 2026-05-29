using System.Security.Claims;

namespace AuthService.Application.Interfaces
{
    public interface ITokenService
    {
        string GenerateAccessToken(Guid userId, string email);
        string GenerateRefreshToken();
        ClaimsPrincipal? ValidateToken(string token);
        Task<bool> RevokeTokenAsync(Guid userId, string refreshToken);
        Task<bool> IsTokenRevokedAsync(Guid userId, string refreshToken);
    }
}
