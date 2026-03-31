using System.Text.Json.Serialization;

namespace JSAPNEW.Models
{
    public class DashboardModels
    {
        public string Message { get; set; }
        public bool Success { get; set; }
    }
    public class ITStandardsModel
    {
        public string Category { get; set; }
        public string SubCategory { get; set; }
        public string Industry_Standard { get; set; }
        public string Current_State { get; set; }
        public string Gap_Level { get; set; }
        public string Risk_Level { get; set; }
        public string Required_Improvement { get; set; }
        public string Priority { get; set; }
        public string Owner { get; set; }
        public string Budget_Needed { get; set; }
        public string Timeline { get; set; }
        public string Tools_Recommended { get; set; }
        public string Notes { get; set; }
    }
    public class ITStandardsSummary
    {
        public int P1 { get; set; }
        public int P2 { get; set; }
        public int P3 { get; set; }
        public int High { get; set; }
        public int Medium { get; set; }
        public int Low { get; set; }
    }
    public class DashboardStatsDTO
    {
        public int TotalTasks { get; set; }
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public int Critical { get; set; }
        public int HighPriority { get; set; }
        public decimal CompletionRate { get; set; }
        public int Overdue { get; set; }
    }
    public class TaskDTO
    {
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        public string Description { get; set; }
        public string ProjectName { get; set; }
        public string ModuleName { get; set; }
        public int AssignedTo { get; set; }
        public int CreatedBy { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpectedEndDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string Status { get; set; }
        public int IsCompleted { get; set; }
        public string Priority { get; set; }
        public int DaysRemaining { get; set; }
    }
    public class DashboardStatsModel
    {
        public string group_name { get; set; }
        public int Total_Tasks { get; set; }
        public int Completed { get; set; }
        public int In_Progress { get; set; }
        public int Critical { get; set; }
        public int High_Priority { get; set; }
        public decimal Completion_Rate { get; set; }
        public int Overdue { get; set; }
    }
    public class TaskModel
    {
        public string Task_Id { get; set; }
        public string Task_Name { get; set; }
        public string Description { get; set; }
        public string Project_Name { get; set; }
        public string Module_Name { get; set; }
        public int Assigned_To { get; set; }
        public string assignedName { get; set; }
        public int Created_By { get; set; }
        public DateTime Created_On { get; set; }
        public DateTime Start_Date { get; set; }
        public DateTime Expected_End_Date { get; set; }
        public DateTime? Completion_Date { get; set; }
        public string Status { get; set; }
        public int Is_Completed { get; set; }
        public int Deadline_Extended { get; set; }
        public DateTime? Original_Expected_End_Date { get; set; }
        public string Priority { get; set; }
        public int? Last_Modified_By { get; set; }
        public DateTime? Last_Modified_On { get; set; }
        public int Dept_Id { get; set; }
        public int Days_Remaining { get; set; }
    }
    public class StatusDistributionModel
    {
        public string Status { get; set; }
        public int Count { get; set; }
    }
    public class PriorityDistributionModel
    {
        public string Priority { get; set; }
        public int Count { get; set; }
    }
    public class EmployeeStatsModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public int Total { get; set; }
        public int Completed { get; set; }
        public int InProgress { get; set; }
        public int Critical { get; set; }
        public int HighPriority { get; set; }
        public decimal CompletionRate { get; set; }
    }
    public class ProjectListModel
    {
        public string Project_Name { get; set; }
    }
    public class EmployeeListModel
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public int TaskCount { get; set; }
    }
    public class ProjectBreakdownModel
    {
        public string ProjectName { get; set; }
        public int Total { get; set; }
        public int Completed { get; set; }
    }
    public class DailyTaskTrendModel
    {
        public DateTime TaskDate { get; set; }
        public int TasksCreated { get; set; }
        public int TasksCompleted { get; set; }
    }
    public class TaskFilterRequest
    {
        [JsonPropertyName("search")]
        public string? Search { get; set; }

        [JsonPropertyName("priority")]
        public string? Priority { get; set; }

        [JsonPropertyName("status")]
        public string? Status { get; set; }

        [JsonPropertyName("project")]
        public string? Project { get; set; }

        [JsonPropertyName("assigned")]
        public string? AssignedTo { get; set; }

        [JsonPropertyName("dept_id")]
        public string? DeptId { get; set; }

        [JsonPropertyName("start_date")]
        public string? StartDate { get; set; }

        [JsonPropertyName("end_date")]
        public string? EndDate { get; set; }
    }
    public class DashboardByCompanyModel
    {
        public int ProjectID { get; set; }
        public string ProjectName { get; set; }
        public string Priority { get; set; }
        public string ProjectStatus { get; set; }
        public int TotalModules { get; set; }
        public decimal ProjectProgress { get; set; }
        public decimal PlannedHours { get; set; }
        public decimal SpentHours { get; set; }
    }
    public class DashboardMasterModel
    {
        public string Client { get; set; }
        public int TotalProjects { get; set; }
        public int TotalModules { get; set; }
        public decimal OverallProgress { get; set; }
        public decimal TotalPlannedHours { get; set; }
        public decimal TotalSpentHours { get; set; }
    }
    public class DashboardProjectModel
    {
        public int ModuleID { get; set; }
        public string ModuleName { get; set; }
        public decimal Weightage { get; set; }
        public decimal ProgressPercent { get; set; }
        public string Priority { get; set; }
        public string Status { get; set; }
        public decimal PlannedHours { get; set; }
        public decimal SpentHours { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }
    }
    public class GetAllMomModel
    {
        public int momId { get; set; }
        public DateTime MeetingDate { get; set; }
        public string Attendees { get; set; }
        public string Agenda { get; set; }
        public string notes { get; set; }
        public string status { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime UpdatedOn { get; set; }

    }
    public class MomPointRequest
    {
        public DateTime MeetingDate { get; set; }   
        public string Attendees { get; set; }
        public string Agenda { get; set; }
        public string notes { get; set; }
        public string status { get; set; }
    }

    public class MomPointResponse
    {
        public int NewMomId { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }

    public class MomStatusUpdateRequest
    {
        public int MomId { get; set; }
        public string status { get; set; }
    }
    public class MomStatusUpdateResponse
    {
        public int MomId { get; set; }
        public string status { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }
    public class budgetAcctModel
    {
        public string AcctName { get; set; }
    }

    public class budgetBudgetModel
    {
        public string Budget { get; set; }
    }


    public class AllbudgetDataModel
    {
        public string? Branch { get; set; }
        public int DocEntry { get; set; }
        public string? ObjectName { get; set; }
        public int ObjType { get; set; }
        public int LineNum { get; set; }
        public int VisOrder { get; set; }
        public string? AcctCode { get; set; }
        public string? AcctName { get; set; }
        public string? CardCode { get; set; }
        public string? CardName { get; set; }
        public string? EffectMonth { get; set; }
        public string? Budget { get; set; }
        public string? SubBudget { get; set; }
        public string? State { get; set; }
        public DateTime? DocDate { get; set; }
        public DateTime? CreateDate { get; set; }
        public decimal? Amount { get; set; }
        public string? CurrentMonth { get; set; }
        public decimal? CurrentMonthPostedAmount { get; set; }
        public string? BudgetOwner { get; set; }
        public string? OwnerCode { get; set; }
        public string? ApproverName { get; set; }
        public decimal? CurrentMonthBudget { get; set; }
        public string? Status { get; set; }
        public string? UserName { get; set; }
        public DateTime? CreatedDate { get; set; }
        public int? CreateTime { get; set; }
        public string? LineRemarks { get; set; }
        public string? Comments { get; set; }
        public string? ProcessStat { get; set; }
    }
}
