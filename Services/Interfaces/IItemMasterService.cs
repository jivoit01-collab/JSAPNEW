using JSAPNEW.Models;
using JSAPNEW.Data.Entities;
using ServiceStack;


namespace JSAPNEW.Services.Interfaces
{
    public interface IItemMasterService
    {

        Task<IEnumerable<HSNModel>> GetHSNAsync(int company);
        Task<IEnumerable<InventoryUOMModel>> GetInventoryUOMAsync(int company);
        Task<IEnumerable<PackingTypeModel>> GetPackingTypeAsync(int GroupCode, int company);
        Task<IEnumerable<PackTypeModel>> GetPackTypeAsync(int GroupCode, int company);
        Task<IEnumerable<PurPackModel>> GetPurPackAsync(int company);
        Task<IEnumerable<SalPackModel>> GetSalPackAsync(int company);
        Task<IEnumerable<SalUnitModel>> GetSalUnitAsync(int GroupCode, int company);
        // Task<IEnumerable<RecieveSKUmodel>> GetSKUAsync(string BRAND, string VARIETY, string SUBGROUP, int GroupCode, int company);
        Task<IEnumerable<RecieveSKUmodel>> GetSKUAsync(int GroupCode, int company);
        Task<IEnumerable<GetVarietyModel>> GetVarietyAsync(string BRAND, int GroupCode, int company);
        Task<IEnumerable<GetsubgroupModel>> GetSubGroupAsync(string BRAND, string VARIETY, int GroupCode, int company);
        Task<IEnumerable<UnitModel>> GetUnitAsync(int GroupCode, int company);
        Task<IEnumerable<GetFAModel>> GetFaAsync(int GroupCode, int company);
        Task<IEnumerable<BuyUnitModel>> GetBuyUnitAsync(int company);
        Task<IEnumerable<GroupModel>> GetGroupAsync(int company);
        Task<IEnumerable<BrandModel>> GetBrandAsync(int GroupCode, int company);
        Task<ItemMasterModel> ApproveItemAsync(ApproveItemModel request);
        Task<IEnumerable<ApprovedItemModel>> GetApprovedItemsAsync(int userId, int company);
        Task<IEnumerable<ItemFullDetailModel>> GetFullItemDetailsAsync(int itemId);
        Task<IEnumerable<PendingItemModel>> GetPendingItemsAsync(long userId, int company);
        Task<IEnumerable<RejectedItemModel>> GetRejectedItemsAsync(long userId, int company);
        Task<IEnumerable<WorkflowInsightModel>> GetWorkflowInsightsAsync(int userId, int companyId, string? month = null);
        Task<ItemMasterModel> InsertInitDataAsync(InsertInitDataModel request);
        Task<ItemMasterModel> InsertSAPDataAsync(InsertSAPDataModel request);
        Task<ItemMasterModel> RejectItemAsync(RejectItemModel request);
        Task<ItemMasterModel> UpdateInitDataAsync(UpdateInitDataModel request);
        Task<ItemMasterModel> UpdateSAPDataAsync(UpdateSAPDataModel request);
        Task<IEnumerable<TaxRateModel>> GetTaxRateAsync(int company);
        Task<IEnumerable<MergedItemModel>> GetAllItemsAsync(int userId, int companyId);
        Task<IEnumerable<BuyUnitMsrModel>> GetBuyUnitMsrAsync(int GroupCode, int company);
        Task<IEnumerable<InventoryUOMModel>> GetInvUnitMsrAsync(int GroupCode, int company);

        Task<IEnumerable<UOMgroupModel>> JsGetUOMGroupAsync(int GroupCode, int company);

        Task<IEnumerable<GetDistinctItemName>> GetDistinctItemNameAsync(int company);
        Task<List<PendingItemApiInsertionsModel>> GetPendingItemApiInsertionsAsync(int itemId);
        Task<List<SapItemSyncResult>> PostItemsToSAPAsync(List<PendingItemApiInsertionsModel> ItemInsertions);
        Task<ItemMasterModel> LogApiErrorAsync(LogApiErrorRequest model);
        Task<IEnumerable<GetIMCApprovalFlowModel>> GetIMCApprovalFlowAsync(int flowId);
        Task<IEnumerable<CreatedByDetailModel>> GetCreatedByDetailAsync(int userId, int companyId);
        Task<IEnumerable<RejectedItemsForCreatorModel>> GetRejectedItemsForCreatorAsync(int userId, int? company);


        // --------------------------- BKDT start --------------------------------
        Task<IEnumerable<GetUserDetailsModel>> GetUserDetailsAsync(int company);
        Task<IEnumerable<GetMobjDetailsModel>> GetMobjDetailsAsync(int company);
        Task<ItemMasterModel> SaveBKDTAsync(BKDTModel request);
        Task<IEnumerable<GetBKDTinsights>> GetBKDTinsightsAsync(int userId, int companyId, string month);
        Task<IEnumerable<BKDTGetDocumentsModels>> GetBKDTPendingDocAsync(int userId, int company, string month);
        Task<IEnumerable<BKDTGetDocumentsModels>> GetBKDTApprovedDocAsync(int userId, int company, string month);
        Task<IEnumerable<BKDTGetDocumentsModels>> GetBKDTRejectedDocAsync(int userId, int company, string month);
        Task<IEnumerable<BKDTGetDocumentsModels>> GetBKDTFullDetailsAsync(int userId, int company, string month);
        Task<IEnumerable<BKDTDocumentDetailModels>> GetBKDTDocumentDetailAsync(int documentId);
        Task<IEnumerable<BKDTDocumentDetailModels>> GetBKDTDocumentDetailBasedOnFlowIdAsync(int flowId);
        Task<CreateDocumentResponse> CreateDocumentAsync(CreateDocumentRequest request);
        Task<ItemMasterModel> ApproveDocumentAsync(ApproveRequestModel request);
        Task<ItemMasterModel> RejectDocumentAsync(RejectRequestModel request);
        Task<IEnumerable<BKDTApprovalFlow>> GetBackDateApprovalFlowAsync(int flowId);
        Task<IEnumerable<UserDocumentInsightsModel>> GetUserDocumentInsightsAsync(string createdBy, string month);
        Task<IEnumerable<UserDocumentsByCreatedByAndMonthModel>> GetUserDocumentsByCreatedByAndMonthAsync(string createdBy, string monthYear, string status);
        Task<IEnumerable<FlowStatus>> GetFlowStatusAsync(int flowId);
        Task<ItemMasterModel> UpdateHanaStatusAsync(UpdateHanaStatusRequest request);
        Task<IEnumerable<UserIdsForNotificationModel>> GetBkdtUserIdsSendNotificatiosAsync(int flowId);
        Task<PrdoModels> SendPendingBkdtCountNotificationAsync();
        Task<IEnumerable<AfterCreatedRequestSendNotificationToUser>> GetBKDTCurrentUsersSendNotificationAsync(int userDocumentId);
        //Task<IEnumerable<RejectedItemsForCreatorModel>> GetRejectedItemsForCreatorAsync(int userId, int? company);

        // --------------------------- BKDT end --------------------------------
        Task<ItemMasterModel> InsertFullItemDataAsync(InsertFullItemDataModel model);

        Task<IEnumerable<GetDistinctItemName>> GetDistinctItemNameSqlAsync();

        Task<IEnumerable<GetItemByIdModel>> GetItemByIdAsync(int userId, int company, string month);
        Task<IEnumerable<UserIdsForNotificationModel>> GetItemUserIdsSendNotificatiosAsync(int flowId);
        Task<PrdoModels> SendPendingItemCountNotificationAsync();
        Task<IEnumerable<AfterCreatedRequestSendNotificationToUser>> GetItemCurrentUsersSendNotificationAsync(int initID);
    }
}
