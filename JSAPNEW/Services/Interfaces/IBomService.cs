using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using Microsoft.AspNetCore.Mvc;
using ServiceStack;

namespace JSAPNEW.Services.Interfaces
{
    public interface IBomService
    {
        Task<IEnumerable<WarehouseModel>> GetWarehouseAsync(int company);
        Task<IEnumerable<TypeModel>> GetBomTypeAsync();
        Task<IEnumerable<TypeModel>> GetChildTypeAsync();
        Task<IEnumerable<MaterialModel>> GetMaterialAsync(string parentCode, int company);
        Task<IEnumerable<ItemModel>> GetFatherMaterialAsync(int company, string type);
        Task<IEnumerable<ResourcesModel>> GetResourcesAsync(int company);
        Task<IEnumerable<BomResponse>> AddBomComponentAsync(AddBomComponentModel request);
        Task<IEnumerable<BomResponse>> RemoveBomComponentAsync(RemoveBomComponentModel request);
        Task<IEnumerable<BomResponse>> CreateBomAsync(CreateBomModel request);
        Task<IEnumerable<PendingBomModel>> PendingBOMsAsync(int userId, int companyId);
        Task<IEnumerable<BomMaterialModel>> BomMaterialAsync(int bomId, int company);
        Task<IEnumerable<BomResourceModel>> BomResourceAsync(int bomId, int company);
        Task<IEnumerable<BomModel>> GetApprovedBOMsAsync(int userId, int company);
        Task<IEnumerable<BomModel>> GetRejectedBOMsAsync(int userId, int company);
        Task<IEnumerable<BomResponse>> BomApproveAsync(int bomId, int userId, string description);
        Task<IEnumerable<BomResponse>> BomRejectAsync(int bomId, int userId, string description);
        Task<IEnumerable<BomResponse>> CreateBomWithComponents(BomRequest request, List<IFormFile> files);
        Task<IEnumerable<TotalBomInsightsModel>> TotalBomInsightsAsync(int userId, int companyId);
        Task<IEnumerable<FullHeaderDetailModel>> FullHeaderDetailModelAsync(int bomId, int company);
        // Task FullMaterialDetailModelAsync(int bomId, int company);
        Task<int> UpdateBomHeaderAsync(int bomId, int qty, string wareHouse, int updatedBy);
        Task<int> UpdateChildAsync(int bomComId, int qty, string wareHouse, int updatedBy);
        Task<IEnumerable<BomAllDataWithDetails>> GetAllBomWithDetailsAsync(int userId, int company);
        Task<IEnumerable<BomFileModel>> GetBomFilesDataAsync(int bomId, IUrlHelper urlHelper);
        Task<IEnumerable<ApprovalFlowRequest>> GetBomApprovalFlowAsync(int bomId);
        //Task<List<SapPendingInsertionModel>> GetPendingInsertionsAsync(int bomid);
        Task<IEnumerable<CreatedByBomApprovalFlow>> GetCreatedByBomApprovalFlowAsync(int createdBy, int companyId);
        Task<IEnumerable<BomByUserIdModel>> GetBomByUserIdAsync(int userId, int company,string month);
        //Bom Updation 
        Task<IEnumerable<GetBomUpdationDataModel>> FetchBomDetailsAsync(string code, int company);
        Task<BomResponse> UpdateBomAsync(UpdateBomRequest request);
        Task<IEnumerable<GetDistinctBom>> GetDistinctBomAsync(int company);
        Task<OldBomPreviewResponseModel> GetOldBomPreviewAsync(int newBomId);
        Task<IEnumerable<AfterCreatedRequestSendNotificationToUser>> GetBomCurrentUsersSendNotificationAsync(int bomId);
        Task<IEnumerable<UserIdsForNotificationModel>> GetBomUserIdsSendNotificatiosAsync(int bomId);
        Task<BomResponse> SendPendingBomCountNotificationAsync();
        Task UpdateBomStatusAsync(int bomId, string apiMessage, string tag);
        Task<List<SapBomSyncResult>> PatchProductTreesToSAPAsync(List<SapPendingInsertionModel> pendingInsertions);
        Task<List<SapPendingInsertionModel>> GetPendingInsertionsAsync(int bomid, string action);
    }
}
