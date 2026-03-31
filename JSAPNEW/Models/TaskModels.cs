using System.ComponentModel.DataAnnotations;
using Newtonsoft.Json;

namespace JSAPNEW.Models
{
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
        public bool DeadlineExtended { get; set; }
        public DateTime? OriginalExpectedEndDate { get; set; }
        public string Priority { get; set; }
        public string LastModifiedBy { get; set; }
        public DateTime LastModifiedOn { get; set; }
        public int? DeptId { get; set; }  // Added
        public string DeptName { get; set; }  // Added
    }
    public class TaskCreateDto
    {
        [Required(ErrorMessage = "Task name is required")]
        [StringLength(255, ErrorMessage = "Task name cannot exceed 255 characters")]
        public string TaskName { get; set; }

        [StringLength(4000, ErrorMessage = "Description cannot exceed 4000 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Project name is required")]
        [StringLength(255)]
        public string ProjectName { get; set; }

        [Required(ErrorMessage = "Module name is required")]
        [StringLength(255)]
        public string ModuleName { get; set; }

        [Required(ErrorMessage = "Assigned to is required")]
        [StringLength(100)]
        public string AssignedTo { get; set; }

        [Required(ErrorMessage = "Created by is required")]
        [StringLength(100)]
        public string CreatedBy { get; set; }

        [Required(ErrorMessage = "Start date is required")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Expected end date is required")]
        public DateTime ExpectedEndDate { get; set; }

        [StringLength(20)]
        public string Priority { get; set; } = "MEDIUM";

        public int? DeptId { get; set; }  // Added
    }
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

        public string SortBy { get; set; } = "created_on";
        public string SortOrder { get; set; } = "DESC";
    }
    public class TaskResponseDto
    {
        public string? TaskId { get; set; }
        public string? TaskName { get; set; }
        public string? Description { get; set; }
        public string? ProjectName { get; set; }
        public string? ModuleName { get; set; }

        public string? AssignedToId { get; set; }
        public string? AssignedTo { get; set; }

        public string? CreatedById { get; set; }
        public string? CreatedBy { get; set; }

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

        public int? TotalCount { get; set; }
        public int? TotalPages { get; set; }
        public int? CurrentPage { get; set; }
    }
    public class TaskResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
    public class CompleteTaskRequestDto
    {
        public string TaskId { get; set; }
        public DateTime? CompletionDate { get; set; }
        public string LastModifiedBy { get; set; }
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
    }

}
