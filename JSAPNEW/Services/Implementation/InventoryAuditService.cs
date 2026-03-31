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
using Renci.SshNet;
using Sap.Data.Hana;
using ServiceStack;
using ServiceStack.Html;
using ServiceStack.Text;
using System.ComponentModel.Design;
using System.Data;
using System.Text;

namespace JSAPNEW.Services.Implementation
{
    public class InventoryAuditService : IInventoryAuditService
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly INotificationService _notificationService;
        private readonly IUserService _userService;
        private readonly IBom2Service _bom2Service;
        private readonly Dictionary<int, HanaCompanySettings> _hanaSettings;
        public InventoryAuditService(IConfiguration configuration, INotificationService notificationService, IUserService userService, IBom2Service bom2Service)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _notificationService = notificationService;
            _userService = userService;
            _bom2Service = bom2Service;
            var activeEnv = configuration["ActiveEnvironment"];  // "Test" or "Live"
            _hanaSettings = configuration.GetSection($"HanaSettings:{activeEnv}")
                                         .Get<Dictionary<int, HanaCompanySettings>>();
        }

        public async Task<IEnumerable<InventoryAuditModels>> GetInventoryAuditAsync(InventoryAuditParamModels model)
        {
            if (!_hanaSettings.TryGetValue(model.company, out var settings))
                throw new ArgumentException($"Invalid company ID: {model.company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("p_warehouses", model.p_warehouses);
                parameters.Add("p_units", model.p_units);
                parameters.Add("p_itemGroups", model.p_itemGroups);
                parameters.Add("p_subGroups", model.p_subGroups);
                parameters.Add("p_itemCodes", model.p_itemCodes);
                parameters.Add("p_locations", model.p_locations);

                var query = $"CALL \"{settings.Schema}\".\"GetItemStockDetails\"(?,?,?,?,?,?)";

                var result = await connection.QueryAsync<InventoryAuditModels>(query, parameters);
                return result;
            }
        }
        public async Task<IEnumerable<UnitModels>> GetUnitAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();

                var query = $"CALL \"{settings.Schema}\".\"GetUnit\"()";

                var result = await connection.QueryAsync<UnitModels>(
                     query
                 );

                return result;
            }
        }

        public async Task<IEnumerable<SubGroupModels>> GetSubGroupAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                var query = $"CALL \"{settings.Schema}\".\"GetSubGroup\"()";
                var result = await connection.QueryAsync<SubGroupModels>(
                     query
                 );
                return result;
            }
        }

        public async Task<IEnumerable<LocationModels>> GetLocationAsync(string Unit, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");

            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("P_Unit", Unit);

                var query = $"CALL \"{settings.Schema}\".\"GetLocationsByUnit\"(?)";

                var result = await connection.QueryAsync<LocationModels>(query, parameters);
                return result;
            }
        }
        public async Task<IEnumerable<OITMmodels>> GetOITMitemsAsync(int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                var query = $"CALL \"{settings.Schema}\".\"GetFgScRmPmItems\"()";
                var result = await connection.QueryAsync<OITMmodels>(
                     query
                 );
                return result;
            }
        }

        public async Task<IEnumerable<WarehouseModels>> GetWarehouseAsync(int LocationCode, int company)
        {
            if (!_hanaSettings.TryGetValue(company, out var settings))
                throw new ArgumentException($"Invalid company ID: {company}");
            using (var connection = new HanaConnection(settings.ConnectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("P_LocationCode", LocationCode);
                var query = $"CALL \"{settings.Schema}\".\"GetWarehouseByLocation\"(?)";
                var result = await connection.QueryAsync<WarehouseModels>(query, parameters);
                return result;
            }
        }

        public async Task<InventoryApiResponse> InsertStockCountDataBulkAsync(InsertStockCountDataBulkRequest request)
        {
            var response = new InventoryApiResponse();
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand("[in].[InsertStockCountDataBulk]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    // Header parameters
                    cmd.Parameters.AddWithValue("@LotNumber", request.LotNumber ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Unit", request.Unit ?? string.Empty);
                    cmd.Parameters.AddWithValue("@LocationCode", request.LocationCode ?? string.Empty);
                    cmd.Parameters.AddWithValue("@LocationName", request.LocationName ?? string.Empty);
                    cmd.Parameters.AddWithValue("@Warehouses", request.Warehouses ?? string.Empty);
                    cmd.Parameters.AddWithValue("@ItemGroups", request.ItemGroups ?? string.Empty);
                    cmd.Parameters.AddWithValue("@ItemSubGroups", request.ItemSubGroups ?? string.Empty);

                    // *** ADD THESE NEW PARAMETERS ***
                    cmd.Parameters.AddWithValue("@DifferenceQty", request.DifferenceQty);
                    cmd.Parameters.AddWithValue("@DifferenceValue", request.DifferenceValue);
                    cmd.Parameters.AddWithValue("@Status", request.Status ?? "Pending");
                    cmd.Parameters.AddWithValue("@UserId", request.UserId ?? string.Empty);
                    cmd.Parameters.AddWithValue("@company", request.Company ?? string.Empty);

                    // Table-Valued Parameter - NO CHANGES NEEDED
                    var tvp = new SqlParameter("@Items", SqlDbType.Structured)
                    {
                        TypeName = "[in].StockCountItemType",
                        Value = ConvertToDataTable(request.Items)
                    };
                    cmd.Parameters.Add(tvp);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    response.Success = true;
                    response.Message = "Stock count data inserted successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error inserting stock count data: {ex.Message}";
            }
            return response;
        }

        // Convert List<StockCountItem> to DataTable
        private DataTable ConvertToDataTable(List<StockCountItem> items)
        {
            DataTable dt = new DataTable();

            // Columns must match stored procedure table type exactly
            dt.Columns.Add("ItemCode", typeof(string));
            dt.Columns.Add("ItemName", typeof(string));
            dt.Columns.Add("WarehouseName", typeof(string));
            dt.Columns.Add("ItemGroupCode", typeof(string));
            dt.Columns.Add("SystemCount", typeof(decimal));
            dt.Columns.Add("LastCount", typeof(decimal));
            dt.Columns.Add("PhysicalCount", typeof(decimal));
            dt.Columns.Add("StockValue", typeof(decimal));
            dt.Columns.Add("itmsGrpNam", typeof(string));      // *** ADD THIS ***
            dt.Columns.Add("u_Sub_Group", typeof(string));     // *** ADD THIS ***

            if (items != null)
            {
                foreach (var item in items)
                {
                    dt.Rows.Add(
                        item.ItemCode,
                        item.ItemName,
                        item.WarehouseName,
                        item.ItemGroupCode,
                        item.SystemCount,
                        item.LastCount,
                        item.PhysicalCount,
                        item.StockValue,
                        item.itmsGrpNam,        // *** ADD THIS ***
                        item.u_Sub_Group        // *** ADD THIS ***
                    );
                }
            }
            return dt;
        }

        public async Task<GenerateUsernameResponse> GenerateUniqueUsernameAsync(int userId)
        {
            var response = new GenerateUsernameResponse();

            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand("sp_GenerateUniqueUsernameWithCounter", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.AddWithValue("@UserId", userId);

                    var outputParam = new SqlParameter("@UniqueUsername", SqlDbType.NVarChar, 50)
                    {
                        Direction = ParameterDirection.Output
                    };
                    cmd.Parameters.Add(outputParam);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();

                    response.Success = true;
                    response.UniqueUsername = outputParam.Value?.ToString();
                    response.Message = "Unique username generated successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error generating username: {ex.Message}";
            }

            return response;
        }

        public async Task<IEnumerable<LotNumberModels>> GetLotNumber(string company)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@company", company);

                var query = "EXEC [in].[GetLotNumberByCompany] @company";
                var result = await connection.QueryAsync<LotNumberModels>(
                     query, parameters
                 );
                return result;
            }
        }

        public async Task<StockCountResponseDto> GetStockCountDataByFilterAsync(StockCountRequest request)
        {
            using var connection = new SqlConnection(_connectionString);
            using var multi = await connection.QueryMultipleAsync(
                "[in].[GetStockCountDataByFilter]",
                new
                {
                    Unit = string.IsNullOrWhiteSpace(request.unit) ? null : request.unit,
                    Location = string.IsNullOrWhiteSpace(request.location) ? null : request.location,
                    Warehouse = string.IsNullOrWhiteSpace(request.warehouse) ? null : request.warehouse,
                    ItemGroup = string.IsNullOrWhiteSpace(request.itemGroup) ? null : request.itemGroup,
                    ItemSubGroup = string.IsNullOrWhiteSpace(request.itemSubGroup) ? null : request.itemSubGroup,
                    Status = string.IsNullOrWhiteSpace(request.status) ? null : request.status
                },
                commandType: CommandType.StoredProcedure
            );

            var headers = (await multi.ReadAsync<StockCountHeaderDto>()).AsList();
            var groups = (await multi.ReadAsync<StockCountItemGroupDto>()).AsList();
            var subGroups = (await multi.ReadAsync<StockCountItemSubGroupDto>()).AsList();
            var warehouse = (await multi.ReadAsync<StockCountWarehouseDto>()).AsList();

            return new StockCountResponseDto
            {
                Headers = headers,
                ItemGroups = groups,
                ItemSubGroups = subGroups,
                Warehouse = warehouse
            };
        }

        public async Task<GetItemsUsingLotNumberResult> GetItemsUsingLotNumberAsync(string lotNumber,int UserId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                connection.Open();

                using (var multi = connection.QueryMultiple(
                    "[in].[GetItemsUsingLotNumber]",
                    new { lotNumber , UserId},
                    commandType: CommandType.StoredProcedure))
                {
                    var headers = multi.Read<StockCountHeaderDto>().ToList();
                    var warehouses = multi.Read<WarehouseDto>().ToList();
                    var itemGroups = multi.Read<ItemGroupDto>().ToList();
                    var itemSubGroups = multi.Read<ItemSubGroupDto>().ToList();
                    var items = multi.Read<StockCountItemDto>().ToList();
                    var session = multi.Read<SessionDto>().ToList();

                    return new GetItemsUsingLotNumberResult
                    {
                        Headers = headers,
                        Warehouses = warehouses,
                        ItemGroups = itemGroups,
                        ItemSubGroups = itemSubGroups,
                        Items = items,
                        Session = session
                    };
                }
            }
        }

        public async Task<InventoryApiResponse> UpdatePhysicalCountAsync(UpdatePhysicalCountDto dto)
        {
            var response = new InventoryApiResponse();
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                using (SqlCommand cmd = new SqlCommand("[in].[UpdatePhysicalCount]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    // Header parameters
                    cmd.Parameters.AddWithValue("@ItemId", dto.ItemId);
                    cmd.Parameters.AddWithValue("@NewPhysicalCount", dto.NewPhysicalCount);
                    //cmd.Parameters.AddWithValue("@HeaderDifferenceQty", dto.HeaderDifferenceQty);
                    //cmd.Parameters.AddWithValue("@HeaderDifferenceValue", dto.HeaderDifferenceValue);
                    cmd.Parameters.AddWithValue("@UpdatedBy", dto.UpdatedBy ?? "System");
                    cmd.Parameters.AddWithValue("@Notes", (object)dto.Notes ?? DBNull.Value);
                    cmd.Parameters.AddWithValue("@status", dto.Status);

                    await conn.OpenAsync();
                    await cmd.ExecuteNonQueryAsync();
                    response.Success = true;
                    response.Message = "Physical count updated successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error updating physical count: {ex.Message}";
            }
            return response;
        }

        //again
        public async Task<CreateBulkStockCountResponse> CreateSessionWithBulkStockCountAsync(CreateBulkStockCountRequest request)
        {
            using (var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();

                var parameters = new DynamicParameters();

                // Session params
                parameters.Add("@sessionName", request.SessionName);
                parameters.Add("@description", request.Description);
                parameters.Add("@startDate", request.StartDate);
                parameters.Add("@endDate", request.EndDate);
                parameters.Add("@createdBy", request.CreatedBy);

                // TVP for UserAssignments
                var dt = new DataTable();
                dt.Columns.Add("userId", typeof(int));
                dt.Columns.Add("roleInSession", typeof(string));
                foreach (var ua in request.UserAssignments)
                {
                    dt.Rows.Add(ua.UserId, ua.RoleInSession);
                }
                parameters.Add("@userAssignments", dt.AsTableValuedParameter("[in].sessionUserType"));

                // StockCountHeader params
                parameters.Add("@LotNumber", request.LotNumber);
                parameters.Add("@Unit", request.Unit);
                parameters.Add("@LocationCode", request.LocationCode);
                parameters.Add("@LocationName", request.LocationName);
                parameters.Add("@DifferenceQty", request.DifferenceQty);
                parameters.Add("@DifferenceValue", request.DifferenceValue);
                parameters.Add("@Status", request.Status);
                parameters.Add("@Company", request.Company);

                // Bulk params
                parameters.Add("@Warehouses", request.Warehouses);
                parameters.Add("@ItemGroups", request.ItemGroups);
                parameters.Add("@ItemSubGroups", request.ItemSubGroups);

                // Output
                parameters.Add("@newSessionId", dbType: DbType.Int32, direction: ParameterDirection.Output);

                var result = await conn.QueryFirstOrDefaultAsync<CreateBulkStockCountResponse>(
                    "[in].[CreateSessionWithBulkStockCount]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                if (result != null)
                {
                    result.SessionId = parameters.Get<int>("@newSessionId");
                }

                return result;
            }
        }
        public async Task<UserActiveSessionsResponse> GetUserActiveSessionsAsync(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var multi = await connection.QueryMultipleAsync(
                    "[in].[getUserActiveSessions]",
                    new { userId },
                    commandType: CommandType.StoredProcedure))
                {
                    var sessions = (await multi.ReadAsync<ActiveSessionDetail>()).ToList();
                    var summary = await multi.ReadFirstOrDefaultAsync<ActiveSessionSummary>();

                    return new UserActiveSessionsResponse
                    {
                        Sessions = sessions,
                        Summary = summary
                    };
                }
            }
        }
        public async Task<UserInactiveSessionsResponse> GetUserInactiveSessionsAsync(int userId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (var multi = await connection.QueryMultipleAsync(
                    "[in].[getUserInactiveSessions]",
                    new { userId },
                    commandType: CommandType.StoredProcedure))
                {
                    var sessions = (await multi.ReadAsync<InactiveSessionDetail>()).ToList();
                    var summary = await multi.ReadFirstOrDefaultAsync<InactiveSessionSummary>();

                    return new UserInactiveSessionsResponse
                    {
                        Sessions = sessions,
                        Summary = summary
                    };
                }
            }
        }
        public async Task<DeactivateSessionResponse> DeactivateSessionAsync(DeactivateSessionRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                var parameters = new DynamicParameters();
                parameters.Add("@sessionId", request.SessionId);
                parameters.Add("@deactivatedBy", request.DeactivatedBy);
                parameters.Add("@deactivationReason", request.DeactivationReason);

                var result = await connection.QueryFirstOrDefaultAsync<DeactivateSessionResponse>(
                    "[in].[deactivateSession]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
        }

        public async Task<InventoryApiResponse> InsertStockCountItemsAsync(InsertStockCountItemsRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var tvp = new DataTable();
                
                tvp.Columns.Add("ItemCode");
                tvp.Columns.Add("ItemName");
                tvp.Columns.Add("WarehouseName");
                tvp.Columns.Add("ItemGroupCode");
                tvp.Columns.Add("SystemCount");
                tvp.Columns.Add("LastCount");
                tvp.Columns.Add("PhysicalCount");
                tvp.Columns.Add("StockValue");
                tvp.Columns.Add("itmsGrpNam");
                tvp.Columns.Add("u_Sub_Group");
                tvp.Columns.Add("userId");
                tvp.Columns.Add("isLitre");
                tvp.Columns.Add("salPackun");

                foreach (var item in request.Items)
                {
                    tvp.Rows.Add(
                        
                        item.ItemCode,
                        item.ItemName,
                        item.WarehouseName,
                        item.ItemGroupCode,
                        item.SystemCount ?? 0,
                        item.LastCount ?? 0,
                        item.PhysicalCount ?? 0,
                        item.StockValue ?? 0,
                        item.itmsGrpNam,
                        item.u_Sub_Group,
                        item.UserId,
                        item.isLitre,
                        item.salPackun
                    );
                }

                var parameters = new DynamicParameters();
                parameters.Add("@LotNumber", request.LotNumber);
                parameters.Add("@Items", tvp.AsTableValuedParameter("[in].StockCountItemType"));
                parameters.Add("@CreatedBy", request.CreatedBy);
                parameters.Add("@AllowDuplicates", request.AllowDuplicates);
                parameters.Add("@ActionType", request.ActionType);

                var result = await connection.QueryFirstOrDefaultAsync<InventoryApiResponse>(
                    "[in].[InsertStockCountItems]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
        }

        public async Task<InventoryApiResponse> UpdateUserSessionStatusAsync(UpdateUserSessionStatusRequest request)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@sessionId", request.SessionId, DbType.Int32);
                parameters.Add("@userId", request.UserId, DbType.Int32);

                var rowsAffected = await connection.ExecuteAsync(
                    "[in].[updateUserSessionStatus]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return new InventoryApiResponse
                {
                    Success = rowsAffected > 0,
                    Message = rowsAffected > 0
                        ? "User session status updated successfully."
                        : "No matching session/user found."
                };
            }
        }

        public async Task<InventoryApiResponse> InsertSingleStockCountItemAsync(InsertSingleStockCountItemRequest req)
        {
            if (req == null) throw new ArgumentNullException(nameof(req));

            using (IDbConnection conn = new SqlConnection(_connectionString))
            {
                var p = new DynamicParameters();

                p.Add("@LotNumber", req.LotNumber);
                p.Add("@ItemCode", req.ItemCode);
                p.Add("@ItemName", req.ItemName);
                p.Add("@WarehouseName", req.WarehouseName);
                p.Add("@ItemGroupCode", req.ItemGroupCode);
                p.Add("@SystemCount", req.SystemCount);
                p.Add("@LastCount", req.LastCount);
                p.Add("@PhysicalCount", req.PhysicalCount);
                p.Add("@StockValue", req.StockValue);
                p.Add("@itmsGrpNam", req.itmsGrpNam);
                p.Add("@u_Sub_Group", req.u_Sub_Group);
                p.Add("@UserId", req.UserId);
                p.Add("@isLitre", req.isLitre);
                p.Add("@salPackun", req.salPackun);
                p.Add("@CreatedBy", req.CreatedBy);
                p.Add("@AllowDuplicates", req.AllowDuplicates);
                p.Add("@ActionType", req.ActionType);

                try
                {
                    // Your proc returns a single SELECT row when successful
                    var result = await conn.QueryFirstOrDefaultAsync<InventoryApiResponse>(
                        sql: "[in].[InsertSingleStockCountItem]",
                        param: p,
                        commandType: CommandType.StoredProcedure
                    );

                    if (result == null)
                        throw new InvalidOperationException("Procedure executed but returned no data.");

                    return result;
                }
                catch (SqlException ex)
                {
                    // The proc uses THROW/RAISERROR; surface its message
                    throw new ApplicationException($"SQL error calling [in].[InsertSingleStockCountItem]: {ex.Message}", ex);
                }
                catch (Exception ex)
                {
                    throw new ApplicationException($"Unexpected error calling [in].[InsertSingleStockCountItem]: {ex.Message}", ex);
                }
            }
        }

        public async Task<InventoryApiResponse> DeactivateSessionIfAllUsersInactiveAsync(int SessionId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@sessionId", SessionId, DbType.Int32);
                var result = await connection.QueryFirstOrDefaultAsync<InventoryApiResponse>(
                    "[in].[deactivateSessionIfAllUsersInactive]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                return result;
            }
        }
        public async Task<IEnumerable<StockCountReportModel>> GetStockCountReportByLotAsync(string lotNumber)
        {
            using (var conn = new SqlConnection(_connectionString))
            {
                var p = new DynamicParameters();
                p.Add("@LotNumber", lotNumber);

                var result = await conn.QueryAsync<StockCountReportModel>(
                    "[in].[GetStockCountReportByLotV4]",
                    p,
                    commandType: CommandType.StoredProcedure
                );

                return result;
            }
        }

        public async Task<List<AllSessionModel>> GetActiveSessionsByUserAsync(int createdBy)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@createdBy", createdBy);

                var result = await connection.QueryAsync<AllSessionModel>(
                    "[in].[GetActiveSessionAccordingToUser]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );

                return result.ToList();
            }
        }

        public async Task<List<AllSessionModel>> GetInActiveSessionsByUserAsync(int createdBy)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@createdBy", createdBy);
                var result = await connection.QueryAsync<AllSessionModel>(
                    "[in].[GetInactiveSessionAccordingToUser]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                return result.ToList();
            }
        }

        public async Task<List<SessionUserModel>> GetSessionUsersAsync(int sessionId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                var parameters = new DynamicParameters();
                parameters.Add("@sessionId", sessionId);
                var result = await connection.QueryAsync<SessionUserModel>(
                    "[in].[GetSessionUser]",
                    parameters,
                    commandType: CommandType.StoredProcedure
                );
                return result.ToList();
            }
        }
    }
}
