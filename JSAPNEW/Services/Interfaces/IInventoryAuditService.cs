using JSAPNEW.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface IInventoryAuditService
    {
        Task<IEnumerable<InventoryAuditModels>> GetInventoryAuditAsync(InventoryAuditParamModels model);
        Task<IEnumerable<UnitModels>> GetUnitAsync(int company);
        Task<IEnumerable<SubGroupModels>> GetSubGroupAsync(int company);
        Task<IEnumerable<OITMmodels>> GetOITMitemsAsync(int company);
        Task<IEnumerable<LocationModels>> GetLocationAsync(string Unit, int company);
        Task<IEnumerable<WarehouseModels>> GetWarehouseAsync(int LocationCode, int company);
        Task<InventoryApiResponse> InsertStockCountDataBulkAsync(InsertStockCountDataBulkRequest request);
        Task<GenerateUsernameResponse> GenerateUniqueUsernameAsync(int userId);
        Task<IEnumerable<LotNumberModels>> GetLotNumber(string company);
        Task<StockCountResponseDto> GetStockCountDataByFilterAsync(StockCountRequest request);
        Task<GetItemsUsingLotNumberResult> GetItemsUsingLotNumberAsync (string lotNumber,int UserId);
        Task<InventoryApiResponse> UpdatePhysicalCountAsync(UpdatePhysicalCountDto dto);

        ////again

        Task<CreateBulkStockCountResponse> CreateSessionWithBulkStockCountAsync(CreateBulkStockCountRequest request);
        Task<UserActiveSessionsResponse> GetUserActiveSessionsAsync(int userId);
        Task<UserInactiveSessionsResponse> GetUserInactiveSessionsAsync(int userId);
        Task<DeactivateSessionResponse> DeactivateSessionAsync(DeactivateSessionRequest request);
        Task<InventoryApiResponse> InsertStockCountItemsAsync(InsertStockCountItemsRequest request);
        Task<InventoryApiResponse> UpdateUserSessionStatusAsync(UpdateUserSessionStatusRequest request);
        Task<InventoryApiResponse> InsertSingleStockCountItemAsync(InsertSingleStockCountItemRequest request);
        Task<InventoryApiResponse> DeactivateSessionIfAllUsersInactiveAsync(int SessionId);
        Task<IEnumerable<StockCountReportModel>> GetStockCountReportByLotAsync(string lotNumber);
        Task<List<AllSessionModel>> GetActiveSessionsByUserAsync(int createdBy);
        Task<List<AllSessionModel>> GetInActiveSessionsByUserAsync(int createdBy);
        Task<List<SessionUserModel>> GetSessionUsersAsync(int sessionId);
    }
}
