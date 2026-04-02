using Azure;
using Dapper;
using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sap.Data.Hana;
using ServiceStack;
using System.Data;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using static JSAPNEW.Models.ItemMasterModel;


namespace JSAPNEW.Services.Implementation
{
    public class ItemMasterService : IItemMasterService
    {
        private readonly IConfiguration _configuration;
        private readonly Dictionary<int, HanaCompanySettings> _hanaSettings;
        private readonly string _connectionString;
        private readonly string _HanaLiveOilconnectionString;
        private readonly string _HanaLiveBevconnectionString;
        private readonly string _HanaLiveMartconnectionString;
        private readonly IBom2Service _bom2Service;
        private readonly string _sapBaseUrl;



        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;

        public ItemMasterService(IConfiguration configuration, IBom2Service bom2Service, INotificationService notificationService, IUserService userService)
        {
            _configuration = configuration;
            var activeEnv = configuration["ActiveEnvironment"];  // "Test" or "Live"
            _hanaSettings = configuration.GetSection($"HanaSettings:{activeEnv}")
                                         .Get<Dictionary<int, HanaCompanySettings>>();
            _HanaLiveOilconnectionString = _configuration.GetConnectionString("LiveHanaConnection");
            _HanaLiveBevconnectionString = _configuration.GetConnectionString("LiveBevHanaConnection");
            _HanaLiveMartconnectionString = _configuration.GetConnectionString("LiveMartHanaConnection");
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _sapBaseUrl = configuration["SapServiceLayer:BaseUrl"]
                ?? throw new ArgumentNullException("SapServiceLayer:BaseUrl not found in configuration.");
            _bom2Service = bom2Service;

            _notificationService = notificationService;
            _userService = userService;
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
        public async Task<IEnumerable<GetVarietyModel>> GetVarietyAsync(string BRAND, int GroupCode, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("BRAND", BRAND);
                parameters.Add("GroupCode", GroupCode);
                var query = $"CALL \"{settings.Schema}\".\"JsGetVariety\"(?,?)";

                var result = await connection.QueryAsync<GetVarietyModel>(query, parameters);
                return result;
            }
        }

        public async Task<IEnumerable<TaxRateModel>> GetTaxRateAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var query = $"CALL \"{settings.Schema}\".\"JsGetTaxRate\"()";

                var result = await connection.QueryAsync<TaxRateModel>(query);
                return result;
            }
        }

        public async Task<IEnumerable<GetsubgroupModel>> GetSubGroupAsync(string BRAND, string VARIETY, int GroupCode, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("BRAND", BRAND);
                parameters.Add("VARIETY", VARIETY);
                parameters.Add("GroupCode", GroupCode);

                var query = $"CALL \"{settings.Schema}\".\"JsGetSubGroup\"(?,?,?)";

                var result = await connection.QueryAsync<GetsubgroupModel>(query, parameters);
                return result;

            }
        }


        public async Task<IEnumerable<RecieveSKUmodel>> GetSKUAsync(int GroupCode, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("GroupCode", GroupCode);

                var query = $"CALL \"{settings.Schema}\".\"JsGetSKU\"(?)";

                var result = await connection.QueryAsync<RecieveSKUmodel>(query, parameters);
                return result;

            }
        }

        public async Task<IEnumerable<HSNModel>> GetHSNAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var query = $"CALL \"{settings.Schema}\".\"JsGetHSN\"()";

                var result = await connection.QueryAsync<HSNModel>(query);
                return result;
            }
        }


        public async Task<IEnumerable<InventoryUOMModel>> GetInventoryUOMAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var query = $"CALL \"{settings.Schema}\".\"JsGetInvntryUom\"()";

                var result = await connection.QueryAsync<InventoryUOMModel>(
                     query
                 );

                return result;
            }
        }


        public async Task<IEnumerable<PackingTypeModel>> GetPackingTypeAsync(int GroupCode, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("GroupCode", GroupCode);

                var query = $"CALL \"{settings.Schema}\".\"JsGetPackingType\"(?)";

                var result = await connection.QueryAsync<PackingTypeModel>(
                     query, parameters
                 );

                return result;
            }
        }


        public async Task<IEnumerable<PackTypeModel>> GetPackTypeAsync(int GroupCode, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("GroupCode", GroupCode);

                var query = $"CALL \"{settings.Schema}\".\"JsGetPackType\"(?)";

                var result = await connection.QueryAsync<PackTypeModel>(
                     query, parameters
                 );

                return result;
            }
        }

        public async Task<IEnumerable<PurPackModel>> GetPurPackAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var query = $"CALL \"{settings.Schema}\".\"JsGetPurPackMsr\"()";

                var result = await connection.QueryAsync<PurPackModel>(
                     query
                 );

                return result;
            }
        }


        public async Task<IEnumerable<SalPackModel>> GetSalPackAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var query = $"CALL \"{settings.Schema}\".\"JsGetSalPackMsr\"()";

                var result = await connection.QueryAsync<SalPackModel>(
                     query
                 );

                return result;
            }
        }


        public async Task<IEnumerable<SalUnitModel>> GetSalUnitAsync(int GroupCode, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("GroupCode", GroupCode);

                var query = $"CALL \"{settings.Schema}\".\"JsGetSalUnitMsr\"(?)";

                var result = await connection.QueryAsync<SalUnitModel>(
                     query, parameters
                 );

                return result;
            }
        }

        public async Task<IEnumerable<UnitModel>> GetUnitAsync(int GroupCode, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("GroupCode", GroupCode);

                var query = $"CALL \"{settings.Schema}\".\"JsGetUnit\"(?)";

                var result = await connection.QueryAsync<UnitModel>(
                     query, parameters
                 );

                return result;
            }
        }
        public async Task<IEnumerable<GetFAModel>> GetFaAsync(int GroupCode, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("GroupCode", GroupCode);

                var query = $"CALL \"{settings.Schema}\".\"JsGetFaType\"(?)";

                var result = await connection.QueryAsync<GetFAModel>(
                     query, parameters
                 );

                return result;
            }
        }

        public async Task<IEnumerable<BuyUnitModel>> GetBuyUnitAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var query = $"CALL \"{settings.Schema}\".\"JsGetBuyUnitUom\"()";

                var result = await connection.QueryAsync<BuyUnitModel>(
                     query
                 );

                return result;
            }
        }

        public async Task<IEnumerable<GroupModel>> GetGroupAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var query = $"CALL \"{settings.Schema}\".\"JsGetGroupNameWithCode\"()";
                var result = await connection.QueryAsync<GroupModel>(
                     query
                 );
                return result;
            }
        }

        public async Task<IEnumerable<BrandModel>> GetBrandAsync(int GroupCode, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("GroupCode", GroupCode);

                var query = $"CALL \"{settings.Schema}\".\"JsGetBrand\"(?)";
                var result = await connection.QueryAsync<BrandModel>(
                     query, parameters
                 );
                return result;
            }
        }

        /* public async Task<ItemMasterModel> ApproveItemAsync(ApproveItemModel request)
         {
             var response = new ItemMasterModel();

             try
             {
                 using var connection = new SqlConnection(_connectionString);
                 await connection.OpenAsync();

                 var parameters = new DynamicParameters();
                 parameters.Add("@itemId", request.itemId);
                 parameters.Add("@company", request.company);
                 parameters.Add("@userId", request.userId);
                 parameters.Add("@remarks", request.remarks);

                 var spResult = await connection.QueryFirstOrDefaultAsync<string>(
                     "[imc].[jsApproveItem]",
                     parameters,
                     commandType: CommandType.StoredProcedure
                 );

                 response.Success = true;
                 response.Message = spResult ?? "No message returned from procedure.";
             }
             catch (Exception ex)
             {
                 response.Success = false;
                 response.Message = $"Error: {ex.Message}";
             }

             return response;
         }
 */

        public async Task<int?> GetItemUserDocumentIdAsync(int flowId)
        {
            int? initId = null;

            using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (var cmd = new SqlCommand("[imc].[jsGetId]", conn))
            {
                cmd.CommandType = System.Data.CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@flowId", flowId);

                await conn.OpenAsync();

                var result = await cmd.ExecuteScalarAsync();

                if (result != null && result != DBNull.Value)
                    initId = Convert.ToInt32(result);
            }

            return initId;
        }

        public async Task<ItemMasterModel> ApproveItemAsync(ApproveItemModel request)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    var resultMessages = new List<string>();
                    var allNotificationModels = new List<UserIdsForNotificationModel>();

                    // Structured status fields for clean Postman response
                    string approvalStatus = "Pending";
                    var sapStatuses  = new List<string>();
                    var martStatuses = new List<string>();

                    using (SqlCommand cmd = new SqlCommand("[imc].[jsApproveItem]", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@itemId", request.itemId);
                        cmd.Parameters.AddWithValue("@company", request.company);
                        cmd.Parameters.AddWithValue("@userId", request.userId);
                        cmd.Parameters.AddWithValue("@remarks", string.IsNullOrEmpty(request.remarks) ? " " : request.remarks);

                        var result = await cmd.ExecuteScalarAsync();
                        approvalStatus = "Done";
                        resultMessages.Add(result?.ToString() ?? $"Approved Document of FlowId {request.itemId}");

                        List<PendingItemApiInsertionsModel> pendingItems = null;

                        // Retry logic (important)
                        for (int i = 0; i < 3; i++)
                        {
                            pendingItems = await GetPendingItemApiInsertionsAsync(request.itemId);

                            if (pendingItems != null && pendingItems.Count > 0)
                                break;

                            await Task.Delay(1000); // wait 1 second
                        }

                        // Call API after data is available
                        if (pendingItems != null && pendingItems.Count > 0)
                        {
                            Console.WriteLine($"[INFO] Approval completed for FlowId: {request.itemId}");
                            Console.WriteLine($"[INFO] {pendingItems.Count} pending item(s) fetched for FlowId: {request.itemId}");

                            // PostItemsToSAPAsync now handles both SAP and MART internally
                            var apiResults = await PostItemsToSAPAsync(pendingItems);
                            resultMessages.Add("API Triggered after final approval");

                            // Collect SAP and MART statuses from results
                            foreach (var r in apiResults)
                            {
                                sapStatuses.Add($"[InitId:{r.ItemId}] {(r.IsSuccess ? "Success" : $"Failed: {r.Message}")}");
                                if (!string.IsNullOrEmpty(r.MartStatus))
                                    martStatuses.Add($"[InitId:{r.ItemId}] {r.MartStatus}");
                            }
                        }
                        var notifications = await GetItemUserIdsSendNotificatiosAsync(request.itemId);
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

                        //int? initId = await GetItemUserDocumentIdAsync(request.itemId);
                        // int docId = initId ?? request.itemId;
                        int docId = request.itemId;

                        string notificationTitle = "Item Master Request";
                        string notificationBody = $"A new Item (Item Id: {docId}) is awaiting your approval.";

                        var data = new Dictionary<string, string>
                     {
                         { "screen", "Item Master" },
                         { "company", request.company.ToString() },
                         { "ItemId", docId.ToString() }
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
                                title = "Item Master",
                                message = notificationBody,
                                pageId = 6,
                                data = $"Flow ID: {request.itemId}",
                                BudgetId = request.itemId
                            });
                        }

                        return new ItemMasterModel
                        {
                            Success       = true,
                            Message       = string.Join(" | ", resultMessages),
                            ApprovalStatus = approvalStatus,
                            SapStatus      = sapStatuses.Count  > 0 ? string.Join("; ", sapStatuses)  : "Skipped (intermediate approval stage)",
                            MartStatus     = martStatuses.Count > 0 ? string.Join("; ", martStatuses) : "Skipped (not FG item or intermediate stage)"
                        };

                    }
                }
            }
            catch (SqlException ex)
            {
                return new ItemMasterModel { Success = false, Message = $"SQL Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ItemMasterModel { Success = false, Message = $"Error: {ex.Message}" };
            }
        }



        public async Task<IEnumerable<ApprovedItemModel>> GetApprovedItemsAsync(int userId, int company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@company", company);
                var result = await connection.QueryAsync<ApprovedItemModel>(
                   "[imc].[jsGetApprovedItems]",
                   parameters,
                   commandType: CommandType.StoredProcedure
               );
                return result;
            }
        }
        public async Task<IEnumerable<ItemFullDetailModel>> GetFullItemDetailsAsync(int itemId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@itemId", itemId);

                var result = await connection.QueryAsync<ItemFullDetailModel>(
                   "[imc].[jsGetFullItemDetails]",
                   parameters,
                   commandType: CommandType.StoredProcedure
               );
                return result;
            }
        }

        public async Task<IEnumerable<PendingItemModel>> GetPendingItemsAsync(long userId, int company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@companyId", company);
                var result = await connection.QueryAsync<PendingItemModel>(
                   "[imc].[jsGetPendingItems]",
                   parameters,
                   commandType: CommandType.StoredProcedure
               );
                return result;
            }
        }

        public async Task<IEnumerable<RejectedItemModel>> GetRejectedItemsAsync(long userId, int company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@company", company);
                var result = await connection.QueryAsync<RejectedItemModel>(
                   "[imc].[jsGetRejectedItems]",
                   parameters,
                   commandType: CommandType.StoredProcedure
               );
                return result;
            }
        }

        public async Task<IEnumerable<WorkflowInsightModel>> GetWorkflowInsightsAsync(int userId, int companyId, string? month = null)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@companyId", companyId);
                parameters.Add("@month", string.IsNullOrWhiteSpace(month) ? null : month);
                var result = await connection.QueryAsync<WorkflowInsightModel>(
                   "[imc].[jsGetWorkflowInsights]",
                   parameters,
                   commandType: CommandType.StoredProcedure
               );
                return result;
            }
        }

        public async Task<ItemMasterModel> InsertInitDataAsync(InsertInitDataModel request)
        {
            var response = new ItemMasterModel();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("imc.jsInsertInitData", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@userId", request.UserId);
                cmd.Parameters.AddWithValue("@company", request.Company);
                cmd.Parameters.AddWithValue("@itemName", (object?)request.ItemName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@itemGroupCode", (object?)request.ItemGroupCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@itemGroupName", (object?)request.itemGroupName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@taxRate", (object?)request.TaxRate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@chapterId", (object?)request.ChapterId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@chapterName", (object?)request.ChapterName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@unit", (object?)request.Unit ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@brand", (object?)request.Brand ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@variety", (object?)request.Variety ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@subGroup", (object?)request.SubGroup ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@sku", (object?)request.Sku ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@isLitre", (object?)request.IsLitre ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@grossWeight", (object?)request.GrossWeight ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@mrp", (object?)request.Mrp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@packType", (object?)request.PackType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@packingType", (object?)request.PackingType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@faType", (object?)request.FaType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@uom", (object?)request.Uom ?? DBNull.Value);

                //cmd.Parameters.AddWithValue("@utype", (object?)request.Utype ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salesUom", (object?)request.SalesUom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@invUom", (object?)request.InvUom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purchaseUom", (object?)request.PurchaseUom ?? DBNull.Value);

                await conn.OpenAsync();

                var result = await cmd.ExecuteScalarAsync();

                if (result != null && int.TryParse(result.ToString(), out int newId))
                {
                    response.Success = true;
                    response.Message = $"Item inserted successfully with ID: {newId}";
                }
                else
                {
                    response.Success = false;
                    response.Message = "Insert failed. No ID returned.";
                }
            }
            catch (SqlException ex)
            {
                response.Success = false;
                response.Message = $"Database Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Unexpected Error: {ex.Message}";
            }

            return response;
        }
        public async Task<ItemMasterModel> InsertSAPDataAsync(InsertSAPDataModel request)
        {
            var response = new ItemMasterModel();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("imc.jsInsertSAPData", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@initId", request.InitId);
                cmd.Parameters.AddWithValue("@franName", (object?)request.FranName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@prchseItem", (object?)request.PrchseItem ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@invItem", (object?)request.InvItem ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@numInBuy", (object?)request.NumInBuy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salUnitMsr", (object?)request.SalUnitMsr ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@numInSale", (object?)request.NumInSale ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@evalSystem", (object?)request.EvalSystem ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@threeType", (object?)request.ThreeType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@manSerNum", (object?)request.ManSerNum ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salFactor1", (object?)request.SalFactor1 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salFactor2", (object?)request.SalFactor2 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salFactor3", (object?)request.SalFactor3 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salFactor4", (object?)request.SalFactor4 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purFactor1", (object?)request.PurFactor1 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purFactor2", (object?)request.PurFactor2 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purFactor3", (object?)request.PurFactor3 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purFactor4", (object?)request.PurFactor4 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purPackMsr", (object?)request.PurPackMsr ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purPackUn", (object?)request.PurPackUn ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salPackUn", (object?)request.SalPackUn ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@manBtchNum", (object?)request.ManBtchNum ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@genEntry", (object?)request.GenEntry ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@wtLiable", (object?)request.WtLiable ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@issueMethod", (object?)request.IssueMethod ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@mngMethod", (object?)request.MngMethod ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@invntoryUom", (object?)request.InvntoryUom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@series", (object?)request.Series ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@gstRelevant", (object?)request.GstRelevant ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@gstTaxCtg", (object?)request.GstTaxCtg ?? DBNull.Value);

                await conn.OpenAsync();

                var result = await cmd.ExecuteScalarAsync();
                if (result != null && long.TryParse(result.ToString(), out var newId))
                {
                    response.Success = true;
                    response.Message = $"SAP Item inserted successfully with ID: {newId}";
                }
                else
                {
                    response.Success = false;
                    response.Message = "Insert failed. No ID returned.";
                }
            }
            catch (SqlException ex)
            {
                response.Success = false;
                response.Message = $"Database Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Unexpected Error: {ex.Message}";
            }

            return response;
        }
        public async Task<ItemMasterModel> RejectItemAsync(RejectItemModel request)
        {
            var response = new ItemMasterModel();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@itemId", request.ItemId);
                parameters.Add("@company", request.Company);
                parameters.Add("@userId", request.UserId);

                var result = await connection.QueryFirstOrDefaultAsync<string>(
                    "[imc].[jsRejectItem]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                response.Success = true;
                response.Message = result ?? "No message returned from procedure.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;
        }
        public async Task<ItemMasterModel> UpdateInitDataAsync(UpdateInitDataModel request)
        {
            var response = new ItemMasterModel();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("imc.jsUpdateInitData", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@id", request.Id);
                cmd.Parameters.AddWithValue("@company", (object?)request.Company ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@itemName", (object?)request.ItemName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@itemGroupCode", (object?)request.ItemGroupCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@itemGroupName", (object?)request.itemGroupName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@taxRate", (object?)request.TaxRate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@chapterId", (object?)request.ChapterId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@chapterName", (object?)request.ChapterName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@unit", (object?)request.Unit ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@brand", (object?)request.Brand ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@variety", (object?)request.Variety ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@subGroup", (object?)request.SubGroup ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@sku", (object?)request.Sku ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@isLitre", (object?)request.IsLitre ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@grossWeight", (object?)request.GrossWeight ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@mrp", (object?)request.Mrp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@packType", (object?)request.PackType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@packingType", (object?)request.PackingType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@faType", (object?)request.FaType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@uom", (object?)request.Uom ?? DBNull.Value);
                // cmd.Parameters.AddWithValue("@utype",(object ?)request.Utype ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salesUom", (object?)request.SalesUom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@invUom", (object?)request.InvUom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purchaseUom", (object?)request.PurchaseUom ?? DBNull.Value);

                await conn.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    response.Success = true;
                    response.Message = reader["Message"].ToString();
                }
                else
                {
                    response.Success = false;
                    response.Message = "No rows were updated.";
                }
            }
            catch (SqlException ex)
            {
                response.Success = false;
                response.Message = $"Database Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Unexpected Error: {ex.Message}";
            }

            return response;
        }

        public async Task<ItemMasterModel> UpdateSAPDataAsync(UpdateSAPDataModel request)
        {
            var response = new ItemMasterModel();

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("imc.jsUpdateSAPData", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                cmd.Parameters.AddWithValue("@initId", request.InitId);
                cmd.Parameters.AddWithValue("@franName", (object?)request.FranName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@prchseItem", (object?)request.PrchseItem ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@invItem", (object?)request.InvItem ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@numInBuy", (object?)request.NumInBuy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salUnitMsr", (object?)request.SalUnitMsr ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@numInSale", (object?)request.NumInSale ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@evalSystem", (object?)request.EvalSystem ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@threeType", (object?)request.ThreeType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@manSerNum", (object?)request.ManSerNum ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salFactor1", (object?)request.SalFactor1 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salFactor2", (object?)request.SalFactor2 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salFactor3", (object?)request.SalFactor3 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salFactor4", (object?)request.SalFactor4 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purFactor1", (object?)request.PurFactor1 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purFactor2", (object?)request.PurFactor2 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purFactor3", (object?)request.PurFactor3 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purFactor4", (object?)request.PurFactor4 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purPackMsr", (object?)request.PurPackMsr ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purPackUn", (object?)request.PurPackUn ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salPackUn", (object?)request.SalPackUn ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@manBtchNum", (object?)request.ManBtchNum ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@genEntry", (object?)request.GenEntry ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@wtLiable", (object?)request.WtLiable ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@issueMethod", (object?)request.IssueMethod ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@mngMethod", (object?)request.MngMethod ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@invntoryUom", (object?)request.InvntoryUom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@series", (object?)request.Series ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@gstRelevant", (object?)request.GstRelevant ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@gstTaxCtg", (object?)request.GstTaxCtg ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@sellItem", (object?)request.SellItem ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@PrcrmntMtd", (object?)request.PrcrmntMtd ?? DBNull.Value);

                await conn.OpenAsync();
                using var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    response.Success = true;
                    response.Message = reader["Message"].ToString();
                }
                else
                {
                    response.Success = false;
                    response.Message = "No rows were updated.";
                }
            }
            catch (SqlException ex)
            {
                response.Success = false;
                response.Message = $"Database Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Unexpected Error: {ex.Message}";
            }

            return response;
        }

        public async Task<IEnumerable<MergedItemModel>> GetAllItemsAsync(int userId, int companyId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var pendingItems = await connection.QueryAsync<MergedItemModel>(
                    "EXEC [imc].[jsGetPendingItems] @userId, @companyId",
                    new { userId, companyId });

                var approvedItems = await connection.QueryAsync<MergedItemModel>(
                    "EXEC [imc].[jsGetApprovedItems] @userId , @companyId",
                    new { userId, companyId });

                var rejectedItems = await connection.QueryAsync<MergedItemModel>(
                    "EXEC [imc].[jsGetRejectedItems] @userId , @companyId",
                    new { userId, companyId });

                // Add status and filter out records with bomId == 0 or null
                var allItems = new List<MergedItemModel>();

                foreach (var Items in pendingItems)
                {
                    Items.Status = "Pending";
                    allItems.Add(Items);
                }

                foreach (var Items in approvedItems)
                {
                    Items.Status = "Approved";
                    allItems.Add(Items);
                }

                foreach (var Items in rejectedItems)
                {
                    Items.Status = "Rejected";
                    allItems.Add(Items);
                }

                return allItems;
            }
        }

        /*public async Task<ItemMasterModel> InsertFullItemDataAsync(InsertFullItemDataModel model)
        {
            var response = new ItemMasterModel();

            using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (var cmd = new SqlCommand("[imc].[jsInsertFullItemData]", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;

                // Add all parameters from model
                cmd.Parameters.AddWithValue("@userId", model.UserId);
                cmd.Parameters.AddWithValue("@company", model.Company);
                cmd.Parameters.AddWithValue("@itemName", (object?)model.ItemName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@itemGroupCode", (object?)model.ItemGroupCode ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@itemGroupName", (object?)model.itemGroupName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@taxRate", (object?)model.TaxRate ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@chapterId", (object?)model.ChapterId ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@chapterName", (object?)model.ChapterName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@unit", (object?)model.Unit ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@brand", (object?)model.Brand ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@variety", (object?)model.Variety ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@subGroup", (object?)model.SubGroup ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@sku", (object?)model.Sku ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@isLitre", (object?)model.IsLitre ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@Litre", (object?)model.Litre ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@grossWeight", (object?)model.GrossWeight ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@mrp", (object?)model.Mrp ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@packType", (object?)model.PackType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@packingType", (object?)model.PackingType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@faType", (object?)model.FaType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@uom", (object?)model.Uom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salesUom", (object?)model.SalesUom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@invUom", (object?)model.InvUom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purchaseUom", (object?)model.PurchaseUom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@boxSize", (object?)model.BoxSize ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UnitSize", (object?)model.UnitSize ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@UomGroup", (object?)model.UomGroup ?? DBNull.Value);



                // SAPData
                cmd.Parameters.AddWithValue("@franName", (object?)model.FranName ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@prchseItem", (object?)model.PrchseItem ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@invItem", (object?)model.InvItem ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@numInBuy", (object?)model.NumInBuy ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salUnitMsr", (object?)model.SalUnitMsr ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@numInSale", (object?)model.NumInSale ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@evalSystem", (object?)model.EvalSystem ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@threeType", (object?)model.ThreeType ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@manSerNum", (object?)model.ManSerNum ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salFactor1", (object?)model.SalFactor1 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salFactor2", (object?)model.SalFactor2 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salFactor3", (object?)model.SalFactor3 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salFactor4", (object?)model.SalFactor4 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purFactor1", (object?)model.PurFactor1 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purFactor2", (object?)model.PurFactor2 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purFactor3", (object?)model.PurFactor3 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purFactor4", (object?)model.PurFactor4 ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purPackMsr", (object?)model.PurPackMsr ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@purPackUn", (object?)model.PurPackUn ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@salPackUn", (object?)model.SalPackUn ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@manBtchNum", (object?)model.ManBtchNum ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@genEntry", (object?)model.GenEntry ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@wtLiable", (object?)model.WtLiable ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@issueMethod", (object?)model.IssueMethod ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@mngMethod", (object?)model.MngMethod ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@invntoryUom", (object?)model.InvntoryUom ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@series", (object?)model.Series ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@gstRelevant", (object?)model.GstRelevant ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@gstTaxCtg", (object?)model.GstTaxCtg ?? DBNull.Value);

                await conn.OpenAsync();
                var reader = await cmd.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    response.Success = true;
                    response.Message = reader["Message"].ToString();
                }
                else
                {
                    response.Success = false;
                    response.Message = "No data returned";
                }
            }

            return response;
        }
*/

        public async Task<ItemMasterModel> InsertFullItemDataAsync(InsertFullItemDataModel model)
        {
            var response = new ItemMasterModel();

            try
            {
                using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                using (var cmd = new SqlCommand("[imc].[jsInsertFullItemData]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Input Parameters
                    cmd.Parameters.AddWithValue("@userId", model.UserId);
                    cmd.Parameters.AddWithValue("@company", model.Company);
                    cmd.Parameters.AddWithValue("@itemName", (object?)model.ItemName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@itemGroupCode", (object?)model.ItemGroupCode ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@itemGroupName", (object?)model.itemGroupName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@taxRate", (object?)model.TaxRate ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@chapterId", (object?)model.ChapterId
                        ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@unit", (object?)model.Unit ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@brand", (object?)model.Brand ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@variety", (object?)model.Variety ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@subGroup", (object?)model.SubGroup ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@sku", (object?)model.Sku ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@isLitre", (object?)model.IsLitre ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@Litre", (object?)model.Litre ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@grossWeight", (object?)model.GrossWeight ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@mrp", (object?)model.Mrp ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@packType", (object?)model.PackType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@packingType", (object?)model.PackingType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@faType", (object?)model.FaType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@uom", (object?)model.Uom ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@salesUom", (object?)model.SalesUom ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@invUom", (object?)model.InvUom ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@purchaseUom", (object?)model.PurchaseUom ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@boxSize", (object?)model.BoxSize ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UnitSize", (object?)model.UnitSize ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@UomGroup", (object?)model.UomGroup ?? DBNull.Value);

                    // SAP Data
                    cmd.Parameters.AddWithValue("@franName", (object?)model.FranName ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@prchseItem", (object?)model.PrchseItem ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@invItem", (object?)model.InvItem ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@numInBuy", (object?)model.NumInBuy ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@salUnitMsr", (object?)model.SalUnitMsr ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@numInSale", (object?)model.NumInSale ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@evalSystem", (object?)model.EvalSystem ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@threeType", (object?)model.ThreeType ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@manSerNum", (object?)model.ManSerNum ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@salFactor1", (object?)model.SalFactor1 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@salFactor2", (object?)model.SalFactor2 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@salFactor3", (object?)model.SalFactor3 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@salFactor4", (object?)model.SalFactor4 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@purFactor1", (object?)model.PurFactor1 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@purFactor2", (object?)model.PurFactor2 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@purFactor3", (object?)model.PurFactor3 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@purFactor4", (object?)model.PurFactor4 ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@purPackMsr", (object?)model.PurPackMsr ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@purPackUn", (object?)model.PurPackUn ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@salPackUn", (object?)model.SalPackUn ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@manBtchNum", (object?)model.ManBtchNum ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@genEntry", (object?)model.GenEntry ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@wtLiable", (object?)model.WtLiable ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@issueMethod", (object?)model.IssueMethod ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@mngMethod", (object?)model.MngMethod ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@invntoryUom", (object?)model.InvntoryUom ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@series", (object?)model.Series ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@gstRelevant", (object?)model.GstRelevant ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@gstTaxCtg", (object?)model.GstTaxCtg ?? DBNull.Value);
                    //cmd.Parameters.AddWithValue("@utype", (object?)model.Utype ?? DBNull.Value);

                    await conn.OpenAsync();
                    var reader = await cmd.ExecuteReaderAsync();

                    int newRecordId = 0;

                    if (await reader.ReadAsync())
                    {
                        response.Success = true;
                        response.Message = reader["Message"]?.ToString() ?? "Item inserted successfully.";
                        newRecordId = reader["NewInitId"] != DBNull.Value ? Convert.ToInt32(reader["NewInitId"]) : 0;
                    }
                    else
                    {
                        response.Success = false;
                        response.Message = "No data returned from stored procedure.";
                    }

                    await reader.CloseAsync();

                    // ✅ Send notifications if record successfully created
                    if (response.Success && newRecordId > 0)
                    {
                        try
                        {
                            // ✅ STEP 1️⃣: Get user list for notification
                            var stageUsers = await GetItemCurrentUsersSendNotificationAsync(newRecordId);

                            // ✅ STEP 2️⃣: Send notifications (only once per token)
                            if (stageUsers != null && stageUsers.Any())
                            {
                                var sentTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                                var notifiedUsers = new HashSet<int>();
                                var notificationLog = new StringBuilder();

                                foreach (var userGroup in stageUsers.GroupBy(u => u.userId))
                                {
                                    int userId = userGroup.Key;
                                    if (notifiedUsers.Contains(userId))
                                        continue;

                                    // Get all FCM tokens for this user
                                    var fcmTokens = await _notificationService.GetUserFcmTokenAsync(userId);
                                    if (fcmTokens == null || fcmTokens.Count == 0)
                                    {
                                        notificationLog.AppendLine($"⚠️ No FCM token for user {userId}.");
                                        continue;
                                    }

                                    var uniqueTokens = fcmTokens
                                        .Select(t => t.fcmToken?.Trim())
                                        .Where(t => !string.IsNullOrWhiteSpace(t))
                                        .Distinct(StringComparer.OrdinalIgnoreCase)
                                        .ToList();

                                    string title = "Item Master Request";
                                    string body = $"A new Item (Item Id: {newRecordId}) is awaiting your approval.";

                                    var data = new Dictionary<string, string>
                                    {
                                        { "userId", userId.ToString() },
                                        { "ItemId", newRecordId.ToString() },
                                        { "screen", "Item Master" }
                                    };

                                    foreach (var token in uniqueTokens)
                                    {
                                        if (sentTokens.Contains(token))
                                            continue;

                                        await _notificationService.SendPushNotificationAsync(title, body, token, data);
                                        sentTokens.Add(token);
                                    }

                                    // ✅ Save notification in DB
                                    await _notificationService.InsertNotificationAsync(new InsertNotificationModel
                                    {
                                        userId = userId,
                                        title = "Item Master",
                                        message = body,
                                        pageId = 6,
                                        data = $"ID: {newRecordId}",
                                        BudgetId = newRecordId  // First budget ID
                                    });

                                    notificationLog.AppendLine($"✅ Sent to user {userId} ({uniqueTokens.Count} device(s)).");
                                    notifiedUsers.Add(userId);
                                }

                                // Optional: Log notification results
                                Console.WriteLine(notificationLog.ToString());
                            }
                        }
                        catch (Exception ex)
                        {
                            response.Message += $" | Notification failed: {ex.Message}";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error inserting item: {ex.Message}";
            }

            return response;
        }

        public async Task<IEnumerable<BuyUnitMsrModel>> GetBuyUnitMsrAsync(int GroupCode, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("GroupCode", GroupCode);

                var query = $"CALL \"{settings.Schema}\".\"JsGetBuyUnitMsr\"(?)";
                var result = await connection.QueryAsync<BuyUnitMsrModel>(
                     query, parameters
                 );
                return result;
            }
        }

        public async Task<IEnumerable<InventoryUOMModel>> GetInvUnitMsrAsync(int GroupCode, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("GroupCode", GroupCode);

                var query = $"CALL \"{settings.Schema}\".\"JsGetInvUnitMsr\"(?)";
                var result = await connection.QueryAsync<InventoryUOMModel>(
                     query, parameters
                 );
                return result;
            }
        }

        public async Task<IEnumerable<UOMgroupModel>> JsGetUOMGroupAsync(int GroupCode, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("GroupCode", GroupCode);
                var query = $"CALL \"{settings.Schema}\".\"JsGetUOMGroup\"(?)";
                var result = await connection.QueryAsync<UOMgroupModel>(query, parameters);
                return result;
            }
        }

        public async Task<IEnumerable<GetDistinctItemName>> GetDistinctItemNameAsync(int company)
        {

            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var query = $"CALL \"{settings.Schema}\".\"JSGETDISTINCTITEMNAMES\"()";
                var result = await connection.QueryAsync<GetDistinctItemName>(query);
                return result;
            }
        }
        public async Task<List<PendingItemApiInsertionsModel>> GetPendingItemApiInsertionsAsync(int itemId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var parameters = new DynamicParameters();
                parameters.Add("@itemId", itemId);
                var result = await connection.QueryAsync<PendingItemApiInsertionsModel>(
                    "[imc].[jsGetPendingItemApiInsertions]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                return result.ToList();
            }
        }

        public async Task<List<SapItemSyncResult>> PostItemsToSAPAsync(List<PendingItemApiInsertionsModel> ItemInsertions)
        {
            var results = new List<SapItemSyncResult>();
            var itemGroups = ItemInsertions.GroupBy(imc => imc.InitId);

            foreach (var group in itemGroups)
            {
                var itemList = group.ToList();
                var first = itemList.First();
                int company = first.Company;

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
                    await LogApiErrorAsync(new LogApiErrorRequest
                    {
                        ReferenceID = first.InitId,
                        ApiName = "SAP/Items",
                        ErrorMessage = msg,
                        ErrorCode = "UNSUPPORTED_COMPANY",
                        Payload = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            ItemId = first.InitId,
                            Company = company
                        }),
                        CreatedBy = first.UserId
                    });

                    results.Add(new SapItemSyncResult
                    {
                        ItemId = first.InitId,
                        IsSuccess = false,
                        Message = msg
                    });
                    // await UpdateItemApiStatusAsync(first.InitId, msg, false.ToString());

                    await UpdateItemApiStatusAsync(first.InitId, msg, "N")
                        ;

                    //await UpdateItemApiStatusAsync(first.InitId, msg, status);

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

                // Determine IssueMethod based on ItemGroupCode
                //string issueMethod = first.ItemGroupCode == "111" || first.ItemGroupCode == "114" ? "im_Backflush" : "im_Manual";
                string issueMethod = first.ItemGroupCode == "109" || first.ItemGroupCode == "111" || first.ItemGroupCode == "114" ? "B" : "M";

                // Determine CostAccountingMethod based on EvalSystem
                string costAccountingMethod = first.EvalSystem == "F" ? "bis_FIFO" : (first.EvalSystem == "B" ? "bis_SNB" : ""); // Default if neither F nor B
                string sapSalesItem = (first.SellItem ?? "").Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) ? "tYES" : "tNO";
                string sapPurchaseItem = (first.PrchseItem ?? "").Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) ? "tYES" : "tNO";
                string sapInventoryItem = (first.InvItem ?? "").Trim().Equals("Y", StringComparison.OrdinalIgnoreCase) ? "tYES" : "tNO";

                bool isSales = sapSalesItem == "tYES";
                bool isPurchase = sapPurchaseItem == "tYES";
                bool isInv = sapInventoryItem == "tYES";

                int uomGroupEntryForSap = first.UomGroup switch
                {
                    "Manual" => -1,
                    "MTS2LITRE" => 1,
                    "KG2LITRE" => 2,
                    "MTS2LITRE(OLIVE)" => 3,
                    _ => 0 // default if none match
                };

                //string uType = first.Utype;
                string uType = null;
                // Apply correct rule: uType only when IsLitre = Y
                if ((first.IsLitre ?? "").Trim().Equals("N", StringComparison.OrdinalIgnoreCase))
                {
                    var premiumItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                    {
                        "CANOLA", "OLIVE", "GROUNDNUT"
                    };

                    uType = premiumItems.Contains(first.Variety?.Trim() ?? "")
                        ? "PREMIUM"
                        : "COMMODITY";
                }
                else
                {
                    uType = "COMMODITY";
                }

                var tree = new ItemsTree
                {
                    ItemName = first.ItemName,
                    ItemsGroupCode = first.ItemGroupCode,
                    U_Rev_tax_Rate = first.TaxRate,
                    U_Tax_Rate = first.TaxRate,
                    PurchaseItem = sapPurchaseItem,
                    InventoryItem = sapInventoryItem,
                    SalesItem = sapSalesItem,
                    ChapterID = int.TryParse(first.ChapterId, out int chapterId) ? chapterId : 0,
                    U_Unit = first.Unit,
                    U_Brand = first.Brand,
                    U_Sub_Group = first.Variety,
                    U_Variety = first.SubGroup,
                    U_SKU = first.Sku,
                    U_IsLitre = first.IsLitre,
                    U_Gross_Weight = first.GrossWeight,
                    U_MRP = first.Mrp,
                    U_PACK_TYPE = first.PackType,
                    //U_FA_TYPE = first.FaType,
                    SalesUnit = isSales ? first.SalesUom : null,
                    SalesPackagingUnit = isSales ? first.SalesUom : null,
                    InventoryUOM = isInv ? first.InvUom : null,
                    PurchaseUnit = isPurchase ? first.PurchaseUom : null,
                    PurchasePackagingUnit = isPurchase ? first.PurchaseUom : null,
                    SalesQtyPerPackUnit = first.UnitSize,
                    SalesFactor2 = first.BoxSize,
                    UoMGroupEntry = uomGroupEntryForSap,
                    CostAccountingMethod = costAccountingMethod, // Use the dynamically set CostAccountingMethod
                    WTLiable = "tYES",
                    IssueMethod = issueMethod, // Use the dynamically set IssueMethod
                    ManageBatchNumbers = first.ManBtchNum,
                    ManageSerialNumbers = "tNO",
                    ForceSelectionOfSerialNumber = "tYES",
                    SRIAndBatchManageMethod = "bomm_OnEveryTransaction",
                    Series = first.Series,
                    TaxType = "tt_Yes",
                    GSTRelevnt = "tYES",
                    GSTTaxCategory = "gtc_Regular",
                    GLMethod = "glm_WH",
                    U_TYPE = uType
                    //SalPackUn = first.SalPackUn
                };
                if (company == 1)
                {
                    tree.U_Packing_Type = first.PackingType;
                }
                if (company == 3)
                {
                    tree.U_FA_TYPE = first.FaType;
                }
                else
                {
                    tree.U_FA_Type = first.FaType;
                }
                try
                {
                    var json = JsonConvert.SerializeObject(tree);
                    var content = new StringContent(json, Encoding.UTF8, "application/json");

                    var response = await client.PostAsync("Items", content);
                    var responseBody = await response.Content.ReadAsStringAsync();

                    string message;
                    string? sapErrCode = null;

                    if (response.IsSuccessStatusCode)
                    {
                        message = "Successfully created";
                    }
                    else
                    {
                        message = ExtractSapErrorCodeAndMessage(responseBody);
                        await LogApiErrorAsync(new LogApiErrorRequest
                        {
                            ReferenceID = first.InitId,
                            ApiName = "SAP/Items",
                            ErrorMessage = message,
                            ErrorCode = sapErrCode ?? response.StatusCode.ToString(),
                            Payload = System.Text.Json.JsonSerializer.Serialize(new
                            {
                                Request = tree,
                                ResponseStatus = (int)response.StatusCode,
                                ResponseBody = responseBody
                            }),
                            CreatedBy = first.UserId
                        });
                    }

                    string status = response.IsSuccessStatusCode ? "Y" : "N";
                    await UpdateItemApiStatusAsync(first.InitId, message, status);

                    // ── MART SAP SYNC (call SAP API for Company 3 if FG item from Oil/Bev) ──
                    string martStatus = "Skipped (SAP failed)";

                    if (response.IsSuccessStatusCode)
                    {
                        bool isMartCandidate =
                            (company == 1 || company == 2) &&
                            (first.ItemGroupCode == "102" ||
                             (!string.IsNullOrEmpty(first.itemGroupName) &&
                              first.itemGroupName.Contains("FINISHED", StringComparison.OrdinalIgnoreCase)));

                        Console.WriteLine($"[INFO] MART SAP Condition → InitId: {first.InitId}, Company: {company}, GroupCode: {first.ItemGroupCode}, GroupName: {first.itemGroupName}, IsCandidate: {isMartCandidate}");

                        if (!isMartCandidate)
                        {
                            martStatus = $"Skipped (not FG item — GroupCode: {first.ItemGroupCode})";
                        }
                        else
                        {
                            Console.WriteLine($"[INFO] MART SAP Condition TRUE → Calling SAP API for Company 3 (MART), InitId: {first.InitId}");
                            try
                            {
                                // Get MART SAP session (Company 3)
                                var martSession = await _bom2Service.GetSAPSessionMartAsync();

                                // MART SAP does not have U_FA_TYPE or U_FA_Type UDF — clear both
                                tree.U_FA_TYPE = null;
                                tree.U_FA_Type = null;

                                var martHandler = new HttpClientHandler
                                {
                                    ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
                                };

                                using var martClient = new HttpClient(martHandler);
                                martClient.BaseAddress = new Uri(_sapBaseUrl);
                                martClient.DefaultRequestHeaders.Clear();
                                martClient.DefaultRequestHeaders.Add("Cookie", $"{martSession.B1Session}; {martSession.RouteId}");
                                martClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                                var martJson    = JsonConvert.SerializeObject(tree);
                                var martContent = new StringContent(martJson, Encoding.UTF8, "application/json");

                                Console.WriteLine($"[INFO] MART SAP API Request → InitId: {first.InitId}");
                                var martResponse     = await martClient.PostAsync("Items", martContent);
                                var martResponseBody = await martResponse.Content.ReadAsStringAsync();

                                if (martResponse.IsSuccessStatusCode)
                                {
                                    Console.WriteLine($"[INFO] MART SAP API Success → InitId: {first.InitId}, Created in Company 3");
                                    martStatus = "SAP Created in MART (Company 3)";
                                }
                                else
                                {
                                    var martErrMsg = ExtractSapErrorCodeAndMessage(martResponseBody);
                                    Console.WriteLine($"[ERROR] MART SAP API Failed → {martErrMsg}, InitId: {first.InitId}");

                                    await LogApiErrorAsync(new LogApiErrorRequest
                                    {
                                        ReferenceID  = first.InitId,
                                        ApiName      = "SAP/Items/MART",
                                        ErrorMessage = martErrMsg,
                                        ErrorCode    = martResponse.StatusCode.ToString(),
                                        Payload      = System.Text.Json.JsonSerializer.Serialize(new
                                        {
                                            Request        = tree,
                                            ResponseStatus = (int)martResponse.StatusCode,
                                            ResponseBody   = martResponseBody
                                        }),
                                        CreatedBy = first.UserId
                                    });

                                    martStatus = $"SAP MART Failed: {martErrMsg}";
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[ERROR] MART SAP API Exception → {ex.Message}, InitId: {first.InitId}");
                                martStatus = $"SAP MART Failed: {ex.Message}";
                            }
                        }
                    }

                    results.Add(new SapItemSyncResult
                    {
                        ItemId     = first.InitId,
                        IsSuccess  = response.IsSuccessStatusCode,
                        Message    = message,
                        MartStatus = martStatus
                    });


                }
                catch (Exception ex)
                {
                    string errMsg = $"-1000: {ex.Message}";
                    await LogApiErrorAsync(new LogApiErrorRequest
                    {
                        ReferenceID = first.InitId,
                        ApiName = "SAP/Items",
                        ErrorMessage = ex.Message,
                        ErrorCode = "EXCEPTION",
                        Payload = System.Text.Json.JsonSerializer.Serialize(new
                        {
                            Request = tree,
                            Exception = ex.ToString()
                        }),
                        CreatedBy = first.UserId
                    });
                    results.Add(new SapItemSyncResult
                    {
                        ItemId = first.InitId,
                        IsSuccess = false,
                        Message = errMsg
                    });
                    await UpdateItemApiStatusAsync(first.InitId, errMsg, false.ToString());
                }
            }

            return results;
        }

        private async Task UpdateItemApiStatusAsync(int itemId, string apiMessage, string tag)
        {
            var sqlQuery = "EXEC  [imc].[jsUpdateItemApiStatus] @itemId,@apiMessage,@tag";

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.ExecuteAsync(sqlQuery, new { itemId, apiMessage, tag });
            }
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

        public async Task<ItemMasterModel> LogApiErrorAsync(LogApiErrorRequest model)
        {
            var result = new ItemMasterModel { Success = false };

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var cmd = new SqlCommand("[imc].[LogApiError]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Nullable INT
                    var pRef = cmd.Parameters.Add("@ReferenceID", SqlDbType.Int);
                    pRef.Value = (object?)model.ReferenceID ?? DBNull.Value;

                    // NVARCHAR(100)
                    var pApi = cmd.Parameters.Add("@ApiName", SqlDbType.NVarChar, 100);
                    pApi.Value = (object?)model.ApiName ?? DBNull.Value;

                    // NVARCHAR(2000) - Required
                    var pMsg = cmd.Parameters.Add("@ErrorMessage", SqlDbType.NVarChar, 2000);
                    pMsg.Value = model.ErrorMessage;

                    // NVARCHAR(50)
                    var pCode = cmd.Parameters.Add("@ErrorCode", SqlDbType.NVarChar, 50);
                    pCode.Value = (object?)model.ErrorCode ?? DBNull.Value;

                    // NVARCHAR(MAX)
                    var pPayload = cmd.Parameters.Add("@Payload", SqlDbType.NVarChar, -1);
                    pPayload.Value = (object?)model.Payload ?? DBNull.Value;

                    // NVARCHAR(100)
                    var pBy = cmd.Parameters.Add("@CreatedBy", SqlDbType.NVarChar, 100);
                    pBy.Value = (object?)model.CreatedBy ?? DBNull.Value;

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    result.Success = true;
                    result.Message = "API error logged.";
                }
            }
            catch (SqlException ex)
            {
                result.Success = false;
                result.Message = ex.Message;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.Message = "Internal error while logging API error.";
            }

            return result;
        }

        public async Task<IEnumerable<GetIMCApprovalFlowModel>> GetIMCApprovalFlowAsync(int flowId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@flowId", flowId);
                var query = "EXEC [imc].[jsGetIMCApprovalFlow] @flowId";
                var result = await connection.QueryAsync<GetIMCApprovalFlowModel>(
                     query, parameters
                 );
                return result;
            }
        }

        public async Task<IEnumerable<CreatedByDetailModel>> GetCreatedByDetailAsync(int userId, int companyId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@companyId", companyId);
                var query = "EXEC [imc].[jsGetCreatedByDetail] @userId, @companyId";
                var result = await connection.QueryAsync<CreatedByDetailModel>(
                     query, parameters
                 );
                return result;
            }
        }

        // --------------------------- BKDT start --------------------------------
        public async Task<IEnumerable<GetUserDetailsModel>> GetUserDetailsAsync(int company)
        {
            var (HanaConnectionString, schema) = GetLiveHanaSettings(company);
            using (var connection = new HanaConnection(HanaConnectionString))
            {
                var query = $"CALL \"{schema}\".\"GETUSERDETAILS\"()";

                var result = await connection.QueryAsync<GetUserDetailsModel>(
                     query
                 );

                return result;
            }
        }

        public async Task<IEnumerable<GetMobjDetailsModel>> GetMobjDetailsAsync(int company)
        {
            var (HanaConnectionString, schema) = GetLiveHanaSettings(company);
            using (var connection = new HanaConnection(HanaConnectionString))
            {
                var query = $"CALL \"{schema}\".\"GETMOBJDETAILS\"()";
                var result = await connection.QueryAsync<GetMobjDetailsModel>(
                     query
                 );
                return result;
            }

        }
        public async Task<ItemMasterModel> SaveBKDTAsync(BKDTModel request)
        {
            var response = new ItemMasterModel();
            try
            {
                // Parse branch list from string input like "1" or "1,2,3"
                var branchCodeStrings = (request.Branch ?? string.Empty)
                    .Split(new[] { ',', ';', '|' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => s.Trim())
                    .Distinct()
                    .ToList();

                if (branchCodeStrings.Count == 0)
                    throw new ArgumentException("Branch is required. Use 1 (OIL), 2 (BEVERAGES), 3 (MART), or a comma-separated list like '1,2'.");

                var executed = new List<string>();

                foreach (var codeStr in branchCodeStrings)
                {
                    if (!int.TryParse(codeStr, out var code))
                        throw new ArgumentException($"Invalid branch code '{codeStr}'. Use 1 (OIL), 2 (BEVERAGES), 3 (MART).");

                    // Map code -> branch name
                    string branchName = code switch
                    {
                        1 => "OIL",
                        2 => "BEVERAGES",
                        3 => "MART",
                        _ => throw new ArgumentException($"Invalid branch code '{code}'. Allowed: 1, 2, 3.")
                    };

                    // Get connection + schema for THIS branch
                    var (hanaConnectionString, schema) = GetLiveHanaSettings(code);

                    using (var connection = new HanaConnection(hanaConnectionString))
                    {
                        await connection.OpenAsync();

                        // Create command
                        var command = connection.CreateCommand();
                        command.CommandText = $"CALL \"{schema}\".\"OPEN_BKDT\"(?,?,?,?,?,?,?,?,?,?,?)";

                        // Parse dates - FromDate and ToDate are still strings in dd-MM-yyyy format
                        DateTime fromDate, toDate;

                        // Parse FromDate (dd-MM-yyyy format)
                        if (!DateTime.TryParseExact(request.FromDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out fromDate))
                        {
                            throw new ArgumentException($"Invalid FromDate format: {request.FromDate}. Expected dd-MM-yyyy format.");
                        }

                        // Parse ToDate (dd-MM-yyyy format)
                        if (!DateTime.TryParseExact(request.ToDate, "dd-MM-yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out toDate))
                        {
                            throw new ArgumentException($"Invalid ToDate format: {request.ToDate}. Expected dd-MM-yyyy format.");
                        }

                        // TimeLimit, CreatedOn, DeletedOn are now DateTime properties
                        DateTime? timeLimit = request.TimeLimit == DateTime.MinValue ? null : request.TimeLimit;
                        DateTime? createdOn = request.CreatedOn == DateTime.MinValue ? null : request.CreatedOn;
                        DateTime? deletedOn = request.DeletedOn == DateTime.MinValue ? null : request.DeletedOn;

                        // Add parameters in correct order - pass DateTime objects to HANA
                        command.Parameters.Add(new HanaParameter("branch", branchName));
                        command.Parameters.Add(new HanaParameter("userId", request.UserId ?? string.Empty));
                        command.Parameters.Add(new HanaParameter("transType", request.TransType));
                        command.Parameters.Add(new HanaParameter("fromDate", fromDate.Date)); // DATE - pass DateTime
                        command.Parameters.Add(new HanaParameter("toDate", toDate.Date)); // DATE - pass DateTime  
                        command.Parameters.Add(new HanaParameter("timeLimit", timeLimit.HasValue ? (object)timeLimit.Value : DBNull.Value)); // TIMESTAMP
                        command.Parameters.Add(new HanaParameter("rights", request.Rights ?? "NO"));
                        command.Parameters.Add(new HanaParameter("createdBy", request.CreatedBy ?? string.Empty));
                        command.Parameters.Add(new HanaParameter("createdOn", createdOn.HasValue ? (object)createdOn.Value : DBNull.Value)); // TIMESTAMP
                        command.Parameters.Add(new HanaParameter("deletedBy", (object)request.DeletedBy ?? DBNull.Value));
                        command.Parameters.Add(new HanaParameter("deletedOn", deletedOn.HasValue ? (object)deletedOn.Value : DBNull.Value)); // TIMESTAMP

                        await command.ExecuteNonQueryAsync();
                        executed.Add($"{branchName} (schema: {schema})");
                    }
                }

                response.Success = true;
                response.Message = $"BKDT executed successfully for: {string.Join(", ", executed)}.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }
            return response;
        }

        public async Task<IEnumerable<GetBKDTinsights>> GetBKDTinsightsAsync(int userId, int companyId, string month)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@companyId", companyId);
                parameters.Add("@month", month);
                var query = "EXEC [backdate].[jsGetDocumentInsight] @userId, @companyId, @month";
                var result = await connection.QueryAsync<GetBKDTinsights>(
                     query, parameters
                 );
                return result;
            }
        }

        public async Task<IEnumerable<BKDTGetDocumentsModels>> GetBKDTPendingDocAsync(int userId, int company, string month)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@company", company);
                parameters.Add("@month", month);
                var query = "EXEC [backdate].[jsGetPendingDocuments] @userId, @company, @month";
                var result = await connection.QueryAsync<BKDTGetDocumentsModels>(
                     query, parameters
                 );
                return result;
            }
        }
        public async Task<IEnumerable<BKDTGetDocumentsModels>> GetBKDTApprovedDocAsync(int userId, int company, string month)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@company", company);
                parameters.Add("@month", month);
                var query = "EXEC [backdate].[jsGetApprovedDocuments] @userId, @company, @month";
                var result = await connection.QueryAsync<BKDTGetDocumentsModels>(
                     query, parameters
                 );
                return result;
            }
        }
        public async Task<IEnumerable<BKDTGetDocumentsModels>> GetBKDTRejectedDocAsync(int userId, int company, string month)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@company", company);
                parameters.Add("@month", month);
                var query = "EXEC [backdate].[jsGetRejectedDocuments] @userId, @company, @month";
                var result = await connection.QueryAsync<BKDTGetDocumentsModels>(
                     query, parameters
                 );
                return result;
            }
        }
        public async Task<IEnumerable<BKDTGetDocumentsModels>> GetBKDTFullDetailsAsync(int userId, int company, string month)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@userId", userId);
                parameters.Add("@company", company);
                parameters.Add("@month", month);

                var allResults = new List<BKDTGetDocumentsModels>();

                // Pending
                var pendingQuery = "EXEC [backdate].[jsGetPendingDocuments] @userId, @company, @month";
                var pendingDocs = await connection.QueryAsync<BKDTGetDocumentsModels>(pendingQuery, parameters);
                foreach (var doc in pendingDocs)
                    doc.Status = "Pending";
                allResults.AddRange(pendingDocs);

                // Approved
                var approvedQuery = "EXEC [backdate].[jsGetApprovedDocuments] @userId, @company, @month";
                var approvedDocs = await connection.QueryAsync<BKDTGetDocumentsModels>(approvedQuery, parameters);
                foreach (var doc in approvedDocs)
                    doc.Status = "Approved";
                allResults.AddRange(approvedDocs);

                // Rejected
                var rejectedQuery = "EXEC [backdate].[jsGetRejectedDocuments] @userId, @company, @month";
                var rejectedDocs = await connection.QueryAsync<BKDTGetDocumentsModels>(rejectedQuery, parameters);
                foreach (var doc in rejectedDocs)
                    doc.Status = "Rejected";
                allResults.AddRange(rejectedDocs);

                return allResults;
            }
        }
        public async Task<IEnumerable<BKDTDocumentDetailModels>> GetBKDTDocumentDetailAsync(int documentId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@documentId", documentId);

                var query = "EXEC [backdate].[jsGetDocumentDetail] @documentId";

                var result = await connection.QueryAsync<BKDTDocumentDetailModels>(
                     query, parameters
                 );
                return result;
            }
        }
        public async Task<IEnumerable<BKDTDocumentDetailModels>> GetBKDTDocumentDetailBasedOnFlowIdAsync(int flowId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@flowId", flowId);

                var query = "EXEC [backdate].[jsGetDocumentDetailUsingFlowId] @flowId";

                var result = await connection.QueryAsync<BKDTDocumentDetailModels>(
                     query, parameters
                 );
                return result;
            }
        }
        /* public async Task<CreateDocumentResponse> CreateDocumentAsync(CreateDocumentRequest request)
         {
             using (var connection = new SqlConnection(_connectionString))
             {
                 var parameters = new DynamicParameters();
                 parameters.Add("@branch", request.Branch);
                 parameters.Add("@username", request.Username);
                 parameters.Add("@documentType", request.DocumentType);
                 parameters.Add("@fromDate", request.FromDate);
                 parameters.Add("@toDate", request.ToDate);
                 parameters.Add("@timeLimit", request.TimeLimit);
                 parameters.Add("@action", request.Action);
                 parameters.Add("@companyId", request.CompanyId);
                 parameters.Add("@createdBy", request.CreatedBy);
                 parameters.Add("@newDocumentId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                 CreateDocumentResponse response = new CreateDocumentResponse();

                 try
                 {
                     var result = await connection.QueryFirstOrDefaultAsync<dynamic>(
                         "[backdate].[jsCreateDocument]",
                         parameters,
                         commandType: CommandType.StoredProcedure
                     );

                     response.NewDocumentId = parameters.Get<int?>("@newDocumentId");
                     response.Success = response.NewDocumentId.HasValue;
                     response.Message = result != null ? (string)result.Message : "No response from procedure.";
                 }
                 catch (Exception ex)
                 {
                     response.NewDocumentId = null;
                     response.Success = false;
                     response.Message = $"Error: {ex.Message}";
                 }

                 return response;
             }
         }*/

        public async Task<CreateDocumentResponse> CreateDocumentAsync(CreateDocumentRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@branch", request.Branch);
                parameters.Add("@username", request.Username);
                parameters.Add("@documentType", request.DocumentType);
                parameters.Add("@fromDate", request.FromDate);
                parameters.Add("@toDate", request.ToDate);
                parameters.Add("@timeLimit", request.TimeLimit);
                parameters.Add("@action", request.Action);
                parameters.Add("@companyId", request.CompanyId);
                parameters.Add("@createdBy", request.CreatedBy);
                parameters.Add("@newDocumentId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                try
                {
                    // STEP 1️⃣: Create document
                    await connection.ExecuteAsync(
                        "[backdate].[jsCreateDocument]",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    int? newId = parameters.Get<int?>("@newDocumentId");
                    if (newId == null || newId <= 0)
                    {
                        return new CreateDocumentResponse
                        {
                            Success = false,
                            Message = "Document creation failed or missing newDocumentId."
                        };
                    }

                    // STEP 2️⃣: Get stage users
                    var stageUsers = await GetBKDTCurrentUsersSendNotificationAsync(newId.Value);
                    if (stageUsers == null || !stageUsers.Any())
                    {
                        return new CreateDocumentResponse
                        {
                            Success = true,
                            Message = "Document created, but no users to notify.",
                            NewDocumentId = newId
                        };
                    }

                    // STEP 3️⃣: Unique token tracking
                    var sentTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    var notifiedUsers = new HashSet<int>();
                    var notificationLog = new StringBuilder();

                    foreach (var userGroup in stageUsers.GroupBy(u => u.userId))
                    {
                        int userId = userGroup.Key;
                        if (notifiedUsers.Contains(userId))
                            continue;

                        var fcmTokens = await _notificationService.GetUserFcmTokenAsync(userId);
                        if (fcmTokens == null || fcmTokens.Count == 0)
                        {
                            notificationLog.AppendLine($"⚠️ No FCM token for user {userId}.");
                            continue;
                        }

                        // Clean + deduplicate tokens
                        var uniqueTokens = fcmTokens
                            .Select(t => t.fcmToken?.Trim())
                            .Where(t => !string.IsNullOrWhiteSpace(t))
                            .Distinct(StringComparer.OrdinalIgnoreCase)
                            .ToList();

                        string title = "BackDate Request";
                        string body = $"A new BackDate document (Doc Id: {newId}) is awaiting your approval.";

                        var data = new Dictionary<string, string>
                {
                    { "userId", userId.ToString() },
                    { "company", request.CompanyId.ToString() },
                    { "DocId", newId.ToString() },
                    { "screen", "BackDate" }
                };

                        int tokensSent = 0;
                        foreach (var token in uniqueTokens)
                        {
                            // ✅ Prevent duplicate sends to same token
                            if (!sentTokens.Add(token))
                                continue;

                            await _notificationService.SendPushNotificationAsync(title, body, token, data);
                            tokensSent++;
                        }

                        if (tokensSent > 0)
                        {
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

                            notificationLog.AppendLine($"✅ Sent to user {userId} ({tokensSent} token(s)).");
                            notifiedUsers.Add(userId);
                        }
                        else
                        {
                            notificationLog.AppendLine($"⚠️ No valid/unique tokens for user {userId}.");
                        }
                    }

                    Console.WriteLine(notificationLog.ToString());

                    // ✅ STEP 4️⃣: Final Response
                    return new CreateDocumentResponse
                    {
                        Success = true,
                        Message = "Document created successfully and notifications sent once per token.",
                        NewDocumentId = newId
                    };
                }
                catch (Exception ex)
                {
                    return new CreateDocumentResponse
                    {
                        Success = false,
                        Message = $"Error: {ex.Message}",
                        NewDocumentId = null
                    };
                }
            }
        }


        /* public async Task<ItemMasterModel> ApproveDocumentAsync(ApproveRequestModel request)
         {
             var response = new ItemMasterModel();

             try
             {
                 using var connection = new SqlConnection(_connectionString);
                 await connection.OpenAsync();

                 var parameters = new DynamicParameters();
                 parameters.Add("@flowId", request.flowId);
                 parameters.Add("@company", request.Company);
                 parameters.Add("@userId", request.UserId);
                 parameters.Add("@remarks", request.remarks);
                 var spResult = await connection.QueryFirstOrDefaultAsync<string>(
                     "[backdate].[jsApproveDocument]",
                     parameters,
                     commandType: CommandType.StoredProcedure
                 );

                 response.Success = true;
                 response.Message = spResult ?? "No message returned from procedure.";
             }
             catch (Exception ex)
             {
                 response.Success = false;
                 response.Message = $"Error: {ex.Message}";
             }

             return response;
         }*/

        public async Task<int?> GetBackDateUserDocumentIdAsync(int flowId)
        {
            int? userDocumentId = null;

            using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            using (var cmd = new SqlCommand("[backdate].[jsGetId]", conn))
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
        public async Task<ItemMasterModel> ApproveDocumentAsync(ApproveRequestModel request)
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
                {
                    await conn.OpenAsync();
                    var resultMessages = new List<string>();
                    var allNotificationModels = new List<UserIdsForNotificationModel>();

                    using (SqlCommand cmd = new SqlCommand("[backdate].[jsApproveDocument]", conn))
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.Parameters.AddWithValue("@flowId", request.flowId);
                        cmd.Parameters.AddWithValue("@company", request.Company);
                        cmd.Parameters.AddWithValue("@userId", request.UserId);
                        cmd.Parameters.AddWithValue("@remarks", string.IsNullOrEmpty(request.remarks) ? " " : request.remarks);

                        var result = await cmd.ExecuteScalarAsync();
                        resultMessages.Add(result?.ToString() ?? $"Approved Document of FlowId {request.flowId}");


                    }
                    var notifications = await GetBkdtUserIdsSendNotificatiosAsync(request.flowId);
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

                    int? userDocumentId = await GetBackDateUserDocumentIdAsync(request.flowId);
                    int docId = userDocumentId ?? request.flowId;

                    string notificationTitle = "BackDate Request";
                    string notificationBody = $"A new BackDate document (DocId: {docId}) is awaiting your approval.";

                    var data = new Dictionary<string, string>
                    {
                        { "screen", "BackDate" },
                        { "company", request.Company.ToString() },
                        { "DocId", docId.ToString() }
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
                            title = "BackDate",
                            message = notificationBody,
                            pageId = 6,
                            data = $"Flow ID: {request.flowId}",
                            BudgetId = request.flowId
                        });
                    }

                    return new ItemMasterModel
                    {
                        Success = true,
                        Message = string.Join(" | ", resultMessages)
                    };

                }
            }
            catch (SqlException ex)
            {
                return new ItemMasterModel { Success = false, Message = $"SQL Error: {ex.Message}" };
            }
            catch (Exception ex)
            {
                return new ItemMasterModel { Success = false, Message = $"Error: {ex.Message}" };
            }
        }

        public async Task<ItemMasterModel> RejectDocumentAsync(RejectRequestModel request)
        {
            var response = new ItemMasterModel();

            try
            {
                using var connection = new SqlConnection(_connectionString);
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@flowId", request.flowId);
                parameters.Add("@company", request.Company);
                parameters.Add("@userId", request.UserId);
                parameters.Add("@remarks", request.remarks);
                var spResult = await connection.QueryFirstOrDefaultAsync<string>(
                    "[backdate].[jsRejectDocument]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                response.Success = true;
                response.Message = spResult ?? "No message returned from procedure.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;
        }

        public async Task<IEnumerable<BKDTApprovalFlow>> GetBackDateApprovalFlowAsync(int flowId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@flowId", flowId);
                var query = "EXEC [backdate].[jsGetBackDateApprovalFlow] @flowId";
                var result = await connection.QueryAsync<BKDTApprovalFlow>(
                     query, parameters
                 );
                return result;
            }
        }

        public async Task<IEnumerable<UserDocumentInsightsModel>> GetUserDocumentInsightsAsync(string createdBy, string month)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@createdBy", createdBy);
                parameters.Add("@month", month);
                var query = "EXEC [backdate].[jsGetUserDocumentInsights] @createdBy, @month";
                var result = await connection.QueryAsync<UserDocumentInsightsModel>(
                     query, parameters
                 );
                return result;
            }
        }
        public async Task<IEnumerable<UserDocumentsByCreatedByAndMonthModel>> GetUserDocumentsByCreatedByAndMonthAsync(string createdBy, string monthYear, string status)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@createdBy", createdBy);
                parameters.Add("@monthYear", monthYear);
                parameters.Add("@status", status);
                var query = "EXEC [backdate].[jsGetUserDocumentsByCreatedByAndMonth] @createdBy, @monthYear, @status";
                var result = await connection.QueryAsync<UserDocumentsByCreatedByAndMonthModel>(
                     query, parameters
                 );
                return result;
            }
        }

        public async Task<IEnumerable<FlowStatus>> GetFlowStatusAsync(int flowId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@flowId", flowId);
                var query = "EXEC [backdate].[jsGetFlowStatus] @flowId";
                var result = await connection.QueryAsync<FlowStatus>(
                     query, parameters
                 );
                return result;
            }
        }

        public async Task<ItemMasterModel> UpdateHanaStatusAsync(UpdateHanaStatusRequest request)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    var parameters = new DynamicParameters();
                    parameters.Add("@flowId", request.FlowId);
                    parameters.Add("@status", request.Status);
                    parameters.Add("@hanastatusText", request.hanastatusText);
                    connection.Execute(
                        "[backdate].[updateHanaStatus]",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );
                }

                return new ItemMasterModel
                {
                    Success = true,
                    Message = "Hana status updated successfully."
                };
            }
            catch (Exception ex)
            {
                return new ItemMasterModel
                {
                    Success = false,
                    Message = $"Error: {ex.Message}"
                };
            }
        }

        public async Task<IEnumerable<UserIdsForNotificationModel>> GetBkdtUserIdsSendNotificatiosAsync(int flowId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@userDocumentId", flowId);
                    var result = await conn.QueryAsync<UserIdsForNotificationModel>("[backdate].[jsBackdateNotify]", parameters, commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error Executing : ", ex);
            }
        }
        public async Task<PrdoModels> SendPendingBkdtCountNotificationAsync()
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

                    int companyId = user.company;
                    string month = DateTime.Now.ToString("MM-yyyy");
                    string createdBy = userId.ToString();

                    // fetch all counts for this single user
                    var counts = await GetUserDocumentInsightsAsync(createdBy, month);
                    if (counts == null || !counts.Any())
                    {
                        responseMessage.AppendLine($"No document counts for user {userId}.");
                        continue;
                    }

                    // total up ALL pendings for this user
                    int totalPending = counts.Sum(c => c.PendingRequests);
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
                                   (totalPending == 1 ? "BackDate pending request" : "BackDate pending requests");
                    string body = "Kindly Approve.";

                    var data = new Dictionary<string, string>
                    {
                        { "userId",  userId.ToString() },
                        { "company", companyId.ToString() },
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
        public async Task<IEnumerable<AfterCreatedRequestSendNotificationToUser>> GetBKDTCurrentUsersSendNotificationAsync(int userDocumentId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@userDocumentId", userDocumentId);
                    var result = await conn.QueryAsync<AfterCreatedRequestSendNotificationToUser>("[backdate].[GetUsersInCurrentStage]", parameters, commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error Executing : ", ex);
            }
        }


        // --------------------------- BKDT end --------------------------------


        public async Task<IEnumerable<GetDistinctItemName>> GetDistinctItemNameSqlAsync()
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var query = "EXEC [imc].[jsGetDistinctItemNames]"; // Assuming you have this proc
                var result = await connection.QueryAsync<GetDistinctItemName>(query);
                return result;
            }
        }

        public async Task<IEnumerable<GetItemByIdModel>> GetItemByIdAsync(int userId, int company, string month)
        {
            var sqlQuery = "EXEC [imc].[jsGetItemByUserId] @userId,@company,@month";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<GetItemByIdModel>(
                    sqlQuery,
                    new { userId, company, month } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<UserIdsForNotificationModel>> GetItemUserIdsSendNotificatiosAsync(int flowId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@imcId", flowId);
                    var result = await conn.QueryAsync<UserIdsForNotificationModel>("[imc].[jsImcNotify]", parameters, commandType: CommandType.StoredProcedure);
                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error Executing : ", ex);
            }
        }

        public async Task<PrdoModels> SendPendingItemCountNotificationAsync()
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

                    int companyId = user.company;
                    string month = DateTime.Now.ToString("MM-yyyy");

                    // fetch all counts for this single user
                    var counts = await GetWorkflowInsightsAsync(userId, companyId, month);
                    if (counts == null || !counts.Any())
                    {
                        responseMessage.AppendLine($"No document counts for user {userId}.");
                        continue;
                    }

                    // total up ALL pendings for this user
                    int totalPending = counts.Sum(c => c.PendingWorkflows);
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
                                   (totalPending == 1 ? "Item pending request" : "Item pending requests");
                    string body = "Kindly Approve.";

                    var data = new Dictionary<string, string>
                    {
                        { "userId",  userId.ToString() },
                        { "company", companyId.ToString() },
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

        public async Task<IEnumerable<AfterCreatedRequestSendNotificationToUser>> GetItemCurrentUsersSendNotificationAsync(int initID)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();
                    var parameters = new DynamicParameters();
                    parameters.Add("@initID", initID);
                    var result = await conn.QueryAsync<AfterCreatedRequestSendNotificationToUser>("[imc].[GetUsersInCurrentStage]", parameters, commandType: CommandType.StoredProcedure);
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
