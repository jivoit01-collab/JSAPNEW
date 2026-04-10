using Org.BouncyCastle.Bcpg.OpenPgp;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace JSAPNEW.Models
{
    #region Database Entities

    [Table("Departments")]
    public class Department
    {
        [Key] public int DepartmentId { get; set; }
        [Required, MaxLength(100)] public string DepartmentName { get; set; }
        [MaxLength(20)] public string DepartmentCode { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public DateTime? ModifiedOn { get; set; }
        public virtual ICollection<SubDepartment> SubDepartments { get; set; }
        public virtual ICollection<Employee> Employees { get; set; }
    }

    [Table("SubDepartments")]
    public class SubDepartment
    {
        [Key] public int SubDepartmentId { get; set; }
        [Required] public int DepartmentId { get; set; }
        [Required, MaxLength(100)] public string SubDepartmentName { get; set; }
        [MaxLength(20)] public string SubDepartmentCode { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public DateTime? ModifiedOn { get; set; }
        [ForeignKey("DepartmentId")] public virtual Department Department { get; set; }
    }

    [Table("RoleTypes")]
    public class RoleType
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)] public int RoleTypeId { get; set; }
        [Required, MaxLength(50)] public string RoleName { get; set; }
        public int RoleLevel { get; set; }
        [MaxLength(200)] public string Description { get; set; }
    }

    [Table("ReportingTypes")]
    public class ReportingType
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.None)] public int ReportingTypeId { get; set; }
        [Required, MaxLength(50)] public string ReportingTypeName { get; set; }
        [MaxLength(200)] public string Description { get; set; }
    }

    [Table("Employees")]
    public class Employee
    {
        [Key] public int EmployeeId { get; set; }
        [MaxLength(20)] public string EmployeeCode { get; set; }
        [Required, MaxLength(100)] public string EmployeeName { get; set; }
        [MaxLength(100), EmailAddress] public string Email { get; set; }
        [MaxLength(20), Phone] public string Phone { get; set; }
        [MaxLength(100)] public string Designation { get; set; }
        [Required] public int RoleTypeId { get; set; }
        public int? PrimaryDepartmentId { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public decimal? Salary { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public DateTime? ModifiedOn { get; set; }
        [ForeignKey("RoleTypeId")] public virtual RoleType RoleType { get; set; }
        [ForeignKey("PrimaryDepartmentId")] public virtual Department PrimaryDepartment { get; set; }
        public virtual ICollection<EmployeeReportingRelationship> ReportingTo { get; set; }
        public virtual ICollection<EmployeeReportingRelationship> DirectReports { get; set; }
    }

    [Table("EmployeeReportingRelationships")]
    public class EmployeeReportingRelationship
    {
        [Key] public int RelationshipId { get; set; }
        [Required] public int EmployeeId { get; set; }
        [Required] public int ReportsToEmployeeId { get; set; }
        public int ReportingTypeId { get; set; } = 1;
        public int? DepartmentId { get; set; }
        public int? SubDepartmentId { get; set; }
        public bool IsPrimary { get; set; } = false;
        [Required] public DateTime EffectiveFrom { get; set; } = DateTime.Today;
        public DateTime? EffectiveTo { get; set; }
        [MaxLength(500)] public string Notes { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedOn { get; set; } = DateTime.Now;
        public DateTime? ModifiedOn { get; set; }
        [MaxLength(100)] public string CreatedBy { get; set; }
        [ForeignKey("EmployeeId")] public virtual Employee Employee { get; set; }
        [ForeignKey("ReportsToEmployeeId")] public virtual Employee Manager { get; set; }
        [ForeignKey("ReportingTypeId")] public virtual ReportingType ReportingType { get; set; }
        [ForeignKey("DepartmentId")] public virtual Department Department { get; set; }
        [ForeignKey("SubDepartmentId")] public virtual SubDepartment SubDepartment { get; set; }
    }

    #endregion

    #region Enums
    public enum RoleTypeEnum { HOD = 1, SubHOD = 2, Executive = 3 }
    public enum ReportingTypeEnum { Administrative = 1, Functional = 2, Project = 3 }
    #endregion

    #region Request DTOs

    public class AddReportingRelationshipRequest
    {
        [Required(ErrorMessage = "Employee ID is required")] public int EmployeeId { get; set; }
        [Required(ErrorMessage = "Reports To Employee ID is required")] public int ReportsToEmployeeId { get; set; }
        public int ReportingTypeId { get; set; } = 1;
        public int? DepartmentId { get; set; }
        public int? SubDepartmentId { get; set; }
        public bool IsPrimary { get; set; } = false;
        public DateTime? EffectiveFrom { get; set; }
        [MaxLength(500)] public string? Notes { get; set; }
        public string? CreatedBy { get; set; }
    }

    public class UpdateReportingRelationshipRequest
    {
        public int RelationshipId { get; set; }
        public int? ReportsToEmployeeId { get; set; }
        public int? ReportingTypeId { get; set; }
        public int? DepartmentId { get; set; }
        public int? SubDepartmentId { get; set; }
        public bool? IsPrimary { get; set; }
        public DateTime? EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public bool? IsActive { get; set; }
        public string Notes { get; set; }
    }

    public class EmployeeRequest
    {
        public int? EmployeeId { get; set; }
        [Required(ErrorMessage = "Employee Code is required"), StringLength(20)] public string EmployeeCode { get; set; }
        [Required(ErrorMessage = "Employee Name is required"), StringLength(100)] public string EmployeeName { get; set; }
        [EmailAddress(ErrorMessage = "Invalid email format"), StringLength(100)] public string? Email { get; set; }
        [StringLength(20)] public string? Phone { get; set; }
        [StringLength(100)] public string? Designation { get; set; }
        [Required(ErrorMessage = "Role Type is required")] public int RoleTypeId { get; set; }
        public int? PrimaryDepartmentId { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public decimal? Salary { get; set; }   // ← NEW
        public int? CreatedBy { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class EmployeeSearchRequest
    {
        public string SearchTerm { get; set; }
        public int? RoleTypeId { get; set; }
        public int? DepartmentId { get; set; }
        public bool? IsActive { get; set; } = true;
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    /// <summary>Lightweight update for grid inline editing — Name/Desig/Salary/DOJ/IsActive only.</summary>
    public class UpdateEmployeeBasicRequest
    {
        [Required] public int EmployeeId { get; set; }
        [Required, StringLength(100)] public string EmployeeName { get; set; }
        [StringLength(100)] public string Designation { get; set; }
        public decimal? Salary { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public bool IsActive { get; set; } = true;
    }

    #endregion

    #region Response DTOs

    public class HierarchyApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
        public List<string> Errors { get; set; } = new List<string>();

        public static HierarchyApiResponse<T> SuccessResponse(T data, string message = "Success") =>
            new HierarchyApiResponse<T> { Success = true, Message = message, Data = data };

        public static HierarchyApiResponse<T> ErrorResponse(string message, List<string> errors = null) =>
            new HierarchyApiResponse<T> { Success = false, Message = message, Errors = errors ?? new List<string>() };
    }

    public class DashboardSummaryDto
    {
        public int TotalHODs { get; set; }
        public int TotalSubHODs { get; set; }
        public int HODCount { get; set; }
        public int SubHODCount { get; set; }
        public int ExecutiveCount { get; set; }
        public int TotalEmployees { get; set; }
        public int TotalExecutives { get; set; }
        public int TotalDepartments { get; set; }
        public int TotalSubDepartments { get; set; }
        public int TotalRelationships { get; set; }
        public int ActiveRelationships { get; set; }
        public List<DepartmentSummaryDto> DepartmentBreakdown { get; set; } = new List<DepartmentSummaryDto>();
        public List<DepartmentSummaryDto> DepartmentSummary { get; set; }
        public int CurrentUserRoleTypeId { get; set; }
        public int CurrentEmployeeId { get; set; }
        public bool IsAdmin { get; set; }
    }

    public class DepartmentSummaryDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int HODCount { get; set; }
        public int SubHODCount { get; set; }
        public int ExecutiveCount { get; set; }
        public int TotalEmployees { get; set; }
    }

    public class EmployeeDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string Designation { get; set; }
        public int RoleTypeId { get; set; }
        public string RoleName { get; set; }
        public int? PrimaryDepartmentId { get; set; }
        public string PrimaryDepartmentName { get; set; }
        public DateTime? DateOfJoining { get; set; }
        public bool IsActive { get; set; }
        public int DirectReportCount { get; set; }
        public int ManagerCount { get; set; }
        public string Salary { get; set; }
        public bool ViewSalary { get; set; }
    }

    public class EmployeeDetailDto : EmployeeDto
    {
        public List<ReportingToDto> ReportsTo { get; set; } = new List<ReportingToDto>();
        public List<DirectReportDto> DirectReports { get; set; } = new List<DirectReportDto>();
    }

    public class ReportingToDto
    {
        public int RelationshipId { get; set; }
        public int ManagerId { get; set; }
        public string ManagerCode { get; set; }
        public string ManagerName { get; set; }
        public string ManagerRole { get; set; }
        public int ReportingTypeId { get; set; }
        public string ReportingTypeName { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int? SubDepartmentId { get; set; }
        public string SubDepartmentName { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
    }

    public class DirectReportDto
    {
        public int RelationshipId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeRole { get; set; }
        public string Designation { get; set; }
        public int ReportingTypeId { get; set; }
        public string ReportingTypeName { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int? SubDepartmentId { get; set; }
        public string SubDepartmentName { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
        public int TeamSize { get; set; }
    }

    public class ReportingRelationshipDto
    {
        public int RelationshipId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public int EmployeeRoleTypeId { get; set; }
        public string EmployeeRoleName { get; set; }
        public int ReportsToEmployeeId { get; set; }
        public string ManagerCode { get; set; }
        public string ManagerName { get; set; }
        public int ManagerRoleTypeId { get; set; }
        public string ManagerRoleName { get; set; }
        public int ReportingTypeId { get; set; }
        public string ReportingTypeName { get; set; }
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int? SubDepartmentId { get; set; }
        public string SubDepartmentName { get; set; }
        public bool IsPrimary { get; set; }
        public DateTime EffectiveFrom { get; set; }
        public DateTime? EffectiveTo { get; set; }
        public string Notes { get; set; }
        public bool IsActive { get; set; }
    }

    public class HODTreeNodeDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string Designation { get; set; }
        public string Salary { get; set; }
        public int DepartmentCount { get; set; }
        public int SubHODCount { get; set; }
        public int ExecutiveCount { get; set; }
        public List<HODDepartmentDto> Departments { get; set; } = new List<HODDepartmentDto>();
        public List<SubHODTreeNodeDto> SubHODs { get; set; } = new List<SubHODTreeNodeDto>();
    }

    public class SubHODTreeNodeDto
    {
        public int DepartmentId { get; set; }
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string Designation { get; set; }
        public string DepartmentName { get; set; }
        public string SubDepartmentName { get; set; }
        public int SubDepartmentId { get; set; }
        public int ExecutiveCount { get; set; }
        public bool IsPrimary { get; set; }
        public string Salary { get; set; }
        public string ReportingTypeName { get; set; }
        public List<ExecutiveTreeNodeDto> Executives { get; set; } = new List<ExecutiveTreeNodeDto>();
    }

    public class ExecutiveTreeNodeDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string Designation { get; set; }
        public string DepartmentName { get; set; }
        public string SubDepartmentName { get; set; }
        public bool IsPrimary { get; set; }
        public string Salary { get; set; }
        public string ReportingTypeName { get; set; }
    }

    public class HierarchyExportRowDto
    {
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string Designation { get; set; }
        public string Role { get; set; }
        public string Department { get; set; }
        public string SubDepartment { get; set; }
        public string ReportsTo { get; set; }
        public string ReportsToRole { get; set; }
        public string ReportingType { get; set; }
        public string IsPrimary { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string DateOfJoining { get; set; }
        public string Status { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; }
    }

    public class OperationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? RelationshipId { get; set; }
    }

    #endregion

    #region Master Data DTOs

    public class DepartmentDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public string DepartmentCode { get; set; }
        public bool IsActive { get; set; }
        public List<SubDepartmentDto> SubDepartments { get; set; } = new List<SubDepartmentDto>();
    }

    public class SubDepartmentDto
    {
        public int SubDepartmentId { get; set; }
        public int DepartmentId { get; set; }
        public string SubDepartmentName { get; set; }
        public string SubDepartmentCode { get; set; }
        public bool IsActive { get; set; }
    }

    public class RoleTypeDto
    {
        public int RoleTypeId { get; set; }
        public string RoleName { get; set; }
        public int RoleLevel { get; set; }
        public string Description { get; set; }
    }

    public class ReportingTypeDto
    {
        public int ReportingTypeId { get; set; }
        public string ReportingTypeName { get; set; }
        public string Description { get; set; }
    }

    public class HODDepartmentDto
    {
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int SubDeptCount { get; set; }
        public int SubHODCount { get; set; }
        public int ExecutiveCount { get; set; }
    }

    #endregion

    #region Import / Export Flat + Admin Master Grid DTOs

    public class MasterFlatRowDto
    {
        public int RowIndex { get; set; }
        public int HodEmployeeId { get; set; }
        public string HodCode { get; set; }
        public string HodName { get; set; }
        public string HodDesignation { get; set; }
        public string HodSalary { get; set; }
        public int DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int SubDepartmentId { get; set; }
        public string SubDepartmentName { get; set; }
        public int SubHodEmployeeId { get; set; }
        public string SubHodCode { get; set; }
        public string SubHodName { get; set; }
        public string SubHodDesignation { get; set; }
        public string SubHodSalary { get; set; }
        public int ExecEmployeeId { get; set; }
        public string ExecCode { get; set; }
        public string ExecName { get; set; }
        public string ExecDesignation { get; set; }
        public string ExecSalary { get; set; }
    }

    /// <summary>
    /// Admin-only flat DTO for the Excel master grid.
    /// Includes RelationshipIds, IsActive flags, and DateOfJoining fields
    /// needed for inline editing and save operations.
    /// </summary>
    public class MasterFlatAdminRowDto
    {
        public int RowIndex { get; set; }
        // HOD
        public int HodEmployeeId { get; set; }
        public string HodCode { get; set; }
        public string HodName { get; set; }
        public string HodDesignation { get; set; }
        public decimal? HodSalary { get; set; }
        public DateTime? HodDateOfJoining { get; set; }
        public bool HodIsActive { get; set; }
        // Department / SubDept
        public int? DepartmentId { get; set; }
        public string DepartmentName { get; set; }
        public int? SubDepartmentId { get; set; }
        public string SubDepartmentName { get; set; }
        // SubHOD
        public int SubHodEmployeeId { get; set; }
        public string SubHodCode { get; set; }
        public string SubHodName { get; set; }
        public string SubHodDesignation { get; set; }
        public decimal? SubHodSalary { get; set; }
        public DateTime? SubHodDateOfJoining { get; set; }
        public bool SubHodIsActive { get; set; }
        public int? SubHodRelationshipId { get; set; }
        // Executive
        public int ExecEmployeeId { get; set; }
        public string ExecCode { get; set; }
        public string ExecName { get; set; }
        public string ExecDesignation { get; set; }
        public decimal? ExecSalary { get; set; }
        public DateTime? ExecDateOfJoining { get; set; }
        public bool ExecIsActive { get; set; }
        public int? ExecRelationshipId { get; set; }
    }

    public class ImportRowRequest
    {
        public int RowNumber { get; set; }
        public string HodCode { get; set; }
        public string HodName { get; set; }
        public string HodDesignation { get; set; }
        public string DepartmentName { get; set; }
        public string SubDepartmentName { get; set; }
        public string SubHodCode { get; set; }
        public string SubHodName { get; set; }
        public string SubHodDesignation { get; set; }
        public string ExecCode { get; set; }
        public string ExecName { get; set; }
        public string ExecDesignation { get; set; }
    }

    public class ImportRowResult
    {
        public int RowNumber { get; set; }
        public string Status { get; set; }
        public string HodAction { get; set; }
        public string DeptAction { get; set; }
        public string SubDeptAction { get; set; }
        public string SubHodAction { get; set; }
        public string ExecAction { get; set; }
        public string ExecName { get; set; }
        public string HodName { get; set; }
        public string Message { get; set; }
    }

    public class ImportSummary
    {
        public int TotalRows { get; set; }
        public int SuccessCount { get; set; }
        public int ErrorCount { get; set; }
        public int NewEmployees { get; set; }
        public int UpdatedEmployees { get; set; }
        public int NewDepartments { get; set; }
        public int NewSubDepartments { get; set; }
        public List<ImportRowResult> Results { get; set; } = new();
    }


    /// <summary>One row from the HR Excel upload (has employee codes for upsert).</summary>
    public class ImportExcelRowRequest
    {
        public int RowNumber { get; set; }
        public string? HodName { get; set; }
        public string? HodCode { get; set; }
        public decimal? HodSalary { get; set; }
        public string? Department { get; set; }
        public string? SubDepartment { get; set; }
        public string? SubHodName { get; set; }
        public string? SubHodCode { get; set; }
        public string? SubHodDesignation { get; set; }
        public decimal? SubHodSalary { get; set; }
        public string? ExecName { get; set; }
        public string? ExecCode { get; set; }
        public string? ExecDesignation { get; set; }
        public decimal? ExecSalary { get; set; }
    }

    public class ImportExcelResult
    {
        public int TotalRows { get; set; }
        public int EmployeesCreated { get; set; }
        public int EmployeesUpdated { get; set; }
        public int RelationshipsCreated { get; set; }
        public int DeptsCreated { get; set; }
        public int TempCodesGenerated { get; set; }   // execs without code → TEMP00XX
        public int Errors { get; set; }
        public List<string> ErrorDetails { get; set; } = new();
        public List<string> TempCodes { get; set; } = new();  // list of generated TEMP codes
    }

    #endregion

    public class UpdateEmployeeFullRequest
    {
        [Required] public int EmployeeId { get; set; }
        [Required, StringLength(100)] public string EmployeeName { get; set; }
        [Required, StringLength(20)] public string EmployeeCode { get; set; }
        public string? Designation { get; set; }   // optional
        public decimal? Salary { get; set; }
        public DateTime? DateOfJoining { get; set; }
        [Required] public int RoleTypeId { get; set; }
        public int? DepartmentId { get; set; }
        public int? SubDepartmentId { get; set; }
        public int? ReportsToEmpId { get; set; } // optional — exec can report direct to HOD or none
        public bool MoveTeamWithDepartment { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DepartmentChangeImpactRequest
    {
        [Required] public int EmployeeId { get; set; }
        [Required] public int RoleTypeId { get; set; }
        public int? DepartmentId { get; set; }
        public int? SubDepartmentId { get; set; }
        public int? CurrentDepartmentId { get; set; }
        public int? CurrentSubDepartmentId { get; set; }
    }

    public class DepartmentChangeImpactDto
    {
        public bool DepartmentChanged { get; set; }
        public bool RequiresMoveTeam { get; set; }
        public int DirectReportCount { get; set; }
        public int SubHodCount { get; set; }
        public int ExecutiveCount { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class BulkAssignTeamRequest
    {
        [Required] public int HodEmployeeId { get; set; }
        public int? DepartmentId { get; set; }
        public int? SubDepartmentId { get; set; }
        public int? SubHodEmployeeId { get; set; }
        public List<int> ExecutiveEmployeeIds { get; set; } = new();
    }

    public class BulkAssignTeamResult
    {
        public int SubHodAssigned { get; set; }
        public int ExecutivesAssigned { get; set; }
        public int RelationshipsUpdated { get; set; }
    }

    // Also add this DTO for dropdown lists:
    public class EmployeeDropdownDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }
    }

    public class AddDepartmentRequest
    {
        [Required, StringLength(150)] public string DepartmentName { get; set; }
        public string? DepartmentCode { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class AddSubDepartmentRequest
    {
        [Required] public int DepartmentId { get; set; }
        [Required, StringLength(150)] public string SubDepartmentName { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateDepartmentRequest
    {
        [Required] public int DepartmentId { get; set; }
        [Required, StringLength(150)] public string DepartmentName { get; set; }
        public string? DepartmentCode { get; set; }
        public bool? IsActive { get; set; }
    }

    public class UpdateSubDepartmentRequest
    {
        [Required] public int SubDepartmentId { get; set; }
        [Required] public int DepartmentId { get; set; }
        [Required, StringLength(150)] public string SubDepartmentName { get; set; }
        public string? Description { get; set; }
        public bool? IsActive { get; set; }
    }

    public class VerifySalaryPasswordRequest
    {
        public string Password { get; set; }
    }

    public class SetSalaryPasswordRequest
    {
        public int EmployeeId { get; set; }
        public string Password { get; set; }
    }

    /// <summary>Two-factor salary unlock: own password + admin master key.</summary>
    public class VerifySalaryAccessRequest
    {
        public string OwnPassword { get; set; }
        public string AdminKey { get; set; }
    }

    /// <summary>Admin sets / changes the global salary master key.</summary>
    public class SetAdminSalaryKeyRequest
    {
        /// <summary>Required when a key already exists — must match stored key before update is allowed.</summary>
        public string? OldKey { get; set; }
        public string AdminKey { get; set; }
    }

    /// <summary>Status returned so the UI knows whether to show the Old Key field.</summary>
    public class AdminKeyStatusDto
    {
        public bool HasPermission { get; set; }
        public bool KeyExists { get; set; }
    }

    public class VerifyAdminKeyRequest
    {
        public string AdminKey { get; set; }
    }

    public class SalaryDbCredentialDto
    {
        public int? CredentialId { get; set; }
        public string ServerName { get; set; } = "";
        public string DatabaseName { get; set; } = "";
        public string DbUserId { get; set; } = "";
    }

    public class UpdateSalaryDbCredentialRequest : SalaryDbCredentialDto
    {
        public string AdminKey { get; set; } = "";
    }

    public class SetSalaryPermissionRequest
    {
        public List<int> EmployeeIds { get; set; } = new();
    }

    /// <summary>
    /// Password is used once to open a read-only DB connection, then immediately discarded.
    /// It is NEVER stored in session, DB, or logs.
    /// </summary>
    public class UnlockSalaryRequest
    {
        public string DbPassword { get; set; } = "";
    }

    public class OrphanEmployeeDto
    {
        public int EmployeeId { get; set; }
        public string EmployeeCode { get; set; }
        public string EmployeeName { get; set; }
        public string Designation { get; set; }
        public int RoleTypeId { get; set; }
        public string RoleName { get; set; }
        public string DepartmentName { get; set; }
        public string SubDepartmentName { get; set; }
        public decimal? Salary { get; set; }
        public bool IsActive { get; set; }
    }

    #region Audit Log

    public class AuditLogDto
    {
        public int LogId { get; set; }
        public string ActionType { get; set; }
        public string EntityType { get; set; }
        public int? EntityId { get; set; }
        public int? EmployeeId { get; set; }
        public string EmployeeName { get; set; }
        public string EmployeeCode { get; set; }
        public string OldValues { get; set; }
        public string NewValues { get; set; }
        public string Description { get; set; }
        public int? ChangedByUserId { get; set; }
        public string ChangedByName { get; set; }
        public string IpAddress { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class AuditLogRequest
    {
        public int? EmployeeId { get; set; }
        public string ActionType { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string SearchTerm { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 50;
    }

    #endregion

    #region Custom Fields (Dynamic Columns)

    public class CustomFieldDto
    {
        public int FieldId { get; set; }
        public string FieldName { get; set; }
        public string FieldType { get; set; }
        public bool IsRequired { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public string Options { get; set; }
    }

    public class AddCustomFieldRequest
    {
        [Required, StringLength(100)] public string FieldName { get; set; } = "";
        public string FieldType { get; set; } = "Text";
        public bool IsRequired { get; set; }
        public string? Options { get; set; }
    }

    public class SetCustomValueRequest
    {
        [Required] public int EmployeeId { get; set; }
        [Required] public int FieldId { get; set; }
        public string Value { get; set; }
    }

    public class EmployeeCustomValueDto
    {
        public int EmployeeId { get; set; }
        public int FieldId { get; set; }
        public string FieldName { get; set; }
        public string Value { get; set; }
    }

    #endregion
}
