using Azure;
using Dapper;
using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Http.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Sap.Data.Hana;
using ServiceStack;
using ServiceStack.Html;
using ServiceStack.Text;
using System.ComponentModel.Design;
using System.Data;
using System.Text;

namespace JSAPNEW.Services.Implementation
{
    public class AdvanceRequestService : IAdvanceRequestService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly IBom2Service _bom2Service;
        public AdvanceRequestService(IConfiguration configuration, INotificationService notificationService, IUserService userService, IBom2Service bom2Service)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _notificationService = notificationService;
            _userService = userService;
            _bom2Service = bom2Service;
        }

        public async Task<IEnumerable<AdvanceRequestModels>> AdvancePaymentRequestAsync(string IN_BRANCH, string IN_TYPE)
        {

            var sqlQuery = "EXEC [adv].[GET_PARTY_DATA] @IN_BRANCH,@IN_TYPE";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<AdvanceRequestModels>(
                    sqlQuery,
                    new { IN_BRANCH, IN_TYPE } // Parameters for the stored procedure
                );
            }
        }

        public async Task<VendorExpenseResponse> InsertVendorExpenseAsync(VendorExpenseRequest request, List<FileDetails> fileDetails)
        {
            var response = new VendorExpenseResponse();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    using (var command = new SqlCommand("adv.insertVendoeExpense", connection))
                    {
                        command.CommandType = CommandType.StoredProcedure;

                        command.Parameters.AddWithValue("@branch", request.Branch);
                        command.Parameters.AddWithValue("@type", request.Type);
                        command.Parameters.AddWithValue("@search", request.Search);
                        command.Parameters.AddWithValue("@department", request.Department);
                        command.Parameters.AddWithValue("@amount", request.Amount);
                        command.Parameters.AddWithValue("@remark", request.Remark);
                        command.Parameters.AddWithValue("@priority", request.Priority);
                        command.Parameters.AddWithValue("@expectedPayDate", request.ExpectedPayDate);
                        command.Parameters.AddWithValue("@userId", request.UserId);
                        command.Parameters.AddWithValue("@purpose", string.IsNullOrWhiteSpace(request.Purpose) ? DBNull.Value : request.Purpose);
                        command.Parameters.AddWithValue("@expectedGrpoDate", request.ExpectedGrpoDate ?? (object)DBNull.Value);
                        command.Parameters.AddWithValue("@emiMonth", string.IsNullOrWhiteSpace(request.emiMonth) ? DBNull.Value : request.emiMonth);
                        command.Parameters.AddWithValue("@po", string.IsNullOrWhiteSpace(request.po) ? DBNull.Value : request.po);

                        var idParam = new SqlParameter("@id", SqlDbType.Int)
                        {
                            Direction = ParameterDirection.Output
                        };
                        command.Parameters.Add(idParam);

                        var table = new DataTable();
                        table.Columns.Add("fileName", typeof(string));
                        table.Columns.Add("fileExtension", typeof(string));
                        table.Columns.Add("filePath", typeof(string));
                        table.Columns.Add("fileSize", typeof(long)); // size in bytes

                        foreach (var file in fileDetails ?? new())
                        {
                            table.Rows.Add(file.AttachmentName, file.AttachmentExtension, file.AttachmentPath, file.AttachmentSize);
                        }

                        var tvpParam = new SqlParameter("@attachments", SqlDbType.Structured)
                        {
                            TypeName = "FileAttachmentType",
                            Value = table
                        };
                        command.Parameters.Add(tvpParam);

                        await connection.OpenAsync();

                        using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (reader.HasRows && await reader.ReadAsync())
                            {
                                response.FlowId = reader["flowId"] != DBNull.Value ? Convert.ToInt32(reader["flowId"]) : 0;
                                response.ExpenseId = reader["expenseId"] != DBNull.Value ? Convert.ToInt32(reader["expenseId"]) : 0;
                            }
                        }

                        response.success = true;
                        response.message = "Expense submitted successfully.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.success = false;
                response.message = $"Error occurred: {ex.Message}";
            }

            return response;
        }


        /*public async Task<IEnumerable<AdvanceRequestModels>> AdvancePaymentRequestAsync(string IN_BRANCH, string IN_TYPE)
        {
            using (var connection = new HanaConnection(_HanaconnectionString))
            {

                var parameters = new DynamicParameters();
                parameters.Add("IN_BRANCH", IN_BRANCH);
                parameters.Add("IN_TYPE", IN_TYPE);

                var result = await connection.QueryAsync<AdvanceRequestModels>(
                    "CALL \"TEST_OIL_11FEB\".\"GET_PARTY_DATA\"(?,?)",
                    parameters);
                return result;
            }
        }
*/
        /*public async Task<(int Id, int Status)> InsertVendorExpenseAsync(VendorExpenseRequest request, FileDetails file)
        {
            int p_id = 0;
            int p_status = 0;
            
                using (var connection = new HanaConnection(_HanaconnectionString))
                {
                    var Parameters = new DynamicParameters();


                    Parameters.Add("p_branch", request.Branch);
                    Parameters.Add("p_type", request.Type);
                    Parameters.Add("p_search", request.Search);
                    Parameters.Add("p_department", request.Department);
                    Parameters.Add("p_amount", request.Amount);
                    Parameters.Add("p_purpose", request.Purpose);
                    Parameters.Add("p_remark", request.Remark);
                    Parameters.Add("p_priority", request.Priority);
                    Parameters.Add("p_expected_pay_date", request.ExpectedPayDate);
                    Parameters.Add("p_expected_grpo_date", request.ExpectedGrpoDate);
                    Parameters.Add("p_user_id", string.IsNullOrEmpty(request.UserId) ? DBNull.Value : (object)request.UserId);

                    // Add file parameters if file exists
                    if (file != null)
                    {
                        Parameters.Add("p_attachment_name", file.AttachmentName);
                        Parameters.Add("p_attachment_extension", file.AttachmentExtension);
                        Parameters.Add("p_attachment_path", file.AttachmentPath);
                        Parameters.Add("p_attachment_size", file.AttachmentSize);
                    }
                    else
                    {
                        Parameters.Add("p_attachment_name", DBNull.Value);
                        Parameters.Add("p_attachment_extension", DBNull.Value);
                        Parameters.Add("p_attachment_path", DBNull.Value);
                        Parameters.Add("p_attachment_size", DBNull.Value);
                    }
                Parameters.Add("p_id", dbType: DbType.Int32, direction: ParameterDirection.Output);
                Parameters.Add("p_status", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var result = await connection.QueryAsync<AdvanceRequestModels>(
                        "CALL \"TEST_OIL_11FEB\".\"PAY_SP_INSERT_PAY_VENDOR_EXPENSE\"(?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)",
                            Parameters);

                p_id = Parameters.Get<int>("p_id");
                p_status = Parameters.Get<int>("p_status");

                return (p_id, p_status);
            }
        }
*/

        public async Task<IEnumerable<ExpensesModels>> GetPendingExpensesAsync(int userId, int companyId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sqlQuery = "EXEC [adv].[jsGetPendingExpenses] @userId, @companyId";
                return await connection.QueryAsync<ExpensesModels>(sqlQuery, new { userId, companyId });
            }
        }

        public async Task<IEnumerable<ExpensesModels>> GetApprovedExpensesAsync(int userId, int companyId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sqlQuery = "EXEC [adv].[jsGetApprovedExpenses] @userId, @companyId";
                return await connection.QueryAsync<ExpensesModels>(sqlQuery, new { userId, companyId });
            }
        }
        public async Task<IEnumerable<ExpensesModels>> GetRejectedExpensesAsync(int userId, int companyId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sqlQuery = "EXEC [adv].[jsGetRejectedExpenses] @userId, @companyId";
                return await connection.QueryAsync<ExpensesModels>(sqlQuery, new { userId, companyId });
            }
        }
        public async Task<IEnumerable<ExpenseInsightsModels>> GetExpenseInsightsAsync(int userId, int companyId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sqlQuery = "EXEC [adv].[jsGetExpenseInsights] @userId, @companyId";
                return await connection.QueryAsync<ExpenseInsightsModels>(sqlQuery, new { userId, companyId });
            }
        }
        public async Task<IEnumerable<ExpensesModels>> GetTotalExpensesAsync(int userId, int companyId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var pending = await connection.QueryAsync<ExpensesModels>(
                    "EXEC [adv].[jsGetPendingExpenses] @userId, @companyId",
                    new { userId, companyId });

                var approved = await connection.QueryAsync<ExpensesModels>(
                    "EXEC [adv].[jsGetApprovedExpenses] @userId, @companyId",
                    new { userId, companyId });

                var rejected = await connection.QueryAsync<ExpensesModels>(
                    "EXEC [adv].[jsGetRejectedExpenses] @userId, @companyId",
                    new { userId, companyId });

                var allExpense = new List<ExpensesModels>();

                foreach (var expense in pending)
                {
                    if (expense.Id != null && expense.Id != 0)
                    {
                        expense.Status = "Pending";
                        allExpense.Add(expense);
                    }
                }

                foreach (var expense in approved)
                {
                    if (expense.Id != null && expense.Id != 0)
                    {
                        expense.Status = "Approved";
                        allExpense.Add(expense);
                    }
                }

                foreach (var expense in rejected)
                {
                    if (expense.Id != null && expense.Id != 0)
                    {
                        expense.Status = "Rejected";
                        allExpense.Add(expense);
                    }
                }

                return allExpense;
            }
        }
        public async Task<ExpenseDetailsResponse> GetExpenseDetailsByFlowIdAsync(int flowId, IUrlHelper urlHelper)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var multi = await connection.QueryMultipleAsync(
                "[adv].[jsGetExpenseDetailsByFlowId]",
                new { flowId },
                commandType: CommandType.StoredProcedure);
            var header = await multi.ReadFirstOrDefaultAsync<ExpensesModels>();
            var payFlow = await multi.ReadFirstOrDefaultAsync<PayFlowModel>();
            var stages = (await multi.ReadAsync<StageDetailsModel>()).ToList();
            var id = payFlow.ExpenseId;

            // Fetch attachments
            var attachments = (await GetAdvanceAttachmentsAsync(id)).ToList();

            // Add DownloadUrl to each attachment
            foreach (var attachment in attachments)
            {
                if (string.IsNullOrEmpty(attachment.filePath) || string.IsNullOrEmpty(attachment.fileName))
                    continue;

                // Ensure forward slashes and clean the path
                string cleanFilePath = attachment.filePath.Replace("\\", "/").Trim();

                // Remove leading slash if present for consistency
                if (cleanFilePath.StartsWith("/"))
                    cleanFilePath = cleanFilePath.Substring(1);

                // Clean file extension by removing leading dot if present
                string cleanExt = attachment.fileExtension?.TrimStart('.') ?? "";
                // Get file name without extension
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(attachment.fileName);
                // Build URL for AdvanceDownloadFile - DON'T remove trailing content from path
                attachment.DownloadUrl = urlHelper.Action("AdvanceDownloadFile", "File", new
                {
                    filePath = cleanFilePath, // Use full path: "Uploads/Advancepayment"
                    fileName = fileNameWithoutExt,
                    fileExt = cleanExt
                }, "https");

                // Debug: Log what we're generating
                // Console.WriteLine($"Generated URL for {attachment.fileName}: filePath='{cleanFilePath}', fileName='{attachment.fileName}', fileExt='{cleanExt}'");
            }

            return new ExpenseDetailsResponse
            {
                Header = header,
                PayFlow = payFlow,
                Stages = stages,
                Attachments = attachments
            };
        }
        public async Task<IEnumerable<AttachmentModels>> GetAdvanceAttachmentsAsync(int id)
        {
            var sqlQuery = "EXEC [adv].[jsGetFilesByExpenseId] @id";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<AttachmentModels>(
                    sqlQuery,
                   new { id } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<ApprovalIdsModel>> GetApprovalUserIdsAsync(int advPayId)
        {
            var sqlQuery = "EXEC [adv].[jsGetNextApprover] @advPayId";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<ApprovalIdsModel>(
                    sqlQuery,
                   new { advPayId } // Parameters for the stored procedure
                );
            }
        }
        /*public async Task<(bool IsSuccess, string Message)> ApproveAdvancePaymentAsync(ApproveAdvPayRequest request)
        {
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@flowId", request.flowId);
                    parameters.Add("@company", request.company);
                    parameters.Add("@userId", request.userId);
                    parameters.Add("@remarks", request.remarks ?? "");

                    var result = await connection.QueryFirstOrDefaultAsync<string>(
                        "[adv].[jsApproveAdvPay]",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    // Get last approved stage priority
                    var flowDetails = await GetExpenseApprovalFlowAsync(request.flowId);
                    var stagesList = flowDetails.OrderBy(s => s.Priority).ToList();

                    int lastApprovedStage = 0;

                    // Find last approved stage
                    foreach (var stage in stagesList)
                    {
                        if (stage.ActionStatus == "A") // Approved
                        {
                            lastApprovedStage = stage.Priority;
                        }
                    }

                    var createdbyId = await GetAdvCreatedBy(request.flowId);
                    if (createdbyId.userId != null && createdbyId.userId>0)
                    {
                        
                            var fcmToken = await _notificationService.GetUserTokenAsync(createdbyId.userId);
                            if (string.IsNullOrEmpty(fcmToken))
                                continue;

                            string notificationTitle = "You Request go to next stage ";
                            string notificationBody = $"New Advance Payment Approval request: Request Id {request.flowId} is Approved by {lastApprovedStage} Stage.";

                            var data = new Dictionary<string, string>
                    {
                        { "screen", "details" },
                        { "company", request.company.ToString() },
                        { "requestId", request.flowId.ToString() }
                    };

                            await _notificationService.SendPushNotificationAsync(
                                notificationTitle,
                                notificationBody,
                                fcmToken,
                                data
                            );

                            await _notificationService.InsertNotificationAsync(new InsertNotificationModel
                            {
                                userId = createdbyId.userId,
                                title = "Pending request",
                                message = notificationBody,
                                pageId = 6,
                                data = $"Request Id: {request.flowId}"
                            });
                        
                    }
                    // Fetch approver user IDs (should return multiple ApprovalIdsModel)
                    var approvalUserModels = await GetApprovalUserIdsAsync(request.flowId);

                    if (approvalUserModels != null && approvalUserModels.Any())
                    {
                        // Deduplicate and filter valid user IDs
                        var uniqueUserIds = new HashSet<int>(
                            approvalUserModels
                                .Where(m => m.userId > 0)
                                .Select(m => m.userId)
                        );

                        // Notify each user
                        foreach (var userId in uniqueUserIds)
                        {
                            var fcmToken = await _notificationService.GetUserTokenAsync(userId);
                            if (string.IsNullOrEmpty(fcmToken))
                                continue;

                            string notificationTitle = "You have received a new request from JSAP";
                            string notificationBody = $"New Advance Payment Approval request: Request Id {request.flowId}.";

                            var data = new Dictionary<string, string>
                    {
                        { "screen", "details" },
                        { "company", request.company.ToString() },
                        { "requestId", request.flowId.ToString() }
                    };

                            await _notificationService.SendPushNotificationAsync(
                                notificationTitle,
                                notificationBody,
                                fcmToken,
                                data
                            );

                            await _notificationService.InsertNotificationAsync(new InsertNotificationModel
                            {
                                userId = userId,
                                title = "Pending request",
                                message = notificationBody,
                                pageId = 6,
                                data = $"Request Id: {request.flowId}"
                            });
                        }
                    }


                    return (true, result ?? "Approval completed and notifications sent.");
                }
            }
            catch (SqlException ex)
            {
                return (false, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error: {ex.Message}");
            }
        }
*/
        public async Task<(bool IsSuccess, string Message)> ApproveAdvancePaymentAsync(ApproveAdvPayRequest request)
        {
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@flowId", request.flowId);
                    parameters.Add("@company", request.company);
                    parameters.Add("@userId", request.userId);
                    parameters.Add("@remarks", request.remarks ?? "");

                    var result = await connection.QueryFirstOrDefaultAsync<string>(
                        "[adv].[jsApproveAdvPay]",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    // Get approval flow details and determine last approved stage
                    var flowDetails = await GetExpenseApprovalFlowAsync(request.flowId);
                    var stagesList = flowDetails.OrderBy(s => s.Priority).ToList();

                    int lastApprovedStage = 0;

                    // HashSet to track users already notified
                    var notifiedUserIds = new HashSet<int>();

                    foreach (var stage in stagesList)
                    {
                        if (stage.ActionStatus == "A")
                        {
                            lastApprovedStage = stage.Priority;
                        }
                    }

                    // Notify creator only once, when all required approvals are done
                    var createdbyId = await GetAdvCreatedBy(request.flowId);
                    if (createdbyId.userId > 0)
                    {
                        var fcmToken = await _notificationService.GetUserTokenAsync(createdbyId.userId);

                        if (!string.IsNullOrEmpty(fcmToken))
                        {
                            string notificationTitle = "Good News! 🎉";
                            string notificationBody = $"Yay! Request #{request.flowId} has been approved at stage {lastApprovedStage}";

                            //string notificationBody = $"New Advance Payment Approval request: Request Id {request.flowId} is Approved by Stage {lastApprovedStage}.";

                            var data = new Dictionary<string, string>
                            {
                                { "screen", "details" },
                                { "company", request.company.ToString() },
                                { "requestId", request.flowId.ToString() }
                            };

                            await _notificationService.SendPushNotificationAsync(
                                notificationTitle,
                                notificationBody,
                                fcmToken,
                                data
                            );

                            await _notificationService.InsertNotificationAsync(new InsertNotificationModel
                            {
                                userId = createdbyId.userId,
                                title = "Request Update",
                                message = notificationBody,
                                pageId = 6,
                                data = $"Request Id: {request.flowId}"
                            });

                            notifiedUserIds.Add(createdbyId.userId);
                        }
                    }

                    // Notify approvers
                    var approvalUserModels = await GetApprovalUserIdsAsync(request.flowId);

                    if (approvalUserModels != null && approvalUserModels.Any())
                    {
                        foreach (var userId in approvalUserModels
                                 .Where(m => m.userId > 0)
                                 .Select(m => m.userId)
                                 .Distinct())
                        {
                            if (notifiedUserIds.Contains(userId))
                                continue;

                            var fcmToken = await _notificationService.GetUserTokenAsync(userId);
                            if (string.IsNullOrEmpty(fcmToken))
                                continue;

                            string notificationTitle = "Advance Payment Request 🔔";
                            string notificationBody = $"Request #{request.flowId} is ready, tap to open and approve.";


                            //string notificationBody = $"New Advance Payment Approval request: Request Id {request.flowId}.";

                            var data = new Dictionary<string, string>
                            {
                                { "screen", "details" },
                                { "company", request.company.ToString() },
                                { "requestId", request.flowId.ToString() }
                            };

                            await _notificationService.SendPushNotificationAsync(
                                notificationTitle,
                                notificationBody,
                                fcmToken,
                                data
                            );

                            await _notificationService.InsertNotificationAsync(new InsertNotificationModel
                            {
                                userId = userId,
                                title = "Pending request",
                                message = notificationBody,
                                pageId = 6,
                                data = $"Request Id: {request.flowId}"
                            });

                            notifiedUserIds.Add(userId);
                        }
                    }

                    return (true, result ?? "Approval completed and notifications sent.");
                }
            }
            catch (SqlException ex)
            {
                return (false, $"Database error: {ex.Message}");
            }
            catch (Exception ex)
            {
                return (false, $"Unexpected error: {ex.Message}");
            }
        }

        public async Task<(bool Success, string Message)> RejectAdvancePaymentAsync(RejectAdvPayRequest request)
        {
            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@flowid", request.FlowId);
                    parameters.Add("@company", request.Company);
                    parameters.Add("@userId", request.UserId);
                    parameters.Add("@remarks", request.Remarks);
                    parameters.Add("@action", request.Action);

                    var result = await connection.QueryFirstOrDefaultAsync<string>(
                        "[adv].[jsRejectAdvPay]",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    // Get approval flow details and determine last approved stage
                    var flowDetails = await GetExpenseApprovalFlowAsync(request.FlowId);
                    var stagesList = flowDetails.OrderBy(s => s.Priority).ToList();

                    int lastApprovedStage = 0;

                    // HashSet to track users already notified
                    var notifiedUserIds = new HashSet<int>();

                    foreach (var stage in stagesList)
                    {
                        if (stage.ActionStatus == "R")
                        {
                            lastApprovedStage = stage.Priority;
                        }
                    }

                    // Notify creator only once, when all required approvals are done
                    var createdbyId = await GetAdvCreatedBy(request.FlowId);
                    if (createdbyId.userId > 0)
                    {
                        var fcmToken = await _notificationService.GetUserTokenAsync(createdbyId.userId);

                        if (!string.IsNullOrEmpty(fcmToken))
                        {
                            string notificationTitle = "Sad News! Request Not Approved 😔";
                            string notificationBody = $"Advance Payment Request #{request.FlowId} was rejected at stage {lastApprovedStage}.";
                            var data = new Dictionary<string, string>
                            {
                                { "screen", "details" },
                                { "company", request.Company.ToString() },
                                { "requestId", request.FlowId.ToString() }
                            };

                            await _notificationService.SendPushNotificationAsync(
                                notificationTitle,
                                notificationBody,
                                fcmToken,
                                data
                            );

                            await _notificationService.InsertNotificationAsync(new InsertNotificationModel
                            {
                                userId = createdbyId.userId,
                                title = "Request Update",
                                message = notificationBody,
                                pageId = 6,
                                data = $"Request Id: {request.FlowId}"
                            });

                            notifiedUserIds.Add(createdbyId.userId);
                        }
                    }
                    return (true, result ?? "Rejected completed and notifications sent.");
                }
            }
            catch (SqlException ex)
            {
                // Log exception details here
                return (false, $"SQL Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                // Log exception details here
                return (false, $"An unexpected error occurred: {ex.Message}");
            }
        }
        public async Task<AdvanceResponse> DeleteVendorExpenseAsync(int id, int deletedBy)
        {
            var response = new AdvanceResponse();

            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (SqlCommand cmd = new SqlCommand("adv.jsDeleteVendorExpense", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@id", id);
                    cmd.Parameters.AddWithValue("@deletedBy", deletedBy);

                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (reader.HasRows)
                        {
                            await reader.ReadAsync();

                            if (reader.FieldCount == 1 && reader.GetName(0) == "Result")
                            {
                                response.Message = reader.GetString(0); // success message
                                response.Success = true;
                            }
                            else
                            {
                                // Error returned from CATCH block
                                response.Message = reader["ErrorMessage"].ToString();
                                response.Success = false;
                            }
                        }
                        else
                        {
                            response.Message = "No response returned from stored procedure.";
                            response.Success = false;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Message = $"Exception occurred: {ex.Message}";
                response.Success = false;
            }

            return response;
        }

        public async Task<AdvanceResponse> UpdateVendorExpenseAsync(VendorExpenseUpdateModel request)
        {
            var response = new AdvanceResponse();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@id", request.Id);
                    parameters.Add("@branch", request.Branch);
                    parameters.Add("@type", request.Type ?? (object)DBNull.Value);
                    parameters.Add("@search", request.Search ?? (object)DBNull.Value);
                    parameters.Add("@department", request.Department);
                    parameters.Add("@amount", request.Amount);
                    parameters.Add("@purpose", string.IsNullOrEmpty(request.Purpose) ? DBNull.Value : request.Purpose, DbType.String);
                    parameters.Add("@remark", request.Remark ?? (object)DBNull.Value);
                    parameters.Add("@priority", request.Priority ?? (object)DBNull.Value);
                    parameters.Add("@expectedPayDate", request.ExpectedPayDate ?? (object)DBNull.Value, DbType.Date);
                    parameters.Add("@expectedGrpoDate", request.ExpectedGrpoDate ?? (object)DBNull.Value, DbType.Date);
                    parameters.Add("@emiMonth", string.IsNullOrEmpty(request.EmiMonth) ? DBNull.Value : request.EmiMonth, DbType.String);
                    parameters.Add("@updatedBy", request.UpdatedBy);
                    parameters.Add("@status", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    // Execute the stored procedure
                    await connection.ExecuteAsync("adv.jsUpdateVendorExpense", parameters, commandType: CommandType.StoredProcedure);

                    // Get the output status code
                    int status = parameters.Get<int>("@status");

                    if (status == 1)
                    {
                        response.Message = "Expense updated successfully.";
                        response.Success = true;
                    }
                    else
                    {
                        response.Message = "Expense not found or already deleted.";
                        response.Success = false;
                    }
                }
            }
            catch (Exception ex)
            {
                response.Message = "Exception occurred: " + ex.Message;
                response.Success = false;
            }

            return response;
        }

        public async Task<IEnumerable<DepartmentsModel>> GetDepartmentsAsync()
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                var sqlQuery = "EXEC [adv].[jsGetDepartments]";
                return await connection.QueryAsync<DepartmentsModel>(sqlQuery);
            }
        }

        public async Task<IEnumerable<GetCustomerBalanceByBranchModel>> GetCustomerBalanceByBranchAsync(string IN_BRANCH, string IN_CARDCODE)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                var sqlQuery = "EXEC [adv].[GetCustomerBalanceByBranch] @IN_BRANCH, @IN_CARDCODE";
                return await connection.QueryAsync<GetCustomerBalanceByBranchModel>(sqlQuery, new { IN_BRANCH, IN_CARDCODE });
            }
        }

        public async Task<IEnumerable<ExpensesModels>> GetExpenseByUserIdAsync(int userId, int company,string month)
        {
            using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                var sqlQuery = "EXEC [adv].[jsGetExpenseByUserId] @userId, @company,@month";
                return await connection.QueryAsync<ExpensesModels>(sqlQuery, new { userId, company ,month});
            }
        }

        public async Task<(bool Success, string Message)> UpdateAmountAsync(int userId, int expenseId, float amount)
        {
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                using (SqlCommand cmd = new SqlCommand("[adv].[jsUpdateAmount]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@expenseId", expenseId);
                    cmd.Parameters.AddWithValue("@amount", amount);

                    try
                    {
                        await conn.OpenAsync();
                        await cmd.ExecuteNonQueryAsync();
                        return (true, "Amount updated successfully.");
                    }
                    catch (Exception ex)
                    {
                        return (false, $"Error: {ex.Message}");
                    }
                }
            }
        }

        public async Task<IEnumerable<StageDetailsModel>> GetExpenseApprovalFlowAsync(int flowId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sqlQuery = "EXEC [adv].[jsGetExpenseApprovalFlow] @flowId";
                return await connection.QueryAsync<StageDetailsModel>(sqlQuery, new { flowId });
            }
        }

        public async Task<AdvanceResponse> SendPaymentPendingCountNotificationAsync()
        {
            var responseMessage = new StringBuilder();
            bool overallSuccess = true;
            bool foundAnyPending = false;

            try
            {
                var activeUsers = await _userService.GetActiveUser();
                if (activeUsers == null || !activeUsers.Any())
                    return new AdvanceResponse { Success = false, Message = "No active users found." };

                // Track which users we've already sent notifications to
                HashSet<int> notifiedUsers = new HashSet<int>();

                foreach (var user in activeUsers)
                {
                    int userId = user.userId;

                    // Skip if we've already notified this user
                    if (notifiedUsers.Contains(userId))
                        continue;

                    int companyId = user.company;
                    // string month = DateTime.Now.ToString("MM-yyyy");

                    // fetch all counts for this single user
                    var counts = await GetExpenseInsightsAsync(userId, companyId);
                    if (counts == null || !counts.Any())
                    {
                        responseMessage.AppendLine($"No budget counts for user {userId}.");
                        continue;
                    }

                    // total up ALL pendings for this user
                    int totalPending = counts.Sum(c => c.pendingExpenses);
                    if (totalPending <= 0)
                        continue;   // nothing to send

                    foundAnyPending = true;

                    // get their FCM token once
                    string token = await _notificationService.GetUserTokenAsync(userId);
                    if (string.IsNullOrEmpty(token))
                    {
                        responseMessage.AppendLine($"No FCM token for user {userId}.");
                        overallSuccess = false;
                        continue;
                    }

                    string title = $"⏳ {totalPending} Request{(totalPending > 1 ? "s are" : " is")} waiting for you!";
                    string body = "Advance payment Requests need your attention.";


                    // build and send exactly one notification
                    // string title = $"You have {totalPending} " + (totalPending == 1 ? "Advance Payment pending request" : "Advance Payment pending requests");
                    //string body = "Please take action";
                    var data = new Dictionary<string, string>
                    {
                        { "userId",  userId.ToString() },
                        { "company", companyId.ToString() },
                        { "screen",  "pending" }
                    };

                    await _notificationService.SendPushNotificationAsync(
                        title, body, token, data
                    );

                    // Add to our tracking set after successful notification
                    notifiedUsers.Add(userId);
                    responseMessage.AppendLine($"Notification sent to user {userId}.");
                }

                if (!foundAnyPending)
                    return new AdvanceResponse
                    {
                        Success = true,
                        Message = "No pending requests for any active user."
                    };

                return new AdvanceResponse
                {
                    Success = overallSuccess,
                    Message = responseMessage.ToString().Trim()
                };
            }
            catch (Exception ex)
            {
                return new AdvanceResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
        public async Task<ExpenseApprovalFlowResult> GetApprovedStageAsync(int flowId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var sqlQuery = "EXEC [adv].[jsGetExpenseApprovalFlow] @flowId";
                var stages = await connection.QueryAsync<StageDetailsModel>(sqlQuery, new { flowId });

                var stagesList = stages.OrderBy(s => s.Priority).ToList();

                int lastApprovedStage = 0;
                int currentStage = 1; // Default to first stage
                string flowStatus = "Pending";

                // Find last approved stage and current stage
                for (int i = 0; i < stagesList.Count; i++)
                {
                    var stage = stagesList[i];

                    if (stage.ActionStatus == "A") // Approved
                    {
                        lastApprovedStage = stage.Priority;
                        // If this is not the last stage, next stage becomes current
                        if (i < stagesList.Count - 1)
                        {
                            currentStage = stagesList[i + 1].Priority;
                        }
                        else
                        {
                            // All stages approved
                            currentStage = stage.Priority;
                            flowStatus = "Approved";
                        }
                    }
                    else if (stage.ActionStatus == "R") // Rejected
                    {
                        currentStage = stage.Priority;
                        flowStatus = "Rejected";
                        break; // Stop processing if rejected
                    }
                    else if (stage.ActionStatus == null) // Pending
                    {
                        currentStage = stage.Priority;
                        break; // This is where it's currently pending
                    }
                }

                return new ExpenseApprovalFlowResult
                {
                    Stages = stagesList,
                    CurrentStage = currentStage,
                    LastApprovedStage = lastApprovedStage,
                    FlowStatus = flowStatus
                };
            }
        }

        public async Task<(int userId, string userName)> GetAdvCreatedBy(int advPayId)
        {
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                using (SqlCommand cmd = new SqlCommand("[adv].[jsGetAdvPayCreatedBy]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@advPayId", advPayId);

                    await conn.OpenAsync();

                    using (SqlDataReader reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            int userId = reader.GetInt32("UserId");
                            string userName = reader.GetString("UserName");

                            return (userId, userName);
                        }
                        else
                        {
                            // Handle case where no record is found
                            throw new InvalidOperationException($"No record found for advPayId: {advPayId}");
                            // Alternative: return (0, string.Empty) or (0, null) depending on your needs
                        }
                    }
                }
            }
        }

        public async Task<List<OilBusinessPartnerModel>> GetOilBusinessPartnersAsync(string type)
        {
            var session = await _bom2Service.GetSAPSessionOilAsync();
            var queryUrl = GetQueryUrl(1, type);

            var json = await CallSAPApiAsync(session, queryUrl);
            var result = JsonConvert.DeserializeObject<SAPResponseWrapper<OilBusinessPartnerModel>>(json);
            return result.Value;
        }

        public async Task<List<BevBusinessPartnerModel>> GetBevBusinessPartnersAsync(string type)
        {
            var session = await _bom2Service.GetSAPSessionBevAsync();
            var queryUrl = GetQueryUrl(2, type);

            var json = await CallSAPApiAsync(session, queryUrl);
            var result = JsonConvert.DeserializeObject<SAPResponseWrapper<BevBusinessPartnerModel>>(json);
            return result.Value;
        }

        private string GetQueryUrl(int company, string type)
        {
            return (company, type?.ToLower()) switch
            {
                (1, "vendor") => "BusinessPartners?$select=CardCode,CardName,CardType,CurrentAccountBalance,U_WG_CardCode,Series&$filter=CardType eq 'cSupplier' and Series eq 87&$orderby=CardCode",
                (2, "vendor") => "BusinessPartners?$select=CardCode,CardName,CardType,CurrentAccountBalance,U_OIL_CardCode,Series&$filter=CardType eq 'cSupplier' and Series eq 87&$orderby=CardCode",
                (1, "employee imprest") => "BusinessPartners?$select=CardCode,CardName,CardType,CurrentAccountBalance,U_WG_CardCode,Series&$filter=CardType eq 'cSupplier' and Series eq 88&$orderby=CardCode",
                (2, "employee imprest") => "BusinessPartners?$select=CardCode,CardName,CardType,CurrentAccountBalance,U_OIL_CardCode,Series&$filter=CardType eq 'cSupplier' and Series eq 88&$orderby=CardCode",
                _ => throw new ArgumentException("Invalid combination of company and type.")
            };
        }

        private async Task<string> CallSAPApiAsync(SAPSessionModel session, string url)
        {
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            using var client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://103.89.44.112:50000/b1s/v1/");

            // ✅ Add session cookies
            client.DefaultRequestHeaders.Add("Cookie", $"{session.B1Session}; {session.RouteId}");

            // ✅ Add B1S-PageSize header
            client.DefaultRequestHeaders.Add("B1S-PageSize", "100000");

            var response = await client.GetAsync(url);
            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                throw new Exception($"SAP call failed: {error}");
            }

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<List<PurchaseOrderModel>> GetOpenOrdersByVendorAsync(int company, string cardCode)
        {
            // Step 1: Get session
            var session = await _bom2Service.GetSAPSessionOilAsync(); // Or BevAsync based on logic

            // Step 2: Get list of DocNum to exclude
            var usedDocNums = await GetPoListAsync(company, cardCode);

            // Step 3: Build filter
            var filterParts = new List<string>
            {
                "DocumentStatus eq 'bost_Open'",
                $"CardCode eq '{cardCode}'"
            };

            foreach (var doc in usedDocNums)
            {
                if (int.TryParse(doc.Po?.Trim(), out var parsedDocNum))
                {
                    filterParts.Add($"DocNum ne {parsedDocNum}"); // ✅ no quotes for numeric
                }
            }


            string filter = string.Join(" and ", filterParts);

            // Step 4: Build query string
            string query = $"PurchaseOrders?$select=DocEntry,DocNum,CardCode,CardName,DocumentStatus,DocDate,DocTotal,DocumentLines" +
                           $"&$filter={Uri.EscapeDataString(filter)}&$orderby=DocEntry";

            // Step 5: Call SAP Service Layer
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            using var client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://103.89.44.112:50000/b1s/v1/");
            client.DefaultRequestHeaders.Add("Cookie", $"{session.B1Session}; {session.RouteId}");
            client.DefaultRequestHeaders.Add("B1S-PageSize", "10000");

            var response = await client.GetAsync(query);
            if (!response.IsSuccessStatusCode)
                throw new Exception(await response.Content.ReadAsStringAsync());

            var json = await response.Content.ReadAsStringAsync();
            var sapData = JsonConvert.DeserializeObject<SapPurchaseOrderResponse>(json);

            // Step 6: Select required fields from DocumentLines
            foreach (var po in sapData.Value)
            {
                po.DocumentLines = po.DocumentLines?.Select(line => new PurchaseOrderLineModel
                {
                    LineNum = line.LineNum,
                    ItemCode = line.ItemCode,
                    ItemDescription = line.ItemDescription,
                    Quantity = line.Quantity,
                    LineTotal = line.LineTotal,
                    OpenAmount = line.OpenAmount,
                    U_Remarks = line.U_Remarks,
                    RemainingOpenQuantity = line.RemainingOpenQuantity,  // ✅ New
                    MeasureUnit = line.MeasureUnit
                }).ToList();
            }

            return sapData.Value;
        }

        public async Task<List<PurchaseOrderModel>> GetCreatedOpenOrdersByVendorAsync(int company, string cardCode)
        {
            // Step 1: Get SAP session
            var session = await _bom2Service.GetSAPSessionOilAsync();

            // Step 2: Get list of used DocNums from your DB
            var usedDocNums = await GetPoListAsync(company, cardCode);

            // Step 3: Parse only valid int DocNums
            var validDocNums = usedDocNums
                .Select(d => d.Po?.Trim())
                .Where(po => int.TryParse(po, out _))
                .Select(int.Parse)
                .ToList();

            if (!validDocNums.Any())
                return new List<PurchaseOrderModel>(); // nothing to fetch from SAP

            // Step 4: Build filter to include only those DocNums
            var filterParts = new List<string>
    {
        $"CardCode eq '{cardCode}'",
        $"DocumentStatus eq 'bost_Open'"
    };

            // Build OR clause for DocNums: (DocNum eq x or DocNum eq y ...)
            var docNumFilters = validDocNums.Select(n => $"DocNum eq {n}");
            var docNumClause = $"({string.Join(" or ", docNumFilters)})";

            filterParts.Add(docNumClause);
            string filter = string.Join(" and ", filterParts);

            // Step 5: Build query string
            string query = $"PurchaseOrders?$select=DocEntry,DocNum,CardCode,CardName,DocumentStatus,DocDate,DocTotal,DocumentLines" +
                           $"&$filter={Uri.EscapeDataString(filter)}&$orderby=DocEntry";

            // Step 6: Call SAP Service Layer
            using var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            using var client = new HttpClient(handler);
            client.BaseAddress = new Uri("https://103.89.44.112:50000/b1s/v1/");
            client.DefaultRequestHeaders.Add("Cookie", $"{session.B1Session}; {session.RouteId}");
            client.DefaultRequestHeaders.Add("B1S-PageSize", "10000");

            var response = await client.GetAsync(query);
            if (!response.IsSuccessStatusCode)
                throw new Exception(await response.Content.ReadAsStringAsync());

            var json = await response.Content.ReadAsStringAsync();
            var sapData = JsonConvert.DeserializeObject<SapPurchaseOrderResponse>(json);

            // Step 7: Refine DocumentLines
            foreach (var po in sapData.Value)
            {
                po.DocumentLines = po.DocumentLines?.Select(line => new PurchaseOrderLineModel
                   {
                    LineNum = line.LineNum,
                    ItemCode = line.ItemCode,
                    ItemDescription = line.ItemDescription,
                    Quantity = line.Quantity,
                    LineTotal = line.LineTotal,
                    OpenAmount = line.OpenAmount,
                    U_Remarks = line.U_Remarks,
                    RemainingOpenQuantity = line.RemainingOpenQuantity,  // ✅ New
                    MeasureUnit = line.MeasureUnit
                }).ToList();
            }

            return sapData.Value;
        }
        public async Task<IEnumerable<PoListModel>> GetPoListAsync(int company, string code)
        {
            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@company", company);
            parameters.Add("@code", code);

            var result = await connection.QueryAsync<PoListModel>(
                "adv.jsGetPoList",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            return result;
        }
    }
}
