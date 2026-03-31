using Dapper;
using JSAPNEW.Data.Entities;
using Microsoft.Data.SqlClient;

namespace JSAPNEW.Services.Implementation
{
    public class UserServiceBase
    {
        /*public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var user = await connection.QueryFirstOrDefaultAsync<User>(
                    "SELECT * FROM jsUser WHERE UserId = @UserId",
                    new { UserId = userId }
                );

                if (user == null || !VerifyPassword(currentPassword, user.Password))
                {
                    return false;
                }

                string hashedPassword = Encryption.Encrypt(newPassword);

                await connection.ExecuteAsync(
                    "UPDATE jsUser SET Password = @Password WHERE UserId = @UserId",
                    new { UserId = userId, Password = hashedPassword }
                );

                return true;
            }
        }
   */
    }
}