using JSAPNEW.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface IAuth2Service
    {
        Task<templateDataCloning> GetTemplateDataForCloningAsync(int templateId, int company);
        Task<Response> CloneTemplateWithNewStagesAsync(CloneTemplateModel model);
        Task<CreateBudget2Response> CreateBudgetWithSubBudgetsAsync(CreateBudget2Request request);
        Task<CreateMonthlyAllocation2Response> CreateMonthlyAllocationsAsync(CreateMonthlyAllocation2Request request);
        Task<IEnumerable<BudgetList2Model>> GetAllBudgetsAsync(int company , bool isActive);
        Task<BudgetAndSubBudget2Response> GetBudgetAndSubBudgetDetailsAsync(int budgetId, int subBudgetId);
        Task<BudgetWithSubBudgetsResponse> GetBudgetWithSubBudgetsAsync(int budgetId);
        Task<BudgetAttributeResponse> GetDistinctBudgetAttributesAsync(string mode);
        Task<SubBudgetResponse> GetSubBudgetsByBudgetIdAsync(int budgetId, bool? isActive);
        Task<IEnumerable<WorkflowActionSummaryModel>> GetWorkflowActionSummaryAsync(int templateId, int company);
        Task<UpdateMonthlyAllocationsResponse> UpdateMonthlyAllocationsAsync(UpdateMonthlyAllocationsRequest request);
        Task<BudgetMonthlyAllocationResponse> GetBudgetMonthlyAllocationViewAsync(string budgetName, DateTime allocationMonth);
        Task<IEnumerable<AllBudgetModel>> GetAllTypeBudgetAsync();
        Task<IEnumerable<SubBudgetByBudgetModel>> GetSubBudgetByBudgetAsync(string budget);
        Task<IEnumerable<PendingBudgetAllocation>> GetPendingBudgetAllocationRequestsAsync(int userId, int companyId, string month);
        Task<IEnumerable<ApprovedBudgetAllocation>> GetApprovedBudgetAllocationRequestsAsync(int userId, int companyId, string month);
        Task<IEnumerable<RejectedBudgetAllocation>> GetRejectedBudgetAllocationRequestsAsync(int userId, int companyId, string month);
        Task<ApproveBudgetAllocationResponse> ApproveBudgetAllocationRequestAsync(ApproveBudgetAllocationRequest request);
        Task<RejectBudgetAllocationResponse> RejectBudgetAllocationRequestAsync(RejectBudgetAllocationRequest request);
        Task<CreateBudgetAllocationRequestResponse> CreateBudgetAllocationRequestAsync(CreateBudgetAllocationRequestModel request);
        Task<BudgetAllocationResponse> GetBudgetAllocationRequestDetail(int requestId);
        Task<BudgetInsightsMonthlyAllocationModel> GetBudgetMonthlyAllocationInsights(int userId, int company, string month);
        Task<IEnumerable<AllBudgetAllocation>> GetAllBudgetAllocationRequestsAsync(int userId, int companyId, string month);
        Task<IEnumerable<budgetAllocationFlowModel>> GetBudgetAllocationFlowAsync(int flowId);
    }
}
