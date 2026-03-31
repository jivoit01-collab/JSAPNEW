using JSAPNEW.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using ServiceStack;
using ServiceStack.Web;
using System.ComponentModel.Design;
using System.Net.NetworkInformation;

namespace JSAPNEW.Services.Interfaces
{
    public interface IUserService
    {
        Task<LoginResponse> ValidateUserAsync(LoginRequest request);
        Task<UserDto> GetUserByIdAsync(int userId);
        Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request);
        Task<ChangePasswordResponse> ChangePasswordAsync2(ChangePasswordRequest2 request);
        Task<bool> ValidateCurrentPasswordAsync(int userId, string currentPassword);
        Task<int> RegisterUserAsync(UserRegistrationDTO request);
        Task<IEnumerable<VarietyModel>> GetVarietyAsync(int company);
        Task<IEnumerable<EffMonthModel>> GeteffMonthAsync(int company);
        Task<IEnumerable<BudgetModel>> GetBudgetAsync(int company);
        Task<IEnumerable<SubBudgetModel>> GetSubBudgetAsync(int company);
        Task<IEnumerable<StateModel>> GetStateAsync(int company);
        Task<IEnumerable<RoleModel>> GetRoleAsync(int company);
        Task<IEnumerable<BranchModel>> GetBranchAsync(int company);
        Task<IEnumerable<DepartmentModel>> GetDepartmentAsync(int company);
        Task<IEnumerable<ReportModel>> GetReportAsync(int company);
        Task<IEnumerable<GetAllUserModel>> GetUserAsync(int company);
        Task<IEnumerable<ApprovalModel>> GetApprovalAsync(int company);
        Task<IEnumerable<CompanyModel>> GetCompanyAsync(int userId);
        Task<int> AddStageAsync(AddStage StageData);
        Task<int> UserAssignPermissionAsync(AssignPermissionDetail Data);
        Task<IEnumerable<StageModel>> GetStageAsync(int company);
        Task<IEnumerable<GetAllUserModel>> GetAllUserAsync(int company);
        Task<IEnumerable<GetAllUserModel>> GetSelectedUserAsync(int company, int userId);
        Task<IEnumerable<CountApprovalModel>> GetCountApprovalAsync();
        Task<IEnumerable<CountRejectionModel>> GetCountRejectionAsync();
        Task<IEnumerable<GetAllUserModel>> GetUserNotRegisterInCompanyAsync(int company);
        Task<bool> UpdateUserStatus(UserStatusUpdateModel model);
        Task<int> GetAddTemplateAsync(AddTemplateModel request);
        Task<int> GetAddPageAsync(AddPageModel request);
        Task<int> GetAddRoleAsync(AddRoleModel request);
        Task<IEnumerable<BudgetApprovalCounts>> GetAllBudgetApprovalCountsAsync(int userId, int company, string month);
        Task<int> UpdateBudgetAsync(int userId, int updatedBy, string budgetId, bool status, int company);
        Task<IEnumerable<QueryNameModel>> GetQueryNameAsync(string type, int company);
        Task<int> UpdateUserApprovalAsync(useraprrovalModel request);
        Task<int> UpdateStateAsync(int userId, int updatedBy, string stateId, bool status, int company);
        Task<int> UpdateSubBudgetAsync(int userId, int updatedBy, string subBudgetId, bool status, int company);
        Task<int> UpdateFromToDateAsync(int userId, int updatedBy, DateTime toDate, DateTime fromDate, int company);
        Task<int> UpdateBranchAsync(int userId, int updatedBy, string branchId, bool status, int company);
        Task<int> UpdateReportAsync(int userId, int updatedBy, string reportId, bool status, int company);
        Task<int> UpdateVarietyAsync(int userId, int updatedBy, string varietyId, bool status, int company);
        Task<int> UpdateUserRoleAsync(int userId, int roleId, int company);
        Task<IEnumerable<PendingBudgetModel>> GetPendingBudgetWithDetailsAsync(int userId, int company, string month);

        Task<IEnumerable<PendingBudgetModel>> GetPendingBudgetWithDetailsAsync2(int userId, int company, string month);

        Task<IEnumerable<ApproveBudgetModel>> GetApprovedBudgetWithDetailsAsync(int userId, int company, string month);

        Task<IEnumerable<ApproveBudgetModel>> GetApprovedBudgetWithDetailsAsync2(int userId, int company, string month);
        Task<IEnumerable<NextApproverModel>> GetNextApproverAsync(int budgetId);
        Task<IEnumerable<RejectedBudgetModel>> GetRejectedBudgetWithDetailsAsync(int userId, int company, string month);
        Task<IEnumerable<RejectedBudgetModel>> GetRejectedBudgetWithDetailsAsync2(int userId, int company, string month);

        Task<IEnumerable<AllBudgetRequestsModel>> GetAllBudgetWithDetailsAsync(int userId, int company, string month);
        Task<IEnumerable<GetUserBudgetSummaryByTypeModel>> GetUserBudgetSummaryByTypeAsync(int userId, int company, string budgetType);
        Task<IEnumerable<DocEntryModel>> getDocEntryDataAsync(int docEntry, int userId, int company);
        Task<Response> ApproveBudgetAsync(BudgetRequest2 request);
       // Task<Response> ApproveBudgetAsync2(BudgetRequest2 request);
        Task<Response> RejectBudgetAsync(BudgetRequest request);
        Task<object> GetAllPermissionsOfOneUserAsync(int userId, int company);
        Task<object> GetUserBudgetTypesAsync(int userId, int company);
        Task<IEnumerable<OneDocEntry>> AmountOfOneDocEntryAsync(int docEntry, int userId, int company);
        Task<string> ExecuteUserQueryAsync(string query);
        Task<int> GetAddQueryAsync(AddQueryModel request);
        Task<string> GetValidateQueryAsync(string query);
        Task<IEnumerable<RemarksModel>> getRemarksDataAsync(int docEntry, int company);
        Task<Response> ResetAdminPasswordAsync(AdminResetPasswordModel request);
        Task<IEnumerable<TemplateListModel>> GetTemplateListAsync(int company);
        Task<object> GetOneTemplateDetailAsync(int tempId, int company);
        Task<IEnumerable<StageListModel>> GetStageListAsync(int company);
        Task<object> GetOneStageDetailAsync(int stageId, int company);
        Task<IEnumerable<FlowDocEntryModel>> GetFlowDocEntryAsync(int docEntry);
        Task<IEnumerable<Response>> AddAlternativeUserToStagesAsync(AddAlternativeUserToStagesModel request);
        Task<IEnumerable<Response>> DeactivateDelegationAsync(DeactivateDelegationModel request);
        Task<IEnumerable<FlowDocEntryTwoModel>> GetFlowDocEntryTwoAsync(int docEntry);
        Task<IEnumerable<UserBudgetAllocationModel>> GetUserBudgetAllocationAsync(int userId, int company, string month);
        Task<BudgetResponseDTO> GetBudgetDetailByIdAsync2(int budgetId);
        Task<IEnumerable<ApprovalDelegationListModel>> GetApprovalDelegationListAsync();
        Task<IEnumerable<TemplateListAccordingToUserModel>> GetTemplateListAccordingToUserAsync(int company, int userId);
        Task<Response> CreateCategoryMonthlyBudgetAsync(MonthlyBudgetModel request);
        Task<IEnumerable<BudgetCategorySummaryDashboardModel>> GetBudgetCategorySummaryDashboardAsync(string month, int company);
        // Task<BudgetResponseDTO> GetBudgetDetailByIdAsync(int budgetId,int company, IUrlHelper urlHelper);
        Task<BudgetResponseDTO> GetBudgetDetailByIdAsync(int budgetId, IUrlHelper urlHelper);
        Task<BudgetResponseDTO> GetBudgetDetailByIdAsyncv2(int budgetId, int company, IUrlHelper urlHelper);
        Task<IEnumerable<CategoryMonthlyBudgetModel>> GetCategoryMonthlyBudgetAsync(string budgetCategory, string subBudget, string month, int company);
        Task<IEnumerable<BudgetAttachmentModels>> GetBudgetAttachmentsAsync(int budgetId);
        Task<IEnumerable<BudgetCategoryDropdownModel>> BudgetCategoryDropdownAsync(int userId, int company);
        Task<IEnumerable<BudgetApprovalFlowModel>> GetBudgetApprovalFlowAsync(int budgetId);
        Task<IEnumerable<Response>> DelegateApprovalStagesTwoAsync(DelegateApprovalStagesTwoModel request);
        Task<IEnumerable<DelegatedUserListTwoModel>> GetDelegatedUserListTwoAsync();
        Task<IEnumerable<Response>> UpdateDelegationDatesTwoAsync(UpdateDelegationDatesTwoModel request);
        Task<IEnumerable<Response>> UpdateUserStageStatusTwoAsync(UpdateUserStageStatusTwoModel request);
        Task<IEnumerable<ActiveTemplateModel>> GetActiveTemplateSync(int company);
        Task<IEnumerable<BudgetRelatedToTemplateModel>> GetBudgetRelatedToTemplateAsync(int templateId);
        Task<IEnumerable<GetOneDelegatedUser>> GetOneDelegatedUserAsync(int stageId, int delegatedBy, int delegatedTo);
        Task<IEnumerable<GetAllBudgetInsight>> GetBudgetInsightAsync(int company, string month);
        Task<IEnumerable<UserIdsForNotificationModel>> GetUserIdsSendNotificatiosAsync(int budgetId);
        Task<Response> SendPendingCountNotificationAsync();
        Task<IEnumerable<ActiveUserModel>> GetActiveUser();
        Task<IEnumerable<SubBudgetCategoryDropdownModel>> GetSubBudgetCategoryDropdownAsync(int userId, int company);
        Task<IEnumerable<BudgetSummaryModel>> GetBudgetSummaryAsync(int userId, string budgetCategory, string subBudget, string month, int company);
        Task<CombinedBudgetDTO> GetCombinedBudgetsAsync(int userId, int company, string month);
        Task<int> GetDocIdsUsingDocEntryAsync(int docEntry);
        Task<IEnumerable<ApprovedDocEntriesModel>> GetApprovedDocEntriesAsync(int company, int docEntry);
        Task<IEnumerable<PendingDocEntriesModel>> GetPendingDocEntriesAsync(int company, int docEntry);
        Task<IEnumerable<RejectedDocEntriesModel>> GetRejectedDocEntriesAsync(int company, int docEntry);

        //for web
        Task<Response> ValidateUsernameAsync(string username);
        Task<ApiResponse> UpdateUserInfoAsync(UserUpdateDTO model);
        Task<UserListResponseDto> GetUsersByDepartmentId(int deptId);
    }
}