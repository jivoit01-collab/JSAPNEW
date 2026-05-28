using Dapper;
using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Renci.SshNet;
using Sap.Data.Hana;
using ServiceStack;
using System.Data;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;

namespace JSAPNEW.Services.Implementation
{
    public class CreditLimitService : ICreditLimitService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly string _HanaLiveOilconnectionString;
        private readonly string _HanaLiveBevconnectionString;
        private readonly string _HanaLiveMartconnectionString;
        private readonly string _sapBaseUrl;
        private readonly IBom2Service _bom2Service;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;

        public CreditLimitService(IConfiguration configuration, IBom2Service bom2Service, INotificationService notificationService, IUserService userService)
        {
            _configuration = configuration;
            _HanaLiveOilconnectionString = _configuration.GetConnectionString("LiveHanaConnection");
            _HanaLiveBevconnectionString = _configuration.GetConnectionString("LiveBevHanaConnection");
            _HanaLiveMartconnectionString = _configuration.GetConnectionString("LiveMartHanaConnection");
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _bom2Service = bom2Service;
            _notificationService = notificationService;
            _userService = userService;
            _sapBaseUrl = configuration["SapServiceLayer:BaseUrl"]
                ?? throw new ArgumentNullException("SapServiceLayer:BaseUrl not found in configuration.");
        }
        private (string HanaConnectionString, string Schema) GetLiveHanaSettings(int company)
        {
            return company switch
            {
                1 => (_HanaLiveOilconnectionString, "JIVO_OIL_HANADB"),
                2 => (_HanaLiveBevconnectionString, "JIVO_BEVERAGES_HANADB"),
                3 => (_HanaLiveMartconnectionString, "JIVO_MART_HANADB"),
                _ => throw new ArgumentException("Invalid company ID (only 1, 2, and 3 are allowed).")
            };
        }
        private string MapBranchId(int branchId)
        {
            var branchMap = new Dictionary<int, string>
            {
                { 1, "OIL" },
                { 2, "BEVERAGES" },
                { 3, "MART" }
            };

            return branchMap.TryGetValue(branchId, out var branchName)
                ? branchName
                : branchId.ToString();
        }

        public async Task<string> GetCustomerNameByCodeAsync(int company, string customerCode)
        {
            var (hanaConnectionString, schema) = GetLiveHanaSettings(company);

            await using var connection = new HanaConnection(hanaConnectionString);
            await connection.OpenAsync();

            var sql = $"CALL \"{schema}\".\"GetCustomerCards\"()";

            var result = await connection.QueryAsync<GetCustomerCardModel>(
                sql,
                commandType: CommandType.Text
            );

            var customer = result.FirstOrDefault(x => x.CardCode == customerCode);
            return customer?.CardName ?? string.Empty;
        }

        public async Task<OpenCslmResponse> OpenCslmAsync(OpenCslmRequest request)
        {
            var response = new OpenCslmResponse();

            try
            {
                // If you already have this helper:
                var (hanaConnectionString, schema) = GetLiveHanaSettings(request.company);

                using (var connection = new HanaConnection(hanaConnectionString))
                {
                    await connection.OpenAsync();

                    // 6 IN params + 1 OUT param
                    var sql = $"CALL \"{schema}\".\"OPENCSLM\"(?,?,?,?,?,?,?)";

                    var p = new DynamicParameters();
                    p.Add("@CardCode", request.CardCode);
                    p.Add("@CurrentLimit", request.CurrentLimit);
                    p.Add("@NewLimit", request.NewLimit);
                    p.Add("@ValidTill", request.ValidTill);
                    p.Add("@createdBy", request.CreatedBy);
                    p.Add("@Balance", request.Balance);

                    // OUT parameter
                    p.Add("@result_id", dbType: DbType.Int32, direction: ParameterDirection.Output);

                    await connection.ExecuteAsync(sql, p, commandType: CommandType.Text);

                    response.Success = true;
                    response.ResultId = p.Get<int>("@result_id");
                    response.Message = "Credit limit record created successfully.";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;
        }
        public async Task<IEnumerable<GetCustomerCardModel>> GetCustomerCardsAsync(int company)
        {
            var (hanaConnectionString, schema) = GetLiveHanaSettings(company);

            await using var connection = new HanaConnection(hanaConnectionString);
            await connection.OpenAsync();

            var sql = $"CALL \"{schema}\".\"GetCustomerCards\"()";

            // Typed mapping
            var result = await connection.QueryAsync<GetCustomerCardModel>(
                sql,
                commandType: CommandType.Text
            );

            return result;
        }


        public async Task<CreateDocumentResult> CreateDocumentAsync(CreateDocumentDto request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@branchId", request.BranchId);
                parameters.Add("@customerCode", request.CustomerCode);
                parameters.Add("@customerValue", request.CustomerValue);
                parameters.Add("@currentBalance", request.CurrentBalance);
                parameters.Add("@currentCreditLimit", request.CurrentCreditLimit);
                parameters.Add("@newCreditLimit", request.NewCreditLimit);
                parameters.Add("@validTill", request.ValidTill);
                parameters.Add("@companyId", request.CompanyId);
                parameters.Add("@createdBy", request.CreatedBy);
                parameters.Add("@newDocumentId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                try
                {
                    // STEP 1️⃣: Create document
                    await connection.ExecuteAsync(
                        "[cl].[jsCreateDocument]",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    int? newId = parameters.Get<int?>("@newDocumentId");

                    if (newId == null || newId <= 0)
                    {
                        return new CreateDocumentResult
                        {
                            Success = false,
                            Message = "Document creation failed or missing newDocumentId."
                        };
                    }

                    // STEP 2️⃣: Use your existing function to get users in current stage
                    var stageUsers = await GetCurrentUsersSendNotificationAsync(newId.Value);

                    // STEP 3️⃣: Send notifications to each unique user
                    if (stageUsers != null && stageUsers.Any())
                    {
                        var sentTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        var notifiedUsers = new HashSet<int>();
                        var notificationLog = new StringBuilder();

                        // Group by user to avoid duplicates if SP returns multiple rows for same user
                        foreach (var userGroup in stageUsers.GroupBy(u => u.userId))
                        {
                            int userId = userGroup.Key;
                            int company = request.CompanyId; // assuming CompanyId exists

                            if (notifiedUsers.Contains(userId))
                                continue; // already handled this user

                            // Get unique FCM tokens for this user
                            var fcmTokens = await _notificationService.GetUserFcmTokenAsync(userId);
                            if (fcmTokens == null || fcmTokens.Count == 0)
                            {
                                notificationLog.AppendLine($"⚠️ No FCM token for user {userId}.");
                                continue;
                            }

                            // Deduplicate and clean tokens
                            var uniqueTokens = fcmTokens
                                .Select(t => t.fcmToken?.Trim())
                                .Where(t => !string.IsNullOrWhiteSpace(t))
                                .Distinct(StringComparer.OrdinalIgnoreCase)
                                .ToList();

                            string title = "Credit Limit Request";
                            string body = $"A new credit limit document (Doc Id: {newId}) is awaiting your approval.";

                            var data = new Dictionary<string, string>
                            {
                                { "userId", userId.ToString() },
                                { "company", company.ToString() },
                                { "DocId", newId.ToString() },
                                { "screen", "Credit Limit" }
                            };

                            int tokensSent = 0;
                            foreach (var token in uniqueTokens)
                            {
                                // Ensure one notification per unique token only
                                if (sentTokens.Contains(token))
                                    continue;

                                await _notificationService.SendPushNotificationAsync(title, body, token, data);
                                sentTokens.Add(token);
                                tokensSent++;
                            }

                            if (tokensSent > 0)
                            {
                                notificationLog.AppendLine($"✅ Sent to user {userId} ({tokensSent} device(s)).");
                                notifiedUsers.Add(userId);

                                // ✅ STEP 3.1: Insert notification in database for each user
                                await _notificationService.InsertNotificationAsync(new InsertNotificationModel
                                {
                                    userId = userId,
                                    title = title,
                                    message = body,
                                    pageId = 5, // your internal page ID for Credit Limit screen
                                    data = $"Document ID: {newId}",
                                    BudgetId = newId.Value // if this maps to same concept
                                });
                            }
                            else
                            {
                                notificationLog.AppendLine($"⚠️ No valid/unique tokens for user {userId}.");
                            }
                        }

                        // Optional: log details
                        Console.WriteLine(notificationLog.ToString());
                    }


                    // STEP 4️⃣: Return final response
                    return new CreateDocumentResult
                    {
                        Success = true,
                        Message = "Document created successfully and notifications sent.",
                        NewDocumentId = newId
                    };
                }
                catch (SqlException ex)
                {
                    return new CreateDocumentResult
                    {
                        Success = false,
                        Message = ex.Message,
                        NewDocumentId = null
                    };
                }
                catch (Exception ex)
                {
                    return new CreateDocumentResult
                    {
                        Success = false,
                        Message = $"Unexpected error: {ex.Message}",
                        NewDocumentId = null
                    };
                }
            }
        }

        public async Task<CreateDocumentResultV2> CreateDocumentWithAttachmentAsyncV2(CreateDocumentDtoV2 request, IFormFile attachment)
        {
            CreateDocumentResultV2 result;

            // 1️⃣ CREATE DOCUMENT
            result = await CreateDocumentAsyncV2(request);

            if (!result.Success)
                return result;

            // 2️⃣ SAVE ATTACHMENT
            if (attachment != null && attachment.Length > 0)
            {
                var uploadPath = Path.Combine(
                    Directory.GetCurrentDirectory(),
                    "wwwroot",
                    "Uploads",
                    "CreditLimit"
                );

                if (!Directory.Exists(uploadPath))
                    Directory.CreateDirectory(uploadPath);

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(attachment.FileName)}";
                var filePath = Path.Combine(uploadPath, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await attachment.CopyToAsync(stream);
                }
                string relativeFolderPath = Path.Combine("Uploads", "CreditLimit").Replace("\\", "/");
                // Save attachment info in DB
                await SaveAttachmentAsync(new CreditLimitAttachmentDto
                {
                    CreditDocumentId = result.CreditDocumentId,
                    FileName = fileName,
                    Extension = Path.GetExtension(attachment.FileName),
                    FilePath = "/" + relativeFolderPath,
                    UploadedBy = request.CreatedBy?.ToString()
                });
            }

            // 3️⃣ SEND NOTIFICATION (LAST STEP)
            await SendNotificationAsync(result.CreditDocumentId);

            return result;
        }
        private async Task<CreateDocumentResultV2> CreateDocumentAsyncV2(CreateDocumentDtoV2 request)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@branchId", request.BranchId);
            parameters.Add("@customerCode", request.CustomerCode);
            parameters.Add("@customerValue", request.CustomerValue);
            parameters.Add("@currentBalance", request.CurrentBalance);
            parameters.Add("@currentCreditLimit", request.CurrentCreditLimit);
            parameters.Add("@newCreditLimit", request.NewCreditLimit);
            parameters.Add("@validTill", request.ValidTill);
            parameters.Add("@companyId", request.CompanyId);
            parameters.Add("@createdBy", request.CreatedBy);
            parameters.Add("@newDocumentId", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "[cl].[jsCreateDocument]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            int newDocId = parameters.Get<int>("@newDocumentId");

            if (newDocId <= 0)
            {
                return new CreateDocumentResultV2
                {
                    Success = false,
                    Message = "Document creation failed"
                };
            }

            return new CreateDocumentResultV2
            {
                Success = true,
                Message = "Document created successfully",
                CreditDocumentId = newDocId
            };
        }
        private async Task SaveAttachmentAsync(CreditLimitAttachmentDto model)
        {
            using var connection = new SqlConnection(_connectionString);

            var parameters = new DynamicParameters();
            parameters.Add("@creditDocumentId", model.CreditDocumentId);
            parameters.Add("@fileName", model.FileName);
            parameters.Add("@fileExtension", model.Extension);
            parameters.Add("@filePath", model.FilePath);
            parameters.Add("@uploadedBy", model.UploadedBy);
            parameters.Add("@attachmentId", dbType: DbType.Int64, direction: ParameterDirection.Output);

            await connection.ExecuteAsync(
                "[cl].[jsInsertCreditDocumentAttachment]",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            long attachmentId = parameters.Get<long>("@attachmentId");

            if (attachmentId <= 0)
                throw new Exception("Attachment insert failed");
        }
        private async Task SendNotificationAsync(int creditDocumentId)
        {
            // 1️⃣ Get users for current workflow stage
            var stageUsers = await GetCurrentUsersSendNotificationAsync(creditDocumentId);

            if (stageUsers == null || !stageUsers.Any())
                return;

            var sentTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var notifiedUsers = new HashSet<int>();

            foreach (var userGroup in stageUsers.GroupBy(u => u.userId))
            {
                int userId = userGroup.Key;

                if (notifiedUsers.Contains(userId))
                    continue;

                var fcmTokens = await _notificationService.GetUserFcmTokenAsync(userId);

                if (fcmTokens == null || fcmTokens.Count == 0)
                    continue;

                var uniqueTokens = fcmTokens
                    .Select(t => t.fcmToken?.Trim())
                    .Where(t => !string.IsNullOrWhiteSpace(t))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                string title = "Credit Limit Request";
                string body = $"Credit Limit document (ID: {creditDocumentId}) is pending for your approval.";

                var data = new Dictionary<string, string>
        {
            { "DocId", creditDocumentId.ToString() },
            { "screen", "Credit Limit" }
        };

                foreach (var token in uniqueTokens)
                {
                    if (sentTokens.Contains(token))
                        continue;

                    await _notificationService.SendPushNotificationAsync(
                        title,
                        body,
                        token,
                        data
                    );

                    sentTokens.Add(token);
                }

                // 2️⃣ Insert notification in DB
                await _notificationService.InsertNotificationAsync(new InsertNotificationModel
                {
                    userId = userId,
                    title = title,
                    message = body,
                    pageId = 5,
                    data = $"Document ID: {creditDocumentId}",
                    BudgetId = creditDocumentId
                });

                notifiedUsers.Add(userId);
            }
        }
        public async Task<IEnumerable<ApprovedDocumentDto>> GetApprovedDocumentsAsync(CLDocumentRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@userId", request.userId);
                parameters.Add("@companyId", request.companyId);
                parameters.Add("@month", request.month);

                var result = await connection.QueryAsync<ApprovedDocumentDto>(
                    "[cl].[jsGetApprovedDocuments]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                foreach (var doc in result)
                {
                    // overwrite int with mapped string
                    //doc.BranchId = MapBranchId(Convert.ToInt32(doc.BranchId));
                    if (!string.IsNullOrEmpty(doc.CustomerCode))
                    {
                        doc.CustomerName = await GetCustomerNameByCodeAsync(Convert.ToInt32(doc.BranchId), doc.CustomerCode);
                    }
                }
                return result;

            }
        }
        public async Task<IEnumerable<PendingDocumentDto>> GetPendingDocumentsAsync(CLDocumentRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@userId", request.userId);
                parameters.Add("@companyId", request.companyId);
                parameters.Add("@month", request.month);
                var result = await connection.QueryAsync<PendingDocumentDto>(
                    "[cl].[jsGetPendingDocuments]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                foreach (var doc in result)
                {
                    // overwrite int with mapped string
                    //doc.BranchId = MapBranchId(Convert.ToInt32(doc.BranchId));
                    if (!string.IsNullOrEmpty(doc.CustomerCode))
                    {
                        doc.CustomerName = await GetCustomerNameByCodeAsync(Convert.ToInt32(doc.BranchId), doc.CustomerCode);
                    }
                }
                return result;
            }
        }
        public async Task<IEnumerable<RejectedDocumentDto>> GetRejectedDocumentsAsync(CLDocumentRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@userId", request.userId);
                parameters.Add("@companyId", request.companyId);
                parameters.Add("@month", request.month);
                var result = await connection.QueryAsync<RejectedDocumentDto>(
                    "[cl].[jsGetRejectedDocuments]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                foreach (var doc in result)
                {
                    // overwrite int with mapped string
                    //doc.BranchId = MapBranchId(Convert.ToInt32(doc.BranchId));
                    if (!string.IsNullOrEmpty(doc.CustomerCode))
                    {
                        doc.CustomerName = await GetCustomerNameByCodeAsync(Convert.ToInt32(doc.BranchId), doc.CustomerCode);
                    }
                }
                return result;
            }
        }
        public async Task<IEnumerable<AllDocumentDto>> GetAllDocumentsAsync(CLDocumentRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@userId", request.userId);
                parameters.Add("@companyId", request.companyId);
                parameters.Add("@month", request.month);

                var Approvedresult = await connection.QueryAsync<AllDocumentDto>(
                    "[cl].[jsGetApprovedDocuments]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var Pendingresult = await connection.QueryAsync<AllDocumentDto>(
                    "[cl].[jsGetPendingDocuments]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var Rejectedresult = await connection.QueryAsync<AllDocumentDto>(
                    "[cl].[jsGetRejectedDocuments]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                var TotalResult = new List<AllDocumentDto>();

                foreach (var data in Pendingresult)
                {
                    data.Status = "Pending";
                    TotalResult.Add(data);
                }

                foreach (var data in Approvedresult)
                {
                    data.Status = "Approved";
                    TotalResult.Add(data);
                }

                foreach (var data in Rejectedresult)
                {
                    data.Status = "Rejected";
                    TotalResult.Add(data);
                }
                foreach (var doc in TotalResult)
                {
                    // overwrite int with mapped string
                    //doc.BranchId = MapBranchId(Convert.ToInt32(doc.BranchId));
                    if (!string.IsNullOrEmpty(doc.CustomerCode))
                    {
                        doc.CustomerName = await GetCustomerNameByCodeAsync(Convert.ToInt32(doc.BranchId), doc.CustomerCode);
                    }
                }
                return TotalResult;
            }
        }
        public async Task<IEnumerable<CreditDocumentInsightResponse>> GetCreditDocumentInsightAsync(CLDocumentRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@userId", request.userId);
                parameters.Add("@companyId", request.companyId);
                parameters.Add("@month", request.month);
                var result = await connection.QueryAsync<CreditDocumentInsightResponse>(
                    "[cl].[jsGetCreditDocumentInsight]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                return result;
            }
        }
        public async Task<IEnumerable<UserDocumentInsightsResponse>> GetUserDocumentInsightsAsync(UserDocumentInsightsRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@createdBy", request.createdBy);
                parameters.Add("@monthYear", request.monthYear);

                var result = await connection.QueryAsync<UserDocumentInsightsResponse>(
                    "[cl].[jsGetUserDocumentInsights]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                return result;
            }
        }
        public async Task<IEnumerable<DocumentDetailDto>> GetDocumentDetailAsync(int documentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@documentId", documentId);

                var result = await connection.QueryAsync<DocumentDetailDto>(
                    "[cl].[jsGetDocumentDetail]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                foreach (var doc in result)
                {
                    // overwrite int with mapped string
                    //doc.BranchId = MapBranchId(Convert.ToInt32(doc.BranchId));
                    if (!string.IsNullOrEmpty(doc.CustomerCode))
                    {
                        doc.CustomerName = await GetCustomerNameByCodeAsync(Convert.ToInt32(doc.BranchId), doc.CustomerCode);
                    }
                }
                return result;
            }
        }

        public async Task<CreditDocumentDetailModel> GetCreditDocumentDetailAsyncV2(int documentId, IUrlHelper urlHelper)
        {
            using var connection = new SqlConnection(_connectionString);

            using var multi = await connection.QueryMultipleAsync(
                "[cl].[jsGetDocumentDetail]",
                new { documentId },
                commandType: CommandType.StoredProcedure
            );

            // Result set 1 → Document
            var document = await multi.ReadFirstOrDefaultAsync<CreditDocumentDetailModel>();
            if (document == null)
                return null;

            // Result set 2 → Attachments
            var attachments = (await multi.ReadAsync<CreditDocumentAttachmentModel>()).ToList();

            foreach (var file in attachments)
            {
                if (string.IsNullOrEmpty(file.FilePath) || string.IsNullOrEmpty(file.FileName))
                    continue;

                string cleanFilePath = file.FilePath.Replace("\\", "/").Trim();
                if (cleanFilePath.StartsWith("/"))
                    cleanFilePath = cleanFilePath.Substring(1);

                string cleanExt = file.FileExtension?.TrimStart('.') ?? "";
                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName);
                // ✅ Same pattern as BOM
                file.DownloadUrl = urlHelper.Action("AdvanceDownloadFile", "File", new
                {
                    filePath = cleanFilePath,
                    fileName = fileNameWithoutExt,
                    fileExt = cleanExt
                },
                    protocol: "http"
                );
            }

            document.Attachments = attachments;
            return document;
        }


        public async Task<IEnumerable<CreditLimitApprovalFlowDto>> GetApprovalFlowAsync(long flowId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@flowId", flowId);
                var result = await connection.QueryAsync<CreditLimitApprovalFlowDto>(
                    "[cl].[jsGetCreditLimitApprovalFlow]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                return result;
            }
        }
        public async Task<IEnumerable<UserDocumentDto>> GetUserDocumentsAsync(UserDocumentRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@createdBy", request.createdBy);
                parameters.Add("@monthYear", request.monthYear);
                parameters.Add("@status", request.status);

                var result = await connection.QueryAsync<UserDocumentDto>(
                    "[cl].[jsGetUserDocumentsByCreatedByAndMonth]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                foreach (var doc in result)
                {
                    // overwrite int with mapped string
                    doc.BranchId = MapBranchId(Convert.ToInt32(doc.BranchId));
                }
                return result;
            }
        }
        /*public async Task<CreditLimitApiResponse> ApproveDocumentAsync(ApproveDocumentRequest request)
        {
            var response = new CreditLimitApiResponse();
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@flowId", request.FlowId);
                parameters.Add("@company", request.Company);
                parameters.Add("@userId", request.UserId);
                parameters.Add("@remarks", request.Remarks);

                try
                {
                    await connection.ExecuteAsync(
                        "[cl].[jsApproveDocument]",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );
                    response.Success = true;
                    response.Message = "Document approved successfully.";
                }
                catch (SqlException ex)
                {
                    response.Success = false;
                    response.Message = $"Error: {ex.Message}";
                }
            }
            return response;
        }*/

        public async Task<int?> GetClUserDocumentIdAsync(int flowId)
        {
            int? userDocumentId = null;

            using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (var cmd = new SqlCommand("[cl].[jsGetId]", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@flowId", flowId);

                await conn.OpenAsync();

                var result = await cmd.ExecuteScalarAsync();

                if (result != null && result != DBNull.Value)
                    userDocumentId = Convert.ToInt32(result);
            }

            return userDocumentId;
        }
        public async Task<CreditLimitApiResponse> ApproveDocumentAsync(ApproveDocumentRequest request)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    var resultMessages = new List<string>();
                    var allNotificationModels = new List<UserIdsForNotificationModel>();

                    using (SqlCommand cmd = new SqlCommand("[cl].[jsApproveDocument]", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@flowId", request.FlowId);
                        cmd.Parameters.AddWithValue("@company", request.Company);
                        cmd.Parameters.AddWithValue("@userId", request.UserId);
                        cmd.Parameters.AddWithValue("@remarks", string.IsNullOrEmpty(request.Remarks) ? " " : request.Remarks);

                        var result = await cmd.ExecuteScalarAsync();
                        resultMessages.Add(result?.ToString() ?? $"Approved Document of FlowId {request.FlowId}");


                    }
                    var notifications = await GetCLUserIdsSendNotificatiosAsync(request.FlowId);
                    if (notifications != null)
                        allNotificationModels.AddRange(notifications);

                    // ✅ FIX 1: Deduplicate notification models (same userId multiple times)
                    allNotificationModels = allNotificationModels
                        .Where(m => !string.IsNullOrWhiteSpace(m.userIdsToApprove))
                        .GroupBy(m => m.userIdsToApprove)
                        .Select(g => g.First())
                        .ToList();

                    // ✅ FIX 2: Get unique user IDs
                    var uniqueUserIds = new HashSet<int>(
                        allNotificationModels
                            .SelectMany(m => m.userIdsToApprove.Split(',', StringSplitOptions.RemoveEmptyEntries))
                            .Select(s => int.Parse(s.Trim()))
                    );

                    int? userDocumentId = await GetClUserDocumentIdAsync(request.FlowId);
                    int docId = userDocumentId ?? request.FlowId;

                    string notificationTitle = "Credit Limit Request";
                    string notificationBody = $"A new Credit Limit document (Doc Id: {docId}) is awaiting your approval.";

                    var data = new Dictionary<string, string>
                    {
                        { "screen", "Credit Limit" },
                        { "company", request.Company.ToString() },
                        { "DocId", request.FlowId.ToString() }
                    };

                    // ✅ Track sent tokens to avoid duplicates
                    var sentTokens = new HashSet<string>();

                    foreach (var userId in uniqueUserIds)
                    {
                        var fcmTokenList = await _notificationService.GetUserFcmTokenAsync(userId);
                        if (fcmTokenList == null || fcmTokenList.Count == 0)
                            continue;

                        foreach (var token in fcmTokenList)
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

                        // ✅ Insert database notification once per user
                        await _notificationService.InsertNotificationAsync(new InsertNotificationModel
                        {
                            userId = userId,
                            title = "Credit Limit Document",
                            message = notificationBody,
                            pageId = 6,
                            data = $"Flow ID: {request.FlowId}",
                            BudgetId = request.FlowId
                        });
                    }

                    return new CreditLimitApiResponse
                    {
                        Success = true,
                        Message = string.Join(" | ", resultMessages)
                    };

                }
            }
            catch (SqlException ex)
            {
                return new CreditLimitApiResponse { Success = false, Message = $"SQL Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new CreditLimitApiResponse { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<CreditLimitApiResponse> RejectDocumentAsync(RejectDocumentRequest request)
        {
            var response = new CreditLimitApiResponse();
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@flowId", request.FlowId);
                parameters.Add("@company", request.Company);
                parameters.Add("@userId", request.UserId);
                parameters.Add("@remarks", request.Remarks);

                try
                {
                    await connection.ExecuteAsync(
                        "[cl].[jsRejectDocument]",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );
                    response.Success = true;
                    response.Message = "Document rejected successfully.";
                }
                catch (SqlException ex)
                {
                    response.Success = false;
                    response.Message = $"Error: {ex.Message}";
                }
            }
            return response;
        }
        public async Task<IEnumerable<DocumentDetailDto>> GetDocumentDetailUsingFlowIdAsync(int flowId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@flowId", flowId);

                var result = await connection.QueryAsync<DocumentDetailDto>(
                    "[cl].[jsGetDocumentDetailUsingFlowId]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                foreach (var doc in result)
                {
                    // overwrite int with mapped string
                    //doc.BranchId = MapBranchId(Convert.ToInt32(doc.BranchId));
                    if (!string.IsNullOrEmpty(doc.CustomerCode))
                    {
                        doc.CustomerName = await GetCustomerNameByCodeAsync(Convert.ToInt32(doc.BranchId), doc.CustomerCode);
                    }
                }
                return result;
            }
        }
        public async Task<FlowStatusRequest> GetFlowStatusAsync(int flowId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@flowId", flowId);
                return await connection.QueryFirstOrDefaultAsync<FlowStatusRequest>(
                    "[cl].[jsGetFlowStatus]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
            }
        }
        public async Task<CreditLimitApiResponse> UpdateHanaStatusAsync(CreditLimitUpdateHanaStatus request)
        {
            var response = new CreditLimitApiResponse();
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@flowId", request.FlowId);
                parameters.Add("@status", request.Status);
                parameters.Add("@hanaStatusText", request.hanaStatusText);

                try
                {
                    await connection.ExecuteAsync(
                        "[cl].[updateHanaStatus]",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );
                    response.Success = true;
                    response.Message = "HANA status updated successfully.";
                }
                catch (SqlException ex)
                {
                    response.Success = false;
                    response.Message = $"Error: {ex.Message}";
                }
            }
            return response;
        }

        public async Task<string> UpdateCreditLimitAsync(int flowId)
        {
            string hanaStatusText = string.Empty;
            bool isSuccess = false;

            try
            {
                // 1. Get Flow Status
                var flowStatus = await GetFlowStatusAsync(flowId);
                if (flowStatus == null || flowStatus.status != "A")
                {
                    hanaStatusText = "Flow is not approved from final stage";
                    await UpdateHanaStatusAsync(new CreditLimitUpdateHanaStatus
                    {
                        FlowId = flowId,
                        Status = false,
                        hanaStatusText = hanaStatusText
                    });
                    return hanaStatusText;
                }

                // 2. Get Document Detail
                var docs = await GetDocumentDetailUsingFlowIdAsync(flowId);
                if (docs == null || !docs.Any())
                {
                    hanaStatusText = "Document detail not found.";
                    await UpdateHanaStatusAsync(new CreditLimitUpdateHanaStatus
                    {
                        FlowId = flowId,
                        Status = false,
                        hanaStatusText = hanaStatusText
                    });
                    return hanaStatusText;
                }

                var doc = docs.First();
                string branch = doc.BranchId;    
                string customerCode = doc.CustomerCode;
                double newCreditLimit = doc.NewCreditLimit;
                double currentCreditLimit = doc.CurrentCreditLimit;

                // 3. Choose SAP session
                SAPSessionModel sapSession;
                
                if (doc.BranchId == "1")
                {
                    sapSession = await _bom2Service.GetSAPSessionOilAsync();
                }
                else if (doc.BranchId == "2")
                {
                    sapSession = await _bom2Service.GetSAPSessionBevAsync();
                }
                else if (doc.BranchId == "3")
                {
                    sapSession = await _bom2Service.GetSAPSessionMartAsync();
                }
                else
                {
                    throw new Exception($"Unknown branch type: {doc.BranchId}");
                }


                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };

                using var client = new HttpClient(handler);
                client.BaseAddress = new Uri(_sapBaseUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Cookie", $"{sapSession.B1Session}; {sapSession.RouteId}");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // 4. Prepare PATCH payload
                var patchPayload = new
                {
                    MaxCommitment = newCreditLimit,
                    CreditLimit = newCreditLimit  // Changed from CreditLimit to CreditLine
                };

                // 5. PATCH to update BP
                var content = new StringContent(
                    JsonConvert.SerializeObject(patchPayload),
                    Encoding.UTF8,
                    "application/json"
                );

                var request = new HttpRequestMessage(new HttpMethod("PATCH"), $"BusinessPartners('{customerCode}')")
                {
                    Content = content
                };

                var hanaResponse = await client.SendAsync(request);

                if (!hanaResponse.IsSuccessStatusCode)
                {
                    string err = await hanaResponse.Content.ReadAsStringAsync();
                    hanaStatusText = $"HANA PATCH failed: {err}";

                    await UpdateHanaStatusAsync(new CreditLimitUpdateHanaStatus
                    {
                        FlowId = flowId,
                        Status = false,
                        hanaStatusText = hanaStatusText
                    });

                    return hanaStatusText;
                }

                // Success case
                isSuccess = true;
                hanaStatusText = $"Credit Limit updated successfully in HANA for Customer: {customerCode}, Branch: {branch}, New Credit Limit: {newCreditLimit}";

                // 6. Update SQL HanaStatus
                var sqlUpdate = await UpdateHanaStatusAsync(new CreditLimitUpdateHanaStatus
                {
                    FlowId = flowId,
                    Status = true,
                    hanaStatusText = hanaStatusText
                });

                return sqlUpdate.Success
                    ? hanaStatusText
                    : $"HANA updated but SQL status update failed: {sqlUpdate.Message}";
            }
            catch (Exception ex)
            {
                hanaStatusText = $"Exception occurred: {ex.Message}";

                await UpdateHanaStatusAsync(new CreditLimitUpdateHanaStatus
                {
                    FlowId = flowId,
                    Status = false,
                    hanaStatusText = hanaStatusText
                });

                return hanaStatusText;
            }
        }

        public async Task<IEnumerable<UserIdsForNotificationModel>> GetCLUserIdsSendNotificatiosAsync(int flowId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@userDocumentId", flowId);
                    var result = await conn.QueryAsync<UserIdsForNotificationModel>("[cl].[jsCreditLimitNotify]", parameters, commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error Executing : ", ex);
            }
        }

        public async Task<CreditLimitApiResponse> SendPendingCLCountNotificationAsync()
        {
            var responseMessage = new StringBuilder();
            bool overallSuccess = true;
            bool foundAnyPending = false;

            try
            {
                var activeUsers = await _userService.GetActiveUser();
                if (activeUsers == null || !activeUsers.Any())
                    return new CreditLimitApiResponse { Success = false, Message = "No active users found." };

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

                    var clRequest = new CLDocumentRequest
                    {
                        userId = userId,
                        companyId = company,
                        month = month
                    };

                    var counts = await GetCreditDocumentInsightAsync(clRequest);

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
                                   (totalPending == 1 ? "Credit Limit pending request" : "Credit Limit pending requests");
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
                    return new CreditLimitApiResponse
                    {
                        Success = true,
                        Message = "No pending requests for any active user."
                    };

                return new CreditLimitApiResponse
                {
                    Success = overallSuccess,
                    Message = responseMessage.ToString().Trim()
                };
            }
            catch (Exception ex)
            {
                return new CreditLimitApiResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<IEnumerable<AfterCreatedRequestSendNotificationToUser>> GetCurrentUsersSendNotificationAsync(int userDocumentId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@userDocumentId", userDocumentId);
                    var result = await conn.QueryAsync<AfterCreatedRequestSendNotificationToUser>("[cl].[GetUsersInCurrentStage]", parameters, commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error Executing : ", ex);
            }
        }
    }
}
