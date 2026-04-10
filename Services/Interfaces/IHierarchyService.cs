using JSAPNEW.Models;

namespace JSAPNEW.Services
{
    public interface IHierarchyService
    {
        #region Dashboard & Summary

        Task<DashboardSummaryDto> GetDashboardSummaryAsync(string employeeCode, bool isAdmin);
        Task<List<HODTreeNodeDto>> GetHierarchyTreeAsync(string employeeCode, bool isAdmin);

        #endregion

        #region Employee Operations

        Task<List<EmployeeDto>> GetEmployeesAsync(EmployeeSearchRequest request, string employeeCode, bool isAdmin);
        Task<EmployeeDetailDto> GetEmployeeByIdAsync(int employeeId, string employeeCode, bool isAdmin);
        Task<EmployeeDto> GetEmployeeByCodeAsync(string employeeCode);
        Task<List<EmployeeDto>> GetAvailableManagersAsync(int employeeId);
        Task<HierarchyApiResponse<EmployeeDto>> CreateEmployeeAsync(EmployeeRequest request);
        Task<HierarchyApiResponse<EmployeeDto>> UpdateEmployeeAsync(EmployeeRequest request);
        Task<HierarchyApiResponse<bool>> DeactivateEmployeeAsync(int employeeId);

        #endregion

        #region Reporting Relationship Operations

        Task<List<ReportingToDto>> GetEmployeeReportingToAsync(int employeeId, bool includeInactive = false);
        Task<List<DirectReportDto>> GetEmployeeDirectReportsAsync(int managerId, bool includeInactive = false);
        Task<ReportingRelationshipDto> GetRelationshipByIdAsync(int relationshipId);
        Task<OperationResult> AddReportingRelationshipAsync(AddReportingRelationshipRequest request);
        Task<OperationResult> UpdateReportingRelationshipAsync(UpdateReportingRelationshipRequest request);
        Task<OperationResult> RemoveReportingRelationshipAsync(int relationshipId);
        Task<OperationResult> SetPrimaryRelationshipAsync(int relationshipId);

        #endregion

        #region Master Data

        Task<List<DepartmentDto>> GetDepartmentsAsync(bool includeSubDepartments = false);
        Task<List<SubDepartmentDto>> GetSubDepartmentsAsync(int departmentId);
        Task<List<RoleTypeDto>> GetRoleTypesAsync();
        Task<List<ReportingTypeDto>> GetReportingTypesAsync();

        #endregion

        #region Export

        Task<List<HierarchyExportRowDto>> GetHierarchyForExportAsync(string employeeCode, bool isAdmin);

        #endregion

        #region Utility

        Task<List<HODDepartmentDto>> GetDepartmentsForHODAsync(int hodId);

        #endregion

        Task<List<MasterFlatRowDto>> GetMasterFlatAsync(string employeeCode, bool isAdmin);
        Task<ImportSummary> ProcessImportAsync(List<ImportRowRequest> rows);

        #region Admin Master Grid

        /// <summary>Returns the full flat hierarchy for the admin Excel grid.</summary>
        Task<List<MasterFlatAdminRowDto>> GetMasterFlatAdminAsync(string employeeCode);

        /// <summary>Lightweight inline save — updates Name/Designation/Salary/DOJ/IsActive only.</summary>
        Task<HierarchyApiResponse<bool>> UpdateEmployeeBasicAsync(UpdateEmployeeBasicRequest request);

        #endregion

        #region Excel Import

        /// <summary>Bulk upsert from HR Excel — creates/updates employees + relationships by employee code.
        /// createdBy = logged-in user's empId. No-code execs get TEMP00XX codes auto-generated.</summary>
        Task<ImportExcelResult> ImportFromExcelAsync(List<ImportExcelRowRequest> rows, int createdBy);

        #endregion

        #region Salary Password

        /// <summary>Verify salary password for the logged-in employee. True = correct.</summary>
        Task<bool> VerifySalaryPasswordAsync(int employeeId, string plainPassword);

        /// <summary>Set/update salary password — stores AES-encrypted value in DB.</summary>
        Task<bool> SetSalaryPasswordAsync(int employeeId, string plainPassword);

        /// <summary>Two-factor salary access: verifies own password AND the global admin master key.</summary>
        Task<(bool Success, string Message)> VerifySalaryAccessAsync(int employeeId, string ownPassword, string adminKey);

        /// <summary>
        /// Set/change the global salary master key.
        /// The caller must have a row in [Hie].[SalaryKeyManager] (IsActive=1).
        /// If a key already exists, oldKey must match the stored key before the new key is saved.
        /// </summary>
        Task<(bool Success, string Message)> SetAdminSalaryKeyAsync(string oldKey, string newKey, int setByUserId);

        /// <summary>Returns true if the admin has set a master salary key.</summary>
        Task<bool> HasAdminSalaryKeySetAsync();

        /// <summary>
        /// Returns whether the given userId has permission to manage the admin salary key
        /// and whether a key has already been set.
        /// </summary>
        Task<AdminKeyStatusDto> GetAdminKeyStatusAsync(int userId);

        /// <summary>Verify just the admin key (no employee password required).</summary>
        Task<bool> VerifyAdminKeyAsync(string adminKey);

        /// <summary>Returns the configured DB server/database/user used for salary unlock.</summary>
        Task<SalaryDbCredentialDto?> GetSalaryDbCredentialAsync();

        /// <summary>Updates the configured DB server/database/user used for salary unlock.</summary>
        Task<(bool Success, string Message)> UpdateSalaryDbCredentialAsync(SalaryDbCredentialDto request, int updatedBy, int? changedEmployeeId);

        /// <summary>Set ViewSalary flag for the given employee IDs (true) and clear it for all others.</summary>
        Task<(bool Success, string Message)> SetSalaryPermissionAsync(List<int> employeeIds, int updatedBy);

        /// <summary>Returns all employees with their current ViewSalary flag for the permission screen.</summary>
        Task<List<EmployeeDto>> GetEmployeesForSalaryPermissionAsync();

        #endregion

        #region Full Employee Edit

        Task<HierarchyApiResponse<bool>> UpdateEmployeeFullAsync(UpdateEmployeeFullRequest request, int updatedBy);
        Task<DepartmentChangeImpactDto> GetDepartmentChangeImpactAsync(DepartmentChangeImpactRequest request);
        Task<HierarchyApiResponse<BulkAssignTeamResult>> BulkAssignTeamAsync(BulkAssignTeamRequest request, int updatedBy);
        Task<List<EmployeeDropdownDto>> GetActiveHODsAsync();
        Task<List<EmployeeDropdownDto>> GetSubHODsForEditAsync(int? hodId);

        #endregion

        #region Department Management

        Task<HierarchyApiResponse<int>> AddDepartmentAsync(AddDepartmentRequest request, int createdBy);
        Task<HierarchyApiResponse<int>> AddSubDepartmentAsync(AddSubDepartmentRequest request, int createdBy);
        Task<HierarchyApiResponse<bool>> UpdateDepartmentAsync(UpdateDepartmentRequest request, int updatedBy);
        Task<HierarchyApiResponse<bool>> UpdateSubDepartmentAsync(UpdateSubDepartmentRequest request, int updatedBy);

        #endregion

        #region Orphan Employees

        /// <summary>Get employees with no active reporting relationship (orphaned SubHODs/Execs without HOD).</summary>
        Task<List<OrphanEmployeeDto>> GetOrphanEmployeesAsync();

        #endregion

        #region Audit Log

        Task LogAuditAsync(string actionType, string entityType, int? entityId, int? employeeId,
            string oldValues, string newValues, string description, int? changedByUserId);
        Task<List<AuditLogDto>> GetAuditLogsAsync(AuditLogRequest request);

        #endregion

        #region Custom Fields (Dynamic Columns)

        Task<List<CustomFieldDto>> GetCustomFieldsAsync();
        Task<HierarchyApiResponse<int>> AddCustomFieldAsync(AddCustomFieldRequest request, int createdBy);
        Task<HierarchyApiResponse<bool>> RemoveCustomFieldAsync(int fieldId);
        Task<List<EmployeeCustomValueDto>> GetEmployeeCustomValuesAsync(int? employeeId = null);
        Task<HierarchyApiResponse<bool>> SetEmployeeCustomValueAsync(SetCustomValueRequest request);

        #endregion
    }
}
