using System.ComponentModel.DataAnnotations;

namespace JSAPNEW.Models
{
    public class LoginRequest
    {
        [Required]
        public string loginUser { get; set; }

        [Required]
        public string password { get; set; }

    }
    public class LoginResponse
    {
        public bool Success { get; set; }
        //public string Token { get; set; }
        public string Message { get; set; }
        public UserDto User { get; set; }
    }
    public class UserRegistrationDTO
    {
        [Required]
        public string firstName { get; set; }
        [Required]
        public string lastName { get; set; }
        [Required]
        public string userEmail { get; set; }
        [Required]
        public string userPhoneNumber { get; set; }
        [Required]
        public string loginUser { get; set; }
        [Required]
        public string deptIds { get; set; }
        [Required]
        public string empId { get; set; }
        [Required]
        public string doj { get; set; }
        [Required]
        public int createdBy { get; set; }
        [Required]
        public string password { get; set; }

    }
    public class UserDto
    {
        public int userId { get; set; }
        public string userName { get; set; }
        public int userPhoneNumber { get; set; }
        public string userEmail { get; set; }
        public string password { get; set; }
        public int isActive { get; set; }
        public string isActiveBy { get; set; }
        public DateTime isActiveOn { get; set; }
        public string loginUser { get; set; }
        public DateTime createdOn { get; set; }
        public string createdBy { get; set; }
        public string Comment { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public int changePassword { get; set; }

    }
    public class VarietyModel
    {
        public String varietyId { get; set; }
        public string varietyName { get; set; }
        public int company { get; set; }
        public List<AnotherData> Data { get; set; }
    }
    public class EffMonthModel
    {
        public String exEffMonthId { get; set; }
        public string exEffMonthName { get; set; }
        public int company { get; set; }
        public List<AnotherData> Data { get; set; }
    }
    public class BudgetModel
    {
        public String budgetId { get; set; }
        public string budgetName { get; set; }
        public int company { get; set; }
        public List<AnotherData> Data { get; set; }
    }
    public class StateModel
    {
        public String StateId { get; set; }
        public string StateName { get; set; }
        public int company { get; set; }
        public List<AnotherData> Data { get; set; }
    }
    public class RoleModel
    {
        public int roleId { get; set; }
        public string roleName { get; set; }
        public string description { get; set; }
        public int company { get; set; }

    }
    public class BranchModel
    {
        public int branchId { get; set; }
        public string branchName { get; set; }

    }

    public class UserBudgetAllocationModel
    {
        public string budgetName { get; set; }
        public string subBudgetName { get; set; }
        public string AllocatedAmount { get; set; }

    }

    public class SubBudgetModel
    {
        public string sBudgetId { get; set; }
        public string sBudgetName { get; set; }
        public int company { get; set; }
        public List<AnotherData> Data { get; set; }
    }
    public class AnotherData
    {
        public string validFrom { get; set; }
        public string validTo { get; set; }
        public string active { get; set; }
        public string cCOwner { get; set; }
        public string uCoOwner { get; set; }
    }
    public class DepartmentModel
    {
        public int deptId { get; set; }
        public string deptName { get; set; }
    }
    public class ReportModel
    {
        public int id { get; set; }
        public string report { get; set; }
        public int view { get; set; }
        public int edit { get; set; }
        public int delete { get; set; }
        public int insert { get; set; }
        public int company { get; set; }

    }
    public class ApprovalModel
    {
        public int id { get; set; }
        public string approvalName { get; set; }
        public int view { get; set; }
        public int edit { get; set; }
        public int delete { get; set; }
        public int insert { get; set; }
        public int company { get; set; }

    }
    public class CompanyModel
    {
        public int id { get; set; }
        public string company { get; set; }
    }
    public class AddStage
    {
        public string stage { get; set; }
        public int approvalId { get; set; }
        public int rejectId { get; set; }
        public string userIds { get; set; }
        public int createdBy { get; set; }
        public int company { get; set; }
        public string description { get; set; }
    }
    public class AssignPermissionDetail
    {
        [Required]
        public string branchIds { get; set; }
        [Required]
        public string varietyIds { get; set; }
        [Required]
        public string budgetIds { get; set; }
        [Required]
        public string sBudgetIds { get; set; }
        [Required]
        public string stateIds { get; set; }
        [Required]
        public string reportIds { get; set; }
        [Required]
        public string approvalIds { get; set; }
        [Required]
        public string fromDate { get; set; }
        [Required]
        public string toDate { get; set; }
        [Required]
        public int userId { get; set; }
        [Required]
        public int company { get; set; }
        [Required]
        public int roleId { get; set; }
        [Required]
        public int adminId { get; set; }
    }
    public class StageModel
    {
        public int id { get; set; }
        public string stage { get; set; }
    }
    public class GetAllUserModel
    {
        public int userId { get; set; }
        public string userName { get; set; }
        public string userPhoneNumber { get; set; }
        public string userEmail { get; set; }
        public string role { get; set; }
        public string department { get; set; }
        public int status { get; set; }
        public string firstName { get; set; }
        public string lastName { get; set; }
        public string empId { get; set; }
        public string dateOfJoining { get; set; }
        public int changePassword { get; set; }
    }
    public class CountApprovalModel
    {
        public int id { get; set; }
        public int approval { get; set; }
    }
    public class CountRejectionModel
    {
        public int id { get; set; }
        public int rejection { get; set; }
    }
    public class UserStatusUpdateModel
    {
        [Required]
        public int UserId { get; set; }
        [Required]
        public int UpdatedBy { get; set; }
        [Required]
        public int CompanyId { get; set; }
        [Required]
        public bool Status { get; set; }
    }
    public class AddTemplateModel
    {
        [Required]
        public string template { get; set; }
        [Required]
        public int createdBy { get; set; }
        [Required]
        public string stageIds { get; set; }
        [Required]
        public string priority { get; set; }
        [Required]
        public string approvalIds { get; set; }
        [Required]
        public int company { get; set; }
        [Required]
        public string queries { get; set; }
    }
    public class AddPageModel
    {
        [Required]
        public string pageName { get; set; }
        [Required]
        public string pageUrl { get; set; }
        [Required]
        public int createdBy { get; set; }
    }
    public class AddRoleModel
    {
        [Required]
        public string role { get; set; }
        [Required]
        public int company { get; set; }
        [Required]
        public int createdBy { get; set; }
        [Required]
        public string description { get; set; }
        [Required]
        public string pageIds { get; set; }
    }
    public class BudgetApprovalCounts
    {
        public int TotalPending { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public int TotalBudget => TotalPending + TotalApproved + TotalRejected;
    }
    public class QueryNameModel
    {
        public int id { get; set; }
        public string name { get; set; }
    }
    public class useraprrovalModel
    {
        public int userId { get; set; }
        public int approvalId { get; set; }
        public int company { get; set; }
        public int view { get; set; }
        public int create { get; set; }
        public int update { get; set; }
        public int delete { get; set; }
        public int status { get; set; }
    }
    public class GetUserBudgetSummaryByTypeModel
    {
        public string BudgetType { get; set; }
        public string TotalBudget { get; set; }
        public string ApprovedAmount { get; set; }
        public string PendingAmount { get; set; }
        public string RejectedAmount { get; set; }
    }
    public class DocEntryModel
    {
        public string Branch { get; set; }
        public int DocEntry { get; set; }
        public string ObjectName { get; set; }
        public int ObjType { get; set; }
        public int LineNum { get; set; }
        public int VisOrder { get; set; }
        public string AcctCode { get; set; }
        public string AcctName { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string EFFECTMONTH { get; set; }
        public string BUDGET { get; set; }
        public string SUB_BUDGET { get; set; }
        public string STATE { get; set; }
        public DateTime DocDate { get; set; }
        public DateTime CreateDate { get; set; }
        public string TrgtPath { get; set; }
        public string FileName { get; set; }
        public string FileExt { get; set; }
        public decimal AMOUNT { get; set; }
        public string CURRENTMONTH { get; set; }
        public decimal Current_month_Posted_Amount { get; set; }
        public int AtcEntry { get; set; }
        public string Budget_Owner { get; set; }
        public string OwnerCode { get; set; }
        public string ApproverName { get; set; }
        public string ApprovalCode { get; set; }
        public decimal Current_month_Budget { get; set; }
        public string Status { get; set; }
        public string U_Name { get; set; }
        public DateTime CreatedDate { get; set; }
        public string CreateTime { get; set; }
        public string LineRemarks { get; set; }
        public string Comments { get; set; }
        public string ProcesStat { get; set; }
        public string ACOMMENT { get; set; }
        public string VCOMMENT { get; set; }
        public string VerifiedStatus { get; set; }
        public string ApprovedStatus { get; set; }
    }
    public class OneDocEntry
    {
        public int DocEntry { get; set; }
        public decimal TotalAmount { get; set; }
    }
    public class QueryRequest
    {
        public string Query { get; set; }
    }
    public class AddQueryModel
    {
        public string query { get; set; }
        public string queryName { get; set; }
        public int type { get; set; }
        public int company { get; set; }

    }
    public class ValidateQueryModel
    {
        public string query { get; set; }
    }
    public class RemarksModel
    {
        public int userId { get; set; }
        public string userName { get; set; }
        public int stageId { get; set; }
        public string status { get; set; }
        public string remark { get; set; }
        public string createdAt { get; set; }
    }
    public class AdminResetPasswordModel
    {
        public int userId { get; set; }

        public int updatedBy { get; set; }

        public string newPassword { get; set; }

    }
    public class TemplateListModel
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int isActive { get; set; }
        public string createdOn { get; set; }
        public string createdBy { get; set; }
    }
    public class OneTemplateDetailModel
    {
        public int tempId { get; set; }
        public string tempName { get; set; }
        public int isActive { get; set; }
        public string tempCreatedOn { get; set; }
        public string createdBy { get; set; }
        public int stageId { get; set; }
        public string stage { get; set; }
        public string description { get; set; }
        public string stageCreatedOn { get; set; }
        public int priority { get; set; }
        public int queryId { get; set; }
        public string queryName { get; set; }
        public string query { get; set; }
        public string approvalName { get; set; }
        public int approvalId { get; set; }
    }
    public class StageListModel
    {
        public int id { get; set; }
        public string stage { get; set; }
        public string createdBy { get; set; }
        public int company { get; set; }
        public string description { get; set; }
    }
    public class OneStageDetailModel
    {
        public int id { get; set; }
        public string stage { get; set; }
        public string createdOn { get; set; }
        public string description { get; set; }
        public string createdBy { get; set; }
        public int approval { get; set; }
        public int rejection { get; set; }
        public string company { get; set; }
        public string userInStage { get; set; }
    }
    public class FlowDocEntryModel
    {
        public int id { get; set; }
        public int userId { get; set; }
        public int stageId { get; set; }
        public string status { get; set; }
        public string remark { get; set; }
        public string createdAt { get; set; }
        public string loginUser { get; set; }
    }
    public class Response
    {
        public string Message { get; set; }
        public bool Success { get; set; }
    }
    public class AddAlternativeUserToStagesModel
    {
        [Required]
        public int userId { get; set; }
        [Required]
        public int alternativeUserId { get; set; }
        [Required]
        public string startDate { get; set; }
        [Required]
        public string endDate { get; set; }
        [Required]
        public string stages { get; set; }
    }
    public class DeactivateDelegationModel
    {
        public int userId { get; set; }
        public int alternativeUserId { get; set; }
    }

    public class GetOneDelegatedUser
    {
        public int stageId { get; set; }
        public string stageName { get; set; }
        public int DelegatedByUserId { get; set; }
        public string DelegatedByUserName { get; set; }

        public int DelegatedToUserId { get; set; }
        public string DelegatedToUserName { get; set; }

        public string tempName { get; set; }

        public int tempId { get; set; }

        public string startDate { get; set; }
        public string endDate { get; set; }

        public int status { get; set; }
    }

    public class GetAllBudgetInsight
    {
        public int UserID { get; set; }
        public string UserName { get; set; }
        public int TotalPending { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public string Type { get; set; }

    }

    public class FlowDocEntryTwoModel
    {
        public string loginUser { get; set; }
        public int priority { get; set; }
        public int stageId { get; set; }
        public string status { get; set; }
        public string remark { get; set; }
        public string createdAt { get; set; }
    }
    public class ApprovalDelegationListModel
    {
        public int userId { get; set; }
        public int alternativeUserId { get; set; }
        public int stageId { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public string createdOn { get; set; }
        public string userName { get; set; }
        public string alternativeUserName { get; set; }
        public int tempId { get; set; }
        public string tempName { get; set; }

    }
    public class TemplateListAccordingToUserModel
    {
        public int stageId { get; set; }
        public string stageName { get; set; }
        public string tempName { get; set; }
        public int tempId { get; set; }
    }
    public class BudgetRequest
    {
        public int? docId { get; set; }
        public string? docIds { get; set; }
        [Required]
        public int company { get; set; }
        [Required]
        public int userId { get; set; }
        public string? remarks { get; set; }
    }
    public class BudgetRequest2
    {
        [Required]
        public string docIds { get; set; }
        [Required]
        public int company { get; set; }
        [Required]
        public int userId { get; set; }
        public string? remarks { get; set; }
    }
    public class MonthlyBudgetModel
    {
        [Required]
        public string budgetCategory { get; set; }
        [Required]
        public string subBudget { get; set; }
        [Required]
        public string month { get; set; }
        [Required]
        public decimal totalAmount { get; set; }
        [Required]
        public int company { get; set; }
    }
    public class ApproveBudgetModel
    {
        public int BudgetId { get; set; }
        public string budget { get; set; }
        public int objType { get; set; }
        public string company { get; set; }
        public string DocEntry { get; set; }
        public string ObjectName { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string DocDate { get; set; }
        public string TotalAmount { get; set; }
        public string ApprovalStatus { get; set; }
        public string ApprovalDate { get; set; }
        public string Description { get; set; }
        public string Status { get; set; } = "Approved";
        public IEnumerable<NextApproverModel> NextApprover { get; set; } // New property
    }
    public class BudgetCategorySummaryDashboardModel
    {
        public string budget { get; set; }
        public string totalAmount { get; set; }
        public string usedAmount { get; set; }
        public string rejectedAmount { get; set; }
        public string remaining { get; set; }
        public string usagePercent { get; set; }
    }
    public class BudgetDetailDTO
    {
        public int BudgetId { get; set; }
        public int DocEntry { get; set; }
        public int TemplateId { get; set; }
        public int TotalStage { get; set; }
        public int CurrentStageId { get; set; }
        public int CurrentStage { get; set; }
        public string CurrentStatus { get; set; }
    }
    public class BudgetLineDetailDTO
    {
        public int ObjType { get; set; }
        public string Company { get; set; }
        public int BudgetAllocationId { get; set; }
        public int LineNum { get; set; }
        public int VisOrder { get; set; }
        public string objectName { get; set; }
        public string AcctCode { get; set; }
        public string AcctName { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public decimal Amount { get; set; }
        public DateTime DocDate { get; set; }
        public string EffectMonth { get; set; }
        public string BudgetOwner { get; set; }
        public decimal Current_month_Budget { get; set; }
        public decimal Current_month_Posted_Amount { get; set; }
        public string LineRemarks { get; set; }
        public string State { get; set; }
        public string Budget { get; set; }
        public string SubBudget { get; set; }
        public string variety { get; set; }
        public string Comments { get; set; }
    }
    public class BudgetResponseDTO
    {
        public BudgetDetailDTO BudgetHeader { get; set; }
        public List<BudgetLineDetailDTO> BudgetLines { get; set; }
        public List<BudgetAttachmentModels> Attachments { get; set; }
    }
    public class CategoryMonthlyBudgetModel
    {
        public int id { get; set; }
        public string budget { get; set; }
        public string month { get; set; }
        public string totalAmount { get; set; }
        public string usedAmount { get; set; }
        public string rejectedAmount { get; set; }
        public string timestamp { get; set; }
    }
    public class PendingBudgetModel
    {
        public int BudgetId { get; set; }
        public string budget { get; set; }
        public int objType { get; set; }
        public string company { get; set; }
        public string DocEntry { get; set; }
        public string ObjectName { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string DocDate { get; set; }
        public string TotalAmount { get; set; }
        public string Status { get; set; } = "Pending";
        public IEnumerable<NextApproverModel> NextApprover { get; set; }
    }
    public class RejectedBudgetModel
    {
        public int BudgetId { get; set; }
        public int objType { get; set; }
        public string company { get; set; }
        public string DocEntry { get; set; }
        public string ObjectName { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string DocDate { get; set; }
        public string TotalAmount { get; set; }
        public string RejectionStatus { get; set; }
        public string RejectedOn { get; set; }
        public string description { get; set; }
        public string Status { get; set; } = "Rejected";
        public IEnumerable<NextApproverModel> NextApprover { get; set; }
    }
    public class NextApproverModel
    {
        public int UserId { get; set; }
        public string LoginUser { get; set; }
    }
    public class BudgetAttachmentModels
    {
        public string Branch { get; set; }
        public int DocEntry { get; set; }
        public string ObjectName { get; set; }
        public int ObjType { get; set; }
        public string trgtPath { get; set; }
        public string FileName { get; set; }
        public string FileExt { get; set; }
        public string AtcEntry { get; set; }
        public string DownloadUrl { get; set; }
    }
    public class BudgetCategoryDropdownModel
    {
        public string budgetId { get; set; }
    }
    public class AllBudgetRequestsModel
    {
        public int BudgetId { get; set; }
        public int objType { get; set; }
        public string company { get; set; }
        public string DocEntry { get; set; }
        public string ObjectName { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string DocDate { get; set; }
        public string TotalAmount { get; set; }
        public string Status { get; set; }
        public IEnumerable<NextApproverModel> NextApprover { get; set; }
    }
    public class BudgetApprovalFlowModel
    {
        public int stageId { get; set; }
        public string stageName { get; set; }
        public int priority { get; set; }
        public string assignedTo { get; set; }
        public string actionStatus { get; set; }
        public string actionDate { get; set; }
        public string description { get; set; }
        public int approvalRequired { get; set; }
        public int rejectionRequired { get; set; }
    }
    public class DelegateApprovalStagesTwoModel
    {
        [Required]
        public int userId { get; set; }
        [Required]
        public int delegatedUserId { get; set; }
        [Required]
        public string stages { get; set; }
        [Required]
        public string startDate { get; set; }
        [Required]
        public string endDate { get; set; }

    }
    public class DelegatedUserListTwoModel
    {
        public int stageId { get; set; }
        public string stageName { get; set; }
        public int DelegatedByUserId { get; set; }
        public string DelegatedByUserName { get; set; }
        public int DelegatedToUserId { get; set; }
        public string DelegatedToUserName { get; set; }
        public int tempId { get; set; }
        public string tempName { get; set; }
        public string startDate { get; set; }
        public string endDate { get; set; }
        public int status { get; set; }
    }
    public class UpdateDelegationDatesTwoModel
    {
        [Required]
        public int userId { get; set; }
        [Required]
        public int delegatedUserId { get; set; }
        [Required]
        public int stageId { get; set; }
        [Required]
        public string newStartDate { get; set; }
        [Required]
        public string newEndDate { get; set; }
    }
    public class UpdateUserStageStatusTwoModel
    {
        [Required]
        public int StageId { get; set; }
        [Required]
        public int UserId { get; set; }
        [Required]
        public int delegatedUserId { get; set; }
        [Required]
        public int Activate { get; set; }
    }

    public class ActiveTemplateModel
    {
        public int tempId { get; set; }
        public string tempname { get; set; }
        public int userId { get; set; }
        public string createdOn { get; set; }
        public string userName { get; set; }
        public string userEmail { get; set; }
    }

    public class BudgetRelatedToTemplateModel
    {
        public int BudgetId { get; set; }
        public int objType { get; set; }
        public string company { get; set; }
        public int DocEntry { get; set; }
        public string ObjectName { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string DocDate { get; set; }
        public string TotalAmount { get; set; }
        public string CURRENTMONTH { get; set; }
    }
    public class UserIdsForNotificationModel
    {
        public string userIdsToApprove { get; set; }
    }
    public class ActiveUserModel
    {
        public int userId { get; set; }
        public int company { get; set; }
    }
    public class SubBudgetCategoryDropdownModel
    {
        public string sBudgetId { get; set; }
    }
    public class BudgetSummaryModel
    {
        public string TotalBudget { get; set; }
        public string ApprovedAmount { get; set; }
        public string RejectedAmount { get; set; }
        public string PendingAmount { get; set; }
    }
    public class CombinedBudgetDTO
    {
        public IEnumerable<AllBudgetRequestsModel> BudgetData { get; set; }
        public IEnumerable<BudgetDetailOnlyDTO> BudgetDetails { get; set; }
    }

    public class BudgetDetailOnlyDTO
    {
        public BudgetDetailDTO BudgetHeader { get; set; }
        public List<BudgetLineDetailDTO> BudgetLines { get; set; }
    }

    public class ApprovedDocEntriesModel
    {
        public int BudgetId { get; set; }
        public int DocEntry { get; set; }
        public int ObjectType { get; set; }
        public string Branch { get; set; }
        public int ApproverUserId { get; set; }
        public string ApproverName { get; set; }
        public string ObjectName { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public DateTime? DocDate { get; set; }
        public string AcctCode { get; set; }
        public string AcctName { get; set; }
        public int LineNum { get; set; }
        public int VisOrder { get; set; }
        public string BUDGET { get; set; }
        public string SUB_BUDGET { get; set; }
        public string STATE { get; set; }
        public string Budget_Owner { get; set; }
        public string OwnerCode { get; set; }
        public string OriginalApproverName { get; set; }
        public string ApprovalCode { get; set; }
        public decimal? RequestAmount { get; set; }
        public decimal? PostedAmount { get; set; }
        public decimal? CurrentMonthBudget { get; set; }
        public decimal? TotalAmount { get; set; }
        public string ApprovalStatus { get; set; }
        public string ApprovalStatusText { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string ApprovalComments { get; set; }
        public string LineRemarks { get; set; }
        public string Comments { get; set; }
        public string ApproverComment { get; set; }
        public string VerifierComment { get; set; }
        public string VerifiedStatus { get; set; }
        public string ApprovedStatus { get; set; }
    }

    public class PendingDocEntriesModel
    {
        public int BudgetId { get; set; }
        public int DocEntry { get; set; }
        public string ObjectType { get; set; }
        public string Branch { get; set; }
        public int AssignedToUserId { get; set; }
        public string AssignedToUser { get; set; }
        public string ObjectName { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public DateTime? DocDate { get; set; }
        public string AcctCode { get; set; }
        public string AcctName { get; set; }
        public int? LineNum { get; set; }
        public int? VisOrder { get; set; }
        public string BUDGET { get; set; }
        public string SUB_BUDGET { get; set; }
        public string STATE { get; set; }
        public string Budget_Owner { get; set; }
        public string OwnerCode { get; set; }
        public string ApproverName { get; set; }
        public string ApprovalCode { get; set; }
        public decimal? RequestAmount { get; set; }
        public decimal? PostedAmount { get; set; }
        public decimal? CurrentMonthBudget { get; set; }
        public string LineRemarks { get; set; }
        public string Comments { get; set; }
        public string ApproverComment { get; set; }
        public string VerifierComment { get; set; }
        public string VerifiedStatus { get; set; }
        public string ApprovedStatus { get; set; }
        public string CURRENTMONTH { get; set; }
    }

    public class RejectedDocEntriesModel
    {
        public int BudgetId { get; set; }
        public int DocEntry { get; set; }
        public int ObjectType { get; set; }
        public string Branch { get; set; }
        public int RejectorUserId { get; set; }
        public string RejectorName { get; set; }
        public string ObjectName { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public DateTime? DocDate { get; set; }
        public string AcctCode { get; set; }
        public string AcctName { get; set; }
        public int LineNum { get; set; }
        public int VisOrder { get; set; }
        public string BUDGET { get; set; }
        public string SUB_BUDGET { get; set; }
        public string STATE { get; set; }
        public string Budget_Owner { get; set; }
        public string OwnerCode { get; set; }
        public string OriginalApproverName { get; set; }
        public string ApprovalCode { get; set; }
        public decimal? RequestAmount { get; set; }
        public decimal? PostedAmount { get; set; }
        public decimal? CurrentMonthBudget { get; set; }
        public decimal? TotalAmount { get; set; }
        public string RejectionStatus { get; set; }
        public string RejectionStatusText { get; set; }
        public DateTime? RejectionDate { get; set; }
        public string RejectionReason { get; set; }
        public string LineRemarks { get; set; }
        public string Comments { get; set; }
        public string ApproverComment { get; set; }
        public string VerifierComment { get; set; }
        public string VerifiedStatus { get; set; }
        public string ApprovedStatus { get; set; }
    }
    public class EditUserViewModel
    {
        public GetAllUserModel SelectedUser { get; set; }  // Just one user
        public dynamic Permissions { get; set; }           // Use dynamic if structure varies
    }

    //for web
    public class UserUpdateDTO
    {
        public int UserId { get; set; }
        public string UserPhoneNumber { get; set; }
        public string UserEmail { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string EmpId { get; set; }
        public string DeptIds { get; set; }
        public int UpdatedBy { get; set; }
    }
    public class ApiResponse
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; }
    }
    public class UserResponseDto
    {
        public int UserId { get; set; }
        public int DeptId { get; set; }
        public string? DeptName { get; set; }
        public string? UserPhoneNumber { get; set; }
        public string? UserEmail { get; set; }
        public string? LoginUser { get; set; }
        public DateTime? CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public string? Comment { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName => $"{FirstName} {LastName}".Trim();
        public string? EmpId { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public bool ChangePassword { get; set; }
    }
    public class UserListResponseDto
    {
        public List<UserResponseDto> Users { get; set; }
        public int TotalCount { get; set; }

        public UserListResponseDto()
        {
            Users = new List<UserResponseDto>();
        }
    }

}