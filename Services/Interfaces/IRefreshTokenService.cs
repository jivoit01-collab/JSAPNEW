namespace JSAPNEW.Services.Interfaces
{
    public interface IRefreshTokenService
    {
        Task<string> CreateRefreshTokenAsync(int userId, string ipAddress, string securityStamp);
        Task<RefreshTokenRotationResult> RotateRefreshTokenAsync(string refreshToken, string ipAddress);
        Task RevokeRefreshTokenAsync(string refreshToken, string ipAddress);
        Task RevokeAllRefreshTokensForUserAsync(int userId, string reason);
    }

    public class RefreshTokenRotationResult
    {
        public bool Success { get; set; }
        public bool ReplayDetected { get; set; }
        public int UserId { get; set; }
        public string? RefreshToken { get; set; }
    }
}
