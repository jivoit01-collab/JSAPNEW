using JSAPNEW.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface IDashboardService
    {
        Task<IEnumerable<ITStandardsModel>> GetITStandardsMasterAsync();
        Task<ITStandardsSummary> GetITStandardsSummaryAsync();
        Task<IEnumerable<ITStandardsModel>> GetITStandardsByPriorityAsync(string priority);

        Task<IEnumerable<DashboardStatsModel>> GetDashboardStatsAsync(string group_by, int? deptId = null);
        Task<IEnumerable<TaskModel>> GetAllTasksAsync(TaskFilterRequest filter);
        Task<TaskModel> GetTaskByIdAsync(string taskId);
        Task<IEnumerable<StatusDistributionModel>> GetStatusDistributionAsync(int? deptId = null);
        Task<IEnumerable<PriorityDistributionModel>> GetPriorityDistributionAsync(int? deptId = null);
        Task<IEnumerable<EmployeeStatsModel>> GetEmployeeStatsAsync(int? deptId = null);
        Task<IEnumerable<TaskModel>> GetEmployeeTasksAsync(int employeeId);
        Task<IEnumerable<ProjectListModel>> GetProjectListAsync();
        Task<IEnumerable<EmployeeListModel>> GetEmployeeListAsync(int? deptId = null);
        Task<IEnumerable<ProjectBreakdownModel>> GetProjectBreakdownAsync(int? deptId = null);
        Task<IEnumerable<DailyTaskTrendModel>> GetDailyTaskTrendAsync(int days = 30, int? deptId = null);
        Task<IEnumerable<TaskModel>> GetOverdueTasksAsync(int? deptId = null);
        Task<IEnumerable<DashboardByCompanyModel>> GetDashboardByCompanyAsync(string clientName);
        Task<IEnumerable<DashboardMasterModel>> GetDashboardMasterAsync();
        Task<IEnumerable<DashboardProjectModel>> GetDashboardProjectAsync(int projectId);
        Task<IEnumerable<GetAllMomModel>> GetAllMoMAsync();
        Task<IEnumerable<MomStatusUpdateResponse>> UpdateMoMStatusAsync (MomStatusUpdateRequest request);
        Task<IEnumerable<MomPointResponse>> AddMoMPointAsync(MomPointRequest request);
        /////Avtar sir dashboard
        Task<IEnumerable<AllbudgetDataModel>> GetAllBudgetDataAsync(string? branch);
        Task<IEnumerable<budgetAcctModel>> GetUniqueAccounts(string? branch);
        Task<IEnumerable<budgetBudgetModel>> GetUniqueBudgets(string? branch);
    } 
}
