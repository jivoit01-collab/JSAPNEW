using BCrypt.Net;

namespace JSAPNEW.Services.Implementation
{
    public static class PasswordHasher
    {
        public static string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, BCrypt.Net.BCrypt.GenerateSalt(12));
        }

        public static bool VerifyPassword(string password, string hashedPassword)
        {
            if (string.IsNullOrWhiteSpace(password) || string.IsNullOrWhiteSpace(hashedPassword))
                return false;
            return BCrypt.Net.BCrypt.Verify(password, hashedPassword);
        }

        public static bool IsBcryptHash(string password)
        {
            return password.StartsWith("$2a$") || password.StartsWith("$2b$") || password.StartsWith("$2y$");
        }
    }
}
