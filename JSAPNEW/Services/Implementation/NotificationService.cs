using Dapper;
using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using JSAPNEW.Services.Implementation;
using ServiceStack;
using System.Text.RegularExpressions;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace JSAPNEW.Services.Implementation
{
    public class NotificationService : INotificationService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _jsonFilePath;
        private static readonly HashSet<string> _sentTokensCache = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static readonly object _lockObject = new object();

        public NotificationService(IConfiguration configuration, IWebHostEnvironment env)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            var relativePath = configuration["Firebase:ServiceAccountPath"];
            _jsonFilePath = Path.Combine(env.ContentRootPath, relativePath);
        }
        public async Task<IEnumerable<NotificationResponse>> DeleteOldNotificationsAsync(int days_old)
        {
            var response = new List<NotificationResponse>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("[nt].[jsDeleteOldNotifications]", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@days_old", days_old);

                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();

                        response.Add(new NotificationResponse
                        {
                            Success = true,
                            Message = "Old Notifications deleted successfully"
                        });
                    }
                }
            }
            catch (SqlException ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"SQL Error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"Error: {ex.Message}" });
            }

            return response;
        }
        public async Task<IEnumerable<NotificationResponse>> DeleteOldUserTokensAsync(int days_old)
        {
            var response = new List<NotificationResponse>();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("[nt].[jsDeleteOldUserTokens]", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@days_old", days_old);

                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();

                        response.Add(new NotificationResponse
                        {
                            Success = true,
                            Message = "Old user tokens deleted successfully"
                        });
                    }
                }
            }
            catch (SqlException ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"SQL Error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"Error: {ex.Message}" });
            }

            return response;
        }
        public async Task<IEnumerable<NotificationModels>> GetUnreadNotificationCountAsync(int userId)
        {
            var sqlQuery = "EXEC [nt].[jsGetUnreadNotificationCount] @userId";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(); // Ensure the connection is opened asynchronously
                return await connection.QueryAsync<NotificationModels>(
                    sqlQuery,
                    new { userId }
                );
            }
        }
        public async Task<IEnumerable<UserNotificationsModel>> GetUserNotificationsAsync(int userId)
        {
            var sqlQuery = "EXEC [nt].[jsGetUserNotifications] @userId";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(); // Ensure the connection is opened asynchronously
                return await connection.QueryAsync<UserNotificationsModel>(
                    sqlQuery,
                    new { userId }
                );
            }
        }
        public async Task<IEnumerable<NotificationResponse>> InsertNotificationAsync(InsertNotificationModel request)
        {
            var response = new List<NotificationResponse>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("[nt].[jsInsertNotification]", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@userId", request.userId);
                        command.Parameters.AddWithValue("@title", request.title);
                        command.Parameters.AddWithValue("@message", request.message);
                        command.Parameters.AddWithValue("@pageId", request.pageId);
                        command.Parameters.AddWithValue("@data", request.data);
                        command.Parameters.AddWithValue("@BudgetId", request.BudgetId);

                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();
                        response.Add(new NotificationResponse
                        {
                            Success = true,
                            Message = "Notification inserted successfully"
                        });
                    }
                }
            }
            catch (SqlException ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"SQL Error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"Error: {ex.Message}" });
            }
            return response;
        }
        public async Task<IEnumerable<NotificationResponse>> MarkAllNotificationsAsReadAsync(int userId)
        {
            var response = new List<NotificationResponse>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("[nt].[jsMarkAllNotificationsAsRead]", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@userId", userId);
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();
                        response.Add(new NotificationResponse
                        {
                            Success = true,
                            Message = "All notifications marked as read successfully"
                        });
                    }
                }
            }
            catch (SqlException ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"SQL Error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"Error: {ex.Message}" });
            }
            return response;
        }
        public async Task<IEnumerable<NotificationResponse>> MarkNotificationAsReadAsync(int notificationId)
        {
            var response = new List<NotificationResponse>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("[nt].[jsMarkNotificationAsRead]", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@notificationId", notificationId);
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();
                        response.Add(new NotificationResponse
                        {
                            Success = true,
                            Message = "Notification marked as read successfully"
                        });
                    }
                }
            }
            catch (SqlException ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"SQL Error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"Error: {ex.Message}" });
            }
            return response;
        }
        public async Task<IEnumerable<NotificationResponse>> SaveUserToken(int userId, string fcmToken, string deviceId)
        {
            var response = new List<NotificationResponse>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("[nt].[jsSaveUserToken]", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@fcmToken", fcmToken);
                        command.Parameters.AddWithValue("@deviceId", deviceId);
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();
                        response.Add(new NotificationResponse
                        {
                            Success = true,
                            Message = "User token saved successfully"
                        });
                    }
                }
            }
            catch (SqlException ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"SQL Error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"Error: {ex.Message}" });
            }
            return response;
        }
        public async Task<IEnumerable<NotificationResponse>> CreatePageAsync(CreatePageRequest request)
        {
            var response = new List<NotificationResponse>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("[dbo].[jsCreatePage]", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@pageName", request.pageName);
                        command.Parameters.AddWithValue("@pageUrl", request.pageUrl);
                        command.Parameters.AddWithValue("@userId", request.userId);
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();
                        response.Add(new NotificationResponse
                        {
                            Success = true,
                            Message = "Page created successfully"
                        });
                    }
                }
            }
            catch (SqlException ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"SQL Error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"Error: {ex.Message}" });
            }
            return response;
        }

        public async Task<string?> GetUserTokenAsync(int userId)
        {
            var sqlQuery = "EXEC [nt].[jsGetUserToken] @userId";
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    var token = await connection.ExecuteScalarAsync<string>(sqlQuery, new { userId });
                    Console.WriteLine($"Retrieved token for user {userId}: {token}");
                    return token;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error retrieving token: {ex.Message}");
                return null;
            }
        }
        public async Task<List<UserTokenModel>> GetUserFcmTokenAsync(int userId)
        {
            var tokens = new List<UserTokenModel>();

            using (var connection = new SqlConnection(_connectionString))
            using (var command = new SqlCommand("[nt].[jsGetUserToken]", connection))
            {
                command.CommandType = CommandType.StoredProcedure;
                command.Parameters.AddWithValue("@userId", userId);

                await connection.OpenAsync();

                using (var reader = await command.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        tokens.Add(new UserTokenModel
                        {
                            fcmToken = reader["fcmToken"] == DBNull.Value ? null : reader["fcmToken"].ToString()
                        });
                    }
                }
            }

            return tokens;
        }

        public async Task SendPushNotificationAsync(string title, string body, string fcmToken, Dictionary<string, string>? data = null)
        {
            var normalizedToken = fcmToken?.Trim();
            if (string.IsNullOrWhiteSpace(normalizedToken))
            {
                Console.WriteLine("❌ Empty token received");
                return;
            }

            Console.WriteLine($"📤 Sending notification to token: {normalizedToken.Substring(0, Math.Min(30, normalizedToken.Length))}...");

            var credential = GoogleCredential
                .FromFile(_jsonFilePath)
                .CreateScoped("https://www.googleapis.com/auth/firebase.messaging");
            var accessToken = await credential.UnderlyingCredential.GetAccessTokenForRequestAsync();

            var message = new
            {
                token = normalizedToken,
                notification = new { title, body },
                android = new { priority = "HIGH", notification = new { sound = "default" } },
                apns = new
                {
                    headers = new Dictionary<string, string> { ["apns-priority"] = "10" },
                    payload = new
                    {
                        aps = new Dictionary<string, object>
                        {
                            ["alert"] = new { title, body },
                            ["badge"] = 1,
                            ["sound"] = "default",
                            ["content-available"] = 1
                        }
                    }
                },
                data = data ?? new Dictionary<string, string>()
            };

            var json = JsonConvert.SerializeObject(new { message });
            using var client = new HttpClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var url = "https://fcm.googleapis.com/v1/projects/jsap-4e458/messages:send";
            var response = await client.PostAsync(url, content);
            var result = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                Console.WriteLine($"❌ FCM Error: {result}");
            else
                Console.WriteLine($"✅ Notification sent successfully to FCM");
        }

        public async Task<IEnumerable<NotificationResponse>> DeleteTokensAsync(int userId, string deviceId)
        {
            var response = new List<NotificationResponse>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("[nt].[deleteToken]", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@deviceId", deviceId);
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();
                        response.Add(new NotificationResponse
                        {
                            Success = true,
                            Message = "Tokens deleted successfully"
                        });
                    }
                }
            }
            catch (SqlException ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"SQL Error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"Error: {ex.Message}" });
            }
            return response;
        }
        public async Task<IEnumerable<NotificationResponse>> InsertDeviceInfoAsync(int userId, string deviceId, string appVersion)
        {
            var response = new List<NotificationResponse>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();
                    using (var command = new SqlCommand("[dbo].[insertDeviceInfo]", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@deviceId", deviceId);
                        command.Parameters.AddWithValue("@appVersion", appVersion);
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();
                        response.Add(new NotificationResponse
                        {
                            Success = true,
                            Message = "Device info inserted successfully"
                        });
                    }
                }
            }
            catch (SqlException ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"SQL Error: {ex.Message}" });
            }
            catch (Exception ex)
            {
                response.Add(new NotificationResponse { Success = false, Message = $"Error: {ex.Message}" });
            }
            return response;
        }


    }
}