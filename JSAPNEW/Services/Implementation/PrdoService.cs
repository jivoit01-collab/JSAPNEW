using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using Sap.Data.Hana;
using ServiceStack;
using JSAPNEW.Data.Entities;
using Microsoft.AspNetCore.Mvc;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text;

namespace JSAPNEW.Services.Implementation
{
    public class PrdoService : IPrdoService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly Dictionary<int, HanaCompanySettings> _hanaSettings;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        public PrdoService(IConfiguration configuration, INotificationService notificationService, IUserService userService)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            var activeEnv = configuration["ActiveEnvironment"];  // "Test" or "Live"
            _hanaSettings = configuration.GetSection($"HanaSettings:{activeEnv}")
                                         .Get<Dictionary<int, HanaCompanySettings>>();
            _notificationService = notificationService;
            _userService = userService;
        }

        public async Task<List<ApprovedProductionOrder>> GetApprovedProductionOrdersAsync(int userId, int company, string month)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@userId", userId);
            parameters.Add("@company", company);
            parameters.Add("@month", month);

            var result = await connection.QueryAsync<ApprovedProductionOrder>(
                "[PRDO].[jsGetApprovedProductionOrders]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        public async Task<List<PendingProductionOrder>> GetPendingProductionOrdersAsync(int userId, int company, string month)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@userId", userId);
            parameters.Add("@company", company);
            parameters.Add("@month", month);

            var result = await connection.QueryAsync<PendingProductionOrder>(
                "[PRDO].[jsGetPendingProductionOrders]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        public async Task<List<RejectProductionOrder>> GetRejectedProductionOrdersAsync(int userId, int company, string month)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            var parameters = new DynamicParameters();
            parameters.Add("@userId", userId);
            parameters.Add("@company", company);
            parameters.Add("@month", month);
            var result = await connection.QueryAsync<RejectProductionOrder>(
                "[PRDO].[jsGetRejectedProductionOrders]",
                parameters,
                commandType: CommandType.StoredProcedure
            );
            return result.ToList();
        }
        public async Task<List<ProductionOrderInsightModel>> GetProductionOrderInsightAsync(int userId, int company, string month)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@userId", userId);
            parameters.Add("@company", company);
            parameters.Add("@month", month);

            var result = await connection.QueryAsync<ProductionOrderInsightModel>(
                "[PRDO].[jsGetProductionOrderInsight]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }
        public async Task<List<ProductionOrderInsightAllModel>> GetProductionOrderInsightAllAsync(int company, string month)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@company", company);
            parameters.Add("@month", month);

            var result = await connection.QueryAsync<ProductionOrderInsightAllModel>(
                "[PRDO].[jsGetProductionOrderInsightAll]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        /*public async Task<PrdoModels> ApproveProductionOrderAsync(ProductionOrderApprovalRequest request)
        {
            var response = new PrdoModels();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("[PRDO].[jsApproveProductionOrder]", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 120;

                        // Add parameters
                        command.Parameters.AddWithValue("@docId", request.DocId);
                        command.Parameters.AddWithValue("@company", request.Company);
                        command.Parameters.AddWithValue("@userId", request.UserId);
                        command.Parameters.AddWithValue("@remarks", string.IsNullOrEmpty(request.Remarks) ? (object)DBNull.Value : request.Remarks);

                        // Execute and read result
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                response.Success = true;
                                response.Message = reader["ResultMessage"]?.ToString();
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                response.Success = false;
                response.Message = ex.Message;

                // Handle specific error codes
                if (ex.Number == 50020)
                {
                    response.Message = "User has already approved this production order";
                }
                else if (ex.Number == 50001)
                {
                    response.Message = "Production order not found";
                }
                else
                {
                    response.Message = "An error occurred while processing the approval";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }
*/

        public async Task<PrdoModels> ApproveProductionOrderAsync(ProductionOrderApprovalRequest request)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    // ✅ Split docIds (comma-separated string) -> List<int>
                    var docIds = request.DocIds?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                                .Select(id => int.Parse(id.Trim()))
                                                .ToList() ?? new List<int>();

                    var resultMessages = new List<string>();
                    var allNotificationModels = new List<UserIdsForNotificationModel>();

                    foreach (var docId in docIds)
                    {
                        using (SqlCommand cmd = new SqlCommand("[PRDO].[jsApproveProductionOrder]", conn))
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.Parameters.AddWithValue("@docId", docId);
                            cmd.Parameters.AddWithValue("@company", request.Company);
                            cmd.Parameters.AddWithValue("@userId", request.UserId);
                            cmd.Parameters.AddWithValue("@remarks", (object?)request.Remarks ?? DBNull.Value);

                            var result = await cmd.ExecuteScalarAsync();
                            resultMessages.Add(result?.ToString() ?? $"Approved Production Order ID {docId}");
                        }

                        var notifications = await GetProductionUserIdsSendNotificatiosAsync(docId);
                        if (notifications != null)
                            allNotificationModels.AddRange(notifications);
                    }

                    // ✅ Deduplicate notification models
                    allNotificationModels = allNotificationModels
                        .Where(m => !string.IsNullOrWhiteSpace(m.userIdsToApprove))
                        .GroupBy(m => m.userIdsToApprove)
                        .Select(g => g.First())
                        .ToList();

                    // ✅ Extract unique user IDs
                    var uniqueUserIds = new HashSet<int>(
                        allNotificationModels
                            .SelectMany(m => m.userIdsToApprove.Split(',', StringSplitOptions.RemoveEmptyEntries))
                            .Select(s => int.Parse(s.Trim()))
                    );

                    // ✅ Dynamic notification text
                    string notificationTitle = docIds.Count == 1
                        ? "You have received a new Production Document"
                        : "You have received new Production Documents";

                    string notificationBody = docIds.Count == 1
                        ? $"New Production document (FlowId: {docIds[0]}) forwarded to you for review."
                        : $"New Production documents ({string.Join(", ", docIds)}) forwarded to you for review.";

                    var data = new Dictionary<string, string>
                    {
                        { "screen", "details" },
                        { "company", request.Company.ToString() },
                        { "flowIds", string.Join(",", docIds) }
                    };

                    var sentTokens = new HashSet<string>();

                    foreach (var userId in uniqueUserIds)
                    {
                        var fcmTokens = await _notificationService.GetUserFcmTokenAsync(userId);
                        if (fcmTokens == null || fcmTokens.Count == 0)
                            continue;

                        foreach (var token in fcmTokens)
                        {
                            if (string.IsNullOrWhiteSpace(token.fcmToken))
                                continue;

                            if (sentTokens.Contains(token.fcmToken))
                                continue;

                            await _notificationService.SendPushNotificationAsync(
                                notificationTitle,
                                notificationBody,
                                token.fcmToken,
                                data
                            );

                            sentTokens.Add(token.fcmToken);
                        }

                        // ✅ One DB notification per user
                        await _notificationService.InsertNotificationAsync(new InsertNotificationModel
                        {
                            userId = userId,
                            title = "Production Document",
                            message = notificationBody,
                            pageId = 6,
                            data = docIds.Count == 1
                                ? $"Flow ID: {docIds[0]}"
                                : $"Flow IDs: {string.Join(", ", docIds)}",
                            BudgetId = docIds.FirstOrDefault() // For reference
                        });
                    }

                    return new PrdoModels
                    {
                        Success = true,
                        Message = string.Join(" | ", resultMessages)
                    };
                }
            }
            catch (SqlException ex)
            {
                return new PrdoModels { Success = false, Message = $"SQL Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new PrdoModels { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<PrdoModels> RejectProductionOrderAsync(ProductionOrderRejectRequest request)
        {
            var response = new PrdoModels();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    using (var command = new SqlCommand("[PRDO].[jsRejectProductionOrder]", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;
                        command.CommandTimeout = 120;

                        // Add parameters
                        command.Parameters.AddWithValue("@docId", request.DocId);
                        command.Parameters.AddWithValue("@company", request.Company);
                        command.Parameters.AddWithValue("@userId", request.UserId);
                        command.Parameters.AddWithValue("@remarks", request.Remarks);

                        // Execute and read result
                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                response.Success = true;
                                response.Message = reader["ResultMessage"]?.ToString();
                            }
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<List<ProductionOrderDetailDTO>> GetProductionOrderDetailByIdAsync(int productionOrderId, int company)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var parameters = new DynamicParameters();
            parameters.Add("@productionOrderId", productionOrderId);
            parameters.Add("@company", company);

            var result = await connection.QueryAsync<ProductionOrderDetailDTO>(
                "[PRDO].[jsGetProductionOrderDetailById]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result.ToList();
        }

        public async Task<List<ProductionOrderApprovalFlowModel>> GetProductionOrderApprovalFlowAsync(int productionOrderId)
        {
            var result = new List<ProductionOrderApprovalFlowModel>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@productionOrderId", productionOrderId, DbType.Int32);

                var query = "[PRDO].[jsGetProductionOrderApprovalFlow]";

                var data = await connection.QueryAsync<ProductionOrderApprovalFlowModel>(
                    query,
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                result = data.AsList();
            }

            return result;
        }

        public async Task<IEnumerable<ItemLocationStockModel>> GetItemLocationStockModelAsync(string ItemCode, string Warehouse, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("I_ItemCode", ItemCode);
                parameters.Add("I_Warehouse", Warehouse);
                var sql = $"CALL \"{settings.Schema}\".\"Get_Item_Location_Stock\"(?,?)";

                var result = await connection.QueryAsync<ItemLocationStockModel>(
                     sql, parameters
                 );

                return result;
            }
        }

        public async Task<IEnumerable<AllProductionOrderDetailDTO>> GetAllProductionOrderAsync(int userId, int company, string month)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var pendingPrdo = await connection.QueryAsync<AllProductionOrderDetailDTO>(
                    "EXEC [PRDO].[jsGetPendingProductionOrders] @userId, @company, @month", new { userId, company, month });

                var approvedPrdo = await connection.QueryAsync<AllProductionOrderDetailDTO>(
                    "EXEC [PRDO].[jsGetApprovedProductionOrders] @userId, @company, @month", new { userId, company, month });

                var rejectedPrdo = await connection.QueryAsync<AllProductionOrderDetailDTO>(
                    "EXEC [PRDO].[jsGetRejectedProductionOrders] @userId, @company, @month", new { userId, company, month });

                var allBP = new List<AllProductionOrderDetailDTO>();

                foreach (var Items in pendingPrdo)
                {
                    Items.status = "pending";
                    allBP.Add(Items);
                }
                foreach (var Items in approvedPrdo)
                {
                    Items.status = "approved";
                    allBP.Add(Items);
                }
                foreach (var Items in rejectedPrdo)
                {
                    Items.status = "rejected";
                    allBP.Add(Items);
                }
                return allBP;
            }
        }

        public async Task<IEnumerable<UserIdsForNotificationModel>> GetProductionUserIdsSendNotificatiosAsync(int flowId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@prdoId", flowId);
                    var result = await conn.QueryAsync<UserIdsForNotificationModel>("[PRDO].[jsPrdoNotify]", parameters, commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error Executing : ", ex);
            }
        }

        public async Task<PrdoModels> SendPendingProductionCountNotificationAsync()
        {
            var responseMessage = new StringBuilder();
            bool overallSuccess = true;
            bool foundAnyPending = false;

            try
            {
                var activeUsers = await _userService.GetActiveUser();
                if (activeUsers == null || !activeUsers.Any())
                    return new PrdoModels { Success = false, Message = "No active users found." };

                // ✅ Track which tokens we've already sent to (to prevent duplicates in this request)
                var sentTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                // Track which users we've already processed
                HashSet<int> notifiedUsers = new HashSet<int>();

                foreach (var user in activeUsers)
                {
                    int userId = user.userId;

                    // Skip if we've already notified this user
                    if (notifiedUsers.Contains(userId))
                        continue;

                    int company = user.company;
                    string month = DateTime.Now.ToString("MM-yyyy");

                    // fetch all counts for this single user
                    var counts = await GetProductionOrderInsightAsync(userId, company, month);
                    if (counts == null || !counts.Any())
                    {
                        responseMessage.AppendLine($"No document counts for user {userId}.");
                        continue;
                    }

                    // total up ALL pendings for this user
                    int totalPending = counts.Sum(c => c.TotalPending);
                    if (totalPending <= 0)
                        continue;   // nothing to send

                    foundAnyPending = true;

                    // ✅ Get list of FCM tokens
                    var fcmTokens = await _notificationService.GetUserFcmTokenAsync(userId);

                    if (fcmTokens == null || fcmTokens.Count == 0)
                    {
                        responseMessage.AppendLine($"No FCM token for user {userId}.");
                        overallSuccess = false;
                        continue;
                    }

                    // build notification data
                    string title = $"You have {totalPending} " +
                                   (totalPending == 1 ? "Production pending request" : "Production pending requests");
                    string body = "Kindly Approve.";

                    var data = new Dictionary<string, string>
                    {
                        { "userId",  userId.ToString() },
                        { "company", company.ToString() },
                        { "screen",  "pending" }
                    };

                    // ✅ Send push notification to all tokens (but only once per token)
                    int tokensSent = 0;
                    foreach (var token in fcmTokens)
                    {
                        var normalizedToken = token.fcmToken?.Trim();

                        if (string.IsNullOrWhiteSpace(normalizedToken))
                            continue;

                        // ✅ Check if already sent to this token
                        if (sentTokens.Contains(normalizedToken))
                        {
                            Console.WriteLine($"⏭️ Skipping duplicate token for userId {userId}");
                            continue;
                        }

                        await _notificationService.SendPushNotificationAsync(
                            title,
                            body,
                            normalizedToken,
                            data
                        );

                        // ✅ Mark this token as sent
                        sentTokens.Add(normalizedToken);
                        tokensSent++;
                    }

                    if (tokensSent > 0)
                    {
                        // Add to our tracking set after successful notification
                        notifiedUsers.Add(userId);
                        responseMessage.AppendLine($"Notification sent to user {userId} ({tokensSent} device(s)).");
                    }
                    else
                    {
                        responseMessage.AppendLine($"No valid/unique tokens for user {userId}.");
                    }
                }

                if (!foundAnyPending)
                    return new PrdoModels
                    {
                        Success = true,
                        Message = "No pending requests for any active user."
                    };

                return new PrdoModels
                {
                    Success = overallSuccess,
                    Message = responseMessage.ToString().Trim()
                };
            }
            catch (Exception ex)
            {
                return new PrdoModels
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
    }
}
