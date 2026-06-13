using System.Security.Cryptography;
using System.Text;
using Dapper;
using Microsoft.Data.SqlClient;
using JSAPNEW.Services;
using JSAPNEW.Services.Interfaces;

namespace JSAPNEW.Services.Implementation
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private const int RefreshTokenDays = 7;
        private readonly string _connectionString;
        private readonly string _jwtSecretKey;

        public RefreshTokenService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection")
                ?? throw new InvalidOperationException("DefaultConnection is not configured.");
            _jwtSecretKey = configuration["Jwt:SecretKey"]
                ?? throw new InvalidOperationException("Jwt:SecretKey is not configured.");
        }

        public async Task<string> CreateRefreshTokenAsync(int userId, string ipAddress, string securityStamp)
        {
            await EnsureStorageAsync();

            if (string.IsNullOrWhiteSpace(securityStamp))
            {
                throw new InvalidOperationException("Security stamp is required for refresh token issuance.");
            }

            var refreshToken = GenerateRefreshToken();
            var tokenHash = HashRefreshToken(refreshToken);
            var now = DateTime.UtcNow;

            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"INSERT INTO dbo.jsRefreshTokens
                    (UserId, TokenHash, SecurityStamp, CreatedOn, ExpiresOn, CreatedByIp)
                  VALUES
                    (@UserId, @TokenHash, @SecurityStamp, @CreatedOn, @ExpiresOn, @CreatedByIp)",
                new
                {
                    UserId = userId,
                    TokenHash = tokenHash,
                    SecurityStamp = securityStamp,
                    CreatedOn = now,
                    ExpiresOn = now.AddDays(RefreshTokenDays),
                    CreatedByIp = ipAddress
                });

            return refreshToken;
        }

        public async Task<RefreshTokenRotationResult> RotateRefreshTokenAsync(string refreshToken, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return new RefreshTokenRotationResult { Success = false };
            }

            await EnsureStorageAsync();

            var tokenHash = HashRefreshToken(refreshToken);
            await using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            await using var transaction = connection.BeginTransaction();
            try
            {
                var existing = await connection.QuerySingleOrDefaultAsync<StoredRefreshToken>(
                    @"SELECT TOP 1 RefreshTokenId, UserId, TokenHash, SecurityStamp, ExpiresOn, RevokedOn
                      FROM dbo.jsRefreshTokens WITH (UPDLOCK, ROWLOCK)
                      WHERE TokenHash = @TokenHash",
                    new { TokenHash = tokenHash },
                    transaction);

                if (existing == null)
                {
                    transaction.Rollback();
                    return new RefreshTokenRotationResult { Success = false };
                }

                if (existing.RevokedOn.HasValue)
                {
                    transaction.Rollback();
                    return new RefreshTokenRotationResult
                    {
                        Success = false,
                        ReplayDetected = true,
                        UserId = existing.UserId
                    };
                }

                if (existing.ExpiresOn <= DateTime.UtcNow)
                {
                    transaction.Rollback();
                    return new RefreshTokenRotationResult
                    {
                        Success = false,
                        UserId = existing.UserId
                    };
                }

                var currentSecurityStamp = await GetCurrentSecurityStampAsync(connection, transaction, existing.UserId);
                if (string.IsNullOrWhiteSpace(currentSecurityStamp)
                    || !string.Equals(existing.SecurityStamp, currentSecurityStamp, StringComparison.Ordinal))
                {
                    transaction.Rollback();
                    return new RefreshTokenRotationResult
                    {
                        Success = false,
                        UserId = existing.UserId
                    };
                }

                var replacementToken = GenerateRefreshToken();
                var replacementHash = HashRefreshToken(replacementToken);
                var now = DateTime.UtcNow;

                var revokedRows = await connection.ExecuteAsync(
                    @"UPDATE dbo.jsRefreshTokens
                      SET RevokedOn = @RevokedOn,
                          ReplacedByTokenHash = @ReplacedByTokenHash
                      WHERE RefreshTokenId = @RefreshTokenId
                        AND RevokedOn IS NULL",
                    new
                    {
                        RevokedOn = now,
                        ReplacedByTokenHash = replacementHash,
                        existing.RefreshTokenId
                    },
                    transaction);

                if (revokedRows != 1)
                {
                    transaction.Rollback();
                    return new RefreshTokenRotationResult
                    {
                        Success = false,
                        ReplayDetected = true,
                        UserId = existing.UserId
                    };
                }

                await connection.ExecuteAsync(
                    @"INSERT INTO dbo.jsRefreshTokens
                        (UserId, TokenHash, SecurityStamp, CreatedOn, ExpiresOn, CreatedByIp)
                      VALUES
                        (@UserId, @TokenHash, @SecurityStamp, @CreatedOn, @ExpiresOn, @CreatedByIp)",
                    new
                    {
                        existing.UserId,
                        TokenHash = replacementHash,
                        SecurityStamp = currentSecurityStamp,
                        CreatedOn = now,
                        ExpiresOn = now.AddDays(RefreshTokenDays),
                        CreatedByIp = ipAddress
                    },
                    transaction);

                transaction.Commit();

                return new RefreshTokenRotationResult
                {
                    Success = true,
                    UserId = existing.UserId,
                    RefreshToken = replacementToken
                };
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task RevokeRefreshTokenAsync(string refreshToken, string ipAddress)
        {
            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                return;
            }

            await EnsureStorageAsync();

            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE dbo.jsRefreshTokens
                  SET RevokedOn = COALESCE(RevokedOn, @RevokedOn),
                      RevokedReason = COALESCE(RevokedReason, @RevokedReason)
                  WHERE TokenHash = @TokenHash",
                new
                {
                    RevokedOn = DateTime.UtcNow,
                    RevokedReason = "Logout",
                    TokenHash = HashRefreshToken(refreshToken)
                });
        }

        public async Task RevokeAllRefreshTokensForUserAsync(int userId, string reason)
        {
            if (userId <= 0)
            {
                return;
            }

            await EnsureStorageAsync();

            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"UPDATE dbo.jsRefreshTokens
                  SET RevokedOn = COALESCE(RevokedOn, @RevokedOn),
                      RevokedReason = COALESCE(RevokedReason, @RevokedReason)
                  WHERE UserId = @UserId
                    AND RevokedOn IS NULL",
                new
                {
                    RevokedOn = DateTime.UtcNow,
                    RevokedReason = reason,
                    UserId = userId
                });
        }

        private async Task EnsureStorageAsync()
        {
            await using var connection = new SqlConnection(_connectionString);
            await connection.ExecuteAsync(
                @"IF OBJECT_ID(N'dbo.jsRefreshTokens', N'U') IS NULL
                  BEGIN
                      CREATE TABLE dbo.jsRefreshTokens
                      (
                          RefreshTokenId BIGINT IDENTITY(1,1) NOT NULL CONSTRAINT PK_jsRefreshTokens PRIMARY KEY,
                          UserId INT NOT NULL,
                          TokenHash NVARCHAR(64) NOT NULL,
                          SecurityStamp NVARCHAR(64) NOT NULL,
                          CreatedOn DATETIME2(7) NOT NULL,
                          ExpiresOn DATETIME2(7) NOT NULL,
                          RevokedOn DATETIME2(7) NULL,
                          CreatedByIp NVARCHAR(64) NULL,
                          RevokedReason NVARCHAR(200) NULL,
                          ReplacedByTokenHash NVARCHAR(64) NULL
                      );
                  END;

                  IF NOT EXISTS
                  (
                      SELECT 1
                      FROM sys.indexes
                      WHERE name = N'UX_jsRefreshTokens_TokenHash'
                        AND object_id = OBJECT_ID(N'dbo.jsRefreshTokens')
                  )
                  BEGIN
                      CREATE UNIQUE INDEX UX_jsRefreshTokens_TokenHash
                      ON dbo.jsRefreshTokens(TokenHash);
                  END;

                  IF COL_LENGTH(N'dbo.jsRefreshTokens', N'SecurityStamp') IS NULL
                  BEGIN
                      ALTER TABLE dbo.jsRefreshTokens
                      ADD SecurityStamp NVARCHAR(64) NULL;
                  END;

                  IF COL_LENGTH(N'dbo.jsRefreshTokens', N'RevokedReason') IS NULL
                  BEGIN
                      ALTER TABLE dbo.jsRefreshTokens
                      ADD RevokedReason NVARCHAR(200) NULL;
                  END;

                  UPDATE dbo.jsRefreshTokens
                  SET SecurityStamp = N''
                  WHERE SecurityStamp IS NULL;

                  IF EXISTS
                  (
                      SELECT 1
                      FROM sys.columns
                      WHERE object_id = OBJECT_ID(N'dbo.jsRefreshTokens')
                        AND name = N'SecurityStamp'
                        AND is_nullable = 1
                  )
                  BEGIN
                      ALTER TABLE dbo.jsRefreshTokens
                      ALTER COLUMN SecurityStamp NVARCHAR(64) NOT NULL;
                  END;");
        }

        private static string GenerateRefreshToken()
        {
            var bytes = RandomNumberGenerator.GetBytes(64);
            return Convert.ToBase64String(bytes)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        private static string HashRefreshToken(string refreshToken)
        {
            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
            return Convert.ToHexString(bytes);
        }

        private async Task<string?> GetCurrentSecurityStampAsync(SqlConnection connection, SqlTransaction transaction, int userId)
        {
            var userRow = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT TOP 1 * FROM jsUser WHERE UserId = @UserId",
                new { UserId = userId },
                transaction);

            if (userRow == null)
            {
                return null;
            }

            var userValues = (IDictionary<string, object>)userRow;
            if (!IsUserRefreshEligible(userValues))
            {
                return null;
            }

            var roleRows = await connection.QueryAsync<dynamic>(
                @"SELECT ur.userId, ur.roleId, r.roleName
                  FROM jsUserRole ur
                  LEFT JOIN jsRole r ON ur.roleId = r.roleId
                  WHERE ur.userId = @UserId
                  ORDER BY ur.roleId, r.roleName",
                new { UserId = userId },
                transaction);

            var roleList = roleRows.ToList();
            if (!roleList.Any())
            {
                return null;
            }

            var roleSnapshot = AuthSecurity.CreateRoleSnapshot(roleList);
            return AuthSecurity.CreateSecurityStamp(userValues, _jwtSecretKey, roleSnapshot);
        }

        private static bool IsUserRefreshEligible(IDictionary<string, object> userValues)
        {
            var isActive = AuthSecurity.GetValue(userValues, "isActive", "IsActive", "active", "Active");
            if (isActive != null && !ToBoolean(isActive))
            {
                return false;
            }

            var isLocked = AuthSecurity.GetValue(userValues, "isLocked", "IsLocked", "locked", "Locked", "isLock", "IsLock");
            if (isLocked != null && ToBoolean(isLocked))
            {
                return false;
            }

            var lockoutEnd = AuthSecurity.GetValue(userValues, "lockoutEnd", "LockoutEnd", "lockedUntil", "LockedUntil");
            if (lockoutEnd != null
                && DateTime.TryParse(lockoutEnd.ToString(), out var lockoutEndValue)
                && lockoutEndValue > DateTime.UtcNow)
            {
                return false;
            }

            return true;
        }

        private static bool ToBoolean(object value)
        {
            if (value is bool boolValue)
            {
                return boolValue;
            }

            if (value is byte byteValue)
            {
                return byteValue != 0;
            }

            if (value is short shortValue)
            {
                return shortValue != 0;
            }

            if (value is int intValue)
            {
                return intValue != 0;
            }

            if (value is long longValue)
            {
                return longValue != 0;
            }

            var text = value.ToString();
            return string.Equals(text, "true", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "1", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "yes", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "active", StringComparison.OrdinalIgnoreCase)
                || string.Equals(text, "locked", StringComparison.OrdinalIgnoreCase);
        }

        private sealed class StoredRefreshToken
        {
            public long RefreshTokenId { get; set; }
            public int UserId { get; set; }
            public string TokenHash { get; set; } = string.Empty;
            public string SecurityStamp { get; set; } = string.Empty;
            public DateTime ExpiresOn { get; set; }
            public DateTime? RevokedOn { get; set; }
        }
    }
}
