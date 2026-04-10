using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace JSAPNEW.Models
{
    // ============================================================
    // Entity
    // ============================================================
    public class TaskEntity
    {
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        public string Description { get; set; }
        public string ProjectName { get; set; }
        public string ModuleName { get; set; }
        public string AssignedTo { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpectedEndDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string Status { get; set; }
        public bool IsCompleted { get; set; }
        public bool? IsDeleted { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public string? DeletedReason { get; set; }
        public bool DeadlineExtended { get; set; }
        public DateTime? OriginalExpectedEndDate { get; set; }
        public string Priority { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTime LastModifiedOn { get; set; }
        public int? DeptId { get; set; }
        public string DeptName { get; set; }

        // Hierarchy fields
        public int? AssignedToEmployeeId { get; set; }
        public int? CreatedByEmployeeId { get; set; }
        public int? AssignedByEmployeeId { get; set; }
        public string TaskType { get; set; }
        public int? SubDepartmentId { get; set; }
        public string Remarks { get; set; }
        public int PercentComplete { get; set; }
        public string? Slot { get; set; }
    }

    // ============================================================
    // Create Task DTO
    // ============================================================
    public class TaskCreateDto
    {
        [Required(ErrorMessage = "Task name is required")]
        [StringLength(255, ErrorMessage = "Task name cannot exceed 255 characters")]
        public string TaskName { get; set; }

        [StringLength(4000, ErrorMessage = "Description cannot exceed 4000 characters")]
        public string? Description { get; set; }

        [StringLength(255)]
        public string ProjectName { get; set; }

        [StringLength(255)]
        public string ModuleName { get; set; }

        [Required(ErrorMessage = "Assigned to employee ID is required")]
        public int AssignedToEmployeeId { get; set; }

        [Required(ErrorMessage = "Created by employee ID is required")]
        public int CreatedByEmployeeId { get; set; }

        public int? AssignedByEmployeeId { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Expected end date is required")]
        public DateTime ExpectedEndDate { get; set; }

        [StringLength(20)]
        public string Priority { get; set; } = "MEDIUM";

        public int? DeptId { get; set; }
        public int? SubDepartmentId { get; set; }

        public string TaskType { get; set; } = "SELF"; // SELF or ASSIGNED
        public string? Slot { get; set; }

        // Keep backward compat
        public string? AssignedTo { get; set; }
        public string? CreatedBy { get; set; }
    }

    // ============================================================
    // Filter DTO (Hierarchy-aware)
    // ============================================================
    public class TaskFilterDto
    {
        public int Page { get; set; } = 1;
        public int Limit { get; set; } = 20;

        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? ProjectName { get; set; }
        public string? ModuleName { get; set; }
        public string? AssignedTo { get; set; }
        public string? CreatedBy { get; set; }
        public int? DeptId { get; set; }
        public int? SubDepartmentId { get; set; }
        public string? TaskType { get; set; }

        // Hierarchy filters
        public int? AssignedToEmployeeId { get; set; }
        public int? CreatedByEmployeeId { get; set; }
        public int? EmployeeId { get; set; }       // Current logged-in employee
        public int? RoleTypeId { get; set; }        // 1=HOD, 2=SubHOD, 3=Executive
        public string ViewMode { get; set; } = "OWN"; // OWN, TEAM, ALL

        public string SortBy { get; set; } = "created_on";
        public string SortOrder { get; set; } = "DESC";
    }

    public class TaskReportRequestDto
    {
        public string ReportType { get; set; } = "WEEKLY";
        public string? Status { get; set; }
        public string? Priority { get; set; }
        public string? ProjectName { get; set; }
        public string? ModuleName { get; set; }
        public string? AssignedTo { get; set; }
        public string? CreatedBy { get; set; }
        public int? DeptId { get; set; }
        public int? SubDepartmentId { get; set; }
        public string? TaskType { get; set; }
        public int? AssignedToEmployeeId { get; set; }
        public int? CreatedByEmployeeId { get; set; }
        public int? EmployeeId { get; set; }
        public int? RoleTypeId { get; set; }
        public string ViewMode { get; set; } = "OWN";
        public string SortBy { get; set; } = "created_on";
        public string SortOrder { get; set; } = "DESC";
    }

    // ============================================================
    // Response DTOs
    // ============================================================
    public class TaskResponseDto
    {
        public string? TaskId { get; set; }
        public string? TaskName { get; set; }
        public string? Description { get; set; }
        public string? ProjectName { get; set; }
        public string? ModuleName { get; set; }

        public int? AssignedToEmployeeId { get; set; }
        public string? AssignedTo { get; set; }
        public string? AssignedToCode { get; set; }

        public int? CreatedByEmployeeId { get; set; }
        public string? CreatedBy { get; set; }

        public int? AssignedByEmployeeId { get; set; }
        public string? AssignedByName { get; set; }

        // Keep old fields for backward compat
        public string? AssignedToId { get; set; }
        public string? CreatedById { get; set; }

        public DateTime CreatedOn { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime ExpectedEndDate { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string? Status { get; set; }
        public bool IsCompleted { get; set; }
        public bool DeadlineExtended { get; set; }
        public DateTime? OriginalExpectedEndDate { get; set; }
        public string? Priority { get; set; }
        public string? LastModifiedBy { get; set; }
        public DateTime LastModifiedOn { get; set; }
        public int? DeptId { get; set; }
        public string? DeptName { get; set; }
        public int? SubDepartmentId { get; set; }
        public string? SubDepartmentName { get; set; }
        public string? TaskType { get; set; }
        public int PercentComplete { get; set; }
        public string? Slot { get; set; }
        public string? Remarks { get; set; }
        public string? AssignedToRole { get; set; }
        public bool? IsDeleted { get; set; }
        public int? DeletedBy { get; set; }
        public DateTime? DeletedOn { get; set; }
        public string? DeletedReason { get; set; }

        public int? TotalCount { get; set; }
        public int? TotalPages { get; set; }
        public int? CurrentPage { get; set; }
    }

    public class TaskResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class DeleteTaskRequestDto
    {
        public string TaskId { get; set; } = string.Empty;
        public int? DeletedByEmployeeId { get; set; }
        public string? DeletedByName { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedReason { get; set; }
    }

    public class TaskReportResponseDto
    {
        public string ReportType { get; set; } = string.Empty;
        public string PeriodLabel { get; set; } = string.Empty;
        public DateTime FromDate { get; set; }
        public DateTime ToDate { get; set; }
        public int TotalTasks { get; set; }
        public int PendingTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public decimal CompletionRate { get; set; }
        public List<TaskResponseDto> Tasks { get; set; } = new();
    }

    // ============================================================
    // Complete Task DTOs
    // ============================================================
    public class CompleteTaskRequestDto
    {
        public string TaskId { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string? LastModifiedBy { get; set; }
        public int? LastModifiedByEmployeeId { get; set; }
        public string? Remarks { get; set; }
    }

    public class CompleteTaskResponseDto
    {
        public string TaskId { get; set; }
        public string TaskName { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? CompletionDate { get; set; }
        public DateTime? ExpectedEndDate { get; set; }
        public string Status { get; set; }
        public int DaysDifference { get; set; }
        public string? Remarks { get; set; }
    }

    // ============================================================
    // Dashboard DTOs
    // ============================================================
    public class TaskDashboardRequestDto
    {
        [Required]
        public int EmployeeId { get; set; }
        [Required]
        public int RoleTypeId { get; set; } // 1=HOD, 2=SubHOD, 3=Executive
    }

    public class TaskCountDto
    {
        public string Section { get; set; }
        public int TotalTasks { get; set; }
        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int OverdueCount { get; set; }
        public int SelfTaskCount { get; set; }
        public int AssignedTaskCount { get; set; }
    }

    public class TeamMemberTaskCountDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public int RoleTypeId { get; set; }
        public int TotalTasks { get; set; }
        public int PendingCount { get; set; }
        public int InProgressCount { get; set; }
        public int CompletedCount { get; set; }
        public int OverdueCount { get; set; }
    }

    public class TaskDashboardResponseDto
    {
        public TaskCountDto MyTasks { get; set; }
        public TaskCountDto TeamTasks { get; set; }
        public List<TeamMemberTaskCountDto> TeamMemberBreakdown { get; set; } = new();
    }

    // ============================================================
    // Progress Update DTOs
    // ============================================================
    public class TaskProgressCreateDto
    {
        [Required]
        public string TaskId { get; set; }
        [Required]
        public int EmployeeId { get; set; }
        [StringLength(2000)]
        public string? UpdateText { get; set; }
        public int? PercentComplete { get; set; }
    }

    public class TaskProgressResponseDto
    {
        public int UpdateId { get; set; }
        public string TaskId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string? EmployeeCode { get; set; }
        public string UpdateText { get; set; }
        public int PercentComplete { get; set; }
        public DateTime UpdateDate { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class TaskLogEntryDto
    {
        public string LogType { get; set; } = string.Empty;
        public string ActionType { get; set; } = string.Empty;
        public string? ActorName { get; set; }
        public string? FromEmployeeName { get; set; }
        public string? ToEmployeeName { get; set; }
        public string? Notes { get; set; }
        public int? PercentComplete { get; set; }
        public DateTime ActionDate { get; set; }
    }

    public class TaskDetailsResponseDto
    {
        public TaskResponseDto? Task { get; set; }
        public List<TaskLogEntryDto> Logs { get; set; } = new();
    }

    // ============================================================
    // Reassign Task DTO
    // ============================================================
    public class ReassignTaskRequestDto
    {
        [Required]
        public string TaskId { get; set; }
        [Required]
        public int NewAssignedToEmployeeId { get; set; }
        [Required]
        public int ReassignedByEmployeeId { get; set; }
        public string? Notes { get; set; }
    }
}
