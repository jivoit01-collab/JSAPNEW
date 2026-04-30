using System.Collections.Concurrent;
using JSAPNEW.Services.Interfaces;
using System.Security.Cryptography;

namespace JSAPNEW.Services.Implementation
{
    public class AuthSecurityService : IAuthSecurityService
    {
        private static readonly ConcurrentDictionary<string, DateTime> _revokedAccessTokens = new();
        private static readonly ConcurrentDictionary<string, Models.RefreshToken> _refreshTokens = new();
        private static readonly ConcurrentDictionary<int, List<string>> _userRefreshTokens = new();

        public Task<bool> IsAccessTokenRevokedAsync(string jti)
        {
            if (_revokedAccessTokens.TryGetValue(jti, out var expiry))
            {
                if (DateTime.UtcNow > expiry)
                    _revokedAccessTokens.TryRemove(jti, out _);
                else
                    return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task RevokeAccessTokenAsync(string jti)
        {
            _revokedAccessTokens.TryAdd(jti, DateTime.UtcNow.AddMinutes(70));
            return Task.CompletedTask;
        }

        public Task<string> GenerateRefreshTokenAsync(int userId, string ipAddress)
        {
            var tokenBytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(tokenBytes);
            var token = Convert.ToBase64String(tokenBytes);

            var refreshToken = new Models.RefreshToken
            {
                UserId = userId,
                Token = token,
                ExpiresUtc = DateTime.UtcNow.AddDays(7),
                CreatedUtc = DateTime.UtcNow
            };

            _refreshTokens.TryAdd(token, refreshToken);
            _userRefreshTokens.AddOrUpdate(userId, _ => new List<string> { token }, (_, list) => { list.Add(token); return list; });
            return Task.FromResult(token);
        }

        public Task<bool> ValidateRefreshTokenAsync(string token, int userId)
        {
            if (_refreshTokens.TryGetValue(token, out var rt))
            {
                if (!rt.IsActive || rt.UserId != userId)
                    return Task.FromResult(false);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task RevokeRefreshTokenAsync(string token, string ipAddress, string? replacedByToken = null)
        {
            if (_refreshTokens.TryGetValue(token, out var rt))
            {
                rt.RevokedByIp = ipAddress;
                rt.ReplacedByToken = replacedByToken;
            }
            return Task.CompletedTask;
        }

        public Task RevokeAllUserTokensAsync(int userId)
        {
            if (_userRefreshTokens.TryGetValue(userId, out var tokens))
            {
                foreach (var token in tokens)
                {
                    if (_refreshTokens.TryGetValue(token, out var rt))
                        rt.RevokedByIp = "admin-revoked";
                }
                tokens.Clear();
            }
            return Task.CompletedTask;
        }
    }
}
