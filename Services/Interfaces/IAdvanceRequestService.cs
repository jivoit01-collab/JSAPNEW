using JSAPNEW.Models;
using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Services.Interfaces
{
    public interface IAdvanceRequestService
    {
        Task<IEnumerable<AdvanceRequestModels>> AdvancePaymentRequestAsync(string IN_BRANCH, string IN_TYPE);
        Task<VendorExpenseResponse> InsertVendorExpenseAsync(VendorExpenseRequest request, List<FileDetails> fileDetails);
        Task<IEnumerable<ExpensesModels>> GetPendingExpensesAsync(int userId, int companyId);
        Task<IEnumerable<ExpensesModels>> GetApprovedExpensesAsync(int userId, int companyId);
        Task<IEnumerable<ExpensesModels>> GetRejectedExpensesAsync(int userId, int companyId);
        Task<IEnumerable<ExpenseInsightsModels>> GetExpenseInsightsAsync(int userId, int companyId);
        Task<IEnumerable<ExpensesModels>> GetTotalExpensesAsync(int userId, int companyId);
        Task<ExpenseDetailsResponse> GetExpenseDetailsByFlowIdAsync(int flowId, IUrlHelper urlHelper);
        Task<IEnumerable<ApprovalIdsModel>> GetApprovalUserIdsAsync(int advPayId);
        Task<(bool IsSuccess, string Message)> ApproveAdvancePaymentAsync(ApproveAdvPayRequest request);
        Task<(bool Success, string Message)> RejectAdvancePaymentAsync(RejectAdvPayRequest request);
        //Task<AdvanceResponse> RejectAdvancePaymentAsync(RejectAdvPayRequest request);
        Task<AdvanceResponse> DeleteVendorExpenseAsync(int id, int deletedBy);
        Task<AdvanceResponse> UpdateVendorExpenseAsync(VendorExpenseUpdateModel request);
        Task<IEnumerable<DepartmentsModel>> GetDepartmentsAsync();
        Task<IEnumerable<AttachmentModels>> GetAdvanceAttachmentsAsync(int id);
        Task<IEnumerable<GetCustomerBalanceByBranchModel>> GetCustomerBalanceByBranchAsync(string IN_BRANCH, string IN_CARDCODE);
        Task<IEnumerable<ExpensesModels>> GetExpenseByUserIdAsync(int userId, int company,string month);
        Task<(bool Success, string Message)> UpdateAmountAsync(int userId, int expenseId, float amount);
        Task<IEnumerable<StageDetailsModel>> GetExpenseApprovalFlowAsync(int flowId);
        Task<AdvanceResponse> SendPaymentPendingCountNotificationAsync();
        Task<(int userId, string userName)> GetAdvCreatedBy(int advPayId);
        Task<List<OilBusinessPartnerModel>> GetOilBusinessPartnersAsync(string type);
        Task<List<BevBusinessPartnerModel>> GetBevBusinessPartnersAsync(string type);
        Task<List<PurchaseOrderModel>> GetOpenOrdersByVendorAsync(int company, string cardCode);
        Task<List<PurchaseOrderModel>> GetCreatedOpenOrdersByVendorAsync(int company, string cardCode);
        Task<IEnumerable<PoListModel>> GetPoListAsync(int company, string code);
    }
}
