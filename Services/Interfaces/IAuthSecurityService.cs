namespace JSAPNEW.Services.Interfaces
{
    public interface IAuthSecurityService
    {
        Task<bool> IsAccessTokenRevokedAsync(string jti);
        Task RevokeAccessTokenAsync(string jti);
        Task<string> GenerateRefreshTokenAsync(int userId, string ipAddress);
        Task<bool> ValidateRefreshTokenAsync(string token, int userId);
        Task RevokeRefreshTokenAsync(string token, string ipAddress, string? replacedByToken = null);
        Task RevokeAllUserTokensAsync(int userId);
    }
}
