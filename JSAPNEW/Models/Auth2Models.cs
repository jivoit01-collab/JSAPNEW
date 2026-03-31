using System.ComponentModel.DataAnnotations;

namespace JSAPNEW.Models
{
    public class Auth2Models
    {
    }

    public class templateCloning
    {
        public int templateId { get; set; }
        public string templateName { get; set; }
        public bool isActive { get; set; }
        public int company { get; set; }
        public int createdBy { get; set; }
        public DateTime createdOn { get; set; }

    }

    public class stageCloning
    {
        public int stageId { get; set; }
        public string stageName { get; set; }
        public string stageDescription { get; set; }
        public int stageApprovalid { get; set; }
        public int stageRejectId { get; set; }
        public int priority { get; set; }
        public string userIds { get; set; }
        public string usernames { get; set; }

    }

    public class approvalCloning
    {
        public int approvalid { get; set; }
        public string approvalName { get; set; }

    }

    public class queryCloning
    {
        public int queryId { get; set; }
        public string queryName { get; set; }
        public string queryText { get; set; }
        public int queryType { get; set; }

    }

    public class summary
    {
        public int totalStages { get; set; }
        public int totalQueries { get; set; }
        public int totalApprovals { get; set; }
    }

    public class templateDataCloning
    {
        public templateCloning template { get; set; }
        public List<stageCloning> stages { get; set; }
        public List<approvalCloning> approvals { get; set; }
        public List<queryCloning> queries { get; set; }
        public summary summary { get; set; }
    }


    public class stagesTemplateModel
    {
        public string stageName { get; set; }
        public string stageDescription { get; set; }
        public int approvalId { get; set; }
        public int rejectid { get; set; }
        public List<int> userIds { get; set; }
        public int priority { get; set; }
    }

    public class CloneTemplateModel
    {
        public int OldTemplateId { get; set; }
        public string NewTemplateName { get; set; }
        public string StagesJson { get; set; }   // JSON array string
        public string ApprovalIds { get; set; }  // comma-separated IDs
        public DateTime NewBudgetDate { get; set; }
        public int CreatedBy { get; set; }
        public int Company { get; set; }
    }

    public class SubBudget2Model
    {
        public string SubBudgetName { get; set; }
        public string Description { get; set; }
    }

    public class CreateBudget2Request
    {
        public int Company { get; set; }
        public string BudgetName { get; set; }
        public string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsActive { get; set; } = true;
        public List<SubBudget2Model> SubBudgets { get; set; }
    }

    public class CreateBudget2Response
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int BudgetId { get; set; }
        public string BudgetName { get; set; }
        public int CompanyId { get; set; }
        public int SubBudgetsCreated { get; set; }
    }
    public class SubBudgetAllocation2Model
    {
        public int SubBudgetId { get; set; }
        public decimal AllocatedAmount { get; set; }
        public string Notes { get; set; }
    }

    public class CreateMonthlyAllocation2Request
    {
        public int BudgetId { get; set; }
        public DateTime AllocationMonth { get; set; } // e.g., "2025-01-01"
        public decimal BudgetAllocatedAmount { get; set; }
        public string BudgetNotes { get; set; }
        public List<SubBudgetAllocation2Model> SubBudgetAllocations { get; set; }
    }

    public class CreateMonthlyAllocation2Response
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int BudgetId { get; set; }
        public DateTime AllocationMonth { get; set; }
        public decimal BudgetAllocatedAmount { get; set; }
        public int SubBudgetAllocationsCreated { get; set; }
        public decimal TotalSubBudgetAmount { get; set; }
    }

    public class BudgetList2Model
    {
        public int BudgetId { get; set; }
        public int Company { get; set; }
        public string BudgetName { get; set; }
        public string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int TotalSubBudgets { get; set; }
    }

    public class BudgetDetails2
    {
        public int BudgetId { get; set; }
        public int Company { get; set; }
        public string BudgetName { get; set; }
        public string BudgetDescription { get; set; }
        public decimal TotalAmount { get; set; }
        public bool BudgetIsActive { get; set; }
        public DateTime BudgetCreatedAt { get; set; }
        public DateTime BudgetUpdatedAt { get; set; }
        public int TotalSubBudgets { get; set; }
        public int TotalMonthlyAllocations { get; set; }
        public decimal TotalAllocatedAmount { get; set; }
    }

    public class SubBudgetDetails2
    {
        public int SubBudgetId { get; set; }
        public int BudgetId { get; set; }
        public string SubBudgetName { get; set; }
        public string SubBudgetDescription { get; set; }
        public bool SubBudgetIsActive { get; set; }
        public DateTime SubBudgetCreatedAt { get; set; }
        public DateTime SubBudgetUpdatedAt { get; set; }
        public string ParentBudgetName { get; set; }
        public int TotalMonthlyAllocations { get; set; }
        public decimal TotalAllocatedAmount { get; set; }
    }

    public class MonthlyAllocationComparison2
    {
        public string AllocationMonth { get; set; }
        public decimal BudgetAllocatedAmount { get; set; }
        public string BudgetNotes { get; set; }
        public decimal SubBudgetAllocatedAmount { get; set; }
        public string SubBudgetNotes { get; set; }
        public string AllocationStatus { get; set; }
    }

    public class BudgetAndSubBudget2Response
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public BudgetDetails2 BudgetInfo { get; set; }
        public SubBudgetDetails2 SubBudgetInfo { get; set; }
        public List<MonthlyAllocationComparison2> MonthlyComparison { get; set; }
    }
    public class BudgetModel2
    {
        public int BudgetId { get; set; }
        public int Company { get; set; }
        public string BudgetName { get; set; }
        public string Description { get; set; }
        public decimal TotalAmount { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int TotalSubBudgets { get; set; }
    }
    public class SubBudgetModel2
    {
        public int SubBudgetId { get; set; }
        public int BudgetId { get; set; }
        public string SubBudgetName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
    public class BudgetWithSubBudgetsResponse
    {
        public BudgetModel2 Budgets { get; set; }
        public List<SubBudgetModel2> SubBudgets { get; set; }
    }
    public class BudgetAttributeModel
    {
        public string? Budget { get; set; }
        public string? SubBudget { get; set; }
        public string? Branch { get; set; }
        public string? ProcesStat { get; set; }
        public string? ObjType { get; set; }
        public string? ObjectName { get; set; }
        public string? AcctCode { get; set; }
        public string? AcctName { get; set; }
        public DateTime? BudgetDate { get; set; }
    }

    public class BudgetAttributeResponse
    {
        public List<dynamic> Data { get; set; }
    }

    public class SubBudgetModelByBudgetId
    {
        public int SubBudgetId { get; set; }
        public int BudgetId { get; set; }
        public string SubBudgetName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public string BudgetName { get; set; }
        public string Company { get; set; }
    }

    public class SubBudgetResponse
    {
        public List<SubBudgetModelByBudgetId> SubBudgets { get; set; } = new();
        public int TotalSubBudgets { get; set; }
    }

    public class WorkflowActionSummaryModel
    {
        public string username { get; set; }
        public int docEntry { get; set; }
        public string branch { get; set; }
        public int stageNumber { get; set; }
        public DateTime creationDate { get; set; }
        public DateTime requestDate { get; set; }
        public DateTime actionDate { get; set; }
        public int daysToProcess { get; set; }
        public decimal totalAmount { get; set; }
        public decimal totalBudget { get; set; }
        public string AcctNames { get; set; }
        public string status { get; set; }
    }

    public class MonthlyAllocationDto
    {
        public int SubBudgetId { get; set; }
        public decimal AllocatedAmount { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateMonthlyAllocationsRequest
    {
        public int BudgetId { get; set; }
        public DateTime AllocationMonth { get; set; } // should be first day of month
        public decimal? BudgetAllocatedAmount { get; set; } = null; // NULL => do not update
        public string? BudgetNotes { get; set; } = null;
        public bool UpdateBudgetNotes { get; set; } = false;
        public List<MonthlyAllocationDto>? SubBudgetAllocations { get; set; } = new List<MonthlyAllocationDto>();
    }

    public class UpdateMonthlyAllocationsResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public int BudgetId { get; set; }
        public DateTime AllocationMonth { get; set; }
        public bool BudgetAllocationUpdated { get; set; }
        public int SubBudgetAllocationsUpdated { get; set; }
    }

    public class BudgetDetailViewModel
    {
        public int BudgetId { get; set; }
        public string BudgetName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
        public decimal totalAmount { get; set; } 
    }

    public class SubBudgetAllocationViewModel
    {
        public int SubBudgetId { get; set; }
        public string SubBudgetName { get; set; }
        public bool IsActive { get; set; }
        public decimal? AllocatedAmount { get; set; }
        public string? Notes { get; set; }
        public DateTime? AllocationMonth { get; set; }
        public string AllocationStatus { get; set; }
    }

    public class BudgetMonthlyAllocationResponse
    {
        public BudgetDetailViewModel? Budget { get; set; }
        public List<SubBudgetAllocationViewModel>? SubBudgets { get; set; }
    }
    public class AllBudgetModel
    {
        public string budget { get; set; }
    }
    public class SubBudgetByBudgetModel
    {
        public string Sub_Budget { get; set; }
    }
    public class PendingBudgetAllocation
    {
        public int id { get; set; }
        public int companyId { get; set; }
        public int budgetId { get; set; }
        public string budgetName { get; set; }
        public int allocationId { get; set; }
        public string alllocationMonth { get; set; }
        public int currentAmount { get; set; }
        public int requestedAmount { get; set; }
        public int amountDiffeence { get; set; }
        public int createdById { get; set; }
        public string createdBy { get; set; }
        public DateTime createdOn { get; set; }
        public int flowId { get; set; }
        public string flowStatus { get; set; }
        public int currentStage { get; set; }
        public int totalStage { get; set; }
    }

    public class ApprovedBudgetAllocation
    {
        public int id { get; set; }
        public int companyId { get; set; }
        public int budgetId { get; set; }
        public string budgetName { get; set; }
        public int allocationId { get; set; }
        public string alllocationMonth { get; set; }
        public int currentAmount { get; set; }
        public int requestedAmount { get; set; }
        public int amountDiffeence { get; set; }
        public int createdById { get; set; }
        public string createdBy { get; set; }
        public DateTime requestCreatedOn { get; set; }
        public DateTime createdOn { get; set; }
        public int flowId { get; set; }
        public string flowStatus { get; set; }
        public int currentStage { get; set; }
        public int totalStage { get; set; }
        public DateTime flowCreatedOn { get; set; }
        public string approvalStatus { get; set; }
        public string approvalRemarks { get; set; }
        public DateTime approvalDate { get; set; }
    }

    public class RejectedBudgetAllocation
    {
        public int id { get; set; }
        public int companyId { get; set; }
        public int budgetId { get; set; }
        public string budgetName { get; set; }
        public int allocationId { get; set; }
        public string alllocationMonth { get; set; }
        public int currentAmount { get; set; }
        public int requestedAmount { get; set; }
        public int amountDiffeence { get; set; }
        public int createdById { get; set; }
        public string createdBy { get; set; }
        public DateTime requestCreatedOn { get; set; }
        public int flowId { get; set; }
        public string flowStatus { get; set; }
        public int currentStage { get; set; }
        public int totalStage { get; set; }
        public DateTime flowCreatedOn { get; set; }
        public string rejectionStatus { get; set; }
        public string rejectionRemarks { get; set; }
        public DateTime rejectionDate { get; set; }
    }

    public class ApproveBudgetAllocationRequest
    {
        public int FlowId { get; set; }
        public int Company { get; set; }
        public int UserId { get; set; }
        public string? Remarks { get; set; }
       // public string Action { get; set; } = "Approve"; // "Approve" or "Reject"
    }

    public class ApproveBudgetAllocationResponse
    {
        public bool Success { get; set; }
        public string? ResultMessage { get; set; }
        public int BudgetAllocationRequestId { get; set; }
        public int CompanyId { get; set; }
        public int FlowId { get; set; }
    }

    public class RejectBudgetAllocationRequest
    {
        public int FlowId { get; set; }
        public int Company { get; set; }
        public int UserId { get; set; }
        public string Remarks { get; set; }
       // public string? Action { get; set; } = "Reject"; // "Reject" or "Cancel"
    }

    public class RejectBudgetAllocationResponse
    {
        public bool Success { get; set; }
        public string? ResultMessage { get; set; }
        public int BudgetAllocationRequestId { get; set; }
        public int CompanyId { get; set; }
        public int FlowId { get; set; }
    }

    public class CreateBudgetAllocationRequestModel
    {
        public int BudgetAllocationId { get; set; }
        public decimal NewAmount { get; set; }
        public int CreatedBy { get; set; }
    }

    public class CreateBudgetAllocationRequestResponse
    {
        public bool Success { get; set; }
        public int NewRequestId { get; set; }
        public string? Message { get; set; }
    }

    public class BudgetAllocationRequestDetail
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int BudgetId { get; set; }
        public string BudgetName { get; set; }
        public int AllocationId { get; set; }
        public DateTime AllocationMonth { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal RequestedAmount { get; set; }
        public decimal AmountDifference { get; set; }
        public int CreatedById { get; set; }
        public string CreatedBy { get; set; }
        public DateTime RequestCreatedOn { get; set; }
        public int FlowId { get; set; }
        public string FlowStatus { get; set; }
        public int CurrentStage { get; set; }
    }

    public class ApprovalStatus
    {
        public int StatusId { get; set; }
        public int FlowId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Status { get; set; }
        public string StatusText { get; set; }
        public string Remarks { get; set; }
        public DateTime? ActionDate { get; set; }
    }

    public class BudgetAllocationResponse
    {
        public BudgetAllocationRequestDetail RequestDetail { get; set; }
        public List<ApprovalStatus> ApprovalHistory { get; set; }
    }

    public class BudgetInsightsMonthlyAllocationModel
    {
        public int TotalPending { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
    }

    public class AllBudgetAllocation
    {
        public int id { get; set; }
        public int companyId { get; set; }
        public int budgetId { get; set; }
        public string budgetName { get; set; }
        public int allocationId { get; set; }
        public string allocationMonth { get; set; }
        public decimal currentAmount { get; set; }
        public decimal requestedAmount { get; set; }
        public decimal amountDifference { get; set; }
        public int createdById { get; set; }
        public string createdBy { get; set; }
        public DateTime createdOn { get; set; }
        public int flowId { get; set; }
        public string flowStatus { get; set; }
        public int currentStage { get; set; }
        public int totalStage { get; set; }
        public string Status { get; set; }
        public string remarks { get; set; }
        public DateTime? actionDate { get; set; }
    }

    public class budgetAllocationFlowModel
    {
        public int stageId { get; set; }
        public string stageName { get; set; }
        public int priority { get; set; }
        public string assignedTo { get; set; }
        public string actionStatus { get; set; }
        public DateTime actionDate { get; set; }
        public string description { get; set; }
        public int approvalRequired { get; set; }
        public int rejectRequired { get; set; }
    }
}
