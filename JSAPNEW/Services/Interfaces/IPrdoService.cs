using JSAPNEW.Models;
using Microsoft.AspNetCore.Mvc;
using ServiceStack;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace JSAPNEW.Services.Interfaces
{
    public interface IPrdoService
    {

        Task<List<ApprovedProductionOrder>> GetApprovedProductionOrdersAsync(int userId, int company, string month);
        Task<List<PendingProductionOrder>> GetPendingProductionOrdersAsync(int userId, int company, string month);
        Task<List<RejectProductionOrder>> GetRejectedProductionOrdersAsync(int userId, int company, string month);
        Task<List<ProductionOrderInsightModel>> GetProductionOrderInsightAsync(int userId, int company, string month);
        Task<List<ProductionOrderInsightAllModel>> GetProductionOrderInsightAllAsync(int company, string month);
        Task<PrdoModels> ApproveProductionOrderAsync(ProductionOrderApprovalRequest request);
        Task<PrdoModels> RejectProductionOrderAsync(ProductionOrderRejectRequest request);
        Task<List<ProductionOrderDetailDTO>> GetProductionOrderDetailByIdAsync(int productionOrderId, int company);
        Task<List<ProductionOrderApprovalFlowModel>> GetProductionOrderApprovalFlowAsync(int productionOrderId);
        Task<IEnumerable<ItemLocationStockModel>> GetItemLocationStockModelAsync(string ItemCode, string Warehouse, int company);
        Task<IEnumerable<AllProductionOrderDetailDTO>> GetAllProductionOrderAsync(int userId, int company, string month);
        Task<IEnumerable<UserIdsForNotificationModel>> GetProductionUserIdsSendNotificatiosAsync(int FlowId);
        Task<PrdoModels> SendPendingProductionCountNotificationAsync();
    }
}
