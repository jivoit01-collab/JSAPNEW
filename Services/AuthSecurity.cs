using System.Security.Cryptography;
using System.Text;
using JSAPNEW.Services.Implementation;

namespace JSAPNEW.Services
{
    public static class AuthSecurity
    {
        private const int BCryptWorkFactor = 12;

        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCryptWorkFactor);
        }

        public static bool IsBCryptHash(string? passwordHash)
        {
            return !string.IsNullOrWhiteSpace(passwordHash)
                && (passwordHash.StartsWith("$2a$")
                    || passwordHash.StartsWith("$2b$")
                    || passwordHash.StartsWith("$2y$"));
        }

        public static bool VerifyPassword(string password, string storedPassword)
        {
            if (IsBCryptHash(storedPassword))
            {
                return BCrypt.Net.BCrypt.Verify(password, storedPassword);
            }

            return string.Equals(Encryption.Encrypt(password), storedPassword, StringComparison.Ordinal);
        }

        public static string CreateSecurityStamp(string? passwordHash, string secretKey, params object?[] stateParts)
        {
            var input = string.Join("|", new[]
            {
                secretKey,
                passwordHash ?? string.Empty
            }.Concat(stateParts.Select(part => part?.ToString() ?? string.Empty)));

            var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
            return Convert.ToHexString(bytes);
        }

        public static object? GetValue(IDictionary<string, object> row, params string[] names)
        {
            foreach (var name in names)
            {
                var match = row.FirstOrDefault(item => item.Key.Equals(name, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrEmpty(match.Key))
                {
                    return match.Value;
                }
            }

            return null;
        }

        public static string CreateSecurityStamp(IDictionary<string, object> userRow, string secretKey)
        {
            var passwordHash = GetValue(userRow, "Password")?.ToString();
            var stateParts = userRow
                .Where(item => !item.Key.Equals("Password", StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(item => item.Value)
                .ToArray();

            return CreateSecurityStamp(passwordHash, secretKey, stateParts);
        }

        public static string CreateSecurityStamp(IDictionary<string, object> userRow, string secretKey, string roleSnapshot)
        {
            var passwordHash = GetValue(userRow, "Password")?.ToString();
            var stateParts = userRow
                .Where(item => !item.Key.Equals("Password", StringComparison.OrdinalIgnoreCase))
                .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                .Select(item => item.Value)
                .Concat(new object?[] { roleSnapshot })
                .ToArray();

            return CreateSecurityStamp(passwordHash, secretKey, stateParts);
        }

        public static string CreateRoleSnapshot(IEnumerable<dynamic> roleRows)
        {
            var rows = roleRows
                .Select(row => (IDictionary<string, object>)row)
                .Select(row => string.Join(";", row
                    .OrderBy(item => item.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(item => $"{item.Key}={item.Value}")))
                .OrderBy(row => row, StringComparer.OrdinalIgnoreCase);

            return string.Join("|", rows);
        }
    }
}
