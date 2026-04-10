using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Text.Json;

namespace JSAPNEW.Services
{
    public class HierarchyService : IHierarchyService
    {
        private readonly string _connectionString;

        public HierarchyService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        private static bool IsDynamicSuccess(dynamic? result)
        {
            if (result == null) return false;

            try
            {
                return Convert.ToInt32(result.Success) == 1;
            }
            catch
            {
                try { return Convert.ToBoolean(result.Success); }
                catch { return false; }
            }
        }

        private static string ToJson(object obj)
        {
            try { return obj == null ? null : JsonSerializer.Serialize(obj, new JsonSerializerOptions { WriteIndented = false }); }
            catch { return null; }
        }

        private static string? EncryptSalary(decimal? salary)
        {
            if (!salary.HasValue) return null;
            return Encryption.Encrypt(salary.Value.ToString(CultureInfo.InvariantCulture));
        }

        private static bool IsValidAdminKeyFormat(string value)
        {
            return !string.IsNullOrWhiteSpace(value)
                && value.Length >= 6
                && value.Any(char.IsUpper)
                && value.Any(ch => !char.IsLetterOrDigit(ch));
        }

        private static bool IsRemovedSalaryColumnError(SqlException ex) =>
            ex.Message.Contains("Invalid column name 'Salary'", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("Invalid column name 'SalaryEncrypted'", StringComparison.OrdinalIgnoreCase)
            || ex.Message.Contains("Invalid column name 'SalaryPassword'", StringComparison.OrdinalIgnoreCase);

        private static bool IsMissingJsUserDependencyError(string? message)
        {
            if (string.IsNullOrWhiteSpace(message)) return false;

            return message.Contains("jsUser", StringComparison.OrdinalIgnoreCase)
                || message.Contains("js user", StringComparison.OrdinalIgnoreCase)
                || (message.Contains("empId", StringComparison.OrdinalIgnoreCase)
                    && message.Contains("user", StringComparison.OrdinalIgnoreCase));
        }

        private static string? DecryptSalaryString(string? storedValue)
        {
            if (string.IsNullOrWhiteSpace(storedValue)) return null;
            var raw = storedValue.Trim();

            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.InvariantCulture, out var plainDecimal))
                return plainDecimal.ToString(CultureInfo.InvariantCulture);

            if (decimal.TryParse(raw, NumberStyles.Any, CultureInfo.CurrentCulture, out plainDecimal))
                return plainDecimal.ToString(CultureInfo.InvariantCulture);

            try
            {
                var decrypted = Encryption.Decrypt(raw)?.Trim();
                if (string.IsNullOrWhiteSpace(decrypted)) return null;

                if (decimal.TryParse(decrypted, NumberStyles.Any, CultureInfo.InvariantCulture, out var dec))
                    return dec.ToString(CultureInfo.InvariantCulture);

                if (decimal.TryParse(decrypted, NumberStyles.Any, CultureInfo.CurrentCulture, out dec))
                    return dec.ToString(CultureInfo.InvariantCulture);
            }
            catch { }

            return null;
        }

        private static void DecryptEmployeeSalary(EmployeeDto? employee)
        {
            if (employee == null) return;
            employee.Salary = DecryptSalaryString(employee.Salary);
        }

        private static void DecryptTreeSalaries(IEnumerable<HODTreeNodeDto>? hods)
        {
            foreach (var hod in hods ?? Enumerable.Empty<HODTreeNodeDto>())
            {
                hod.Salary = DecryptSalaryString(hod.Salary);
                foreach (var sub in hod.SubHODs ?? Enumerable.Empty<SubHODTreeNodeDto>())
                {
                    sub.Salary = DecryptSalaryString(sub.Salary);
                    foreach (var exec in sub.Executives ?? Enumerable.Empty<ExecutiveTreeNodeDto>())
                    {
                        exec.Salary = DecryptSalaryString(exec.Salary);
                    }
                }
            }
        }

        #region Dashboard & Summary

        public async Task<DashboardSummaryDto> GetDashboardSummaryAsync(string employeeCode, bool isAdmin)
        {
            const string sql = "EXEC [Hie].[sp_GetDashboardSummary_RoleBased] @EmployeeCode, @IsAdmin";

            using var connection = new SqlConnection(_connectionString);
            using var multi = await connection.QueryMultipleAsync(sql, new { EmployeeCode = employeeCode, IsAdmin = isAdmin });

            var counts = await multi.ReadFirstOrDefaultAsync<dynamic>();
            var departments = (await multi.ReadAsync<DepartmentSummaryDto>()).ToList();

            return new DashboardSummaryDto
            {
                TotalEmployees = counts?.TotalEmployees ?? 0,
                TotalHODs = counts?.TotalHODs ?? 0,
                TotalSubHODs = counts?.TotalSubHODs ?? 0,
                TotalExecutives = counts?.TotalExecutives ?? 0,
                TotalRelationships = counts?.TotalRelationships ?? 0,
                TotalDepartments = counts?.TotalDepartments ?? 0,
                ActiveRelationships = counts?.ActiveRelationships ?? 0,
                DepartmentSummary = departments,
                CurrentUserRoleTypeId = counts?.CurrentUserRoleTypeId ?? 0,
                CurrentEmployeeId = counts?.CurrentEmployeeId ?? 0,
                IsAdmin = (counts?.IsAdmin ?? 0) == 1
            };
        }

        public async Task<List<HODTreeNodeDto>> GetHierarchyTreeAsync(string employeeCode, bool isAdmin)
        {
            const string sql = "EXEC [Hie].[sp_GetHierarchyTree_RoleBased] @EmployeeCode, @IsAdmin";

            using var connection = new SqlConnection(_connectionString);
            List<HODTreeNodeDto> hods;
            try
            {
                hods = (await connection.QueryAsync<HODTreeNodeDto>(sql, new { EmployeeCode = employeeCode, IsAdmin = isAdmin })).ToList();
            }
            catch (SqlException ex) when (IsRemovedSalaryColumnError(ex))
            {
                hods = await GetHierarchyTreeWithoutSalaryAsync(connection, employeeCode, isAdmin);
            }

            foreach (var hod in hods)
            {
                var activeRelationshipKeys = await GetActiveRelationshipKeysForHodAsync(connection, hod.EmployeeId);
                hod.Departments = await GetDepartmentsForHODAsync(hod.EmployeeId);
                hod.SubHODs = await GetSubHODsForHODAsync(hod.EmployeeId, employeeCode, isAdmin);

                // Fetch ALL executives that report directly to this HOD (SubHODId=0)
                // The SP's NOT EXISTS filter misses direct execs when a SubHOD also exists in the same sub-dept
                var directExecs = await GetExecutivesForSubHODAsync(0, employeeCode, isAdmin, null, null, hod.EmployeeId, activeRelationshipKeys);
                if (directExecs.Any())
                {
                    MergeDirectExecsIntoSubHODs(hod, directExecs);
                }
            }

            DecryptTreeSalaries(hods);
            return hods;
        }

        private async Task<List<HODTreeNodeDto>> GetHierarchyTreeWithoutSalaryAsync(SqlConnection connection, string employeeCode, bool isAdmin)
        {
            const string sql = @"
SELECT
    e.EmployeeId,
    e.EmployeeCode,
    e.EmployeeName,
    e.Designation,
    CAST(NULL AS NVARCHAR(50)) AS Salary,
    COUNT(DISTINCT rr.DepartmentId) AS DepartmentCount,
    COUNT(DISTINCT CASE WHEN ce.RoleTypeId = 2 THEN rr.EmployeeId END) AS SubHODCount,
    COUNT(DISTINCT CASE WHEN ce.RoleTypeId = 3 THEN rr.EmployeeId END) AS ExecutiveCount
FROM [Hie].[Employees] e
LEFT JOIN [Hie].[EmployeeReportingRelationships] rr
    ON rr.ReportsToEmployeeId = e.EmployeeId
    AND rr.IsActive = 1
LEFT JOIN [Hie].[Employees] ce
    ON ce.EmployeeId = rr.EmployeeId
    AND ce.IsActive = 1
WHERE e.IsActive = 1
  AND e.RoleTypeId = 1
  AND (
        @IsAdmin = 1
        OR e.EmployeeCode = @EmployeeCode
        OR EXISTS
        (
            SELECT 1
            FROM [Hie].[EmployeeReportingRelationships] rrScope
            INNER JOIN [Hie].[Employees] currentUser
                ON currentUser.EmployeeId = rrScope.ReportsToEmployeeId
            WHERE currentUser.EmployeeCode = @EmployeeCode
              AND rrScope.EmployeeId = e.EmployeeId
              AND rrScope.IsActive = 1
        )
      )
GROUP BY e.EmployeeId, e.EmployeeCode, e.EmployeeName, e.Designation
ORDER BY e.EmployeeName";

            return (await connection.QueryAsync<HODTreeNodeDto>(sql, new { EmployeeCode = employeeCode, IsAdmin = isAdmin })).ToList();
        }

        private async Task<HashSet<string>> GetActiveRelationshipKeysForHodAsync(SqlConnection connection, int hodId)
        {
            const string sql = @"
WITH HodSubHods AS
(
    SELECT DISTINCT rr.EmployeeId
    FROM [Hie].[EmployeeReportingRelationships] rr
    INNER JOIN [Hie].[Employees] e ON e.EmployeeId = rr.EmployeeId
    WHERE rr.IsActive = 1
      AND rr.ReportsToEmployeeId = @HODId
      AND e.RoleTypeId = 2
)
SELECT DISTINCT
    CONCAT(
        rr.EmployeeId, '|',
        rr.ReportsToEmployeeId, '|',
        ISNULL(rr.DepartmentId, 0), '|',
        ISNULL(rr.SubDepartmentId, 0)
    ) AS [Key]
FROM [Hie].[EmployeeReportingRelationships] rr
WHERE rr.IsActive = 1
  AND (
        rr.ReportsToEmployeeId = @HODId
        OR rr.ReportsToEmployeeId IN (SELECT EmployeeId FROM HodSubHods)
      )";

            var keys = await connection.QueryAsync<string>(sql, new { HODId = hodId });
            return keys
                .Where(k => !string.IsNullOrWhiteSpace(k))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static string BuildRelationshipKey(int employeeId, int reportsToEmployeeId, int? departmentId, int? subDepartmentId) =>
            $"{employeeId}|{reportsToEmployeeId}|{departmentId ?? 0}|{subDepartmentId ?? 0}";

        private static string BuildRelationshipPairKey(int employeeId, int reportsToEmployeeId) =>
            $"{employeeId}|{reportsToEmployeeId}";

        /// <summary>
        /// Merges direct-to-HOD executives into the SubHODs list.
        /// For each exec, finds the matching (Dept, SubDept) entry. If only SubHOD entries exist
        /// for that sub-dept (no EmployeeId=0 phantom row), creates one and adds the execs.
        /// </summary>
        private void MergeDirectExecsIntoSubHODs(HODTreeNodeDto hod, List<ExecutiveTreeNodeDto> directExecs)
        {
            // Group execs by (DepartmentName, SubDepartmentName)
            var groups = directExecs
                .GroupBy(e => (e.DepartmentName ?? "", e.SubDepartmentName ?? ""))
                .ToList();

            foreach (var group in groups)
            {
                var deptName = group.Key.Item1;
                var subDeptName = group.Key.Item2;

                // Find existing phantom SubHOD entry (EmployeeId=0) for this dept/subdept
                var phantomEntry = hod.SubHODs.FirstOrDefault(s =>
                    s.EmployeeId == 0 &&
                    (s.DepartmentName ?? "") == deptName &&
                    (s.SubDepartmentName ?? "") == subDeptName);

                if (phantomEntry != null)
                {
                    // Already has a phantom entry — merge execs that aren't already there
                    var existingCodes = new HashSet<string>(
                        phantomEntry.Executives.Select(e => e.EmployeeCode ?? ""),
                        StringComparer.OrdinalIgnoreCase);
                    foreach (var exec in group)
                    {
                        if (!existingCodes.Contains(exec.EmployeeCode ?? ""))
                            phantomEntry.Executives.Add(exec);
                    }
                }
                else
                {
                    // No phantom entry — check if a real SubHOD already covers this sub-dept
                    // If so, the SP missed the direct execs. Create a phantom row for them.
                    var matchingSubDept = hod.SubHODs.FirstOrDefault(s =>
                        s.EmployeeId > 0 &&
                        (s.DepartmentName ?? "") == deptName &&
                        (s.SubDepartmentName ?? "") == subDeptName);

                    // Deduplicate: remove execs that are already under a SubHOD
                    var subHodExecCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var sh in hod.SubHODs.Where(s => s.EmployeeId > 0 &&
                        (s.DepartmentName ?? "") == deptName &&
                        (s.SubDepartmentName ?? "") == subDeptName))
                    {
                        foreach (var e in sh.Executives)
                            subHodExecCodes.Add(e.EmployeeCode ?? "");
                    }

                    var uniqueExecs = group.Where(e => !subHodExecCodes.Contains(e.EmployeeCode ?? "")).ToList();
                    if (!uniqueExecs.Any()) continue;

                    // Get DepartmentId/SubDepartmentId from the matching SubHOD entry
                    int deptId = matchingSubDept?.DepartmentId ?? 0;
                    int subDeptId = matchingSubDept?.SubDepartmentId ?? 0;

                    hod.SubHODs.Add(new SubHODTreeNodeDto
                    {
                        EmployeeId = 0,
                        EmployeeCode = "-",
                        EmployeeName = "-",
                        Designation = null,
                        Salary = "0",
                        DepartmentId = deptId,
                        DepartmentName = deptName,
                        SubDepartmentId = subDeptId,
                        SubDepartmentName = subDeptName,
                        ExecutiveCount = uniqueExecs.Count,
                        IsPrimary = false,
                        ReportingTypeName = "Administrative",
                        Executives = uniqueExecs
                    });
                }
            }
        }

        private async Task<List<SubHODTreeNodeDto>> GetSubHODsForHODAsync(int hodId, string employeeCode, bool isAdmin)
        {
            const string sql = "EXEC [Hie].[sp_GetSubHODsForHOD_RoleBased] @HODId, @EmployeeCode, @IsAdmin";

            using var connection = new SqlConnection(_connectionString);
            var activeRelationshipKeys = await GetActiveRelationshipKeysForHodAsync(connection, hodId);
            List<SubHODTreeNodeDto> subHods;
            try
            {
                subHods = (await connection.QueryAsync<SubHODTreeNodeDto>(
                    sql,
                    new { HODId = hodId, EmployeeCode = employeeCode, IsAdmin = isAdmin }
                )).ToList();
            }
            catch (SqlException ex) when (IsRemovedSalaryColumnError(ex))
            {
                subHods = await GetSubHODsForHODWithoutSalaryAsync(connection, hodId);
            }

            subHods = subHods
            .Where(subHod =>
                subHod.EmployeeId <= 0 ||
                activeRelationshipKeys.Contains(
                    BuildRelationshipKey(
                        subHod.EmployeeId,
                        hodId,
                        subHod.DepartmentId,
                        subHod.SubDepartmentId)))
            .ToList();

            var missingScopes = await GetMissingSubHodScopesAsync(connection, hodId, subHods, activeRelationshipKeys);
            if (missingScopes.Any())
                subHods.AddRange(missingScopes);

            foreach (var subHod in subHods)
            {
                subHod.Executives = await GetExecutivesForSubHODAsync(
                    subHod.EmployeeId,
                    employeeCode,
                    isAdmin,
                    subHod.DepartmentId,
                    subHod.SubDepartmentId,
                    hodId,
                    activeRelationshipKeys
                );
            }

            return subHods;
        }

        private async Task<List<SubHODTreeNodeDto>> GetSubHODsForHODWithoutSalaryAsync(SqlConnection connection, int hodId)
        {
            const string sql = @"
SELECT
    rr.DepartmentId,
    e.EmployeeId,
    e.EmployeeCode,
    e.EmployeeName,
    e.Designation,
    d.DepartmentName,
    sd.SubDepartmentName,
    ISNULL(rr.SubDepartmentId, 0) AS SubDepartmentId,
    0 AS ExecutiveCount,
    rr.IsPrimary,
    CAST(NULL AS NVARCHAR(50)) AS Salary,
    ISNULL(rt.ReportingTypeName, 'Administrative') AS ReportingTypeName
FROM [Hie].[EmployeeReportingRelationships] rr
INNER JOIN [Hie].[Employees] e
    ON e.EmployeeId = rr.EmployeeId
    AND e.IsActive = 1
    AND e.RoleTypeId = 2
LEFT JOIN [Hie].[Departments] d
    ON d.DepartmentId = rr.DepartmentId
LEFT JOIN [Hie].[SubDepartments] sd
    ON sd.SubDepartmentId = rr.SubDepartmentId
LEFT JOIN [Hie].[ReportingTypes] rt
    ON rt.ReportingTypeId = rr.ReportingTypeId
WHERE rr.IsActive = 1
  AND rr.ReportsToEmployeeId = @HODId
ORDER BY d.DepartmentName, sd.SubDepartmentName, e.EmployeeName";

            return (await connection.QueryAsync<SubHODTreeNodeDto>(sql, new { HODId = hodId })).ToList();
        }

        private async Task<List<SubHODTreeNodeDto>> GetMissingSubHodScopesAsync(
            SqlConnection connection,
            int hodId,
            List<SubHODTreeNodeDto> existingSubHods,
            HashSet<string> activeRelationshipKeys)
        {
            const string sql = @"
SELECT
    rr.EmployeeId,
    e.EmployeeCode,
    e.EmployeeName,
    e.Designation,
    CAST(NULL AS NVARCHAR(50)) AS Salary,
    rr.DepartmentId,
    d.DepartmentName,
    rr.SubDepartmentId,
    sd.SubDepartmentName,
    rr.IsPrimary,
    rt.ReportingTypeName
FROM [Hie].[EmployeeReportingRelationships] rr
INNER JOIN [Hie].[Employees] e
    ON e.EmployeeId = rr.EmployeeId
LEFT JOIN [Hie].[Departments] d
    ON d.DepartmentId = rr.DepartmentId
LEFT JOIN [Hie].[SubDepartments] sd
    ON sd.SubDepartmentId = rr.SubDepartmentId
LEFT JOIN [Hie].[ReportingTypes] rt
    ON rt.ReportingTypeId = rr.ReportingTypeId
WHERE rr.IsActive = 1
  AND rr.ReportsToEmployeeId = @HodId
  AND e.RoleTypeId = 2";

            var existingKeys = existingSubHods
                .Where(s => s.EmployeeId > 0)
                .Select(s => BuildRelationshipKey(s.EmployeeId, hodId, s.DepartmentId, s.SubDepartmentId))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var rows = await connection.QueryAsync<dynamic>(sql, new { HodId = hodId });
            var additions = new List<SubHODTreeNodeDto>();

            foreach (var row in rows)
            {
                int employeeId = row.EmployeeId;
                int? departmentId = row.DepartmentId == null ? null : (int?)row.DepartmentId;
                int? subDepartmentId = row.SubDepartmentId == null ? null : (int?)row.SubDepartmentId;
                var relationshipKey = BuildRelationshipKey(employeeId, hodId, departmentId, subDepartmentId);

                if (!activeRelationshipKeys.Contains(relationshipKey) || existingKeys.Contains(relationshipKey))
                    continue;

                additions.Add(new SubHODTreeNodeDto
                {
                    EmployeeId = employeeId,
                    EmployeeCode = row.EmployeeCode ?? "",
                    EmployeeName = row.EmployeeName ?? "",
                    Designation = row.Designation ?? "",
                    Salary = row.Salary ?? "",
                    DepartmentId = departmentId ?? 0,
                    DepartmentName = row.DepartmentName ?? "",
                    SubDepartmentId = subDepartmentId ?? 0,
                    SubDepartmentName = row.SubDepartmentName ?? "",
                    ExecutiveCount = 0,
                    IsPrimary = row.IsPrimary == null ? false : (bool)row.IsPrimary,
                    ReportingTypeName = row.ReportingTypeName ?? "Administrative"
                });

                existingKeys.Add(relationshipKey);
            }

            return additions;
        }

        private async Task<List<ExecutiveTreeNodeDto>> GetExecutivesForSubHODAsync(
            int subHodId, string employeeCode, bool isAdmin,
            int? departmentId = null, int? subDepartmentId = null, int? hodId = null,
            HashSet<string>? activeRelationshipKeys = null)
        {
            const string sql = "EXEC [Hie].[sp_GetExecutivesForSubHOD_RoleBased] @SubHODId, @EmployeeCode, @IsAdmin, @DepartmentId, @SubDepartmentId, @HODId";

            using var connection = new SqlConnection(_connectionString);
            List<ExecutiveTreeNodeDto> executives;
            try
            {
                executives = (await connection.QueryAsync<ExecutiveTreeNodeDto>(sql, new
                {
                    SubHODId = subHodId,
                    EmployeeCode = employeeCode,
                    IsAdmin = isAdmin,
                    DepartmentId = departmentId,
                    SubDepartmentId = subDepartmentId,
                    HODId = hodId
                })).ToList();
            }
            catch (SqlException ex) when (IsRemovedSalaryColumnError(ex))
            {
                executives = await GetExecutivesForSubHODWithoutSalaryAsync(connection, subHodId, departmentId, subDepartmentId, hodId);
            }

            if (activeRelationshipKeys == null || !hodId.HasValue)
                return executives;

            var managerId = subHodId > 0 ? subHodId : hodId.Value;
            var activePairKeys = activeRelationshipKeys
                .Select(key =>
                {
                    var parts = key.Split('|');
                    return parts.Length >= 2 ? $"{parts[0]}|{parts[1]}" : key;
                })
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return executives
                .Where(exec => activeRelationshipKeys.Contains(
                    BuildRelationshipKey(
                        exec.EmployeeId,
                        managerId,
                        departmentId,
                        subDepartmentId))
                    || activePairKeys.Contains(BuildRelationshipPairKey(exec.EmployeeId, managerId)))
                .ToList();
        }

        private async Task<List<ExecutiveTreeNodeDto>> GetExecutivesForSubHODWithoutSalaryAsync(
            SqlConnection connection,
            int subHodId,
            int? departmentId,
            int? subDepartmentId,
            int? hodId)
        {
            const string sql = @"
SELECT
    e.EmployeeId,
    e.EmployeeCode,
    e.EmployeeName,
    e.Designation,
    d.DepartmentName,
    sd.SubDepartmentName,
    rr.IsPrimary,
    CAST(NULL AS NVARCHAR(50)) AS Salary,
    ISNULL(rt.ReportingTypeName, 'Administrative') AS ReportingTypeName
FROM [Hie].[EmployeeReportingRelationships] rr
INNER JOIN [Hie].[Employees] e
    ON e.EmployeeId = rr.EmployeeId
    AND e.IsActive = 1
    AND e.RoleTypeId = 3
LEFT JOIN [Hie].[Departments] d
    ON d.DepartmentId = rr.DepartmentId
LEFT JOIN [Hie].[SubDepartments] sd
    ON sd.SubDepartmentId = rr.SubDepartmentId
LEFT JOIN [Hie].[ReportingTypes] rt
    ON rt.ReportingTypeId = rr.ReportingTypeId
WHERE rr.IsActive = 1
  AND rr.ReportsToEmployeeId = CASE WHEN @SubHODId > 0 THEN @SubHODId ELSE @HODId END
  AND (@DepartmentId IS NULL OR rr.DepartmentId = @DepartmentId)
  AND (@SubDepartmentId IS NULL OR rr.SubDepartmentId = @SubDepartmentId)
ORDER BY e.EmployeeName";

            return (await connection.QueryAsync<ExecutiveTreeNodeDto>(sql, new
            {
                SubHODId = subHodId,
                HODId = hodId,
                DepartmentId = departmentId,
                SubDepartmentId = subDepartmentId
            })).ToList();
        }

        #endregion

        #region Employee Operations

        public async Task<List<EmployeeDto>> GetEmployeesAsync(EmployeeSearchRequest request, string employeeCode, bool isAdmin)
        {
            const string sql = "EXEC [Hie].[sp_SearchEmployees_RoleBased] @EmployeeCode, @IsAdmin, @SearchTerm, @RoleTypeId, @DepartmentId, @IsActive, @PageNumber, @PageSize";

            using var connection = new SqlConnection(_connectionString);
            List<EmployeeDto> employees;
            try
            {
                employees = (await connection.QueryAsync<EmployeeDto>(sql, new
                {
                    EmployeeCode = employeeCode,
                    IsAdmin = isAdmin,
                    SearchTerm = request.SearchTerm,
                    RoleTypeId = request.RoleTypeId,
                    DepartmentId = request.DepartmentId,
                    IsActive = request.IsActive,
                    PageNumber = request.PageNumber,
                    PageSize = request.PageSize
                })).ToList();
            }
            catch (SqlException ex) when (IsRemovedSalaryColumnError(ex))
            {
                employees = await SearchEmployeesWithoutSalaryAsync(connection, request);
            }

            employees.ForEach(DecryptEmployeeSalary);
            return employees;
        }

        private async Task<List<EmployeeDto>> SearchEmployeesWithoutSalaryAsync(SqlConnection connection, EmployeeSearchRequest request)
        {
            const string sql = @"
WITH FilteredEmployees AS
(
    SELECT
        e.EmployeeId,
        e.EmployeeCode,
        e.EmployeeName,
        e.Email,
        e.Phone,
        e.Designation,
        e.RoleTypeId,
        CASE e.RoleTypeId
            WHEN 1 THEN 'HOD'
            WHEN 2 THEN 'Sub-HOD'
            WHEN 3 THEN 'Executive'
            ELSE 'Employee'
        END AS RoleName,
        e.PrimaryDepartmentId,
        d.DepartmentName AS PrimaryDepartmentName,
        e.DateOfJoining,
        e.IsActive,
        CAST(NULL AS NVARCHAR(50)) AS Salary,
        CASE WHEN COL_LENGTH('Hie.Employees', 'ViewSalary') IS NULL THEN CAST(0 AS BIT) ELSE ISNULL(e.ViewSalary, 0) END AS ViewSalary,
        COUNT(DISTINCT dr.RelationshipId) AS DirectReportCount,
        COUNT(DISTINCT mgr.RelationshipId) AS ManagerCount,
        ROW_NUMBER() OVER (ORDER BY e.EmployeeName) AS RowNumber
    FROM [Hie].[Employees] e
    LEFT JOIN [Hie].[Departments] d
        ON d.DepartmentId = e.PrimaryDepartmentId
    LEFT JOIN [Hie].[EmployeeReportingRelationships] dr
        ON dr.ReportsToEmployeeId = e.EmployeeId
        AND dr.IsActive = 1
    LEFT JOIN [Hie].[EmployeeReportingRelationships] mgr
        ON mgr.EmployeeId = e.EmployeeId
        AND mgr.IsActive = 1
    WHERE (@IsActive IS NULL OR e.IsActive = @IsActive)
      AND (@RoleTypeId IS NULL OR e.RoleTypeId = @RoleTypeId)
      AND (@DepartmentId IS NULL OR e.PrimaryDepartmentId = @DepartmentId)
      AND (
            @SearchTerm IS NULL
            OR @SearchTerm = ''
            OR e.EmployeeName LIKE '%' + @SearchTerm + '%'
            OR e.EmployeeCode LIKE '%' + @SearchTerm + '%'
            OR e.Designation LIKE '%' + @SearchTerm + '%'
          )
    GROUP BY e.EmployeeId, e.EmployeeCode, e.EmployeeName, e.Email, e.Phone, e.Designation,
             e.RoleTypeId, e.PrimaryDepartmentId, d.DepartmentName, e.DateOfJoining,
             e.IsActive, e.ViewSalary
)
SELECT *
FROM FilteredEmployees
WHERE RowNumber BETWEEN ((@PageNumber - 1) * @PageSize + 1) AND (@PageNumber * @PageSize)
ORDER BY RowNumber";

            return (await connection.QueryAsync<EmployeeDto>(sql, new
            {
                request.SearchTerm,
                request.RoleTypeId,
                request.DepartmentId,
                request.IsActive,
                request.PageNumber,
                request.PageSize
            })).ToList();
        }

        public async Task<EmployeeDetailDto> GetEmployeeByIdAsync(int employeeId, string employeeCode, bool isAdmin)
        {
            const string sql = "EXEC [Hie].[sp_GetEmployeeById_RoleBased] @TargetEmployeeId, @EmployeeCode, @IsAdmin";

            using var connection = new SqlConnection(_connectionString);
            var employee = await connection.QueryFirstOrDefaultAsync<EmployeeDetailDto>(
                sql,
                new { TargetEmployeeId = employeeId, EmployeeCode = employeeCode, IsAdmin = isAdmin }
            );

            if (employee != null && employee.EmployeeId > 0)
            {
                DecryptEmployeeSalary(employee);
                employee.ReportsTo = await GetEmployeeReportingToAsync(employeeId);
                employee.DirectReports = await GetEmployeeDirectReportsAsync(employeeId);
            }

            return employee;
        }

        public async Task<EmployeeDto> GetEmployeeByCodeAsync(string employeeCode)
        {
            const string sql = "EXEC [Hie].[sp_GetEmployeeByCode] @EmployeeCode";

            using var connection = new SqlConnection(_connectionString);
            var employee = await connection.QueryFirstOrDefaultAsync<EmployeeDto>(sql, new { EmployeeCode = employeeCode });
            DecryptEmployeeSalary(employee);
            return employee;
        }

        public async Task<List<EmployeeDto>> GetAvailableManagersAsync(int employeeId)
        {
            const string sql = "EXEC [Hie].[sp_GetAvailableManagers] @EmployeeId";

            using var connection = new SqlConnection(_connectionString);
            return (await connection.QueryAsync<EmployeeDto>(sql, new { EmployeeId = employeeId })).ToList();
        }

        public async Task<HierarchyApiResponse<EmployeeDto>> CreateEmployeeAsync(EmployeeRequest request)
        {
            const string sql = "EXEC [Hie].[sp_CreateEmployee] @EmployeeCode, @EmployeeName, @Email, @Phone, @Designation, @RoleTypeId, @PrimaryDepartmentId, @DateOfJoining, @CreatedBy";

            using var connection = new SqlConnection(_connectionString);
            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
                {
                    request.EmployeeCode,
                    request.EmployeeName,
                    request.Email,
                    request.Phone,
                    request.Designation,
                    request.RoleTypeId,
                    request.PrimaryDepartmentId,
                    request.DateOfJoining,
                    request.CreatedBy
                });

                if (IsDynamicSuccess(result))
                {
                    int newId = (int)result.EmployeeId;
                    var employee = await GetEmployeeByIdAsync(newId, "", true);
                    _ = LogAuditAsync("Create", "Employee", newId, newId, null, ToJson(request), $"Created {request.EmployeeName} ({request.EmployeeCode}) as {(request.RoleTypeId == 1 ? "HOD" : request.RoleTypeId == 2 ? "SubHOD" : "Executive")}", request.CreatedBy);
                    return HierarchyApiResponse<EmployeeDto>.SuccessResponse(employee, result?.Message?.ToString() ?? "Employee created successfully");
                }

                if (IsMissingJsUserDependencyError(result?.Message?.ToString()))
                    return await CreateEmployeeWithoutJsUserFallbackAsync(request);

                return HierarchyApiResponse<EmployeeDto>.ErrorResponse(result?.Message ?? "Error creating employee");
            }
            catch (SqlException ex) when (IsMissingJsUserDependencyError(ex.Message))
            {
                return await CreateEmployeeWithoutJsUserFallbackAsync(request);
            }
            catch (Exception ex)
            {
                return HierarchyApiResponse<EmployeeDto>.ErrorResponse("Error creating employee: " + ex.Message);
            }
        }

        private async Task<HierarchyApiResponse<EmployeeDto>> CreateEmployeeWithoutJsUserFallbackAsync(EmployeeRequest request)
        {
            const string duplicateSql = @"
SELECT TOP (1) EmployeeId
FROM [Hie].[Employees]
WHERE EmployeeCode = @EmployeeCode";

            const string insertSql = @"
INSERT INTO [Hie].[Employees]
(
    EmployeeCode,
    EmployeeName,
    Email,
    Phone,
    Designation,
    RoleTypeId,
    PrimaryDepartmentId,
    DateOfJoining,
    IsActive,
    CreatedOn,
    ModifiedOn
)
OUTPUT INSERTED.EmployeeId
VALUES
(
    @EmployeeCode,
    @EmployeeName,
    @Email,
    @Phone,
    @Designation,
    @RoleTypeId,
    @PrimaryDepartmentId,
    @DateOfJoining,
    @IsActive,
    GETDATE(),
    NULL
)";

            using var connection = new SqlConnection(_connectionString);

            try
            {
                var existingId = await connection.QueryFirstOrDefaultAsync<int?>(duplicateSql, new { request.EmployeeCode });
                if (existingId.HasValue)
                    return HierarchyApiResponse<EmployeeDto>.ErrorResponse($"Employee code '{request.EmployeeCode}' already exists.");

                var newId = await connection.ExecuteScalarAsync<int>(insertSql, new
                {
                    request.EmployeeCode,
                    request.EmployeeName,
                    request.Email,
                    request.Phone,
                    request.Designation,
                    request.RoleTypeId,
                    request.PrimaryDepartmentId,
                    request.DateOfJoining,
                    request.IsActive
                });

                var employee = await GetEmployeeByIdAsync(newId, "", true);
                _ = LogAuditAsync("Create", "Employee", newId, newId, null, ToJson(request), $"Created {request.EmployeeName} ({request.EmployeeCode}) without jsUser dependency", request.CreatedBy);

                return HierarchyApiResponse<EmployeeDto>.SuccessResponse(employee, "Employee created successfully");
            }
            catch (Exception ex)
            {
                return HierarchyApiResponse<EmployeeDto>.ErrorResponse("Error creating employee: " + ex.Message);
            }
        }

        public async Task<HierarchyApiResponse<EmployeeDto>> UpdateEmployeeAsync(EmployeeRequest request)
        {
            const string sql = "EXEC [Hie].[sp_UpdateEmployee] @EmployeeId, @EmployeeCode, @EmployeeName, @Email, @Phone, @Designation, @RoleTypeId, @PrimaryDepartmentId, @DateOfJoining, @IsActive, @ModifiedBy";

            using var connection = new SqlConnection(_connectionString);
            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
                {
                    request.EmployeeId,
                    request.EmployeeCode,
                    request.EmployeeName,
                    request.Email,
                    request.Phone,
                    request.Designation,
                    request.RoleTypeId,
                    request.PrimaryDepartmentId,
                    request.DateOfJoining,
                    request.IsActive,
                    ModifiedBy = request.CreatedBy
                });

                if (IsDynamicSuccess(result))
                {
                    var employee = await GetEmployeeByIdAsync(request.EmployeeId.Value, "", true);
                    return HierarchyApiResponse<EmployeeDto>.SuccessResponse(employee, result?.Message?.ToString() ?? "Employee updated successfully");
                }

                return HierarchyApiResponse<EmployeeDto>.ErrorResponse(result?.Message ?? "Error updating employee");
            }
            catch (Exception ex)
            {
                return HierarchyApiResponse<EmployeeDto>.ErrorResponse("Error updating employee: " + ex.Message);
            }
        }

        public async Task<HierarchyApiResponse<bool>> UpdateEmployeeFullAsync(UpdateEmployeeFullRequest req, int updatedBy)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var tx = conn.BeginTransaction();

                // SP 1: Get original role + HOD before update
                var origInfo = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "EXEC [Hie].[sp_GetEmployeeOriginalInfo] @EmployeeId",
                    new { req.EmployeeId }, tx);
                int originalRoleTypeId = (int)(origInfo?.RoleTypeId ?? req.RoleTypeId);
                int? originalHodId = origInfo?.OriginalHodId == null ? null : (int?)origInfo.OriginalHodId;
                int? originalDeptId = origInfo?.PrimaryDepartmentId == null ? null : (int?)origInfo.PrimaryDepartmentId;

                // SP 2: Get department change impact
                var impact = await conn.QueryFirstOrDefaultAsync<DepartmentChangeImpactDto>(
                    "EXEC [Hie].[sp_GetDeptChangeImpact] @EmployeeId, @NewDepartmentId, @NewSubDepartmentId",
                    new { req.EmployeeId, NewDepartmentId = req.DepartmentId, NewSubDepartmentId = req.SubDepartmentId }, tx);
                impact ??= new DepartmentChangeImpactDto();

                bool roleChangedFromSubHodToHod = originalRoleTypeId == (int)RoleTypeEnum.SubHOD && req.RoleTypeId == (int)RoleTypeEnum.HOD;

                const string updateEmployeeSql = @"
UPDATE [Hie].[Employees]
SET EmployeeName = @EmployeeName,
    EmployeeCode = @EmployeeCode,
    Designation = @Designation,
    DateOfJoining = @DateOfJoining,
    RoleTypeId = @RoleTypeId,
    PrimaryDepartmentId = @DepartmentId,
    IsActive = @IsActive
WHERE EmployeeId = @EmployeeId";

                var rowsAffected = await conn.ExecuteAsync(updateEmployeeSql, new
                {
                    req.EmployeeId,
                    req.EmployeeName,
                    req.EmployeeCode,
                    req.Designation,
                    req.DateOfJoining,
                    req.RoleTypeId,
                    req.DepartmentId,
                    req.IsActive
                }, tx);

                if (rowsAffected <= 0)
                {
                    tx.Rollback();
                    return HierarchyApiResponse<bool>.ErrorResponse("Employee not found or update failed");
                }

                string message = "Employee updated successfully";

                // SP 3: Sync relationships
                await conn.ExecuteAsync(
                    "EXEC [Hie].[sp_SyncEmployeeRelationships] @EmployeeId, @RoleTypeId, @ReportsToEmpId, @DepartmentId, @SubDepartmentId",
                    new { req.EmployeeId, req.RoleTypeId, req.ReportsToEmpId, req.DepartmentId, req.SubDepartmentId }, tx);

                string detachInfo = "";
                if (impact.RequiresMoveTeam)
                {
                    if (req.MoveTeamWithDepartment)
                    {
                        // SP 4: Move team
                        await conn.ExecuteAsync("EXEC [Hie].[sp_MoveChildTeamWithDept] @EmployeeId, @RoleTypeId, @DepartmentId, @SubDepartmentId",
                            new { req.EmployeeId, req.RoleTypeId, req.DepartmentId, req.SubDepartmentId }, tx);
                    }
                    else
                    {
                        // SP 5: Detach team
                        var detachResult = await conn.QueryFirstOrDefaultAsync<dynamic>(
                            "EXEC [Hie].[sp_DetachChildTeam] @EmployeeId, @OriginalRoleTypeId, @PreloadedHodId",
                            new { req.EmployeeId, OriginalRoleTypeId = originalRoleTypeId, PreloadedHodId = originalHodId }, tx);
                        int detachedCount = (int)(detachResult?.DetachedCount ?? 0);
                        if (detachedCount > 0) detachInfo = $" {detachedCount} team member(s) were detached and may need reassignment.";
                    }
                }
                else if (roleChangedFromSubHodToHod)
                {
                    // SP 6: Reassign execs on role change
                    var reassignResult = await conn.QueryFirstOrDefaultAsync<dynamic>(
                        "EXEC [Hie].[sp_ReassignExecsOnRoleChange] @EmployeeId, @OriginalHodId",
                        new { req.EmployeeId, OriginalHodId = originalHodId }, tx);
                    int reassigned = (int)(reassignResult?.ReassignedCount ?? 0);
                    if (reassigned > 0) detachInfo = $" {reassigned} executive(s) reassigned to HOD.";
                }
                tx.Commit();

                // Audit log
                var actions = new List<string>();
                if (roleChangedFromSubHodToHod) actions.Add("RoleChange");
                if (impact.RequiresMoveTeam && req.MoveTeamWithDepartment) actions.Add("MoveTeam");
                if (impact.RequiresMoveTeam && !req.MoveTeamWithDepartment) actions.Add("DetachTeam");
                if (impact.DepartmentChanged) actions.Add("DeptChange");
                string actionType = actions.Count > 0 ? string.Join("+", actions) : "Update";
                _ = LogAuditAsync(actionType, "Employee", req.EmployeeId, req.EmployeeId,
                    ToJson(new { RoleTypeId = originalRoleTypeId, DepartmentId = originalDeptId }),
                    ToJson(new { req.RoleTypeId, req.DepartmentId, req.SubDepartmentId, req.ReportsToEmpId, req.IsActive }),
                    $"Updated {req.EmployeeName} ({req.EmployeeCode}).{detachInfo}", updatedBy);

                string finalMessage = string.IsNullOrWhiteSpace(message) ? "Employee updated successfully" : message;
                return HierarchyApiResponse<bool>.SuccessResponse(true, finalMessage + detachInfo);
            }
            catch (Exception ex) { return HierarchyApiResponse<bool>.ErrorResponse(ex.Message); }
        }

        public async Task<DepartmentChangeImpactDto> GetDepartmentChangeImpactAsync(DepartmentChangeImpactRequest request)
        {
            using var conn = new SqlConnection(_connectionString);
            var result = await conn.QueryFirstOrDefaultAsync<DepartmentChangeImpactDto>(
                "EXEC [Hie].[sp_GetDeptChangeImpact] @EmployeeId, @NewDepartmentId, @NewSubDepartmentId",
                new { request.EmployeeId, NewDepartmentId = request.DepartmentId, NewSubDepartmentId = request.SubDepartmentId });
            return result ?? new DepartmentChangeImpactDto();
        }

        // Private helpers replaced by stored procedures:
        // sp_GetDeptChangeImpact, sp_SyncEmployeeRelationships, sp_MoveChildTeamWithDept,
        // sp_DetachChildTeam, sp_ReassignExecsOnRoleChange, sp_UpsertPrimaryRelationship

        public async Task<HierarchyApiResponse<BulkAssignTeamResult>> BulkAssignTeamAsync(BulkAssignTeamRequest request, int updatedBy)
        {
            try
            {
                if (request.HodEmployeeId <= 0)
                    return HierarchyApiResponse<BulkAssignTeamResult>.ErrorResponse("Select a HOD");
                if (!request.DepartmentId.HasValue || request.DepartmentId.Value <= 0)
                    return HierarchyApiResponse<BulkAssignTeamResult>.ErrorResponse("Select a department");

                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var tx = conn.BeginTransaction();

                // Validate HOD
                var hodCheck = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "EXEC [Hie].[sp_ValidateAndSetDepartment] @EmployeeId, @ExpectedRoleTypeId, @DepartmentId",
                    new { EmployeeId = request.HodEmployeeId, ExpectedRoleTypeId = 1, DepartmentId = (int?)null }, tx);
                if ((int)(hodCheck?.Success ?? 0) != 1)
                { tx.Rollback(); return HierarchyApiResponse<BulkAssignTeamResult>.ErrorResponse("Selected HOD is invalid"); }

                var result = new BulkAssignTeamResult();

                if (request.SubHodEmployeeId.HasValue && request.SubHodEmployeeId.Value > 0)
                {
                    // Pass null for DepartmentId so the SP only validates role type (same as HOD validation),
                    // avoiding any side-effect that updates the Sub-HOD's primary department and removes them
                    // from their existing team(s).
                    var subCheck = await conn.QueryFirstOrDefaultAsync<dynamic>(
                        "EXEC [Hie].[sp_ValidateAndSetDepartment] @EmployeeId, @ExpectedRoleTypeId, @DepartmentId",
                        new { EmployeeId = request.SubHodEmployeeId.Value, ExpectedRoleTypeId = 2, DepartmentId = (int?)null }, tx);
                    if ((int)(subCheck?.Success ?? 0) != 1)
                    { tx.Rollback(); return HierarchyApiResponse<BulkAssignTeamResult>.ErrorResponse("Selected Sub-HOD is invalid"); }

                    // Additive-only: add the new Sub-HOD scope without deactivating prior sub-department teams.
                    // One Sub-HOD can manage many sub-departments simultaneously.
                    await BulkAddSubHodRelationshipAsync(
                        conn,
                        tx,
                        request.SubHodEmployeeId.Value,
                        request.HodEmployeeId,
                        request.DepartmentId,
                        request.SubDepartmentId);
                    result.RelationshipsUpdated++;
                    result.SubHodAssigned = 1;
                }

                int managerId = request.SubHodEmployeeId ?? request.HodEmployeeId;
                var execIds = (request.ExecutiveEmployeeIds ?? new List<int>()).Where(id => id > 0).Distinct().ToList();
                foreach (var execId in execIds)
                {
                    var execCheck = await conn.QueryFirstOrDefaultAsync<dynamic>(
                        "EXEC [Hie].[sp_ValidateAndSetDepartment] @EmployeeId, @ExpectedRoleTypeId, @DepartmentId",
                        new { EmployeeId = execId, ExpectedRoleTypeId = 3, DepartmentId = request.DepartmentId }, tx);
                    if ((int)(execCheck?.Success ?? 0) != 1) continue;

                    await conn.ExecuteAsync(
                        "EXEC [Hie].[sp_UpsertPrimaryRelationship] @EmployeeId, @ReportsToEmployeeId, @DepartmentId, @SubDepartmentId",
                        new { EmployeeId = execId, ReportsToEmployeeId = managerId, DepartmentId = request.DepartmentId, SubDepartmentId = request.SubDepartmentId }, tx);
                    result.RelationshipsUpdated++;
                    result.ExecutivesAssigned++;
                }

                _ = LogAuditAsync("BulkAssign", "Employee", request.HodEmployeeId, request.HodEmployeeId, null, ToJson(new { request.DepartmentId, request.SubDepartmentId, request.SubHodEmployeeId, ExecCount = result.ExecutivesAssigned }), $"Bulk assigned {result.ExecutivesAssigned} exec(s), {result.SubHodAssigned} SubHOD(s)", updatedBy);
                tx.Commit();
                return HierarchyApiResponse<BulkAssignTeamResult>.SuccessResponse(result, "Team assigned successfully");
            }
            catch (Exception ex)
            {
                return HierarchyApiResponse<BulkAssignTeamResult>.ErrorResponse(ex.Message);
            }
        }

        public async Task<HierarchyApiResponse<bool>> DeactivateEmployeeAsync(int employeeId)
        {
            const string sql = "EXEC [Hie].[sp_DeactivateEmployee] @EmployeeId";

            using var connection = new SqlConnection(_connectionString);
            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { EmployeeId = employeeId });

                if (IsDynamicSuccess(result))
                {
                    _ = LogAuditAsync("Deactivate", "Employee", employeeId, employeeId, null, null, "Employee deactivated", null);
                    return HierarchyApiResponse<bool>.SuccessResponse(true, "Employee deactivated successfully");
                }
                return HierarchyApiResponse<bool>.ErrorResponse(result?.Message ?? "Error deactivating employee");
            }
            catch (Exception ex)
            {
                return HierarchyApiResponse<bool>.ErrorResponse("Error deactivating employee: " + ex.Message);
            }
        }

        #endregion

        #region Reporting Relationship Operations

        public async Task<List<ReportingToDto>> GetEmployeeReportingToAsync(int employeeId, bool includeInactive = false)
        {
            const string sql = "EXEC [Hie].[sp_GetEmployeeReportingTo] @EmployeeId, @IncludeInactive";
            using var connection = new SqlConnection(_connectionString);
            return (await connection.QueryAsync<ReportingToDto>(sql, new { EmployeeId = employeeId, IncludeInactive = includeInactive })).ToList();
        }

        public async Task<List<DirectReportDto>> GetEmployeeDirectReportsAsync(int managerId, bool includeInactive = false)
        {
            const string sql = "EXEC [Hie].[sp_GetEmployeeDirectReports] @ManagerId, @IncludeInactive";
            using var connection = new SqlConnection(_connectionString);
            return (await connection.QueryAsync<DirectReportDto>(sql, new { ManagerId = managerId, IncludeInactive = includeInactive })).ToList();
        }

        public async Task<ReportingRelationshipDto> GetRelationshipByIdAsync(int relationshipId)
        {
            const string sql = "EXEC [Hie].[sp_GetRelationshipById] @RelationshipId";
            using var connection = new SqlConnection(_connectionString);
            return await connection.QueryFirstOrDefaultAsync<ReportingRelationshipDto>(sql, new { RelationshipId = relationshipId });
        }

        public async Task<OperationResult> AddReportingRelationshipAsync(AddReportingRelationshipRequest request)
        {
            const string sql = "EXEC [Hie].[sp_AddReportingRelationship] @EmployeeId, @ReportsToEmployeeId, @ReportingTypeId, @DepartmentId, @SubDepartmentId, @IsPrimary, @EffectiveFrom, @Notes, @CreatedBy";
            using var connection = new SqlConnection(_connectionString);
            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
                {
                    request.EmployeeId,
                    request.ReportsToEmployeeId,
                    request.ReportingTypeId,
                    request.DepartmentId,
                    request.SubDepartmentId,
                    request.IsPrimary,
                    EffectiveFrom = request.EffectiveFrom ?? DateTime.Today,
                    request.Notes,
                    request.CreatedBy
                });
                return new OperationResult { Success = IsDynamicSuccess(result), Message = result?.Message ?? "Error", RelationshipId = result?.RelationshipId ?? 0 };
            }
            catch (Exception ex) { return new OperationResult { Success = false, Message = ex.Message, RelationshipId = 0 }; }
        }

        public async Task<OperationResult> UpdateReportingRelationshipAsync(UpdateReportingRelationshipRequest request)
        {
            const string sql = "EXEC [Hie].[sp_UpdateReportingRelationship] @RelationshipId, @ReportsToEmployeeId, @ReportingTypeId, @DepartmentId, @SubDepartmentId, @IsPrimary, @EffectiveFrom, @EffectiveTo, @IsActive, @Notes";
            using var connection = new SqlConnection(_connectionString);
            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new
                {
                    request.RelationshipId,
                    request.ReportsToEmployeeId,
                    request.ReportingTypeId,
                    request.DepartmentId,
                    request.SubDepartmentId,
                    request.IsPrimary,
                    request.EffectiveFrom,
                    request.EffectiveTo,
                    request.IsActive,
                    request.Notes
                });
                return new OperationResult { Success = IsDynamicSuccess(result), Message = result?.Message ?? "Error", RelationshipId = request.RelationshipId };
            }
            catch (Exception ex) { return new OperationResult { Success = false, Message = ex.Message, RelationshipId = request.RelationshipId }; }
        }

        public async Task<OperationResult> RemoveReportingRelationshipAsync(int relationshipId)
        {
            const string sql = "EXEC [Hie].[sp_RemoveReportingRelationship] @RelationshipId";
            using var connection = new SqlConnection(_connectionString);
            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { RelationshipId = relationshipId });
                return new OperationResult { Success = IsDynamicSuccess(result), Message = result?.Message ?? "Error", RelationshipId = relationshipId };
            }
            catch (Exception ex) { return new OperationResult { Success = false, Message = ex.Message, RelationshipId = relationshipId }; }
        }

        public async Task<OperationResult> SetPrimaryRelationshipAsync(int relationshipId)
        {
            const string sql = "EXEC [Hie].[sp_SetPrimaryRelationship] @RelationshipId";
            using var connection = new SqlConnection(_connectionString);
            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { RelationshipId = relationshipId });
                return new OperationResult { Success = IsDynamicSuccess(result), Message = result?.Message ?? "Error", RelationshipId = relationshipId };
            }
            catch (Exception ex) { return new OperationResult { Success = false, Message = ex.Message, RelationshipId = relationshipId }; }
        }

        #endregion

        #region Master Data

        public async Task<List<DepartmentDto>> GetDepartmentsAsync(bool includeSubDepartments = false)
        {
            const string sql = "EXEC [Hie].[sp_GetDepartments] @IncludeSubDepartments";
            using var connection = new SqlConnection(_connectionString);
            var departments = (await connection.QueryAsync<DepartmentDto>(sql, new { IncludeSubDepartments = includeSubDepartments })).ToList();
            if (includeSubDepartments)
                foreach (var dept in departments)
                    dept.SubDepartments = await GetSubDepartmentsAsync(dept.DepartmentId);
            return departments;
        }

        public async Task<List<SubDepartmentDto>> GetSubDepartmentsAsync(int departmentId)
        {
            const string sql = "EXEC [Hie].[sp_GetSubDepartments] @DepartmentId";
            using var connection = new SqlConnection(_connectionString);
            return (await connection.QueryAsync<SubDepartmentDto>(sql, new { DepartmentId = departmentId })).ToList();
        }

        public async Task<List<RoleTypeDto>> GetRoleTypesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return (await connection.QueryAsync<RoleTypeDto>("EXEC [Hie].[sp_GetRoleTypes]")).ToList();
        }

        public async Task<List<ReportingTypeDto>> GetReportingTypesAsync()
        {
            using var connection = new SqlConnection(_connectionString);
            return (await connection.QueryAsync<ReportingTypeDto>("EXEC [Hie].[sp_GetReportingTypes]")).ToList();
        }

        #endregion

        #region Export

        public async Task<List<HierarchyExportRowDto>> GetHierarchyForExportAsync(string employeeCode, bool isAdmin)
        {
            const string sql = "EXEC [Hie].[sp_ExportHierarchyFlat_RoleBased] @EmployeeCode, @IsAdmin";
            using var connection = new SqlConnection(_connectionString);
            return (await connection.QueryAsync<HierarchyExportRowDto>(sql, new { EmployeeCode = employeeCode, IsAdmin = isAdmin })).ToList();
        }

        #endregion

        #region Utility

        public async Task<List<HODDepartmentDto>> GetDepartmentsForHODAsync(int hodId)
        {
            const string sql = "EXEC [Hie].[sp_GetDepartmentsForHOD] @HODId";
            using var connection = new SqlConnection(_connectionString);
            return (await connection.QueryAsync<HODDepartmentDto>(sql, new { HODId = hodId })).ToList();
        }

        #endregion

        #region Master Flat & Import

        public async Task<List<MasterFlatRowDto>> GetMasterFlatAsync(string employeeCode, bool isAdmin)
        {
            var tree = await GetHierarchyTreeAsync(employeeCode, isAdmin);
            var rows = new List<MasterFlatRowDto>();
            int idx = 0;

            foreach (var hod in tree)
            {
                if (!hod.SubHODs.Any())
                {
                    rows.Add(new MasterFlatRowDto { RowIndex = ++idx, HodEmployeeId = hod.EmployeeId, HodCode = hod.EmployeeCode, HodName = hod.EmployeeName, HodDesignation = hod.Designation ?? "", HodSalary = hod.Salary?.ToString() ?? "" });
                    continue;
                }

                foreach (var sub in hod.SubHODs)
                {
                    bool hasSubHod = sub.EmployeeId > 0;
                    if (!sub.Executives.Any())
                    {
                        rows.Add(new MasterFlatRowDto { RowIndex = ++idx, HodEmployeeId = hod.EmployeeId, HodCode = hod.EmployeeCode, HodName = hod.EmployeeName, HodSalary = hod.Salary?.ToString() ?? "", DepartmentId = sub.DepartmentId, DepartmentName = sub.DepartmentName ?? "", SubDepartmentId = sub.SubDepartmentId, SubDepartmentName = sub.SubDepartmentName ?? "", SubHodEmployeeId = hasSubHod ? sub.EmployeeId : 0, SubHodCode = hasSubHod ? sub.EmployeeCode : "", SubHodName = hasSubHod ? sub.EmployeeName : "", SubHodDesignation = sub.Designation ?? "", SubHodSalary = sub.Salary ?? "" });
                        continue;
                    }
                    foreach (var ex in sub.Executives)
                    {
                        rows.Add(new MasterFlatRowDto { RowIndex = ++idx, HodEmployeeId = hod.EmployeeId, HodCode = hod.EmployeeCode, HodName = hod.EmployeeName, HodSalary = hod.Salary?.ToString() ?? "", DepartmentId = sub.DepartmentId, DepartmentName = sub.DepartmentName ?? "", SubDepartmentId = sub.SubDepartmentId, SubDepartmentName = sub.SubDepartmentName ?? "", SubHodEmployeeId = hasSubHod ? sub.EmployeeId : 0, SubHodCode = hasSubHod ? sub.EmployeeCode : "", SubHodName = hasSubHod ? sub.EmployeeName : "", SubHodDesignation = sub.Designation ?? "", SubHodSalary = sub.Salary ?? "", ExecEmployeeId = ex.EmployeeId, ExecCode = ex.EmployeeCode ?? "", ExecName = ex.EmployeeName ?? "", ExecDesignation = ex.Designation ?? "", ExecSalary = ex.Salary ?? "" });
                    }
                }
            }
            return rows;
        }

        public async Task<ImportSummary> ProcessImportAsync(List<ImportRowRequest> rows)
        {
            var summary = new ImportSummary { TotalRows = rows.Count };
            var deptCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var subDeptCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var empCache = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var row in rows)
            {
                var result = new ImportRowResult { RowNumber = row.RowNumber, HodName = row.HodName, ExecName = row.ExecName };
                try
                {
                    int deptId = 0;
                    if (!string.IsNullOrWhiteSpace(row.DepartmentName))
                    {
                        bool pre = deptCache.ContainsKey(row.DepartmentName);
                        deptId = await UpsertDepartmentAsync(connection, row.DepartmentName, deptCache);
                        result.DeptAction = pre ? "existing" : (deptCache[row.DepartmentName] > 0 ? "created" : "existing");
                        if (result.DeptAction == "created") summary.NewDepartments++;
                    }
                    int subDeptId = 0;
                    if (!string.IsNullOrWhiteSpace(row.SubDepartmentName) && deptId > 0)
                    {
                        var sdKey = $"{deptId}_{row.SubDepartmentName}";
                        bool pre = subDeptCache.ContainsKey(sdKey);
                        subDeptId = await UpsertSubDepartmentAsync(connection, row.SubDepartmentName, deptId, subDeptCache);
                        result.SubDeptAction = pre ? "existing" : "created";
                        if (result.SubDeptAction == "created") summary.NewSubDepartments++;
                    }
                    int hodId = 0;
                    bool hodValid = !string.IsNullOrWhiteSpace(row.HodCode) && row.HodCode.Trim() != "0";
                    if (hodValid)
                    {
                        var (id, action, isNew) = await UpsertEmployeeAsync(connection, row.HodCode, row.HodName, row.HodDesignation, 1, deptId, empCache);
                        hodId = id; result.HodAction = action;
                        if (isNew) summary.NewEmployees++; else if (action == "updated") summary.UpdatedEmployees++;
                    }
                    int subHodId = 0;
                    bool shValid = !string.IsNullOrWhiteSpace(row.SubHodCode) && row.SubHodCode.Trim() != "0";
                    if (shValid)
                    {
                        var (id, action, isNew) = await UpsertEmployeeAsync(connection, row.SubHodCode, row.SubHodName, row.SubHodDesignation, 2, deptId, empCache);
                        subHodId = id; result.SubHodAction = action;
                        if (isNew) summary.NewEmployees++; else if (action == "updated") summary.UpdatedEmployees++;
                        if (hodId > 0)
                            await UpsertRelationshipAsync(connection, subHodId, hodId, deptId, subDeptId > 0 ? (int?)subDeptId : null);
                    }
                    bool exValid = !string.IsNullOrWhiteSpace(row.ExecCode) && row.ExecCode.Trim() != "0";
                    if (exValid)
                    {
                        var (id, action, isNew) = await UpsertEmployeeAsync(connection, row.ExecCode, row.ExecName, row.ExecDesignation, 3, deptId, empCache);
                        result.ExecAction = action;
                        if (isNew) summary.NewEmployees++; else if (action == "updated") summary.UpdatedEmployees++;
                        int reportsTo = subHodId > 0 ? subHodId : hodId;
                        if (reportsTo > 0)
                            await UpsertRelationshipAsync(connection, id, reportsTo, deptId, subDeptId > 0 ? (int?)subDeptId : null);
                    }
                    result.Status = "success"; summary.SuccessCount++;
                }
                catch (Exception ex) { result.Status = "error"; result.Message = ex.Message; summary.ErrorCount++; }
                summary.Results.Add(result);
            }
            return summary;
        }

        private async Task<int> UpsertDepartmentAsync(SqlConnection conn, string name, Dictionary<string, int> cache)
        {
            name = name.Trim();
            if (cache.TryGetValue(name, out int hit)) return hit;
            var result = await conn.QueryFirstOrDefaultAsync<dynamic>("EXEC [Hie].[sp_UpsertDepartment] @DepartmentName", new { DepartmentName = name });
            int id = (int)result.DepartmentId;
            cache[name] = id;
            return id;
        }

        private async Task<int> UpsertSubDepartmentAsync(SqlConnection conn, string name, int deptId, Dictionary<string, int> cache)
        {
            name = name.Trim();
            var key = $"{deptId}_{name}";
            if (cache.TryGetValue(key, out int hit)) return hit;
            var result = await conn.QueryFirstOrDefaultAsync<dynamic>("EXEC [Hie].[sp_UpsertSubDepartment] @DepartmentId, @SubDepartmentName", new { DepartmentId = deptId, SubDepartmentName = name });
            int id = (int)result.SubDepartmentId;
            cache[key] = id;
            return id;
        }

        private async Task<(int Id, string Action, bool IsNew)> UpsertEmployeeAsync(SqlConnection conn, string code, string name, string desig, int roleTypeId, int deptId, Dictionary<string, int> cache)
        {
            code = code.Trim();
            if (cache.TryGetValue(code, out int cachedId)) return (cachedId, "existing", false);
            var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                "EXEC [Hie].[sp_UpsertEmployee] @EmployeeCode, @EmployeeName, @Designation, @RoleTypeId, @DepartmentId",
                new { EmployeeCode = code, EmployeeName = name?.Trim(), Designation = desig?.Trim(), RoleTypeId = roleTypeId, DepartmentId = deptId > 0 ? (int?)deptId : null });
            int eid = (int)result.EmployeeId;
            bool wasCreated = (int)(result.WasCreated ?? 0) == 1;
            cache[code] = eid;
            return (eid, wasCreated ? "created" : "updated", wasCreated);
        }

        private async Task UpsertRelationshipAsync(SqlConnection conn, int empId, int mgrId, int deptId, int? subDeptId)
        {
            var exists = await conn.QueryFirstOrDefaultAsync<int?>("SELECT RelationshipId FROM [Hie].[EmployeeReportingRelationships] WHERE EmployeeId = @E AND ReportsToEmployeeId = @M AND IsActive = 1", new { E = empId, M = mgrId });
            if (!exists.HasValue)
                await conn.ExecuteAsync("INSERT INTO [Hie].[EmployeeReportingRelationships](EmployeeId, ReportsToEmployeeId, ReportingTypeId, DepartmentId, SubDepartmentId, IsPrimary, EffectiveFrom, IsActive) VALUES(@E, @M, 1, @D, @SD, 1, GETDATE(), 1)", new { E = empId, M = mgrId, D = deptId > 0 ? (int?)deptId : null, SD = subDeptId });
        }

        #endregion

        #region Admin Master Grid

        public async Task<List<MasterFlatAdminRowDto>> GetMasterFlatAdminAsync(string employeeCode)
        {
            try
            {
                // Use the same tree-based data source as the Org Tree view
                // This ensures the Hierarchy Master table shows the same data
                var flatRows = await GetMasterFlatAsync(employeeCode, true);

                // Collect all unique employee IDs for metadata enrichment
                var empIds = new HashSet<int>();
                foreach (var r in flatRows)
                {
                    if (r.HodEmployeeId > 0) empIds.Add(r.HodEmployeeId);
                    if (r.SubHodEmployeeId > 0) empIds.Add(r.SubHodEmployeeId);
                    if (r.ExecEmployeeId > 0) empIds.Add(r.ExecEmployeeId);
                }

                // Fetch metadata (DOJ, IsActive, Designation) for all employees
                var employeeMeta = new Dictionary<int, dynamic>();
                if (empIds.Any())
                {
                    using var conn = new SqlConnection(_connectionString);
                    var metas = await conn.QueryAsync<dynamic>(
                        "SELECT EmployeeId, EmployeeName, Designation, DateOfJoining, IsActive " +
                        "FROM [Hie].[Employees] WHERE EmployeeId IN (SELECT value FROM STRING_SPLIT(@Ids, ','))",
                        new { Ids = string.Join(",", empIds) });
                    foreach (var m in metas)
                        employeeMeta[(int)m.EmployeeId] = m;
                }

                // Map flat rows to admin rows with metadata
                var adminRows = flatRows.Select(r => MapTreeRowToAdminRow(r, employeeMeta)).ToList();

                // Deduplicate and sort
                var deduped = adminRows
                    .GroupBy(GetMasterFlatAdminKey)
                    .Select(g => g.First())
                    .OrderBy(x => x.HodName)
                    .ThenBy(x => x.DepartmentName)
                    .ThenBy(x => x.SubDepartmentName)
                    .ThenBy(x => x.SubHodName)
                    .ThenBy(x => x.ExecName)
                    .ToList();

                for (var i = 0; i < deduped.Count; i++)
                    deduped[i].RowIndex = i + 1;

                return deduped;
            }
            catch (Exception ex) { throw new Exception("Error loading admin master grid: " + ex.Message, ex); }
        }

        private static string GetMasterFlatAdminKey(MasterFlatAdminRowDto row) =>
            $"{row.HodEmployeeId}|{row.DepartmentId ?? 0}|{row.SubDepartmentId ?? 0}|{row.SubHodEmployeeId}|{row.ExecEmployeeId}";

        private static string GetMasterFlatKey(MasterFlatRowDto row) =>
            $"{row.HodEmployeeId}|{row.DepartmentId}|{row.SubDepartmentId}|{row.SubHodEmployeeId}|{row.ExecEmployeeId}";

        private static MasterFlatAdminRowDto MapTreeRowToAdminRow(
            MasterFlatRowDto row,
            IReadOnlyDictionary<int, dynamic> employeeMeta)
        {
            employeeMeta.TryGetValue(row.HodEmployeeId, out var hodMeta);
            employeeMeta.TryGetValue(row.SubHodEmployeeId, out var subMeta);
            employeeMeta.TryGetValue(row.ExecEmployeeId, out var execMeta);

            return new MasterFlatAdminRowDto
            {
                RowIndex = row.RowIndex,
                HodEmployeeId = row.HodEmployeeId,
                HodCode = row.HodCode,
                HodName = row.HodName,
                HodDesignation = !string.IsNullOrWhiteSpace(row.HodDesignation) ? row.HodDesignation : hodMeta?.Designation?.ToString(),
                HodSalary = null,
                HodDateOfJoining = TryGetDate(hodMeta?.DateOfJoining),
                HodIsActive = TryGetBool(hodMeta?.IsActive, true),
                DepartmentId = row.DepartmentId > 0 ? row.DepartmentId : null,
                DepartmentName = row.DepartmentName,
                SubDepartmentId = row.SubDepartmentId > 0 ? row.SubDepartmentId : null,
                SubDepartmentName = row.SubDepartmentName,
                SubHodEmployeeId = row.SubHodEmployeeId,
                SubHodCode = row.SubHodCode,
                SubHodName = row.SubHodName,
                SubHodDesignation = !string.IsNullOrWhiteSpace(row.SubHodDesignation) ? row.SubHodDesignation : subMeta?.Designation?.ToString(),
                SubHodSalary = null,
                SubHodDateOfJoining = TryGetDate(subMeta?.DateOfJoining),
                SubHodIsActive = row.SubHodEmployeeId > 0 ? TryGetBool(subMeta?.IsActive, true) : false,
                ExecEmployeeId = row.ExecEmployeeId,
                ExecCode = row.ExecCode,
                ExecName = row.ExecName,
                ExecDesignation = !string.IsNullOrWhiteSpace(row.ExecDesignation) ? row.ExecDesignation : execMeta?.Designation?.ToString(),
                ExecSalary = null,
                ExecDateOfJoining = TryGetDate(execMeta?.DateOfJoining),
                ExecIsActive = row.ExecEmployeeId > 0 ? TryGetBool(execMeta?.IsActive, true) : false
            };
        }

        private static decimal? TryGetDecimal(object? dbValue, string? fallback = null)
        {
            var decryptedDb = DecryptSalaryString(dbValue?.ToString());
            if (!string.IsNullOrWhiteSpace(decryptedDb) && decimal.TryParse(decryptedDb, NumberStyles.Any, CultureInfo.InvariantCulture, out var dbParsed))
                return dbParsed;

            var decryptedFallback = DecryptSalaryString(fallback);
            if (!string.IsNullOrWhiteSpace(decryptedFallback) && decimal.TryParse(decryptedFallback, NumberStyles.Any, CultureInfo.InvariantCulture, out var fallbackParsed))
                return fallbackParsed;

            return null;
        }

        private static DateTime? TryGetDate(object? dbValue)
        {
            if (dbValue != null && DateTime.TryParse(dbValue.ToString(), out var parsed))
                return parsed;

            return null;
        }

        private static bool TryGetBool(object? dbValue, bool fallback)
        {
            if (dbValue == null) return fallback;

            if (bool.TryParse(dbValue.ToString(), out var parsedBool))
                return parsedBool;

            if (int.TryParse(dbValue.ToString(), out var parsedInt))
                return parsedInt != 0;

            return fallback;
        }

        public async Task<HierarchyApiResponse<bool>> UpdateEmployeeBasicAsync(UpdateEmployeeBasicRequest request)
        {
            const string sql = "EXEC [Hie].[sp_UpdateEmployeeBasic] @EmployeeId, @EmployeeName, @Designation, @DateOfJoining, @IsActive";
            using var connection = new SqlConnection(_connectionString);
            try
            {
                var result = await connection.QueryFirstOrDefaultAsync<dynamic>(sql, new { request.EmployeeId, request.EmployeeName, request.Designation, request.DateOfJoining, request.IsActive });
                return IsDynamicSuccess(result)
                    ? HierarchyApiResponse<bool>.SuccessResponse(true, "Employee updated")
                    : HierarchyApiResponse<bool>.ErrorResponse(result?.Message ?? "Update failed");
            }
            catch (Exception ex) { return HierarchyApiResponse<bool>.ErrorResponse("Error: " + ex.Message); }
        }

        #endregion

        #region Excel Import (New — with duplicate code protection)

        // Holds tempSeq state without ref parameter in async method
        private sealed class TempSeqBox { public int Seq; }

        private static string NormalizeImportName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return string.Empty;

            var cleaned = value.Trim().ToLowerInvariant();
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ");
            cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s*\(.*?\)\s*", " ").Trim();
            return cleaned;
        }

        private async Task UpsertImportPrimaryRelationshipAsync(
            SqlConnection conn,
            SqlTransaction tx,
            int employeeId,
            int reportsToEmployeeId,
            int? departmentId,
            int? subDepartmentId)
        {
            const string exactMatchSql = @"
SELECT TOP 1 RelationshipId
FROM [Hie].[EmployeeReportingRelationships]
WHERE EmployeeId = @EmployeeId
  AND ReportsToEmployeeId = @ReportsToEmployeeId
  AND ISNULL(DepartmentId, 0) = ISNULL(@DepartmentId, 0)
  AND ISNULL(SubDepartmentId, 0) = ISNULL(@SubDepartmentId, 0)
  AND IsActive = 1
ORDER BY RelationshipId DESC";

            const string promoteExistingSql = @"
UPDATE [Hie].[EmployeeReportingRelationships]
SET IsPrimary = 1,
    IsActive = 1,
    EffectiveTo = NULL
WHERE RelationshipId = @RelationshipId";

            const string deactivateScopeSql = @"
UPDATE [Hie].[EmployeeReportingRelationships]
SET IsActive = 0,
    IsPrimary = 0,
    EffectiveTo = COALESCE(EffectiveTo, GETDATE())
WHERE EmployeeId = @EmployeeId
  AND IsActive = 1
  AND ISNULL(DepartmentId, 0) = ISNULL(@DepartmentId, 0)
  AND ISNULL(SubDepartmentId, 0) = ISNULL(@SubDepartmentId, 0)
  AND ReportsToEmployeeId <> @ReportsToEmployeeId";

            const string insertSql = @"
INSERT INTO [Hie].[EmployeeReportingRelationships]
(
    EmployeeId,
    ReportsToEmployeeId,
    ReportingTypeId,
    DepartmentId,
    SubDepartmentId,
    IsPrimary,
    EffectiveFrom,
    IsActive
)
VALUES
(
    @EmployeeId,
    @ReportsToEmployeeId,
    1,
    @DepartmentId,
    @SubDepartmentId,
    1,
    GETDATE(),
    1
)";

            var existingId = await conn.QueryFirstOrDefaultAsync<int?>(
                exactMatchSql,
                new
                {
                    EmployeeId = employeeId,
                    ReportsToEmployeeId = reportsToEmployeeId,
                    DepartmentId = departmentId,
                    SubDepartmentId = subDepartmentId
                },
                tx);

            if (existingId.HasValue)
            {
                await conn.ExecuteAsync(
                    promoteExistingSql,
                    new { RelationshipId = existingId.Value },
                    tx);
                return;
            }

            await conn.ExecuteAsync(
                deactivateScopeSql,
                new
                {
                    EmployeeId = employeeId,
                    ReportsToEmployeeId = reportsToEmployeeId,
                    DepartmentId = departmentId,
                    SubDepartmentId = subDepartmentId
                },
                tx);
            await conn.ExecuteAsync(insertSql, new
            {
                EmployeeId = employeeId,
                ReportsToEmployeeId = reportsToEmployeeId,
                DepartmentId = departmentId,
                SubDepartmentId = subDepartmentId
            }, tx);
        }

        /// <summary>
        /// Additive-only Sub-HOD relationship for bulk assign.
        /// Promotes an existing exact match to primary, or inserts a new relationship.
        /// Does NOT deactivate any prior relationships — allows one Sub-HOD to manage
        /// multiple sub-departments (and multiple HODs) simultaneously.
        /// </summary>
        private async Task BulkAddSubHodRelationshipAsync(
            SqlConnection conn,
            SqlTransaction tx,
            int employeeId,
            int reportsToEmployeeId,
            int? departmentId,
            int? subDepartmentId)
        {
            const string exactMatchSql = @"
SELECT TOP 1 RelationshipId
FROM [Hie].[EmployeeReportingRelationships]
WHERE EmployeeId = @EmployeeId
  AND ReportsToEmployeeId = @ReportsToEmployeeId
  AND ISNULL(DepartmentId, 0) = ISNULL(@DepartmentId, 0)
  AND ISNULL(SubDepartmentId, 0) = ISNULL(@SubDepartmentId, 0)
  AND IsActive = 1
ORDER BY RelationshipId DESC";

            const string promoteSql = @"
UPDATE [Hie].[EmployeeReportingRelationships]
SET IsPrimary = 1,
    IsActive = 1,
    EffectiveTo = NULL
WHERE RelationshipId = @RelationshipId";

            const string insertSql = @"
INSERT INTO [Hie].[EmployeeReportingRelationships]
(EmployeeId, ReportsToEmployeeId, ReportingTypeId, DepartmentId, SubDepartmentId, IsPrimary, EffectiveFrom, IsActive)
VALUES (@EmployeeId, @ReportsToEmployeeId, 1, @DepartmentId, @SubDepartmentId, 1, GETDATE(), 1)";

            var existingId = await conn.QueryFirstOrDefaultAsync<int?>(
                exactMatchSql,
                new { EmployeeId = employeeId, ReportsToEmployeeId = reportsToEmployeeId, DepartmentId = departmentId, SubDepartmentId = subDepartmentId },
                tx);

            if (existingId.HasValue)
            {
                await conn.ExecuteAsync(promoteSql, new { RelationshipId = existingId.Value }, tx);
                return;
            }

            await conn.ExecuteAsync(insertSql,
                new { EmployeeId = employeeId, ReportsToEmployeeId = reportsToEmployeeId, DepartmentId = departmentId, SubDepartmentId = subDepartmentId },
                tx);
        }

        private async Task<int?> ResolveDepartmentIdForImportAsync(
            SqlConnection conn,
            SqlTransaction tx,
            string? departmentName)
        {
            if (string.IsNullOrWhiteSpace(departmentName)) return null;

            var trimmed = departmentName.Trim();
            var existingId = await conn.QueryFirstOrDefaultAsync<int?>(
                @"SELECT TOP 1 DepartmentId
                  FROM [Hie].[Departments]
                  WHERE LTRIM(RTRIM(DepartmentName)) = @DepartmentName",
                new { DepartmentName = trimmed }, tx);

            if (existingId.HasValue) return existingId.Value;

            await conn.QueryFirstOrDefaultAsync<dynamic>(
                "EXEC [Hie].[sp_UpsertDepartment] @DepartmentName",
                new { DepartmentName = trimmed }, tx);

            return await conn.QueryFirstOrDefaultAsync<int?>(
                @"SELECT TOP 1 DepartmentId
                  FROM [Hie].[Departments]
                  WHERE LTRIM(RTRIM(DepartmentName)) = @DepartmentName",
                new { DepartmentName = trimmed }, tx);
        }

        private async Task<int?> ResolveSubDepartmentIdForImportAsync(
            SqlConnection conn,
            SqlTransaction tx,
            int? departmentId,
            string? subDepartmentName)
        {
            if (!departmentId.HasValue || departmentId.Value <= 0 || string.IsNullOrWhiteSpace(subDepartmentName))
                return null;

            var trimmed = subDepartmentName.Trim();
            var existingId = await conn.QueryFirstOrDefaultAsync<int?>(
                @"SELECT TOP 1 SubDepartmentId
                  FROM [Hie].[SubDepartments]
                  WHERE DepartmentId = @DepartmentId
                    AND LTRIM(RTRIM(SubDepartmentName)) = @SubDepartmentName",
                new { DepartmentId = departmentId.Value, SubDepartmentName = trimmed }, tx);

            if (existingId.HasValue) return existingId.Value;

            await conn.QueryFirstOrDefaultAsync<dynamic>(
                "EXEC [Hie].[sp_UpsertSubDepartment] @DepartmentId, @SubDepartmentName",
                new { DepartmentId = departmentId.Value, SubDepartmentName = trimmed }, tx);

            return await conn.QueryFirstOrDefaultAsync<int?>(
                @"SELECT TOP 1 SubDepartmentId
                  FROM [Hie].[SubDepartments]
                  WHERE DepartmentId = @DepartmentId
                    AND LTRIM(RTRIM(SubDepartmentName)) = @SubDepartmentName",
                new { DepartmentId = departmentId.Value, SubDepartmentName = trimmed }, tx);
        }

        /// <summary>
        /// Resolves code for import:
        /// - Empty/#N/A/0 → generate TEMP
        /// - Code in DB with same first name → reuse (same person)
        /// - Code in DB with different name → generate new TEMP (avoid overwrite)
        /// - Code not in DB → use as-is
        /// </summary>
        private async Task<string> ResolveCodeAsync(
            SqlConnection conn, string code, string name,
            Dictionary<string, string> cache, TempSeqBox seqBox, SqlTransaction? tx = null)
        {
            var cleanName = (name ?? "").Trim();

            // No code → TEMP
            if (string.IsNullOrWhiteSpace(code) || code.StartsWith("#") || code == "0")
            {
                var k = cleanName.ToLowerInvariant();
                if (cache.TryGetValue(k, out var c)) return c;
                seqBox.Seq++;
                var tc = $"TEMP{seqBox.Seq:D4}";
                cache[k] = tc;
                return tc;
            }

            // Check DB
            var existing = await conn.QueryFirstOrDefaultAsync<string>(
                "SELECT TOP 1 EmployeeName FROM [Hie].[Employees] WHERE EmployeeCode = @Code",
                new { Code = code }, tx);

            if (existing == null) return code; // New code — use it

            // Same person? Match on normalized full name, not just first name.
            // First-name-only matching was causing unrelated people with similar names
            // to reuse the same employee code during import.
            if (NormalizeImportName(existing) == NormalizeImportName(cleanName))
                return code; // Same person → update normally

            // Different person has this code → TEMP
            var nk = cleanName.ToLowerInvariant();
            if (cache.TryGetValue(nk, out var cached)) return cached;
            seqBox.Seq++;
            var temp = $"TEMP{seqBox.Seq:D4}";
            cache[nk] = temp;
            return temp;
        }

        public async Task<ImportExcelResult> ImportFromExcelAsync(List<ImportExcelRowRequest> rows, int createdBy)
        {
            var result = new ImportExcelResult { TotalRows = rows.Count };
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();
            using var tx = connection.BeginTransaction();

            // Get last TEMP sequence from DB
            var lastTemp = await connection.QueryFirstOrDefaultAsync<string>(
                "SELECT TOP 1 EmployeeCode FROM [Hie].[Employees] WHERE EmployeeCode LIKE 'TEMP%' ORDER BY EmployeeCode DESC",
                transaction: tx);
            int tempSeq = 0;
            if (!string.IsNullOrEmpty(lastTemp) && lastTemp.Length > 4)
                int.TryParse(lastTemp.Substring(4), out tempSeq);

            // Single counter box shared by ResolveCodeAsync (no ref in async)
            var seqBox = new TempSeqBox { Seq = tempSeq };

            // name → TEMP code cache (same name = same TEMP within batch)
            var tempCodeCache = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                foreach (var row in rows)
                {
                    try
                    {
                        // ── 1. HOD ────────────────────────────────────────────
                        if (string.IsNullOrWhiteSpace(row.HodName)
                            && string.IsNullOrWhiteSpace(row.SubHodName)
                            && string.IsNullOrWhiteSpace(row.ExecName))
                        {
                            result.Errors++;
                            result.ErrorDetails.Add($"Row {row.RowNumber}: No hierarchy values found — skipped.");
                            continue;
                        }

                        int? hodId = null;
                        if (!string.IsNullOrWhiteSpace(row.HodName))
                        {
                            row.HodCode = await ResolveCodeAsync(connection, row.HodCode?.Trim() ?? "", row.HodName.Trim(), tempCodeCache, seqBox, tx);

                            var hodResult = await connection.QueryFirstOrDefaultAsync<dynamic>(
                                "EXEC [Hie].[sp_UpsertEmployeeByCode] " +
                                "@EmployeeCode=@EmployeeCode, " +
                                "@EmployeeName=@EmployeeName, " +
                                "@RoleTypeId=@RoleTypeId, " +
                                "@Designation=@Designation, " +
                                "@CreatedBy=@CreatedBy",
                                new { EmployeeCode = row.HodCode, EmployeeName = row.HodName.Trim(), RoleTypeId = 1, Designation = (string)null, CreatedBy = createdBy },
                                tx);

                            if (hodResult == null) { result.Errors++; continue; }
                            hodId = (int)hodResult.EmployeeId;
                            if (hodResult.WasCreated == 1) result.EmployeesCreated++; else result.EmployeesUpdated++;

                            if (row.HodCode.StartsWith("TEMP"))
                            {
                                result.TempCodesGenerated++;
                                result.TempCodes.Add($"{row.HodCode} → {row.HodName} (HOD)");
                            }
                        }

                        // ── 2. Department ─────────────────────────────────────
                        int? deptId = await ResolveDepartmentIdForImportAsync(connection, tx, row.Department);

                        // ── 3. SubDepartment ──────────────────────────────────
                        int? subDeptId = await ResolveSubDepartmentIdForImportAsync(connection, tx, deptId, row.SubDepartment);

                        // ── 4. SubHOD (optional) ──────────────────────────────
                        int? subHodId = null;
                        if (!string.IsNullOrWhiteSpace(row.SubHodName))
                        {
                            // FIX: Only ResolveCodeAsync handles all code cases (empty, duplicate, #N/A)
                            row.SubHodCode = await ResolveCodeAsync(connection, row.SubHodCode?.Trim() ?? "", row.SubHodName.Trim(), tempCodeCache, seqBox, tx);

                            var subResult = await connection.QueryFirstOrDefaultAsync<dynamic>(
                                "EXEC [Hie].[sp_UpsertEmployeeByCode] " +
                                "@EmployeeCode=@EmployeeCode, " +
                                "@EmployeeName=@EmployeeName, " +
                                "@RoleTypeId=@RoleTypeId, " +
                                "@Designation=@Designation, " +
                                "@CreatedBy=@CreatedBy",
                                new { EmployeeCode = row.SubHodCode, EmployeeName = row.SubHodName.Trim(), RoleTypeId = 2, Designation = row.SubHodDesignation, CreatedBy = createdBy },
                                tx);

                            if (subResult != null)
                            {
                                subHodId = (int)subResult.EmployeeId;
                                if (subResult.WasCreated == 1) result.EmployeesCreated++; else result.EmployeesUpdated++;

                                if (row.SubHodCode.StartsWith("TEMP"))
                                {
                                    result.TempCodesGenerated++;
                                    result.TempCodes.Add($"{row.SubHodCode} → {row.SubHodName} (SubHOD)");
                                }

                                if (hodId.HasValue)
                                {
                                    await UpsertImportPrimaryRelationshipAsync(
                                        connection,
                                        tx,
                                        subHodId.Value,
                                        hodId.Value,
                                        deptId,
                                        subDeptId);
                                    result.RelationshipsCreated++;
                                }
                            }
                        }

                        // ── 5. Executive (optional) ───────────────────────────
                        if (!string.IsNullOrWhiteSpace(row.ExecName))
                        {
                            // FIX: Only ResolveCodeAsync handles all code cases
                            row.ExecCode = await ResolveCodeAsync(connection, row.ExecCode?.Trim() ?? "", row.ExecName.Trim(), tempCodeCache, seqBox, tx);

                            var execResult = await connection.QueryFirstOrDefaultAsync<dynamic>(
                                "EXEC [Hie].[sp_UpsertEmployeeByCode] " +
                                "@EmployeeCode=@EmployeeCode, " +
                                "@EmployeeName=@EmployeeName, " +
                                "@RoleTypeId=@RoleTypeId, " +
                                "@Designation=@Designation, " +
                                "@CreatedBy=@CreatedBy",
                                new { EmployeeCode = row.ExecCode, EmployeeName = row.ExecName.Trim(), RoleTypeId = 3, Designation = row.ExecDesignation, CreatedBy = createdBy },
                                tx);

                            if (execResult != null)
                            {
                                int execId = (int)execResult.EmployeeId;
                                if (execResult.WasCreated == 1) result.EmployeesCreated++; else result.EmployeesUpdated++;

                                if (row.ExecCode.StartsWith("TEMP"))
                                {
                                    result.TempCodesGenerated++;
                                    result.TempCodes.Add($"{row.ExecCode} → {row.ExecName} (Exec)");
                                }

                                int? managerId = subHodId ?? hodId;
                                if (managerId.HasValue)
                                {
                                    await UpsertImportPrimaryRelationshipAsync(
                                        connection,
                                        tx,
                                        execId,
                                        managerId.Value,
                                        deptId,
                                        subDeptId);
                                    result.RelationshipsCreated++;
                                }
                            }
                        }
                    }

                    catch (Exception ex)
                    {
                        result.Errors++;
                        result.ErrorDetails.Add($"Row {row.RowNumber}: {ex.Message}");
                    }
                }

                tx.Commit();
            }
            catch
            {
                tx.Rollback();
                throw;
            }

            return result;
        }

        #endregion

        #region Salary Password

        public async Task<bool> VerifySalaryPasswordAsync(int employeeId, string plainPassword)
        {
            await Task.CompletedTask;
            return false;
        }

        public async Task<bool> SetSalaryPasswordAsync(int employeeId, string plainPassword)
        {
            await Task.CompletedTask;
            return false;
        }

        public async Task<(bool Success, string Message)> VerifySalaryAccessAsync(int employeeId, string ownPassword, string adminKey)
        {
            if (string.IsNullOrWhiteSpace(ownPassword))
                return (false, "Your salary password is required");
            if (string.IsNullOrWhiteSpace(adminKey))
                return (false, "Admin master key is required");

            // Step 1: verify the employee's own salary password
            var ownOk = await VerifySalaryPasswordAsync(employeeId, ownPassword);
            if (!ownOk)
                return (false, "Your salary password is incorrect");

            // Step 2: verify the global admin master key
            var adminOk = await VerifyAdminSalaryKeyInternalAsync(adminKey);
            if (!adminOk)
                return (false, "Admin master key is incorrect");

            return (true, "Access granted");
        }

        private async Task<bool> VerifyAdminSalaryKeyInternalAsync(string adminKey)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT ConfigValue FROM [Hie].[SalaryConfig] WHERE ConfigKey = 'SalaryMasterKey'");
                if (row == null || string.IsNullOrEmpty(row.ConfigValue as string)) return false;
                var decrypted = Encryption.Decrypt((string)row.ConfigValue);
                return decrypted == adminKey;
            }
            catch { return false; }
        }

        public async Task<(bool Success, string Message)> SetAdminSalaryKeyAsync(string oldKey, string newKey, int setByUserId)
        {
            if (!IsValidAdminKeyFormat(newKey))
                return (false, "New key must be at least 6 characters and include one uppercase letter and one special character");

            try
            {
                using var conn = new SqlConnection(_connectionString);

                // 1. Check permission in SalaryKeyManager
                var hasPerm = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM [Hie].[SalaryKeyManager] WHERE UserId = @UserId AND IsActive = 1",
                    new { UserId = setByUserId });
                if (hasPerm == 0)
                    return (false, "You do not have permission to manage the admin salary key");

                // 2. Check if a key already exists
                var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT ConfigValue FROM [Hie].[SalaryConfig] WHERE ConfigKey = 'SalaryMasterKey'");
                string? existingEncrypted = existing?.ConfigValue as string;
                bool keyExists = !string.IsNullOrEmpty(existingEncrypted);

                if (keyExists)
                {
                    // 3. Old key must be provided and must match
                    if (string.IsNullOrWhiteSpace(oldKey))
                        return (false, "Current key is required to change the admin salary key");
                    var decrypted = Encryption.Decrypt(existingEncrypted!);
                    if (decrypted != oldKey)
                        return (false, "Current key is incorrect");
                }

                // 4. Save new key (encrypted)
                var encrypted = Encryption.Encrypt(newKey);
                await conn.ExecuteAsync(
                    @"IF EXISTS (SELECT 1 FROM [Hie].[SalaryConfig] WHERE ConfigKey = 'SalaryMasterKey')
                          UPDATE [Hie].[SalaryConfig] SET ConfigValue = @Val, UpdatedAt = GETDATE(), UpdatedBy = @By WHERE ConfigKey = 'SalaryMasterKey'
                      ELSE
                          INSERT INTO [Hie].[SalaryConfig] (ConfigKey, ConfigValue, UpdatedAt, UpdatedBy) VALUES ('SalaryMasterKey', @Val, GETDATE(), @By)",
                    new { Val = encrypted, By = setByUserId });

                _ = LogAuditAsync("SetAdminSalaryKey", "SalaryConfig", null, null, null, null,
                    keyExists ? "Admin salary master key changed" : "Admin salary master key created", setByUserId);
                return (true, keyExists ? "Admin salary key updated successfully" : "Admin salary key created successfully");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<bool> HasAdminSalaryKeySetAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT ConfigValue FROM [Hie].[SalaryConfig] WHERE ConfigKey = 'SalaryMasterKey'");
                string? val = row?.ConfigValue as string;
                return !string.IsNullOrEmpty(val);
            }
            catch { return false; }
        }

        public async Task<AdminKeyStatusDto> GetAdminKeyStatusAsync(int userId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var hasPerm = await conn.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM [Hie].[SalaryKeyManager] WHERE UserId = @UserId AND IsActive = 1",
                    new { UserId = userId });
                var row = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "SELECT ConfigValue FROM [Hie].[SalaryConfig] WHERE ConfigKey = 'SalaryMasterKey'");
                string? cfgVal = row?.ConfigValue as string;
                return new AdminKeyStatusDto
                {
                    HasPermission = hasPerm > 0,
                    KeyExists = !string.IsNullOrEmpty(cfgVal)
                };
            }
            catch { return new AdminKeyStatusDto { HasPermission = false, KeyExists = false }; }
        }

        public async Task<bool> VerifyAdminKeyAsync(string adminKey)
        {
            if (string.IsNullOrWhiteSpace(adminKey)) return false;
            return await VerifyAdminSalaryKeyInternalAsync(adminKey);
        }

        public async Task<SalaryDbCredentialDto?> GetSalaryDbCredentialAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            return await conn.QueryFirstOrDefaultAsync<SalaryDbCredentialDto>(
                "EXEC [Hie].[sp_GetSalaryDbCredentials]");
        }

        public async Task<(bool Success, string Message)> UpdateSalaryDbCredentialAsync(SalaryDbCredentialDto request, int updatedBy, int? changedEmployeeId)
        {
            if (request == null
                || string.IsNullOrWhiteSpace(request.ServerName)
                || string.IsNullOrWhiteSpace(request.DatabaseName)
                || string.IsNullOrWhiteSpace(request.DbUserId))
                return (false, "Server, database, and user id are required");

            try
            {
                using var conn = new SqlConnection(_connectionString);
                var oldCredentials = await GetSalaryDbCredentialAsync();
                var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "EXEC [Hie].[sp_UpsertSalaryDbCredentials] @ServerName, @DatabaseName, @DbUserId, @UpdatedBy",
                    new
                    {
                        ServerName = request.ServerName.Trim(),
                        DatabaseName = request.DatabaseName.Trim(),
                        DbUserId = request.DbUserId.Trim(),
                        UpdatedBy = updatedBy
                    });

                var ok = result == null || IsDynamicSuccess(result);
                if (ok)
                {
                    int? credentialEntityId = null;
                    try { credentialEntityId = result?.CredentialId == null ? null : Convert.ToInt32(result.CredentialId); }
                    catch { }

                    var newCredentials = new
                    {
                        ServerName = request.ServerName.Trim(),
                        DatabaseName = request.DatabaseName.Trim(),
                        DbUserId = request.DbUserId.Trim()
                    };

                    _ = LogAuditAsync(
                        "UpdateSalaryDbCredentials",
                        "SalaryDbCredentials",
                        credentialEntityId ?? changedEmployeeId,
                        changedEmployeeId,
                        ToJson(oldCredentials == null ? null : new
                        {
                            oldCredentials.CredentialId,
                            oldCredentials.ServerName,
                            oldCredentials.DatabaseName,
                            oldCredentials.DbUserId
                        }),
                        ToJson(newCredentials),
                        "Salary database credentials updated",
                        updatedBy);
                    return (true, "Salary database credentials updated successfully");
                }

                return (false, result?.Message ?? "Failed to update salary database credentials");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<(bool Success, string Message)> SetSalaryPermissionAsync(List<int> employeeIds, int updatedBy)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                using var tx = conn.BeginTransaction();

                // Clear ViewSalary for all employees
                await conn.ExecuteAsync(
                    "UPDATE [Hie].[Employees] SET ViewSalary = 0 WHERE ViewSalary = 1",
                    null, tx);

                // Set ViewSalary = 1 for the selected employees
                if (employeeIds != null && employeeIds.Count > 0)
                {
                    foreach (var empId in employeeIds)
                    {
                        await conn.ExecuteAsync(
                            "UPDATE [Hie].[Employees] SET ViewSalary = 1 WHERE EmployeeId = @EmpId",
                            new { EmpId = empId }, tx);
                    }
                }

                tx.Commit();
                _ = LogAuditAsync("SetSalaryPermission", "Employee", null, null, null,
                    null, $"Salary permission updated for {employeeIds?.Count ?? 0} employees", updatedBy);
                return (true, $"Salary permission updated for {employeeIds?.Count ?? 0} employee(s).");
            }
            catch (Exception ex) { return (false, ex.Message); }
        }

        public async Task<List<EmployeeDto>> GetEmployeesForSalaryPermissionAsync()
        {
            const string sql = @"
SELECT EmployeeId, EmployeeName, EmployeeCode, RoleTypeId,
       ISNULL(ViewSalary, 0) AS ViewSalary
FROM [Hie].[Employees]
WHERE IsActive = 1
ORDER BY EmployeeName";
            using var conn = new SqlConnection(_connectionString);
            var rows = await conn.QueryAsync<dynamic>(sql);
            return rows.Select(r => new EmployeeDto
            {
                EmployeeId   = (int)r.EmployeeId,
                EmployeeName = r.EmployeeName as string ?? "",
                EmployeeCode = r.EmployeeCode as string ?? "",
                RoleTypeId   = (int)(r.RoleTypeId ?? 0),
                ViewSalary   = r.ViewSalary == true || Convert.ToBoolean(r.ViewSalary)
            }).ToList();
        }

        #endregion

        #region Edit Dropdowns

        public async Task<List<EmployeeDropdownDto>> GetActiveHODsAsync()
        {
            const string sql = @"
SELECT DISTINCT
    e.EmployeeId,
    e.EmployeeName,
    e.EmployeeCode
FROM [Hie].[Employees] e
WHERE e.IsActive = 1
  AND e.RoleTypeId = 1
ORDER BY e.EmployeeName, e.EmployeeCode";
            using var conn = new SqlConnection(_connectionString);
            return (await conn.QueryAsync<EmployeeDropdownDto>(sql)).ToList();
        }

        public async Task<List<EmployeeDropdownDto>> GetSubHODsForEditAsync(int? hodId)
        {
            const string sql = @"
SELECT DISTINCT
    e.EmployeeId,
    e.EmployeeName,
    e.EmployeeCode
FROM [Hie].[Employees] e
INNER JOIN [Hie].[EmployeeReportingRelationships] rr
    ON rr.EmployeeId = e.EmployeeId
   AND rr.IsActive = 1
WHERE e.IsActive = 1
  AND e.RoleTypeId = 2
  AND (@HODId IS NULL OR rr.ReportsToEmployeeId = @HODId)
ORDER BY e.EmployeeName, e.EmployeeCode";
            using var conn = new SqlConnection(_connectionString);
            return (await conn.QueryAsync<EmployeeDropdownDto>(sql, new { HODId = hodId })).ToList();
        }

        #endregion

        #region Department Management

        public async Task<HierarchyApiResponse<int>> AddDepartmentAsync(AddDepartmentRequest req, int createdBy)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "EXEC [Hie].[sp_UpsertDepartment] @DepartmentName",
                    new { DepartmentName = req.DepartmentName.Trim() });

                int newId = (int)result.DepartmentId;
                bool wasCreated = (int)(result.WasCreated ?? 0) == 1;

                if (!wasCreated)
                    return HierarchyApiResponse<int>.ErrorResponse("Department already exists");

                _ = LogAuditAsync("AddDept", "Department", newId, null, null, ToJson(new { req.DepartmentName }), $"Department '{req.DepartmentName}' created", createdBy);
                return HierarchyApiResponse<int>.SuccessResponse(newId, "Department added successfully");
            }
            catch (Exception ex) { return HierarchyApiResponse<int>.ErrorResponse(ex.Message); }
        }

        public async Task<HierarchyApiResponse<int>> AddSubDepartmentAsync(AddSubDepartmentRequest req, int createdBy)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "EXEC [Hie].[sp_UpsertSubDepartment] @DepartmentId, @SubDepartmentName",
                    new { req.DepartmentId, SubDepartmentName = req.SubDepartmentName.Trim() });

                int newId = (int)result.SubDepartmentId;
                _ = LogAuditAsync("AddSubDept", "SubDepartment", newId, null, null, ToJson(new { req.DepartmentId, req.SubDepartmentName }), $"Sub-department '{req.SubDepartmentName}' created", createdBy);
                return HierarchyApiResponse<int>.SuccessResponse(newId, "Sub-department added successfully");
            }
            catch (Exception ex) { return HierarchyApiResponse<int>.ErrorResponse(ex.Message); }
        }

        public async Task<HierarchyApiResponse<bool>> UpdateDepartmentAsync(UpdateDepartmentRequest req, int updatedBy)
        {
            const string sql = "EXEC [Hie].[sp_UpdateDepartment] @DepartmentId, @DepartmentName, @DepartmentCode, @IsActive, @ModifiedBy";
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new
                {
                    req.DepartmentId,
                    DepartmentName = req.DepartmentName?.Trim(),
                    req.DepartmentCode,
                    req.IsActive,
                    ModifiedBy = updatedBy
                });

                if (IsDynamicSuccess(result))
                {
                    _ = LogAuditAsync("UpdateDept", "Department", req.DepartmentId, null, null,
                        ToJson(new { req.DepartmentId, req.DepartmentName, req.DepartmentCode, req.IsActive }),
                        $"Department '{req.DepartmentName}' updated", updatedBy);
                    return HierarchyApiResponse<bool>.SuccessResponse(true, result?.Message?.ToString() ?? "Department updated");
                }

                return HierarchyApiResponse<bool>.ErrorResponse(result?.Message ?? "Update failed");
            }
            catch (Exception ex) { return HierarchyApiResponse<bool>.ErrorResponse(ex.Message); }
        }

        public async Task<HierarchyApiResponse<bool>> UpdateSubDepartmentAsync(UpdateSubDepartmentRequest req, int updatedBy)
        {
            const string sql = "EXEC [Hie].[sp_UpdateSubDepartment] @SubDepartmentId, @DepartmentId, @SubDepartmentName, @Description, @IsActive";
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QueryFirstOrDefaultAsync<dynamic>(sql, new
                {
                    req.SubDepartmentId,
                    req.DepartmentId,
                    SubDepartmentName = req.SubDepartmentName?.Trim(),
                    req.Description,
                    req.IsActive
                });

                if (IsDynamicSuccess(result))
                {
                    _ = LogAuditAsync("UpdateSubDept", "SubDepartment", req.SubDepartmentId, null, null,
                        ToJson(new { req.SubDepartmentId, req.DepartmentId, req.SubDepartmentName, req.Description, req.IsActive }),
                        $"Sub-department '{req.SubDepartmentName}' updated", updatedBy);
                    return HierarchyApiResponse<bool>.SuccessResponse(true, result?.Message?.ToString() ?? "Sub-department updated");
                }

                return HierarchyApiResponse<bool>.ErrorResponse(result?.Message ?? "Update failed");
            }
            catch (Exception ex) { return HierarchyApiResponse<bool>.ErrorResponse(ex.Message); }
        }

        #endregion

        #region Orphan Employees

        public async Task<List<OrphanEmployeeDto>> GetOrphanEmployeesAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            const string sql = @"
SELECT
    e.EmployeeId,
    e.EmployeeCode,
    e.EmployeeName,
    e.Designation,
    e.RoleTypeId,
    CASE e.RoleTypeId
        WHEN 1 THEN 'HOD'
        WHEN 2 THEN 'Sub-HOD'
        WHEN 3 THEN 'Executive'
        ELSE 'Employee'
    END AS RoleName,
    d.DepartmentName,
    CAST(NULL AS NVARCHAR(100)) AS SubDepartmentName,
    CAST(NULL AS DECIMAL(18, 2)) AS Salary,
    e.IsActive
FROM [Hie].[Employees] e
LEFT JOIN [Hie].[Departments] d
    ON d.DepartmentId = e.PrimaryDepartmentId
WHERE e.IsActive = 1
  AND e.RoleTypeId IN (2, 3)
  AND NOT EXISTS
  (
      SELECT 1
      FROM [Hie].[EmployeeReportingRelationships] rr
      WHERE rr.EmployeeId = e.EmployeeId
        AND rr.IsActive = 1
  )
ORDER BY e.EmployeeName";

            return (await conn.QueryAsync<OrphanEmployeeDto>(sql)).ToList();
        }

        #endregion

        #region Audit Log

        public async Task LogAuditAsync(string actionType, string entityType, int? entityId, int? employeeId,
            string oldValues, string newValues, string description, int? changedByUserId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.ExecuteAsync(
                    "EXEC [Hie].[sp_InsertAuditLog] @ActionType, @EntityType, @EntityId, @EmployeeId, @EmployeeName, @EmployeeCode, @OldValues, @NewValues, @Description, @ChangedByUserId, @ChangedByName, @IpAddress",
                    new
                    {
                        ActionType = actionType,
                        EntityType = entityType,
                        EntityId = entityId,
                        EmployeeId = employeeId,
                        EmployeeName = (string)null,   // SP auto-fills
                        EmployeeCode = (string)null,   // SP auto-fills
                        OldValues = oldValues,
                        NewValues = newValues,
                        Description = description,
                        ChangedByUserId = changedByUserId,
                        ChangedByName = (string)null,  // SP auto-fills
                        IpAddress = (string)null
                    });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[AuditLog] Failed to log: {ex.Message}");
            }
        }

        public async Task<List<AuditLogDto>> GetAuditLogsAsync(AuditLogRequest request)
        {
            using var conn = new SqlConnection(_connectionString);
            return (await conn.QueryAsync<AuditLogDto>(
                "EXEC [Hie].[sp_GetAuditLogs] @EmployeeId, @ActionType, @FromDate, @ToDate, @SearchTerm, @PageNumber, @PageSize",
                new
                {
                    request.EmployeeId,
                    request.ActionType,
                    request.FromDate,
                    request.ToDate,
                    request.SearchTerm,
                    request.PageNumber,
                    request.PageSize
                })).ToList();
        }

        #endregion

        #region Custom Fields (Dynamic Columns)

        public async Task<List<CustomFieldDto>> GetCustomFieldsAsync()
        {
            using var conn = new SqlConnection(_connectionString);
            return (await conn.QueryAsync<CustomFieldDto>("EXEC [Hie].[sp_GetCustomFields]")).ToList();
        }

        public async Task<HierarchyApiResponse<int>> AddCustomFieldAsync(AddCustomFieldRequest request, int createdBy)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QueryFirstOrDefaultAsync<dynamic>(
                    "EXEC [Hie].[sp_AddCustomField] @FieldName, @FieldType, @IsRequired, @Options, @CreatedBy",
                    new { request.FieldName, request.FieldType, request.IsRequired, request.Options, CreatedBy = createdBy });
                if ((int)(result?.Success ?? 0) == 1)
                {
                    _ = LogAuditAsync("AddColumn", "CustomField", (int)result.FieldId, null, null, ToJson(request), $"Custom column '{request.FieldName}' ({request.FieldType}) added", createdBy);
                    return HierarchyApiResponse<int>.SuccessResponse((int)result.FieldId, result?.Message?.ToString() ?? "Field added");
                }
                return HierarchyApiResponse<int>.ErrorResponse(result?.Message?.ToString() ?? "Failed");
            }
            catch (Exception ex) { return HierarchyApiResponse<int>.ErrorResponse(ex.Message); }
        }

        public async Task<HierarchyApiResponse<bool>> RemoveCustomFieldAsync(int fieldId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var field = await conn.QueryFirstOrDefaultAsync<CustomFieldDto>("SELECT FieldId, FieldName FROM [Hie].[CustomFields] WHERE FieldId = @FieldId", new { FieldId = fieldId });
                await conn.ExecuteAsync("EXEC [Hie].[sp_RemoveCustomField] @FieldId", new { FieldId = fieldId });
                _ = LogAuditAsync("RemoveColumn", "CustomField", fieldId, null, ToJson(new { field?.FieldName }), null, $"Custom column '{field?.FieldName}' removed", null);
                return HierarchyApiResponse<bool>.SuccessResponse(true, "Field removed");
            }
            catch (Exception ex) { return HierarchyApiResponse<bool>.ErrorResponse(ex.Message); }
        }

        public async Task<List<EmployeeCustomValueDto>> GetEmployeeCustomValuesAsync(int? employeeId = null)
        {
            using var conn = new SqlConnection(_connectionString);
            return (await conn.QueryAsync<EmployeeCustomValueDto>(
                "EXEC [Hie].[sp_GetEmployeeCustomValues] @EmployeeId",
                new { EmployeeId = employeeId })).ToList();
        }

        public async Task<HierarchyApiResponse<bool>> SetEmployeeCustomValueAsync(SetCustomValueRequest request)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.ExecuteAsync(
                    "EXEC [Hie].[sp_SetEmployeeCustomValue] @EmployeeId, @FieldId, @Value",
                    new { request.EmployeeId, request.FieldId, request.Value });
                return HierarchyApiResponse<bool>.SuccessResponse(true, "Value saved");
            }
            catch (Exception ex) { return HierarchyApiResponse<bool>.ErrorResponse(ex.Message); }
        }

        #endregion
    }
}
