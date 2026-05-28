using Dapper;
using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Sap.Data.Hana;
using ServiceStack;
using System.Data;
using System.Net.Http.Headers;
using System.Text;

namespace JSAPNEW.Services.Implementation
{
    public class BomService : IBomService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly Dictionary<int, HanaCompanySettings> _hanaSettings;
        private readonly IWebHostEnvironment _hostingEnvironment;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly IBom2Service _bom2Service;
        private readonly string _sapBaseUrl;
        public BomService(IConfiguration configuration, Interfaces.ITokenService tokenService, IWebHostEnvironment hostingEnvironment, IBom2Service bom2Service, INotificationService notificationService, IUserService userService)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            var activeEnv = configuration["ActiveEnvironment"];  // "Test" or "Live"
            _hanaSettings = configuration.GetSection($"HanaSettings:{activeEnv}")
                                         .Get<Dictionary<int, HanaCompanySettings>>();
            _hostingEnvironment = hostingEnvironment;
            _notificationService = notificationService;
            _userService = userService;
            _bom2Service = bom2Service;
            _sapBaseUrl = configuration["SapServiceLayer:BaseUrl"]
                ?? throw new ArgumentNullException("SapServiceLayer:BaseUrl not found in configuration.");
        }
        public async Task<IEnumerable<WarehouseModel>> GetWarehouseAsync(int company)
        {
            var sqlQuery = "EXEC bom.jsGetWareHouse @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<WarehouseModel>(
                    sqlQuery,
                    new { company }
                );
            }
        }
        public async Task<IEnumerable<TypeModel>> GetBomTypeAsync()
        {
            var sqlQuery = "EXEC bom.jsGetBomType";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<TypeModel>(
                    sqlQuery
                );
            }
        }
        public async Task<IEnumerable<TypeModel>> GetChildTypeAsync()
        {
            var sqlQuery = "EXEC bom.jsGetChildType";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<TypeModel>(
                    sqlQuery
                );
            }
        }
        public async Task<IEnumerable<MaterialModel>> GetMaterialAsync(string parentCode, int company)
        {
            var sqlQuery = "EXEC bom.jsGetMaterial @parentCode ,  @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<MaterialModel>(
                    sqlQuery,
                    new { parentCode, company }
                );
            }
        }
        public async Task<IEnumerable<ItemModel>> GetFatherMaterialAsync(int company, string type)
        {
            var sqlQuery = "EXEC bom.jsGetFatherMaterial @company,@type";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<ItemModel>(
                  sqlQuery,
                  new { company, type }
                );
            }
        }
        public async Task<IEnumerable<ResourcesModel>> GetResourcesAsync(int company)
        {
            var sqlQuery = "EXEC bom.jsGetResources @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<ResourcesModel>(
                    sqlQuery,
                    new { company }
                );
            }
        }
        public async Task<IEnumerable<BomResponse>> AddBomComponentAsync(AddBomComponentModel request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@bomId", request.bomId);
                parameters.Add("@type", request.type);
                parameters.Add("@componentCode", request.componentCode);
                parameters.Add("@qty", request.qty);
                parameters.Add("@company", request.company);
                parameters.Add("@wareHouse", request.wareHouse);
                parameters.Add("@updatedBy", request.updatedBy);
                parameters.Add("@update", request.update);
                // Execute the stored procedure and get the result
                var result = await connection.QueryAsync<BomResponse>("bom.jsAddBomComponent", parameters, commandType: CommandType.StoredProcedure);

                return result; // This should return 1 on success
            }
        }
        public async Task<IEnumerable<BomResponse>> RemoveBomComponentAsync(RemoveBomComponentModel request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@bomComId", request.bomComId);
                parameters.Add("@updatedBy", request.updatedBy);
                parameters.Add("@update", request.update);
                // Execute the stored procedure and get the result
                var result = await connection.QueryAsync<BomResponse>("bom.jsRemoveBomComponent", parameters, commandType: CommandType.StoredProcedure);

                return result; // This should return 1 on success
            }
        }
        public async Task<IEnumerable<BomResponse>> CreateBomAsync(CreateBomModel request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@parentCode", request.parentCode);
                parameters.Add("@type", request.type);
                parameters.Add("@qty", request.qty);
                parameters.Add("@company", request.company);
                parameters.Add("@createdBy", request.createdBy);

                var results = await connection.QueryAsync<BomResponse>("bom.jsCreateBom", parameters, commandType: CommandType.StoredProcedure);

                return results; // This will return all messages from the procedure
            }
        }
        public async Task<IEnumerable<PendingBomModel>> PendingBOMsAsync(int userId, int companyId)
        {
            var sqlQuery = "EXEC bom.jsGetPendingBOMs @userId , @companyId";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<PendingBomModel>(
                    sqlQuery,
                    new { userId, companyId }
                );
            }
        }
        public async Task<IEnumerable<BomMaterialModel>> BomMaterialAsync(int bomId, int company)
        {
            var sqlQuery = "EXEC bom.jsGetDetailOfBomMaterial @bomId , @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<BomMaterialModel>(
                    sqlQuery,
                    new { bomId, company }
                );
            }
        }
        public async Task<IEnumerable<BomResourceModel>> BomResourceAsync(int bomId, int company)
        {
            var sqlQuery = "EXEC bom.jsGetDetailOfBomResource @bomId , @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<BomResourceModel>(
                    sqlQuery,
                    new { bomId, company }
                );
            }
        }
        public async Task<IEnumerable<BomModel>> GetApprovedBOMsAsync(int userId, int company)
        {
            var sqlQuery = "EXEC bom.jsGetApprovedBOMs @userId , @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<BomModel>(
                   sqlQuery, new { userId, company }
                );
            }
        }
        public async Task<IEnumerable<BomModel>> GetRejectedBOMsAsync(int userId, int company)
        {
            var sqlQuery = "EXEC bom.jsGetRejectedBOMs @userId , @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<BomModel>(
                    sqlQuery, new { userId, company }
                );
            }
        }
        /* public async Task<IEnumerable<BomResponse>> BomApproveAsync(int bomId, int userId, string description = null)
         {
             using (var connection = new SqlConnection(_connectionString))
             {
                 await connection.OpenAsync();

                 var parameters = new DynamicParameters();
                 parameters.Add("@bomId", bomId);
                 parameters.Add("@userId", userId);

                 if (!string.IsNullOrWhiteSpace(description))
                     parameters.Add("@description", description);
                 else
                     parameters.Add("@description", dbType: DbType.String, value: null);

                 var result = await connection.QueryAsync<BomResponse>(
                     "bom.jsBomApprove",
                     parameters,
                     commandType: CommandType.StoredProcedure
                 );

                 return result;
             }
         }*/

        public async Task<IEnumerable<BomResponse>> BomApproveAsync(int bomId, int userId, string description = null)
        {
            var resultList = new List<BomResponse>();

            try
            {
                var resultMessages = new List<string>();
                var allNotificationModels = new List<UserIdsForNotificationModel>();

                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync();

                    var parameters = new DynamicParameters();
                    parameters.Add("@bomId", bomId);
                    parameters.Add("@userId", userId);

                    if (!string.IsNullOrWhiteSpace(description))
                        parameters.Add("@description", description);
                    else
                        parameters.Add("@description", dbType: DbType.String, value: null);

                    var result = await connection.QueryAsync<BomResponse>(
                        "bom.jsBomApprove",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    resultList = result.ToList();
                }

                // -------------------------------
                // 🔔 NOTIFICATION LOGIC (Added)
                // -------------------------------

                // STEP 1: Get all userIds who should receive notification (same as Credit Limit logic)
                var notifications = await GetBomUserIdsSendNotificatiosAsync(bomId);
                if (notifications != null)
                    allNotificationModels.AddRange(notifications);

                // STEP 2: Deduplicate userId groups
                allNotificationModels = allNotificationModels
                        .Where(m => !string.IsNullOrWhiteSpace(m.userIdsToApprove))
                        .GroupBy(m => m.userIdsToApprove)
                        .Select(g => g.First())
                        .ToList();

                // STEP 3: Convert comma-separated userId list → distinct IDs
                var uniqueUserIds = new HashSet<int>(
                    allNotificationModels
                        .SelectMany(m => m.userIdsToApprove.Split(',', StringSplitOptions.RemoveEmptyEntries))
                        .Select(s => int.Parse(s.Trim()))
                );

                // STEP 4: Create BOM Notification Message
                string notificationTitle = "BOM";
                string notificationBody = $"A new BOM (BOM Id: {bomId}) is awaiting your approval.";

                var data = new Dictionary<string, string>
                {
                    { "screen", "BOM" },
                    { "bomId", bomId.ToString() }
                };

                // STEP 5: Track sent tokens to avoid duplicate FCM push
                var sentTokens = new HashSet<string>();

                foreach (var uid in uniqueUserIds)
                {
                    var fcmTokenList = await _notificationService.GetUserFcmTokenAsync(uid);
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

                    // Insert database notification once per user
                    await _notificationService.InsertNotificationAsync(new InsertNotificationModel
                    {
                        userId = uid,
                        title = "BOM",
                        message = notificationBody,
                        pageId = 6,             // YOU CAN CHANGE THIS PAGE ID IF REQUIRED
                        data = $"BOM ID: {bomId}",
                        BudgetId = bomId         // Not mandatory → but matches your pattern
                    });
                }

                // Return response
                return resultList;
            }
            catch (SqlException ex)
            {
                throw new Exception($"SQL Error: {ex.Message}");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error: {ex.Message}");
            }
        }

        public async Task<IEnumerable<UserIdsForNotificationModel>> GetBomUserIdsSendNotificatiosAsync(int bomId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@bomId", bomId);
                    var result = await conn.QueryAsync<UserIdsForNotificationModel>("[bom].[jsBomNotify]", parameters, commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error Executing : ", ex);
            }
        }
        public async Task<IEnumerable<BomResponse>> BomRejectAsync(int bomId, int userId, string description)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@bomId", bomId);
                parameters.Add("@userId", userId);
                parameters.Add("@description", description);

                var result = await connection.QueryAsync<BomResponse>("bom.jsBomReject", parameters, commandType: CommandType.StoredProcedure);

                return result;
            }
        }
        public async Task<IEnumerable<BomResponse>> CreateBomWithComponents(BomRequest request, List<IFormFile> files)
        {
            // Define the upload folder path within wwwroot
            string uploadFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "Uploads", "BOM");

            if (string.IsNullOrWhiteSpace(uploadFolderPath))
            {
                throw new Exception("Upload folder path is missing in configuration.");
            }

            // Ensure directory exists
            if (!Directory.Exists(uploadFolderPath))
            {
                Directory.CreateDirectory(uploadFolderPath);
                Console.WriteLine("✅ Directory created: " + uploadFolderPath);
            }

            List<BomFile> uploadedFiles = new List<BomFile>();
            if (files != null && files.Any())
            {
                foreach (var file in files)
                {
                    if (file.Length > 0)
                    {
                        // ✅ Generate unique filename
                        string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
                        string filePath = Path.Combine(uploadFolderPath, uniqueFileName);
                        string relativeFolderPath = Path.Combine("Uploads", "BOM").Replace("\\", "/");

                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        Console.WriteLine($"✅ File uploaded: {uniqueFileName}");

                        uploadedFiles.Add(new BomFile
                        {
                            Path = "/" + relativeFolderPath, // ✅ Relative path for DB
                            FileName = uniqueFileName,       // ✅ Saved name
                            FileExt = Path.GetExtension(file.FileName),
                            FileSize = (int)file.Length,
                            Description = "Uploaded file"
                        });
                    }
                }
            }

            // Attach files to the BOM request
            request.Files = uploadedFiles;
            Console.WriteLine("🔹 Step 6: Calling SaveBomToDatabase...");

            try
            {
                // STEP 1️⃣: Save BOM
                int newBomId = await SaveBomToDatabase(request);   // <-- Return ID here

                // STEP 2️⃣: Get users for notification (same as credit limit)
                var stageUsers = await GetBomCurrentUsersSendNotificationAsync(newBomId);
                // STEP 3️⃣: Send Notifications (NO CHANGE in functions)
                if (stageUsers != null && stageUsers.Any())
                {
                    var sentTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var notifiedUsers = new HashSet<int>();

                    foreach (var group in stageUsers.GroupBy(u => u.userId))
                    {
                        int userId = group.Key;
                        int company = request.Company;

                        // Avoid duplicate user
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

                        string title = "BOM";
                        string body = $"A new BOM (Bom Id: {newBomId}) created and waiting for approval.";

                        var data = new Dictionary<string, string>
                        {
                            { "userId", userId.ToString() },
                            { "company", company.ToString() },
                            { "bomId", newBomId.ToString() },
                            { "screen", "BOM" }
                        };

                        foreach (var token in uniqueTokens)
                        {
                            if (sentTokens.Contains(token))
                                continue;

                            await _notificationService.SendPushNotificationAsync(title, body, token, data);
                            sentTokens.Add(token);
                        }

                        // Insert Notification
                        await _notificationService.InsertNotificationAsync(new InsertNotificationModel
                        {
                            userId = userId,
                            title = title,
                            message = body,
                            pageId = 6,              // 🔥 Set your BOM page ID
                            data = $"BOM ID: {newBomId}",
                            BudgetId = newBomId
                        });

                        notifiedUsers.Add(userId);
                    }
                }

                // STEP 4️⃣: Return success
                return new List<BomResponse>
                {
                    new BomResponse { Message = "BOM created and notifications sent.", Success = true }
                };
            }
            catch (Exception ex)
            {
                return new List<BomResponse>
                {
                    new BomResponse { Message = $"Error: {ex.Message}", Success = false }
                };
            }
        }
        private async Task<int> SaveBomToDatabase(BomRequest request)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();

                    using (SqlCommand cmd = new SqlCommand("[bom].[jsCreateBomWithComponents]", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;

                        cmd.Parameters.AddWithValue("@parentCode", request.ParentCode);
                        cmd.Parameters.AddWithValue("@type", request.Type);
                        cmd.Parameters.AddWithValue("@qty", request.Qty);
                        cmd.Parameters.AddWithValue("@company", request.Company);
                        cmd.Parameters.AddWithValue("@createdBy", request.CreatedBy);
                        cmd.Parameters.AddWithValue("@wareHouseCode", request.WareHouseCode);

                        // COMPONENTS TVP
                        DataTable componentTable = new DataTable();
                        componentTable.Columns.Add("type", typeof(string));
                        componentTable.Columns.Add("componentCode", typeof(string));
                        componentTable.Columns.Add("qty", typeof(int));
                        componentTable.Columns.Add("uom", typeof(string));
                        componentTable.Columns.Add("Company", typeof(int));
                        componentTable.Columns.Add("wareHouse", typeof(string));

                        foreach (var c in request.Components)
                        {
                            componentTable.Rows.Add(c.Type, c.ComponentCode, c.Qty, c.Uom, c.Company, c.WareHouse);
                        }

                        var tvpComponents = cmd.Parameters.AddWithValue("@components", componentTable);
                        tvpComponents.SqlDbType = SqlDbType.Structured;

                        // FILES TVP
                        DataTable fileTable = new DataTable();
                        fileTable.Columns.Add("path", typeof(string));
                        fileTable.Columns.Add("fileName", typeof(string));
                        fileTable.Columns.Add("fileExt", typeof(string));
                        fileTable.Columns.Add("fileSize", typeof(int));
                        fileTable.Columns.Add("description", typeof(string));

                        if (request.Files != null)
                        {
                            foreach (var f in request.Files)
                            {
                                fileTable.Rows.Add(f.Path, f.FileName, f.FileExt, f.FileSize, f.Description);
                            }
                        }

                        var tvpFiles = cmd.Parameters.AddWithValue("@files", fileTable);
                        tvpFiles.SqlDbType = SqlDbType.Structured;

                        // EXECUTE AND GET RETURNED BOM ID
                        object result = await cmd.ExecuteScalarAsync();

                        return Convert.ToInt32(result);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Database Error: " + ex.Message);
                throw;
            }
        }

        public async Task<IEnumerable<TotalBomInsightsModel>> TotalBomInsightsAsync(int userId, int companyId)
        {
            var sqlQuery = "EXEC bom.jsGetBOMInsights @userId , @companyId";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<TotalBomInsightsModel>(
                    sqlQuery, new { userId, companyId }
                );
            }
        }
        public async Task<IEnumerable<FullHeaderDetailModel>> FullHeaderDetailModelAsync(int bomId, int company)
        {
            var sqlQuery = "EXEC bom.jsGetFullDetailOfBomHeader @bomId , @company";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<FullHeaderDetailModel>(
                    sqlQuery, new { bomId, company }
                );
            }
        }
        public async Task<int> UpdateBomHeaderAsync(int bomId, int qty, string wareHouse, int updatedBy)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@bomId", bomId);
                parameters.Add("@qty", qty);
                parameters.Add("@wareHouse", wareHouse);
                parameters.Add("@updatedBy", updatedBy);

                try
                {
                    // Execute the stored procedure and get the result (1 = success, 0 = failure)
                    var result = await connection.ExecuteScalarAsync<int>(
                        "bom.jsUpdateBomHeader",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    return result; // 1 for success, 0 for failure
                }
                catch (SqlException ex) // Catch SQL errors
                {
                    if (ex.Number == 50000) // Custom error from THROW
                    {
                        throw new Exception("Bom not exists");
                    }
                    throw; // Re-throw other SQL exceptions
                }
            }
        }
        public async Task<int> UpdateChildAsync(int bomComId, int qty, string wareHouse, int updatedBy)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@bomComId", bomComId);
                parameters.Add("@qty", qty);
                parameters.Add("@wareHouse", wareHouse);
                parameters.Add("@updatedBy", updatedBy);

                try
                {
                    // Execute the stored procedure and get the result (1 = success, 0 = failure)
                    var result = await connection.ExecuteScalarAsync<int>(
                        "bom.jsUpdateChild",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    return result; // 1 for success, 0 for failure
                }
                catch (SqlException ex) // Catch SQL errors
                {
                    if (ex.Number == 50000) // Custom error from THROW
                    {
                        throw new Exception("Bom Child not exists");
                    }
                    throw; // Re-throw other SQL exceptions
                }
            }
        }
        public async Task<IEnumerable<BomAllDataWithDetails>> GetAllBomWithDetailsAsync(int userId, int company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var pendingBoms = await connection.QueryAsync<BomAllDataWithDetails>(
                    "EXEC bom.jsGetPendingBOMs @userId, @company",
                    new { userId, company });

                var approvedBoms = await connection.QueryAsync<BomAllDataWithDetails>(
                    "EXEC bom.jsGetApprovedBOMs @userId , @company",
                    new { userId, company });

                var rejectedBoms = await connection.QueryAsync<BomAllDataWithDetails>(
                    "EXEC bom.jsGetRejectedBOMs @userId , @company",
                    new { userId, company });

                // Add status and filter out records with bomId == 0 or null
                var allBoms = new List<BomAllDataWithDetails>();

                foreach (var bom in pendingBoms)
                {
                    if (bom.bomId != null && bom.bomId != 0)
                    {
                        bom.Status = "Pending";
                        allBoms.Add(bom);
                    }
                }

                foreach (var bom in approvedBoms)
                {
                    if (bom.bomId != null && bom.bomId != 0)
                    {
                        bom.Status = "Approved";
                        allBoms.Add(bom);
                    }
                }

                foreach (var bom in rejectedBoms)
                {
                    if (bom.bomId != null && bom.bomId != 0)
                    {
                        bom.Status = "Rejected";
                        allBoms.Add(bom);
                    }
                }

                return allBoms;
            }
        }

        public async Task<IEnumerable<BomFileModel>> GetBomFilesDataAsync(int bomId, IUrlHelper urlHelper)
        {
            var sqlQuery = "EXEC [bom].[jsGetBomFiles] @bomId";

            using (var connection = new SqlConnection(_connectionString))
            {
                var files = (await connection.QueryAsync<BomFileModel>(sqlQuery, new { bomId })).ToList();

                foreach (var file in files)
                {
                    if (string.IsNullOrEmpty(file.path) || string.IsNullOrEmpty(file.fileName))
                        continue;

                    string cleanFilePath = file.path.Replace("\\", "/").Trim();
                    if (cleanFilePath.StartsWith("/"))
                        cleanFilePath = cleanFilePath.Substring(1);

                    string cleanExt = file.fileExt?.TrimStart('.') ?? "";
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.fileName);

                    file.DownloadUrl = urlHelper.Action("AdvanceDownloadFile", "File", new
                    {
                        filePath = cleanFilePath,
                        fileName = fileNameWithoutExt,
                        fileExt = cleanExt
                    }, protocol: "http");
                }

                return files;
            }

        }

        public async Task<IEnumerable<ApprovalFlowRequest>> GetBomApprovalFlowAsync(int bomId)
        {
            var sqlQuery = "EXEC [bom].[jsGetBomApprovalFlow] @bomId";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<ApprovalFlowRequest>(
                   sqlQuery, new { bomId }
                );
            }

        }
       /* public async Task<List<SapPendingInsertionModel>> GetPendingInsertionsAsync(int bomid)
        {
            var sql = "EXEC [bom].[jsGetPendingApiInsertions] @bomId";

            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@bomId", bomid);

                var result = await connection.QueryAsync<SapPendingInsertionModel>(sql, parameters);
                return result.ToList();
            }
        }*/

        public async Task<IEnumerable<CreatedByBomApprovalFlow>> GetCreatedByBomApprovalFlowAsync(int createdBy, int companyId)
        {
            var sqlQuery = "EXEC [bom].[jsGetCreatedByBomApprovalFlow] @createdBy , @companyId";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<CreatedByBomApprovalFlow>(
                    sqlQuery, new { createdBy, companyId }
                );
            }
        }

        public async Task<IEnumerable<BomByUserIdModel>> GetBomByUserIdAsync(int userId, int company,string month)
        {
            var sqlQuery = "EXEC [bom].[jsGetBomByUserId] @userId , @company,@month";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<BomByUserIdModel>(
                    sqlQuery, new { userId, company,month }
                );
            }
        }

        // Bom Updation
        public async Task<IEnumerable<GetBomUpdationDataModel>> FetchBomDetailsAsync(string code, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("code", code);

                var query = $"CALL \"{settings.Schema}\".\"sp_FetchUpdate_BOM_Details\"(?)";

                var result = await connection.QueryAsync<GetBomUpdationDataModel>(query, parameters);
                return result;
            }

        }

        /*public async Task<BomResponse> UpdateBomAsync(UpdateBomRequestModel model, List<IFormFile> files)
        {
            var response = new BomResponse();

            try
            {
                // 1. Save uploaded files
                var uploadedFiles = new List<UpdateBomFile>();
                if (files != null && files.Any())
                {
                    string uploadPath = Path.Combine(_hostingEnvironment.WebRootPath, "Uploads", "BOM");
                    if (!Directory.Exists(uploadPath))
                        Directory.CreateDirectory(uploadPath);

                    foreach (var file in files)
                    {
                        string ext = Path.GetExtension(file.FileName);
                        string newFileName = $"{Guid.NewGuid()}{ext}";
                        string savePath = Path.Combine(uploadPath, newFileName);

                        using var stream = new FileStream(savePath, FileMode.Create);
                        await file.CopyToAsync(stream);

                        uploadedFiles.Add(new UpdateBomFile
                        {
                            Path = "/Uploads/BOM",
                            FileName = newFileName,
                            FileExt = ext,
                            FileSize = (int)file.Length,
                            Description = "Uploaded via API"
                            //UploadedBy = model.CreatedBy
                        });
                    }
                }

                model.Files = uploadedFiles;

                // 2. Call stored procedure
                using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                using var cmd = new SqlCommand("[bom].[jsUpdateBom]", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@action", model.Action ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@parentCode", model.ParentCode ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@type", model.Type ?? (object)DBNull.Value);
                cmd.Parameters.AddWithValue("@qty", model.Qty);
                cmd.Parameters.AddWithValue("@company", model.Company);
                cmd.Parameters.AddWithValue("@createdBy", model.CreatedBy);
                cmd.Parameters.AddWithValue("@wareHouseCode", model.WareHouseCode ?? (object)DBNull.Value);

                cmd.Parameters.AddWithValue("@components", CreateComponentTable(model.Components ?? new()));
                cmd.Parameters.AddWithValue("@files", CreateFileTable(model.Files ?? new()));
                cmd.Parameters.AddWithValue("@historyHeader", CreateHistoryHeaderTable(model.HistoryHeader ?? new()));
                cmd.Parameters.AddWithValue("@historyComponents", CreateHistoryComponentTable(model.HistoryComponents ?? new()));
                cmd.Parameters.AddWithValue("@historyFiles", CreateHistoryFileTable(model.HistoryFiles ?? new()));

                await conn.OpenAsync();
                var result = await cmd.ExecuteScalarAsync();

                if (result != null && int.TryParse(result.ToString(), out int bomId))
                {
                    response.Success = true;
                    response.Message = $"BOM successfully {(model.Action == "create" ? "created" : "updated")}. BOM ID: {bomId}";
                }
                else
                {
                    response.Success = false;
                    response.Message = "BOM updated, but no BOM ID returned.";
                }
            }
            catch (SqlException sqlEx)
            {
                response.Success = false;
                response.Message = $"SQL Error: {sqlEx.Message}";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Unhandled Error: {ex.Message}";
            }

            return response;
        }*/


        public async Task<BomResponse> UpdateBomAsync(UpdateBomRequest request)
        {
            using var conn = new SqlConnection(_connectionString);
            using var cmd = new SqlCommand("[bom].[jsUpdateBom]", conn)
            {
                CommandType = CommandType.StoredProcedure
            };

            cmd.Parameters.AddWithValue("@action", request.Action);
            cmd.Parameters.AddWithValue("@parentCode", request.ParentCode);
            cmd.Parameters.AddWithValue("@type", request.Type);
            cmd.Parameters.AddWithValue("@qty", request.Qty);
            cmd.Parameters.AddWithValue("@company", request.Company);
            cmd.Parameters.AddWithValue("@createdBy", request.CreatedBy);
            cmd.Parameters.AddWithValue("@wareHouseCode", request.WareHouseCode);

            cmd.Parameters.AddWithValue("@components", CreateComponentTable(request.Components));

            // Optional: only send if files are present
            if (request.Files != null && request.Files.Any())
                cmd.Parameters.AddWithValue("@files", CreateFileTable(request.Files));

            // Optional: history header
            if (request.HistoryHeader != null && request.HistoryHeader.Any())
                cmd.Parameters.AddWithValue("@historyHeader", CreateHistoryHeaderTable(request.HistoryHeader));

            // Optional: history components
            if (request.HistoryComponents != null && request.HistoryComponents.Any())
                cmd.Parameters.AddWithValue("@historyComponents", CreateHistoryComponentTable(request.HistoryComponents));

            // Optional: history files
            if (request.HistoryFiles != null && request.HistoryFiles.Any())
                cmd.Parameters.AddWithValue("@historyFiles", CreateHistoryFileTable(request.HistoryFiles));

            await conn.OpenAsync();

            try
            {
                var result = await cmd.ExecuteScalarAsync();
                return new BomResponse
                {
                    Success = true,
                    Message = "BOM saved successfully. New BOM ID: " + result
                };
            }
            catch (Exception ex)
            {
                return new BomResponse
                {
                    Success = false,
                    Message = "SQL Error: " + ex.Message
                };
            }
        }

        private DataTable CreateComponentTable(List<UpdateBomComponent> components)
        {
            var dt = new DataTable();
            dt.Columns.Add("type", typeof(string));
            dt.Columns.Add("componentCode", typeof(string));
            dt.Columns.Add("qty", typeof(int));
            dt.Columns.Add("uom", typeof(string));
            dt.Columns.Add("company", typeof(int));
            dt.Columns.Add("wareHouse", typeof(string));

            foreach (var item in components ?? new List<UpdateBomComponent>())
                dt.Rows.Add(item.Type, item.ComponentCode, item.Qty, item.Uom, item.Company, item.WareHouse);

            return dt;
        }

        private DataTable CreateFileTable(List<UpdateBomFile> list)
        {
            var dt = new DataTable();
            dt.Columns.Add("path", typeof(string));
            dt.Columns.Add("fileName", typeof(string));
            dt.Columns.Add("fileExt", typeof(string));
            dt.Columns.Add("fileSize", typeof(long));
            dt.Columns.Add("description", typeof(string));

            foreach (var file in list ?? new List<UpdateBomFile>())
            {
                dt.Rows.Add(file.Path ?? "/Uploads/BOM/",
                            file.FileName,
                            file.FileExt,
                            file.FileSize,
                            file.Description ?? "Uploaded via API");
            }

            return dt;
        }

        private DataTable CreateHistoryHeaderTable(List<UpdateBomHistoryHeader> list)
        {
            var dt = new DataTable();
            dt.Columns.Add("bomId", typeof(int));
            dt.Columns.Add("parentCode", typeof(string));
            dt.Columns.Add("type", typeof(string));
            dt.Columns.Add("qty", typeof(int));
            dt.Columns.Add("company", typeof(int));
            dt.Columns.Add("version", typeof(int));
            dt.Columns.Add("createdBy", typeof(int));
            dt.Columns.Add("updatedBy", typeof(int));
            dt.Columns.Add("createdDate", typeof(DateTime));
            dt.Columns.Add("updatedDate", typeof(DateTime));
            dt.Columns.Add("wareHouse", typeof(string));

            foreach (var item in list ?? new List<UpdateBomHistoryHeader>())
            {
                dt.Rows.Add(item.BomId, item.ParentCode, item.Type, item.Qty, item.Company, item.Version,
                            item.CreatedBy, item.UpdatedBy, item.CreatedDate, item.UpdatedDate, item.WareHouse);
            }

            return dt;
        }

        private DataTable CreateHistoryComponentTable(List<UpdateBomHistoryComponent> list)
        {
            var dt = new DataTable();
            dt.Columns.Add("bomId", typeof(int));
            dt.Columns.Add("type", typeof(string));
            dt.Columns.Add("componentCode", typeof(string));
            dt.Columns.Add("qty", typeof(int));
            dt.Columns.Add("company", typeof(int));
            dt.Columns.Add("wareHouse", typeof(string));
            dt.Columns.Add("uom", typeof(string));

            foreach (var item in list ?? new List<UpdateBomHistoryComponent>())
            {
                dt.Rows.Add(item.BomId, item.Type, item.ComponentCode, item.Qty, item.Company, item.WareHouse, item.Uom);
            }

            return dt;
        }

        private DataTable CreateHistoryFileTable(List<UpdateBomHistoryFile> list)
        {
            var dt = new DataTable();
            dt.Columns.Add("bomId", typeof(int));
            dt.Columns.Add("path", typeof(string));
            dt.Columns.Add("fileName", typeof(string));
            dt.Columns.Add("fileExt", typeof(string));
            dt.Columns.Add("fileSize", typeof(int));
            dt.Columns.Add("description", typeof(string));
            dt.Columns.Add("createdOn", typeof(DateTime));
            dt.Columns.Add("version", typeof(int));
            dt.Columns.Add("company", typeof(int));
            dt.Columns.Add("uploadedBy", typeof(int));

            foreach (var item in list ?? new List<UpdateBomHistoryFile>())
            {
                dt.Rows.Add(item.BomId, item.Path, item.FileName, item.FileExt, item.FileSize, item.Description,
                            item.CreatedOn, item.Version, item.Company, item.UploadedBy);
            }

            return dt;
        }
        public async Task<IEnumerable<GetDistinctBom>> GetDistinctBomAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var query = $"CALL \"{settings.Schema}\".\"GETDISTINCTBOM\"()";

                var result = await connection.QueryAsync<GetDistinctBom>(query);
                return result;
            }

        }

        public async Task<OldBomPreviewResponseModel> GetOldBomPreviewAsync(int newBomId)
        {
            var response = new OldBomPreviewResponseModel();

            try
            {
                using (var connection = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (var command = new SqlCommand("[bom].[jsGetOldBomPreview]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@newBomId", newBomId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // 1. Header
                        if (await reader.ReadAsync())
                        {
                            response.Header = new OldBomHeaderModel
                            {
                                BomId = reader["BomId"] != DBNull.Value ? Convert.ToInt32(reader["BomId"]) : 0,
                                ParentCode = reader["ParentCode"]?.ToString(),
                                Type = reader["Type"]?.ToString(),
                                Qty = reader["Qty"] != DBNull.Value ? Convert.ToInt32(reader["Qty"]) : 0,
                                Version = reader["Version"] != DBNull.Value ? Convert.ToInt32(reader["Version"]) : 0,
                                CreatedBy = reader["CreatedBy"] != DBNull.Value ? Convert.ToInt32(reader["CreatedBy"]) : 0,
                                UpdatedBy = reader["UpdatedBy"] != DBNull.Value ? Convert.ToInt32(reader["UpdatedBy"]) : 0,
                                CreatedDate = reader["CreatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedDate"]) : DateTime.MinValue,
                                UpdatedDate = reader["UpdatedDate"] != DBNull.Value ? Convert.ToDateTime(reader["UpdatedDate"]) : (DateTime?)null,
                                WareHouse = reader["WareHouse"]?.ToString(),

                                ItemCode = reader["ItemCode"]?.ToString(),
                                ItemName = reader["ItemName"]?.ToString(),
                                ItemGroup = reader["ItemGroup"]?.ToString(),
                                ItemGroupCode = reader["ItemGroupCode"] != DBNull.Value ? Convert.ToInt32(reader["ItemGroupCode"]) : 0,
                                UOM = reader["UOM"]?.ToString(),
                                Company = reader["Company"] != DBNull.Value ? Convert.ToInt32(reader["Company"]) : 0,
                                SubGroup = reader["SubGroup"]?.ToString(),
                                Unit = reader["Unit"]?.ToString(),
                                IsLitre = reader["IsLitre"]?.ToString(),
                                SalFactor2 = reader["SalFactor2"] != DBNull.Value ? Convert.ToInt32(reader["SalFactor2"]) : 0
                            };
                        }

                        // 2. IT Components
                        if (await reader.NextResultAsync())
                        {
                            response.ItComponents = new List<OldITComponentModel>();
                            while (await reader.ReadAsync())
                            {
                                response.ItComponents.Add(new OldITComponentModel
                                {
                                    BomId = reader["BomId"] != DBNull.Value ? Convert.ToInt32(reader["BomId"]) : 0,
                                    Type = reader["Type"]?.ToString(),
                                    ComponentCode = reader["ComponentCode"]?.ToString(),
                                    Qty = reader["Qty"] != DBNull.Value ? Convert.ToInt32(reader["Qty"]) : 0,
                                    WareHouse = reader["WareHouse"]?.ToString(),

                                    ItemCode = reader["ItemCode"]?.ToString(),
                                    ItemName = reader["ItemName"]?.ToString(),
                                    ItemGroup = reader["ItemGroup"]?.ToString(),
                                    ItemGroupCode = reader["ItemGroupCode"] != DBNull.Value ? Convert.ToInt32(reader["ItemGroupCode"]) : 0,
                                    UOM = reader["UOM"]?.ToString(),
                                    Company = reader["Company"] != DBNull.Value ? Convert.ToInt32(reader["Company"]) : 0,
                                    SubGroup = reader["SubGroup"]?.ToString(),
                                    Unit = reader["Unit"]?.ToString(),
                                    IsLitre = reader["IsLitre"]?.ToString()
                                });
                            }
                        }

                        // 3. RS Components
                        if (await reader.NextResultAsync())
                        {
                            response.RsComponents = new List<OldRSComponentModel>();
                            while (await reader.ReadAsync())
                            {
                                response.RsComponents.Add(new OldRSComponentModel
                                {
                                    BomId = reader["BomId"] != DBNull.Value ? Convert.ToInt32(reader["BomId"]) : 0,
                                    Type = reader["Type"]?.ToString(),
                                    ComponentCode = reader["ComponentCode"]?.ToString(),
                                    Qty = reader["Qty"] != DBNull.Value ? Convert.ToInt32(reader["Qty"]) : 0,
                                    WareHouse = reader["WareHouse"]?.ToString(),

                                    ResCode = reader["ResCode"]?.ToString(),
                                    ResName = reader["ResName"]?.ToString(),
                                    Company = reader["Company"] != DBNull.Value ? Convert.ToInt32(reader["Company"]) : 0
                                });
                            }
                        }

                        // 4. Files
                        if (await reader.NextResultAsync())
                        {
                            response.Files = new List<OldBomFileModel>();
                            while (await reader.ReadAsync())
                            {
                                response.Files.Add(new OldBomFileModel
                                {
                                    BomId = reader["BomId"] != DBNull.Value ? Convert.ToInt32(reader["BomId"]) : 0,
                                    Path = reader["Path"]?.ToString(),
                                    FileName = reader["FileName"]?.ToString(),
                                    FileExt = reader["FileExt"]?.ToString(),
                                    FileSize = reader["FileSize"] != DBNull.Value ? Convert.ToInt64(reader["FileSize"]) : 0,
                                    Description = reader["Description"]?.ToString(),
                                    CreatedOn = reader["CreatedOn"] != DBNull.Value ? Convert.ToDateTime(reader["CreatedOn"]) : DateTime.MinValue,
                                    Version = reader["Version"] != DBNull.Value ? Convert.ToInt32(reader["Version"]) : 0,
                                    Company = reader["Company"] != DBNull.Value ? Convert.ToInt32(reader["Company"]) : 0,
                                    UploadedBy = reader["UploadedBy"] != DBNull.Value ? Convert.ToInt32(reader["UploadedBy"]) : 0
                                });
                            }
                        }

                        response.Success = true;
                        response.Message = "BOM data loaded successfully.";
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "An unexpected error occurred.";
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<List<SapPendingInsertionModel>> GetPendingInsertionsAsync(int bomid, string action)
        {
            var sql = "EXEC [bom].[jsGetPendingApiInsertions] @bomId, @action";

            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@bomId", bomid);
                parameters.Add("@action", action);

                var result = await connection.QueryAsync<SapPendingInsertionModel>(sql, parameters);
                return result.ToList();
            }
        }

        public async Task<List<SapBomSyncResult>> PatchProductTreesToSAPAsync(List<SapPendingInsertionModel> pendingInsertions)
        {
            var results = new List<SapBomSyncResult>();
            var bomGroups = pendingInsertions.GroupBy(b => b.bomId);

            foreach (var group in bomGroups)
            {
                var bomList = group.ToList();
                var first = bomList.First();
                int company = first.bomCompany;

                SAPSessionModel session;

                if (company == 1)
                    session = await _bom2Service.GetSAPSessionOilAsync();
                else if (company == 2)
                    session = await _bom2Service.GetSAPSessionBevAsync();
                else if (company == 3)
                    session = await _bom2Service.GetSAPSessionMartAsync();
                else
                {
                    string msg = $"Unsupported company: {company}";
                    results.Add(new SapBomSyncResult
                    {
                        BomId = first.bomId,
                        TreeCode = first.parentCode,
                        IsSuccess = false,
                        Message = msg
                    });
                    await UpdateBomStatusAsync(first.bomId, msg, false.ToString());
                    continue;
                }

                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                };

                using var client = new HttpClient(handler);
                client.BaseAddress = new Uri(_sapBaseUrl);
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("Cookie", $"{session.B1Session}; {session.RouteId}");
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Prepare PATCH body - only include fields that are updatable
                var updateTree = new
                {
                    ProductTreeLines = bomList.Select(c => new
                    {
                        ItemCode = c.componentCode,
                        ItemName = c.componentName,
                        Quantity = c.componentQty,
                        Warehouse = c.componentWareHouse,
                        //Currency = "INR",
                        ParentItem = c.parentCode,
                        ItemType = c.componentType == "IT" ? "pit_Item" : "pit_Resource",
                        //IssueMethod = "im_Manual"
                    }).ToList()
                };

                try
                {
                    var json = JsonConvert.SerializeObject(updateTree);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    // PATCH request to specific TreeCode
                    var patchUrl = $"ProductTrees('{first.parentCode}')";
                    var request = new HttpRequestMessage(new HttpMethod("PATCH"), patchUrl)
                    {
                        Content = content
                    };

                    var response = await client.SendAsync(request);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    string message;

                    if (response.IsSuccessStatusCode)
                    {
                        message = "Code - 204 : Updated Successfully";
                    }
                    else
                    {
                        message = ExtractSapErrorCodeAndMessage(responseBody);
                    }

                    results.Add(new SapBomSyncResult
                    {
                        BomId = first.bomId,
                        TreeCode = first.parentCode,
                        IsSuccess = response.IsSuccessStatusCode,
                        Message = message
                    });

                    await UpdateBomStatusAsync(first.bomId, message, response.IsSuccessStatusCode.ToString());
                }
                catch (Exception ex)
                {
                    string errMsg = $"-1000: {ex.Message}";
                    results.Add(new SapBomSyncResult
                    {
                        BomId = first.bomId,
                        TreeCode = first.parentCode,
                        IsSuccess = false,
                        Message = errMsg
                    });
                    await UpdateBomStatusAsync(first.bomId, errMsg, false.ToString());
                }
            }

            return results;
        }
        private string ExtractSapErrorCodeAndMessage(string responseJson)
        {
            try
            {
                var obj = JObject.Parse(responseJson);
                var code = obj["error"]?["code"]?.ToString();
                var value = obj["error"]?["message"]?["value"]?.ToString();

                return !string.IsNullOrEmpty(code) && !string.IsNullOrEmpty(value)
                    ? $"Code - {code}:Message- {value}"
                    : "-5000: Unknown SAP error structure";
            }
            catch
            {
                return "-5001: Failed to parse SAP error response";
            }
        }
        public async Task UpdateBomStatusAsync(int bomId, string apiMessage, string tag)
        {
            var sqlQuery = "EXEC [bom].[jsUpdateBomApiStatus] @bomId,@apiMessage,@tag";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(sqlQuery, new { bomId, apiMessage, tag });
            }
        }
        public async Task<IEnumerable<AfterCreatedRequestSendNotificationToUser>> GetBomCurrentUsersSendNotificationAsync(int bomId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@bomId", bomId);
                    var result = await conn.QueryAsync<AfterCreatedRequestSendNotificationToUser>("[bom].[GetUsersInCurrentStage]", parameters, commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error Executing : ", ex);
            }
        }

        public async Task<BomResponse> SendPendingBomCountNotificationAsync()
        {
            var responseMessage = new StringBuilder();
            bool overallSuccess = true;
            bool foundAnyPending = false;

            try
            {
                var activeUsers = await _userService.GetActiveUser();
                if (activeUsers == null || !activeUsers.Any())
                    return new BomResponse { Success = false, Message = "No active users found." };

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



                    var counts = await TotalBomInsightsAsync(userId, company);

                    if (counts == null || !counts.Any())
                    {
                        responseMessage.AppendLine($"No boms counts for user {userId}.");
                        continue;
                    }

                    // total up ALL pendings for this user
                    int totalPending = counts.Sum(c => c.TotalPendingBOMs);
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
                                   (totalPending == 1 ? "BOM pending request" : "BOM pending requests");
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
                    return new BomResponse
                    {
                        Success = true,
                        Message = "No pending requests for any active user."
                    };

                return new BomResponse
                {
                    Success = overallSuccess,
                    Message = responseMessage.ToString().Trim()
                };
            }
            catch (Exception ex)
            {
                return new BomResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }
    }
}
