namespace JSAPNEW.Models
{
    public class JwtSettings
    {
        public string SecretKey { get; set; } = string.Empty;
        public string Issuer { get; set; } = string.Empty;
        public string Audience { get; set; } = string.Empty;
        public int ExpiryInMinutes { get; set; } = 60;
        public int RefreshTokenExpiryInDays { get; set; } = 7;
    }

    public class RefreshToken
    {
        public int UserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; }
        public DateTime CreatedUtc { get; set; }
        public string? RevokedByIp { get; set; }
        public string? ReplacedByToken { get; set; }
        public bool IsExpired => DateTime.UtcNow >= ExpiresUtc;
        public bool IsActive => RevokedByIp == null && !IsExpired;
    }

    public class RefreshTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTime ExpiresUtc { get; set; }
    }

    public class LoginResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public UserDto? User { get; set; }
        public string? AccessToken { get; set; }
        public string? RefreshToken { get; set; }
        public DateTime? TokenExpiresUtc { get; set; }
    }

    public class RefreshTokenRequest
    {
        public string RefreshToken { get; set; } = string.Empty;
    }

    public class RevokeTokenRequest
    {
        public string? Token { get; set; }
    }
}
