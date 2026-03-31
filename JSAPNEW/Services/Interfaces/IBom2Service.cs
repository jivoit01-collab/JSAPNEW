using JSAPNEW.Models;
using JSAPNEW.Data.Entities;
using ServiceStack;
//using static JSAPNEW.Models.Bom2Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface IBom2Service
    {
        Task<SAPSessionModel> GetSAPSessionOilAsync();
        Task<SAPSessionModel> GetSAPSessionBevAsync();
        Task<SAPSessionModel> GetSAPSessionMartAsync();
        Task<List<SapPendingInsertionModel>> GetPendingInsertionsAsync2(int bomid);
        Task<List<SapBomSyncResult>> PostProductTreesToSAPAsync(List<SapPendingInsertionModel> pendingInsertions);
        Task<BomResponse2> BomApproveAsync(ApproveModel2 request);
        Task<IEnumerable<GetBomApprove2>> GetApprovedBOMsAsync(int userId, int company);
        Task<IEnumerable<TotalBomInsightsModel>> TotalBomInsightsAsync(int userId, int companyId);
        Task<IEnumerable<BomPendingRequest>> GetPendingBOMsAsync(int userId, int company);
        Task<IEnumerable<GetBomReject2>> GetRejectedBOMsAsync(int userId, int company);
        Task<IEnumerable<BomResponse>> BomRejectAsync(RejectModel2 request);
        Task<IEnumerable<BomFileModel>> GetBomFilesAsync(int bomId);
        Task<IEnumerable<BomHeaderModel>> GetBomHeadersByIdsAsync(int company);
        Task<IEnumerable<TypeModel>> GetBomTypeAsync();
        Task<IEnumerable<TypeModel>> GetChildTypeAsync();
        Task<IEnumerable<TypeModel>> GetChildTypeById(int childId);
        Task<IEnumerable<BomMaterialModel>> BomMaterialAsync(int bomId, int company);
        Task<IEnumerable<BomResourceModel>> BomResourceAsync(int bomId, int company);
        Task<IEnumerable<ItemModel>> GetFatherMaterialAsync(int company, string type);
        Task<IEnumerable<FullHeaderDetailModel>> FullHeaderDetailAsync(int bomId, int company);
        Task<IEnumerable<MaterialModel>> GetMaterialAsync(string parentCode, int company);
        Task<IEnumerable<ResourcesModel>> GetResourcesAsync(int company);
        Task<IEnumerable<WarehouseModel>> GetWarehouseAsync(int company);
        Task<IEnumerable<BomResponse>> RemoveBomComponentAsync(RemoveBomComponentModel2 request);
        Task<IEnumerable<BomResponse>> UpdateBomHeaderAsync(int bomId, int qty, string wareHouse, int updatedBy);
        Task<IEnumerable<BomResponse>> CreateBomWithComponentsAsync(BomRequest2 request, List<IFormFile> files);
        Task<IEnumerable<BomHeaderData>> BOMGetBomHeadersByIdsAsync(string IdsList, int company);
        Task<IEnumerable<TotalBomDetails>> GetTotalBomInDetailsAsync(int userId, int company);
        Task<IEnumerable<ApprovalFlowRequest>> GetBomApprovalFlowAsync(int bomId);
      
    }
}
