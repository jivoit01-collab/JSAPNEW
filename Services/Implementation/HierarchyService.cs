using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using Microsoft.Data.SqlClient;
using System.Globalization;
using System.Text;
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

            await EnrichTreeDateOfJoiningAsync(connection, hods);
            DecryptTreeSalaries(hods);
            return hods;
        }

        private async Task EnrichTreeDateOfJoiningAsync(SqlConnection connection, List<HODTreeNodeDto> hods)
        {
            var employeeIds = hods
                .Select(h => h.EmployeeId)
                .Concat(hods.SelectMany(h => h.SubHODs ?? new List<SubHODTreeNodeDto>()).Select(s => s.EmployeeId))
                .Concat(hods.SelectMany(h => h.SubHODs ?? new List<SubHODTreeNodeDto>())
                    .SelectMany(s => s.Executives ?? new List<ExecutiveTreeNodeDto>())
                    .Select(e => e.EmployeeId))
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (!employeeIds.Any()) return;

            var rows = await connection.QueryAsync<dynamic>(
                "SELECT EmployeeId, DateOfJoining FROM [Hie].[Employees] WHERE EmployeeId IN @EmployeeIds",
                new { EmployeeIds = employeeIds });

            var dates = rows.ToDictionary(
                x => (int)x.EmployeeId,
                x => TryGetDate(x.DateOfJoining));
            var sikhValues = await GetSikhValuesByEmployeeIdsAsync(connection, employeeIds);

            foreach (var hod in hods)
            {
                if (dates.TryGetValue(hod.EmployeeId, out var hodDate))
                    hod.DateOfJoining = hodDate;
                if (sikhValues.TryGetValue(hod.EmployeeId, out var hodSikh))
                    hod.SikhNonSikh = hodSikh ?? string.Empty;

                foreach (var subHod in hod.SubHODs ?? new List<SubHODTreeNodeDto>())
                {
                    if (dates.TryGetValue(subHod.EmployeeId, out var subHodDate))
                        subHod.DateOfJoining = subHodDate;
                    if (sikhValues.TryGetValue(subHod.EmployeeId, out var subHodSikh))
                        subHod.SikhNonSikh = subHodSikh ?? string.Empty;

                    foreach (var exec in subHod.Executives ?? new List<ExecutiveTreeNodeDto>())
                    {
                        if (dates.TryGetValue(exec.EmployeeId, out var execDate))
                            exec.DateOfJoining = execDate;
                        if (sikhValues.TryGetValue(exec.EmployeeId, out var execSikh))
                            exec.SikhNonSikh = execSikh ?? string.Empty;
                    }
                }
            }
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

            await PopulateEmployeeSikhValuesAsync(connection, employees);
            employees.ForEach(DecryptEmployeeSalary);
            return employees;
        }

        private async Task PopulateEmployeeSikhValuesAsync(SqlConnection connection, List<EmployeeDto> employees)
        {
            if (employees == null || employees.Count == 0)
                return;

            var employeeIds = employees
                .Where(e => e.EmployeeId > 0)
                .Select(e => e.EmployeeId)
                .Distinct()
                .ToList();

            if (employeeIds.Count == 0)
                return;

            var valuesByEmployeeId = await GetSikhValuesByEmployeeIdsAsync(connection, employeeIds);

            foreach (var employee in employees)
            {
                if (employee.EmployeeId > 0 && valuesByEmployeeId.TryGetValue(employee.EmployeeId, out var sikhValue))
                    employee.SikhNonSikh = sikhValue ?? string.Empty;
            }
        }

        private async Task<Dictionary<int, string?>> GetSikhValuesByEmployeeIdsAsync(SqlConnection connection, IReadOnlyCollection<int> employeeIds)
        {
            var valuesByEmployeeId = new Dictionary<int, string?>();
            if (employeeIds == null || employeeIds.Count == 0)
                return valuesByEmployeeId;

            var employeeColumns = (await connection.QueryAsync<string>(
                "SELECT [name] FROM sys.columns WHERE object_id = OBJECT_ID(N'[Hie].[Employees]')"))
                .ToList();

            string? sikhNonSikhColumn = FindFirstExistingColumn(
                employeeColumns,
                "Sikh / Non-Sikh",
                "Sikh/Non-Sikh",
                "SikhNonSikh",
                "Sikh_Non_Sikh",
                "sikh_no_sikh");
            string? genderColumn = FindFirstExistingColumn(employeeColumns, "Gender");
            string? qualificationColumn = FindFirstExistingColumn(employeeColumns, "Qualification", "Qulaification");
            string? areaColumn = FindFirstExistingColumn(employeeColumns, "Area");

            if (!string.IsNullOrWhiteSpace(sikhNonSikhColumn))
            {
                string sql = $@"
SELECT
    EmployeeId,
    {BuildOptionalStringColumnExpression(sikhNonSikhColumn)} AS SikhNonSikh
FROM [Hie].[Employees]
WHERE EmployeeId IN @EmployeeIds";

                var employeeValues = await connection.QueryAsync<EmployeeSikhValueRow>(sql, new { EmployeeIds = employeeIds.ToArray() });
                foreach (var row in employeeValues)
                {
                    if (row.EmployeeId > 0 && !string.IsNullOrWhiteSpace(row.SikhNonSikh))
                        valuesByEmployeeId[row.EmployeeId] = row.SikhNonSikh;
                }
            }

            var customFieldNames = new[]
            {
                "Sikh / Non-Sikh",
                "Sikh/Non-Sikh",
                "SikhNonSikh",
                "Sikh_Non_Sikh",
                "sikh_no_sikh"
            };

            const string customFieldSql = @"
SELECT
    ecv.EmployeeId,
    CONVERT(NVARCHAR(255), ecv.Value) AS SikhNonSikh
FROM [Hie].[EmployeeCustomValues] ecv
INNER JOIN [Hie].[CustomFields] cf ON cf.FieldId = ecv.FieldId
WHERE ecv.EmployeeId IN @EmployeeIds
  AND cf.FieldName IN @FieldNames";

            var customValues = await connection.QueryAsync<EmployeeSikhValueRow>(customFieldSql, new
            {
                EmployeeIds = employeeIds.ToArray(),
                FieldNames = customFieldNames
            });

            foreach (var row in customValues)
            {
                if (row.EmployeeId <= 0 || string.IsNullOrWhiteSpace(row.SikhNonSikh))
                    continue;

                if (!valuesByEmployeeId.ContainsKey(row.EmployeeId))
                    valuesByEmployeeId[row.EmployeeId] = row.SikhNonSikh;
            }

            return valuesByEmployeeId;
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

            if (!request.PrimaryDepartmentId.HasValue)
                return await CreateEmployeeWithoutJsUserFallbackAsync(request);

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
                    await UpdateEmployeeCreateExtraColumnsAsync(connection, newId, request);
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

                await UpdateEmployeeCreateExtraColumnsAsync(connection, newId, request);
                var employee = await GetEmployeeByIdAsync(newId, "", true);
                _ = LogAuditAsync("Create", "Employee", newId, newId, null, ToJson(request), $"Created {request.EmployeeName} ({request.EmployeeCode}) without jsUser dependency", request.CreatedBy);

                return HierarchyApiResponse<EmployeeDto>.SuccessResponse(employee, "Employee created successfully");
            }
            catch (Exception ex)
            {
                return HierarchyApiResponse<EmployeeDto>.ErrorResponse("Error creating employee: " + ex.Message);
            }
        }

        private async Task UpdateEmployeeCreateExtraColumnsAsync(SqlConnection connection, int employeeId, EmployeeRequest request)
        {
            var employeeColumns = (await connection.QueryAsync<string>(
                "SELECT [name] FROM sys.columns WHERE object_id = OBJECT_ID(N'[Hie].[Employees]')")).ToList();

            string? genderColumn = FindFirstExistingColumn(employeeColumns, "Gender");
            string? qualificationColumn = FindFirstExistingColumn(employeeColumns, "Qualification", "Qulaification");
            string? areaColumn = FindFirstExistingColumn(employeeColumns, "Area");
            string? sikhNonSikhColumn = FindFirstExistingColumn(
                employeeColumns,
                "Sikh / Non-Sikh",
                "Sikh/Non-Sikh",
                "SikhNonSikh",
                "Sikh_Non_Sikh",
                "sikh_no_sikh");

            var setParts = new List<string>();
            if (!string.IsNullOrWhiteSpace(genderColumn))
                setParts.Add($"[{genderColumn.Replace("]", "]]")}] = @Gender");
            if (!string.IsNullOrWhiteSpace(qualificationColumn))
                setParts.Add($"[{qualificationColumn.Replace("]", "]]")}] = @Qualification");
            if (!string.IsNullOrWhiteSpace(areaColumn))
                setParts.Add($"[{areaColumn.Replace("]", "]]")}] = @Area");
            if (!string.IsNullOrWhiteSpace(sikhNonSikhColumn))
                setParts.Add($"[{sikhNonSikhColumn.Replace("]", "]]")}] = @SikhNonSikh");

            if (!setParts.Any()) return;

            var sql = $@"
UPDATE [Hie].[Employees]
SET {string.Join(", ", setParts)}
WHERE EmployeeId = @EmployeeId";

            await connection.ExecuteAsync(sql, new
            {
                EmployeeId = employeeId,
                Gender = string.IsNullOrWhiteSpace(request.Gender) ? null : request.Gender.Trim(),
                Qualification = string.IsNullOrWhiteSpace(request.Qualification) ? null : request.Qualification.Trim(),
                Area = string.IsNullOrWhiteSpace(request.Area) ? null : request.Area.Trim(),
                SikhNonSikh = NormalizeSikhNonSikhCode(request.SikhNonSikh)
            });
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
                    if (request.EmployeeId.HasValue)
                    {
                        await UpdateEmployeeCreateExtraColumnsAsync(connection, request.EmployeeId.Value, request);
                    }

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

                if (req.IsUnassigned)
                {
                    req.DepartmentId = null;
                    req.SubDepartmentId = null;
                    req.ReportsToEmpId = null;
                    req.MoveTeamWithDepartment = false;
                }

                if (req.ReportsToEmpId.HasValue && req.ReportsToEmpId.Value == req.EmployeeId)
                {
                    tx.Rollback();
                    return HierarchyApiResponse<bool>.ErrorResponse("An employee cannot report to themselves.");
                }

                if (req.ReportsToEmpId.HasValue && req.ReportsToEmpId.Value > 0)
                {
                    var managerInfo = await conn.QueryFirstOrDefaultAsync<dynamic>(
                        "SELECT TOP 1 RoleTypeId, IsActive FROM [Hie].[Employees] WHERE EmployeeId = @EmployeeId",
                        new { EmployeeId = req.ReportsToEmpId.Value }, tx);
                    bool managerActive = managerInfo != null && Convert.ToBoolean(managerInfo.IsActive);
                    if (!managerActive)
                    {
                        tx.Rollback();
                        return HierarchyApiResponse<bool>.ErrorResponse("Selected manager is inactive. Please choose an active HOD/Sub-HOD.");
                    }
                }

                // SP 2: Get department change impact
                var impact = await conn.QueryFirstOrDefaultAsync<DepartmentChangeImpactDto>(
                    "EXEC [Hie].[sp_GetDeptChangeImpact] @EmployeeId, @NewDepartmentId, @NewSubDepartmentId",
                    new { req.EmployeeId, NewDepartmentId = req.DepartmentId, NewSubDepartmentId = req.SubDepartmentId }, tx);
                impact ??= new DepartmentChangeImpactDto();

                bool roleChangedFromSubHodToHod = originalRoleTypeId == (int)RoleTypeEnum.SubHOD && req.RoleTypeId == (int)RoleTypeEnum.HOD;
                bool roleChangedFromSubHodToExecutive = originalRoleTypeId == (int)RoleTypeEnum.SubHOD && req.RoleTypeId == (int)RoleTypeEnum.Executive;
                int? originalScopeDeptId = req.RoleTypeId == (int)RoleTypeEnum.HOD
                    ? req.CurrentDepartmentId ?? await GetActivePrimaryScopeDepartmentIdAsync(conn, tx, req.EmployeeId)
                    : originalDeptId;

                bool departmentChanged = !req.IsUnassigned
                    && originalScopeDeptId != req.DepartmentId
                    && originalRoleTypeId == req.RoleTypeId
                    && (req.RoleTypeId == (int)RoleTypeEnum.HOD || req.RoleTypeId == (int)RoleTypeEnum.SubHOD);
                bool skipRelationshipSync = req.SkipRelationshipSync
                    && !req.IsUnassigned
                    && req.RoleTypeId == (int)RoleTypeEnum.SubHOD
                    && originalRoleTypeId == req.RoleTypeId;

                var employeeColumns = (await conn.QueryAsync<string>(
                    "SELECT [name] FROM sys.columns WHERE object_id = OBJECT_ID(N'[Hie].[Employees]')",
                    transaction: tx)).ToList();

                string? genderColumn = FindFirstExistingColumn(employeeColumns, "Gender");
                string? qualificationColumn = FindFirstExistingColumn(employeeColumns, "Qualification", "Qulaification");
                string? areaColumn = FindFirstExistingColumn(employeeColumns, "Area");
                string? sikhNonSikhColumn = FindFirstExistingColumn(
                    employeeColumns,
                    "Sikh / Non-Sikh",
                    "Sikh/Non-Sikh",
                    "SikhNonSikh",
                    "Sikh_Non_Sikh",
                    "sikh_no_sikh");

                var updateEmployeeSql = new StringBuilder(@"
UPDATE [Hie].[Employees]
SET EmployeeName = @EmployeeName,
    EmployeeCode = @EmployeeCode,
    Designation = @Designation,
    DateOfJoining = @DateOfJoining,
    RoleTypeId = @RoleTypeId,
    PrimaryDepartmentId = @DepartmentId,
    IsActive = @IsActive");

                if (!string.IsNullOrWhiteSpace(genderColumn))
                    updateEmployeeSql.AppendLine($",    [{genderColumn.Replace("]", "]]")}] = @Gender");

                if (!string.IsNullOrWhiteSpace(qualificationColumn))
                    updateEmployeeSql.AppendLine($",    [{qualificationColumn.Replace("]", "]]")}] = @Qualification");

                if (!string.IsNullOrWhiteSpace(areaColumn))
                    updateEmployeeSql.AppendLine($",    [{areaColumn.Replace("]", "]]")}] = @Area");

                if (!string.IsNullOrWhiteSpace(sikhNonSikhColumn))
                    updateEmployeeSql.AppendLine($",    [{sikhNonSikhColumn.Replace("]", "]]")}] = @SikhNonSikh");

                updateEmployeeSql.AppendLine("WHERE EmployeeId = @EmployeeId");

                var rowsAffected = await conn.ExecuteAsync(updateEmployeeSql.ToString(), new
                {
                    req.EmployeeId,
                    req.EmployeeName,
                    req.EmployeeCode,
                    req.Designation,
                    req.DateOfJoining,
                    Gender = string.IsNullOrWhiteSpace(req.Gender) ? null : req.Gender.Trim(),
                    Qualification = string.IsNullOrWhiteSpace(req.Qualification) ? null : req.Qualification.Trim(),
                    Area = string.IsNullOrWhiteSpace(req.Area) ? null : req.Area.Trim(),
                    SikhNonSikh = NormalizeSikhNonSikhCode(req.SikhNonSikh),
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

                // HOD has no parent relationship to sync. Keeping this SP away from HOD edits prevents
                // existing Sub-HOD/Executive reporting links from being detached during department changes.
                if (req.RoleTypeId != (int)RoleTypeEnum.HOD && !skipRelationshipSync)
                {
                    await conn.ExecuteAsync(
                        "EXEC [Hie].[sp_SyncEmployeeRelationships] @EmployeeId, @RoleTypeId, @ReportsToEmpId, @DepartmentId, @SubDepartmentId",
                        new { req.EmployeeId, req.RoleTypeId, req.ReportsToEmpId, req.DepartmentId, req.SubDepartmentId }, tx);
                }

                string detachInfo = "";

                if (req.IsUnassigned)
                {
                    await conn.ExecuteAsync(@"
UPDATE [Hie].[EmployeeReportingRelationships]
SET IsActive = 0,
    EffectiveTo = COALESCE(EffectiveTo, GETDATE())
WHERE EmployeeId = @EmployeeId
  AND IsActive = 1",
                        new { req.EmployeeId }, tx);

                    if (originalRoleTypeId == (int)RoleTypeEnum.HOD)
                    {
                        var detachedTeamMembers = await UnassignHodTeamAsync(conn, tx, req.EmployeeId, updatedBy);
                        if (detachedTeamMembers > 0)
                            detachInfo = $" {detachedTeamMembers} team member(s) moved to unassigned.";
                    }
                }

                if (req.IsUnassigned
                    && originalRoleTypeId == (int)RoleTypeEnum.SubHOD
                    && originalHodId.HasValue
                    && originalHodId.Value > 0)
                {
                    var reassignedExecutives = await ReassignSubHodExecutivesToHodForDepartmentChangeAsync(
                        conn,
                        tx,
                        req.EmployeeId,
                        originalHodId);
                    if (reassignedExecutives > 0)
                        detachInfo = $" {reassignedExecutives} executive(s) now report directly to HOD.";
                }
                else if (!skipRelationshipSync && departmentChanged && req.RoleTypeId == (int)RoleTypeEnum.SubHOD)
                {
                    var reassignedExecutives = await ReassignSubHodExecutivesToHodForDepartmentChangeAsync(
                        conn,
                        tx,
                        req.EmployeeId,
                        originalHodId);
                    if (reassignedExecutives > 0)
                        detachInfo = $" {reassignedExecutives} executive(s) now report directly to HOD.";
                }
                else if (!skipRelationshipSync && !req.IsUnassigned && req.RoleTypeId != (int)RoleTypeEnum.HOD && impact.RequiresMoveTeam)
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

                if (roleChangedFromSubHodToHod || roleChangedFromSubHodToExecutive)
                {
                    // SP 6: Reassign execs from the old Sub-HOD to the HOD on role change
                    var reassignResult = await conn.QueryFirstOrDefaultAsync<dynamic>(
                        "EXEC [Hie].[sp_ReassignExecsOnRoleChange] @EmployeeId, @OriginalHodId",
                        new { req.EmployeeId, OriginalHodId = originalHodId }, tx);
                    int reassigned = (int)(reassignResult?.ReassignedCount ?? 0);
                    if (reassigned > 0) detachInfo = $" {reassigned} executive(s) reassigned to HOD.";
                }

                if (!skipRelationshipSync && req.RoleTypeId == (int)RoleTypeEnum.SubHOD && req.ReportsToEmpId.HasValue && req.ReportsToEmpId.Value > 0)
                {
                    if (!departmentChanged)
                    {
                        var adopted = await MoveDirectExecutivesToSubHodAsync(
                            conn,
                            tx,
                            req.EmployeeId,
                            req.ReportsToEmpId.Value,
                            req.DepartmentId,
                            req.SubDepartmentId);
                        if (adopted > 0) detachInfo += $" {adopted} direct executive(s) moved under Sub-HOD.";
                    }
                }
                tx.Commit();

                // Audit log
                var actions = new List<string>();
                if (req.IsUnassigned) actions.Add("Unassign");
                if (roleChangedFromSubHodToHod || roleChangedFromSubHodToExecutive) actions.Add("RoleChange");
                if (!departmentChanged && impact.RequiresMoveTeam && req.MoveTeamWithDepartment) actions.Add("MoveTeam");
                if (!departmentChanged && impact.RequiresMoveTeam && !req.MoveTeamWithDepartment) actions.Add("DetachTeam");
                if (impact.DepartmentChanged) actions.Add("DeptChange");
                string actionType = actions.Count > 0 ? string.Join("+", actions) : "Update";
                _ = LogAuditAsync(actionType, "Employee", req.EmployeeId, req.EmployeeId,
                    ToJson(new { RoleTypeId = originalRoleTypeId, DepartmentId = originalDeptId }),
                    ToJson(new { req.RoleTypeId, req.DepartmentId, req.SubDepartmentId, req.ReportsToEmpId, req.IsUnassigned, req.IsActive, req.DateOfJoining, req.Gender, req.Qualification, req.Area }),
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

        public async Task<HierarchyApiResponse<bool>> SyncHodDepartmentsAsync(SyncHodDepartmentsRequest request, int updatedBy)
        {
            var departmentIds = (request.DepartmentIds ?? new List<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (request.HodEmployeeId <= 0)
                return HierarchyApiResponse<bool>.ErrorResponse("HOD is required.");

            if (!departmentIds.Any())
                return HierarchyApiResponse<bool>.ErrorResponse("Select at least one department.");

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await EnsureHierarchyDepartmentForeignKeysAsync(conn);
                using var tx = conn.BeginTransaction();
                await EnsureHodDepartmentAssignmentsTableAsync(conn, tx);

                var hod = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
SELECT TOP 1 EmployeeId, EmployeeName, EmployeeCode, RoleTypeId, PrimaryDepartmentId
FROM [Hie].[Employees]
WHERE EmployeeId = @HodEmployeeId",
                    new { request.HodEmployeeId }, tx);

                if (hod == null)
                {
                    tx.Rollback();
                    return HierarchyApiResponse<bool>.ErrorResponse("HOD not found.");
                }

                if ((int)hod.RoleTypeId != (int)RoleTypeEnum.HOD)
                {
                    tx.Rollback();
                    return HierarchyApiResponse<bool>.ErrorResponse("Department multi-select is only allowed for HOD.");
                }

                var oldDepartmentIds = (await conn.QueryAsync<int>(@"
SELECT DISTINCT rr.DepartmentId
FROM [Hie].[EmployeeReportingRelationships] rr
WHERE rr.ReportsToEmployeeId = @HodEmployeeId
  AND rr.IsActive = 1
  AND rr.DepartmentId IS NOT NULL
UNION
SELECT e.PrimaryDepartmentId
FROM [Hie].[Employees] e
WHERE e.EmployeeId = @HodEmployeeId
  AND e.PrimaryDepartmentId IS NOT NULL
UNION
SELECT hda.DepartmentId
FROM [Hie].[HodDepartmentAssignments] hda
WHERE hda.HodEmployeeId = @HodEmployeeId
  AND hda.IsActive = 1",
                    new { request.HodEmployeeId }, tx)).ToList();
                var removedDepartmentIds = oldDepartmentIds
                    .Distinct()
                    .Except(departmentIds)
                    .ToList();

                await conn.ExecuteAsync(@"
UPDATE [Hie].[Employees]
SET PrimaryDepartmentId = @PrimaryDepartmentId
WHERE EmployeeId = @HodEmployeeId",
                    new { request.HodEmployeeId, PrimaryDepartmentId = departmentIds.First() }, tx);

                await conn.ExecuteAsync(@"
UPDATE [Hie].[HodDepartmentAssignments]
SET IsActive = 0,
    ModifiedOn = GETDATE(),
    ModifiedBy = @UpdatedBy
WHERE HodEmployeeId = @HodEmployeeId
  AND IsActive = 1
  AND DepartmentId NOT IN @DepartmentIds;

UPDATE hda
SET hda.IsActive = 1,
    hda.ModifiedOn = GETDATE(),
    hda.ModifiedBy = @UpdatedBy
FROM [Hie].[HodDepartmentAssignments] hda
WHERE hda.HodEmployeeId = @HodEmployeeId
  AND hda.DepartmentId IN @DepartmentIds
  AND hda.IsActive = 0
  AND NOT EXISTS (
      SELECT 1
      FROM [Hie].[HodDepartmentAssignments] activeRow
      WHERE activeRow.HodEmployeeId = hda.HodEmployeeId
        AND activeRow.DepartmentId = hda.DepartmentId
        AND activeRow.IsActive = 1
  );

INSERT INTO [Hie].[HodDepartmentAssignments] (HodEmployeeId, DepartmentId, IsActive, CreatedOn, CreatedBy)
SELECT @HodEmployeeId, selected.DepartmentId, 1, GETDATE(), @UpdatedBy
FROM (
    SELECT DISTINCT CAST(value AS INT) AS DepartmentId
    FROM STRING_SPLIT(@DepartmentIdCsv, ',')
    WHERE TRY_CAST(value AS INT) IS NOT NULL
) selected
WHERE NOT EXISTS (
    SELECT 1
    FROM [Hie].[HodDepartmentAssignments] existing
    WHERE existing.HodEmployeeId = @HodEmployeeId
      AND existing.DepartmentId = selected.DepartmentId
      AND existing.IsActive = 1
);",
                    new
                    {
                        request.HodEmployeeId,
                        DepartmentIds = departmentIds,
                        DepartmentIdCsv = string.Join(",", departmentIds),
                        UpdatedBy = updatedBy
                    }, tx);

                var detachedHodLinks = 0;
                foreach (var removedDepartmentId in removedDepartmentIds)
                {
                    detachedHodLinks += await DetachHodRelationshipsForRemovedDepartmentAsync(
                        conn,
                        tx,
                        request.HodEmployeeId,
                        removedDepartmentId);
                }

                tx.Commit();

                _ = LogAuditAsync("HODDepartmentSync", "Employee", request.HodEmployeeId, request.HodEmployeeId,
                    ToJson(new { DepartmentIds = oldDepartmentIds }),
                    ToJson(new { DepartmentIds = departmentIds }),
                    $"Updated HOD department selection. {detachedHodLinks} HOD reporting link(s) removed from deselected department(s); team department assignments were kept.",
                    updatedBy);

                return HierarchyApiResponse<bool>.SuccessResponse(true, "HOD departments updated successfully.");
            }
            catch (Exception ex)
            {
                return HierarchyApiResponse<bool>.ErrorResponse(ex.Message);
            }
        }

        public async Task<HierarchyApiResponse<bool>> SyncSubHodSubDepartmentsAsync(SyncSubHodSubDepartmentsRequest request, int updatedBy)
        {
            var subDepartmentIds = (request.SubDepartmentIds ?? new List<int>())
                .Where(id => id > 0)
                .Distinct()
                .ToList();

            if (request.SubHodEmployeeId <= 0)
                return HierarchyApiResponse<bool>.ErrorResponse("Sub-HOD is required.");
            if (request.HodEmployeeId <= 0)
                return HierarchyApiResponse<bool>.ErrorResponse("HOD is required.");
            if (request.DepartmentId <= 0)
                return HierarchyApiResponse<bool>.ErrorResponse("Department is required.");
            if (!subDepartmentIds.Any())
                return HierarchyApiResponse<bool>.ErrorResponse("Select at least one sub-department.");

            try
            {
                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();
                await EnsureHierarchyDepartmentForeignKeysAsync(conn);
                using var tx = conn.BeginTransaction();

                var subHod = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
SELECT TOP 1 EmployeeId, EmployeeName, EmployeeCode, RoleTypeId
FROM [Hie].[Employees]
WHERE EmployeeId = @SubHodEmployeeId",
                    new { request.SubHodEmployeeId }, tx);

                if (subHod == null)
                {
                    tx.Rollback();
                    return HierarchyApiResponse<bool>.ErrorResponse("Sub-HOD not found.");
                }

                if ((int)subHod.RoleTypeId != (int)RoleTypeEnum.SubHOD)
                {
                    tx.Rollback();
                    return HierarchyApiResponse<bool>.ErrorResponse("Sub-department multi-select is only allowed for Sub-HOD.");
                }

                var hodExists = await conn.ExecuteScalarAsync<int>(@"
SELECT COUNT(1)
FROM [Hie].[Employees]
WHERE EmployeeId = @HodEmployeeId
  AND RoleTypeId = @HodRoleTypeId",
                    new { request.HodEmployeeId, HodRoleTypeId = (int)RoleTypeEnum.HOD }, tx);

                if (hodExists == 0)
                {
                    tx.Rollback();
                    return HierarchyApiResponse<bool>.ErrorResponse("HOD not found.");
                }

                var validSubDepartmentCount = await conn.ExecuteScalarAsync<int>(@"
SELECT COUNT(DISTINCT SubDepartmentId)
FROM [Hie].[SubDepartments]
WHERE DepartmentId = @DepartmentId
  AND IsActive = 1
  AND SubDepartmentId IN @SubDepartmentIds",
                    new { request.DepartmentId, SubDepartmentIds = subDepartmentIds }, tx);

                if (validSubDepartmentCount != subDepartmentIds.Count)
                {
                    tx.Rollback();
                    return HierarchyApiResponse<bool>.ErrorResponse("One or more selected sub-departments do not belong to this department.");
                }

                var oldSubDepartmentIds = (await conn.QueryAsync<int>(@"
SELECT DISTINCT rr.SubDepartmentId
FROM [Hie].[EmployeeReportingRelationships] rr
WHERE rr.EmployeeId = @SubHodEmployeeId
  AND rr.ReportsToEmployeeId = @HodEmployeeId
  AND rr.IsActive = 1
  AND rr.DepartmentId = @DepartmentId
  AND rr.SubDepartmentId IS NOT NULL",
                    new { request.SubHodEmployeeId, request.HodEmployeeId, request.DepartmentId }, tx)).ToList();

                var removedSubDepartmentIds = oldSubDepartmentIds
                    .Distinct()
                    .Except(subDepartmentIds)
                    .ToList();
                var addedSubDepartmentIds = subDepartmentIds
                    .Except(oldSubDepartmentIds.Distinct())
                    .ToList();

                await conn.ExecuteAsync(@"
UPDATE [Hie].[Employees]
SET PrimaryDepartmentId = @DepartmentId
WHERE EmployeeId = @SubHodEmployeeId;

UPDATE rr
SET rr.IsActive = 0,
    rr.IsPrimary = 0,
    rr.EffectiveTo = COALESCE(rr.EffectiveTo, GETDATE())
FROM [Hie].[EmployeeReportingRelationships] rr
WHERE rr.EmployeeId = @SubHodEmployeeId
  AND rr.ReportsToEmployeeId = @HodEmployeeId
  AND rr.IsActive = 1
  AND rr.DepartmentId = @DepartmentId
  AND rr.SubDepartmentId NOT IN @SubDepartmentIds;

UPDATE rr
SET rr.IsActive = 1,
    rr.IsPrimary = 1,
    rr.EffectiveTo = NULL
FROM [Hie].[EmployeeReportingRelationships] rr
WHERE rr.EmployeeId = @SubHodEmployeeId
  AND rr.ReportsToEmployeeId = @HodEmployeeId
  AND rr.DepartmentId = @DepartmentId
  AND rr.SubDepartmentId IN @SubDepartmentIds
  AND rr.IsActive = 0
  AND NOT EXISTS (
      SELECT 1
      FROM [Hie].[EmployeeReportingRelationships] activeRow
      WHERE activeRow.EmployeeId = rr.EmployeeId
        AND activeRow.ReportsToEmployeeId = rr.ReportsToEmployeeId
        AND activeRow.DepartmentId = rr.DepartmentId
        AND activeRow.SubDepartmentId = rr.SubDepartmentId
        AND activeRow.IsActive = 1
  );

INSERT INTO [Hie].[EmployeeReportingRelationships]
    (EmployeeId, ReportsToEmployeeId, ReportingTypeId, DepartmentId, SubDepartmentId, IsPrimary, EffectiveFrom, EffectiveTo, IsActive)
SELECT @SubHodEmployeeId, @HodEmployeeId, 1, @DepartmentId, selected.SubDepartmentId, 1, GETDATE(), NULL, 1
FROM (
    SELECT DISTINCT CAST(value AS INT) AS SubDepartmentId
    FROM STRING_SPLIT(@SubDepartmentIdCsv, ',')
    WHERE TRY_CAST(value AS INT) IS NOT NULL
) selected
WHERE NOT EXISTS (
    SELECT 1
    FROM [Hie].[EmployeeReportingRelationships] existing
    WHERE existing.EmployeeId = @SubHodEmployeeId
      AND existing.ReportsToEmployeeId = @HodEmployeeId
      AND existing.DepartmentId = @DepartmentId
      AND existing.SubDepartmentId = selected.SubDepartmentId
      AND existing.IsActive = 1
);",
                    new
                    {
                        request.SubHodEmployeeId,
                        request.HodEmployeeId,
                        request.DepartmentId,
                        SubDepartmentIds = subDepartmentIds,
                        SubDepartmentIdCsv = string.Join(",", subDepartmentIds)
                    }, tx);

                var movedExecutives = 0;
                foreach (var removedSubDepartmentId in removedSubDepartmentIds)
                {
                    movedExecutives += await MoveSubHodExecutivesToHodForRemovedSubDepartmentAsync(
                        conn,
                        tx,
                        request.SubHodEmployeeId,
                        request.HodEmployeeId,
                        request.DepartmentId,
                        removedSubDepartmentId);
                }
                var adoptedExecutives = 0;
                foreach (var addedSubDepartmentId in addedSubDepartmentIds)
                {
                    adoptedExecutives += await MoveDirectExecutivesToSubHodAsync(
                        conn,
                        tx,
                        request.SubHodEmployeeId,
                        request.HodEmployeeId,
                        request.DepartmentId,
                        addedSubDepartmentId);
                }

                tx.Commit();

                _ = LogAuditAsync("SubHODSubDeptSync", "Employee", request.SubHodEmployeeId, request.SubHodEmployeeId,
                    ToJson(new { SubDepartmentIds = oldSubDepartmentIds }),
                    ToJson(new { request.HodEmployeeId, request.DepartmentId, SubDepartmentIds = subDepartmentIds }),
                    $"Updated Sub-HOD sub-department selection. {movedExecutives} executive(s) moved direct to HOD from deselected sub-department(s); {adoptedExecutives} direct executive(s) moved under Sub-HOD for selected sub-department(s).",
                    updatedBy);

                return HierarchyApiResponse<bool>.SuccessResponse(true, "Sub-HOD sub-departments updated successfully.");
            }
            catch (Exception ex)
            {
                return HierarchyApiResponse<bool>.ErrorResponse(ex.Message);
            }
        }

        private static async Task EnsureHodDepartmentAssignmentsTableAsync(SqlConnection conn, SqlTransaction? tx = null)
        {
            const string sql = @"
IF OBJECT_ID(N'[Hie].[HodDepartmentAssignments]', N'U') IS NULL
BEGIN
    CREATE TABLE [Hie].[HodDepartmentAssignments](
        HodDepartmentAssignmentId INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        HodEmployeeId INT NOT NULL,
        DepartmentId INT NOT NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_HodDepartmentAssignments_IsActive DEFAULT(1),
        CreatedOn DATETIME NOT NULL CONSTRAINT DF_HodDepartmentAssignments_CreatedOn DEFAULT(GETDATE()),
        CreatedBy INT NULL,
        ModifiedOn DATETIME NULL,
        ModifiedBy INT NULL
    );

    CREATE UNIQUE INDEX UX_HodDepartmentAssignments_Active
        ON [Hie].[HodDepartmentAssignments](HodEmployeeId, DepartmentId, IsActive);
END;";

            await conn.ExecuteAsync(sql, transaction: tx);
        }

        private static async Task EnsureHierarchyDepartmentForeignKeysAsync(SqlConnection conn)
        {
            const string sql = @"
IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys fk
    WHERE fk.name = N'FK_Rel_Dept'
      AND fk.parent_object_id = OBJECT_ID(N'[Hie].[EmployeeReportingRelationships]')
      AND fk.referenced_object_id = OBJECT_ID(N'[dbo].[jsDepartment]')
)
BEGIN
    ALTER TABLE [Hie].[EmployeeReportingRelationships] DROP CONSTRAINT [FK_Rel_Dept];
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys fk
    WHERE fk.name = N'FK_Rel_Dept'
      AND fk.parent_object_id = OBJECT_ID(N'[Hie].[EmployeeReportingRelationships]')
)
BEGIN
    ALTER TABLE [Hie].[EmployeeReportingRelationships] WITH CHECK
    ADD CONSTRAINT [FK_Rel_Dept] FOREIGN KEY ([DepartmentId])
    REFERENCES [Hie].[Departments] ([DepartmentId]);
END;

IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys fk
    WHERE fk.name = N'FK_Emp_Dept'
      AND fk.parent_object_id = OBJECT_ID(N'[Hie].[Employees]')
      AND fk.referenced_object_id = OBJECT_ID(N'[dbo].[jsDepartment]')
)
BEGIN
    ALTER TABLE [Hie].[Employees] DROP CONSTRAINT [FK_Emp_Dept];
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys fk
    WHERE fk.name = N'FK_Emp_Dept'
      AND fk.parent_object_id = OBJECT_ID(N'[Hie].[Employees]')
)
BEGIN
    ALTER TABLE [Hie].[Employees] WITH CHECK
    ADD CONSTRAINT [FK_Emp_Dept] FOREIGN KEY ([PrimaryDepartmentId])
    REFERENCES [Hie].[Departments] ([DepartmentId]);
END;

IF EXISTS (
    SELECT 1
    FROM sys.foreign_keys fk
    WHERE fk.name = N'FK_SubDept_Dept'
      AND fk.parent_object_id = OBJECT_ID(N'[Hie].[SubDepartments]')
      AND fk.referenced_object_id = OBJECT_ID(N'[dbo].[jsDepartment]')
)
BEGIN
    ALTER TABLE [Hie].[SubDepartments] DROP CONSTRAINT [FK_SubDept_Dept];
END;

IF NOT EXISTS (
    SELECT 1
    FROM sys.foreign_keys fk
    WHERE fk.name = N'FK_SubDept_Dept'
      AND fk.parent_object_id = OBJECT_ID(N'[Hie].[SubDepartments]')
)
BEGIN
    ALTER TABLE [Hie].[SubDepartments] WITH CHECK
    ADD CONSTRAINT [FK_SubDept_Dept] FOREIGN KEY ([DepartmentId])
    REFERENCES [Hie].[Departments] ([DepartmentId]);
END;";

            await conn.ExecuteAsync(sql);
        }

        private async Task<int?> GetActivePrimaryScopeDepartmentIdAsync(SqlConnection conn, SqlTransaction tx, int hodEmployeeId)
        {
            const string sql = @"
SELECT TOP 1 rr.DepartmentId
FROM [Hie].[EmployeeReportingRelationships] rr
WHERE rr.ReportsToEmployeeId = @HodEmployeeId
  AND rr.IsActive = 1
ORDER BY
    CASE WHEN rr.IsPrimary = 1 THEN 0 ELSE 1 END,
    rr.RelationshipId DESC";

            return await conn.QueryFirstOrDefaultAsync<int?>(sql, new { HodEmployeeId = hodEmployeeId }, tx);
        }

        private async Task<int> DetachHodRelationshipsForRemovedDepartmentAsync(
            SqlConnection conn,
            SqlTransaction tx,
            int hodEmployeeId,
            int? originalDepartmentId)
        {
            const string sql = @"
DECLARE @Detached TABLE (RelationshipId INT PRIMARY KEY);

INSERT INTO @Detached (RelationshipId)
SELECT rr.RelationshipId
FROM [Hie].[EmployeeReportingRelationships] rr
WHERE rr.ReportsToEmployeeId = @HodEmployeeId
  AND rr.IsActive = 1
  AND ISNULL(rr.DepartmentId, 0) = ISNULL(@OriginalDepartmentId, 0);

UPDATE rr
SET rr.IsActive = 0,
    rr.IsPrimary = 0,
    rr.EffectiveTo = COALESCE(rr.EffectiveTo, GETDATE())
FROM [Hie].[EmployeeReportingRelationships] rr
INNER JOIN @Detached d ON d.RelationshipId = rr.RelationshipId;

SELECT COUNT(*) FROM @Detached;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                HodEmployeeId = hodEmployeeId,
                OriginalDepartmentId = originalDepartmentId
            }, tx);
        }

        private async Task<int> UnassignHodTeamAsync(
            SqlConnection conn,
            SqlTransaction tx,
            int hodEmployeeId,
            int updatedBy)
        {
            await EnsureHodDepartmentAssignmentsTableAsync(conn, tx);

            const string sql = @"
DECLARE @Detached TABLE (EmployeeId INT PRIMARY KEY);

INSERT INTO @Detached (EmployeeId)
SELECT DISTINCT rr.EmployeeId
FROM [Hie].[EmployeeReportingRelationships] rr
WHERE rr.ReportsToEmployeeId = @HodEmployeeId
  AND rr.IsActive = 1;

UPDATE rr
SET rr.IsActive = 0,
    rr.IsPrimary = 0,
    rr.EffectiveTo = COALESCE(rr.EffectiveTo, GETDATE())
FROM [Hie].[EmployeeReportingRelationships] rr
WHERE rr.ReportsToEmployeeId = @HodEmployeeId
  AND rr.IsActive = 1;

UPDATE e
SET e.PrimaryDepartmentId = NULL
FROM [Hie].[Employees] e
INNER JOIN @Detached d ON d.EmployeeId = e.EmployeeId
WHERE e.RoleTypeId IN (2, 3);

UPDATE [Hie].[HodDepartmentAssignments]
SET IsActive = 0,
    ModifiedOn = GETDATE(),
    ModifiedBy = @UpdatedBy
WHERE HodEmployeeId = @HodEmployeeId
  AND IsActive = 1;

SELECT COUNT(*) FROM @Detached;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                HodEmployeeId = hodEmployeeId,
                UpdatedBy = updatedBy
            }, tx);
        }

        private async Task<int> ReassignSubHodExecutivesToHodForDepartmentChangeAsync(
            SqlConnection conn,
            SqlTransaction tx,
            int subHodEmployeeId,
            int? originalHodId)
        {
            if (!originalHodId.HasValue || originalHodId.Value <= 0) return 0;

            const string sql = @"
DECLARE @Moved TABLE (EmployeeId INT, DepartmentId INT, SubDepartmentId INT);

INSERT INTO @Moved (EmployeeId, DepartmentId, SubDepartmentId)
SELECT rr.EmployeeId, rr.DepartmentId, rr.SubDepartmentId
FROM [Hie].[EmployeeReportingRelationships] rr
INNER JOIN [Hie].[Employees] e ON e.EmployeeId = rr.EmployeeId
WHERE rr.ReportsToEmployeeId = @SubHodEmployeeId
  AND rr.IsActive = 1
  AND e.RoleTypeId = 3;

UPDATE rr
SET rr.IsActive = 0,
    rr.IsPrimary = 0,
    rr.EffectiveTo = COALESCE(rr.EffectiveTo, GETDATE())
FROM [Hie].[EmployeeReportingRelationships] rr
INNER JOIN @Moved m ON m.EmployeeId = rr.EmployeeId
WHERE rr.ReportsToEmployeeId = @SubHodEmployeeId
  AND rr.IsActive = 1;

INSERT INTO [Hie].[EmployeeReportingRelationships]
    (EmployeeId, ReportsToEmployeeId, ReportingTypeId, DepartmentId, SubDepartmentId, IsPrimary, EffectiveFrom, EffectiveTo, IsActive)
SELECT m.EmployeeId, @OriginalHodId, 1, m.DepartmentId, m.SubDepartmentId, 1, GETDATE(), NULL, 1
FROM @Moved m
WHERE NOT EXISTS (
    SELECT 1
    FROM [Hie].[EmployeeReportingRelationships] existing
    WHERE existing.EmployeeId = m.EmployeeId
      AND existing.ReportsToEmployeeId = @OriginalHodId
      AND existing.IsActive = 1
      AND ISNULL(existing.DepartmentId, 0) = ISNULL(m.DepartmentId, 0)
      AND ISNULL(existing.SubDepartmentId, 0) = ISNULL(m.SubDepartmentId, 0)
);

SELECT COUNT(*) FROM @Moved;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                SubHodEmployeeId = subHodEmployeeId,
                OriginalHodId = originalHodId.Value
            }, tx);
        }

        private async Task<int> MoveDirectExecutivesToSubHodAsync(
            SqlConnection conn,
            SqlTransaction tx,
            int subHodEmployeeId,
            int hodEmployeeId,
            int? departmentId,
            int? subDepartmentId)
        {
            const string sql = @"
DECLARE @Moved TABLE (EmployeeId INT, DepartmentId INT, SubDepartmentId INT);

INSERT INTO @Moved (EmployeeId, DepartmentId, SubDepartmentId)
SELECT rr.EmployeeId, rr.DepartmentId, rr.SubDepartmentId
FROM [Hie].[EmployeeReportingRelationships] rr
INNER JOIN [Hie].[Employees] e ON e.EmployeeId = rr.EmployeeId
WHERE rr.ReportsToEmployeeId = @HodEmployeeId
  AND rr.IsActive = 1
  AND e.RoleTypeId = 3
  AND rr.EmployeeId <> @SubHodEmployeeId
  AND ISNULL(rr.DepartmentId, 0) = ISNULL(@DepartmentId, 0)
  AND ISNULL(rr.SubDepartmentId, 0) = ISNULL(@SubDepartmentId, 0);

UPDATE rr
SET rr.IsActive = 0,
    rr.IsPrimary = 0,
    rr.EffectiveTo = COALESCE(rr.EffectiveTo, GETDATE())
FROM [Hie].[EmployeeReportingRelationships] rr
INNER JOIN @Moved m ON m.EmployeeId = rr.EmployeeId
WHERE rr.ReportsToEmployeeId = @HodEmployeeId
  AND rr.IsActive = 1
  AND ISNULL(rr.DepartmentId, 0) = ISNULL(@DepartmentId, 0)
  AND ISNULL(rr.SubDepartmentId, 0) = ISNULL(@SubDepartmentId, 0);

INSERT INTO [Hie].[EmployeeReportingRelationships]
    (EmployeeId, ReportsToEmployeeId, ReportingTypeId, DepartmentId, SubDepartmentId, IsPrimary, EffectiveFrom, EffectiveTo, IsActive)
SELECT m.EmployeeId, @SubHodEmployeeId, 1, m.DepartmentId, m.SubDepartmentId, 1, GETDATE(), NULL, 1
FROM @Moved m
WHERE NOT EXISTS (
    SELECT 1
    FROM [Hie].[EmployeeReportingRelationships] existing
    WHERE existing.EmployeeId = m.EmployeeId
      AND existing.ReportsToEmployeeId = @SubHodEmployeeId
      AND existing.IsActive = 1
      AND ISNULL(existing.DepartmentId, 0) = ISNULL(m.DepartmentId, 0)
      AND ISNULL(existing.SubDepartmentId, 0) = ISNULL(m.SubDepartmentId, 0)
);

SELECT COUNT(*) FROM @Moved;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                SubHodEmployeeId = subHodEmployeeId,
                HodEmployeeId = hodEmployeeId,
                DepartmentId = departmentId,
                SubDepartmentId = subDepartmentId
            }, tx);
        }

        private async Task<int> MoveSubHodExecutivesToHodForRemovedSubDepartmentAsync(
            SqlConnection conn,
            SqlTransaction tx,
            int subHodEmployeeId,
            int hodEmployeeId,
            int departmentId,
            int subDepartmentId)
        {
            const string sql = @"
DECLARE @Moved TABLE (EmployeeId INT, DepartmentId INT, SubDepartmentId INT);

INSERT INTO @Moved (EmployeeId, DepartmentId, SubDepartmentId)
SELECT rr.EmployeeId, rr.DepartmentId, rr.SubDepartmentId
FROM [Hie].[EmployeeReportingRelationships] rr
INNER JOIN [Hie].[Employees] e ON e.EmployeeId = rr.EmployeeId
WHERE rr.ReportsToEmployeeId = @SubHodEmployeeId
  AND rr.IsActive = 1
  AND e.RoleTypeId = 3
  AND rr.DepartmentId = @DepartmentId
  AND rr.SubDepartmentId = @SubDepartmentId;

UPDATE rr
SET rr.IsActive = 0,
    rr.IsPrimary = 0,
    rr.EffectiveTo = COALESCE(rr.EffectiveTo, GETDATE())
FROM [Hie].[EmployeeReportingRelationships] rr
INNER JOIN @Moved m ON m.EmployeeId = rr.EmployeeId
WHERE rr.ReportsToEmployeeId = @SubHodEmployeeId
  AND rr.IsActive = 1
  AND rr.DepartmentId = @DepartmentId
  AND rr.SubDepartmentId = @SubDepartmentId;

INSERT INTO [Hie].[EmployeeReportingRelationships]
    (EmployeeId, ReportsToEmployeeId, ReportingTypeId, DepartmentId, SubDepartmentId, IsPrimary, EffectiveFrom, EffectiveTo, IsActive)
SELECT m.EmployeeId, @HodEmployeeId, 1, m.DepartmentId, m.SubDepartmentId, 1, GETDATE(), NULL, 1
FROM @Moved m
WHERE NOT EXISTS (
    SELECT 1
    FROM [Hie].[EmployeeReportingRelationships] existing
    WHERE existing.EmployeeId = m.EmployeeId
      AND existing.ReportsToEmployeeId = @HodEmployeeId
      AND existing.IsActive = 1
      AND existing.DepartmentId = m.DepartmentId
      AND existing.SubDepartmentId = m.SubDepartmentId
);

SELECT COUNT(*) FROM @Moved;";

            return await conn.ExecuteScalarAsync<int>(sql, new
            {
                SubHodEmployeeId = subHodEmployeeId,
                HodEmployeeId = hodEmployeeId,
                DepartmentId = departmentId,
                SubDepartmentId = subDepartmentId
            }, tx);
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
                await EnsureHierarchyDepartmentForeignKeysAsync(conn);
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
            await connection.OpenAsync();
            var departments = (await connection.QueryAsync<HODDepartmentDto>(sql, new { HODId = hodId })).ToList();

            await EnsureHodDepartmentAssignmentsTableAsync(connection);
            var assignedDepartments = (await connection.QueryAsync<HODDepartmentDto>(@"
SELECT
    d.DepartmentId,
    d.DepartmentName,
    CAST(0 AS INT) AS SubDeptCount,
    CAST(0 AS INT) AS SubHODCount,
    CAST(0 AS INT) AS ExecutiveCount
FROM [Hie].[HodDepartmentAssignments] hda
INNER JOIN [Hie].[Departments] d ON d.DepartmentId = hda.DepartmentId
WHERE hda.HodEmployeeId = @HODId
  AND hda.IsActive = 1",
                new { HODId = hodId })).ToList();

            return departments
                .Concat(assignedDepartments)
                .GroupBy(d => d.DepartmentId)
                .Select(g =>
                {
                    var first = g.First();
                    first.SubDeptCount = g.Max(x => x.SubDeptCount);
                    first.SubHODCount = g.Max(x => x.SubHODCount);
                    first.ExecutiveCount = g.Max(x => x.ExecutiveCount);
                    return first;
                })
                .OrderBy(d => d.DepartmentName)
                .ToList();
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

                await AddHodPrimaryDepartmentRowsAsync(adminRows);
                await AddSubHodTeamRowsWithoutHodAsync(adminRows);

                var existingEmployeeIds = new HashSet<int>(
                    adminRows.SelectMany(r => new[] { r.HodEmployeeId, r.SubHodEmployeeId, r.ExecEmployeeId })
                        .Where(id => id > 0));

                var orphanRows = (await GetOrphanEmployeesAsync())
                    .Where(e => e.EmployeeId > 0 && !existingEmployeeIds.Contains(e.EmployeeId))
                    .Select(e => new MasterFlatAdminRowDto
                    {
                        HodEmployeeId = e.RoleTypeId == (int)RoleTypeEnum.HOD ? e.EmployeeId : 0,
                        HodCode = e.RoleTypeId == (int)RoleTypeEnum.HOD ? e.EmployeeCode : "",
                        HodName = e.RoleTypeId == (int)RoleTypeEnum.HOD ? e.EmployeeName : "",
                        HodDesignation = e.RoleTypeId == (int)RoleTypeEnum.HOD ? e.Designation : "",
                        HodIsActive = e.RoleTypeId == (int)RoleTypeEnum.HOD && e.IsActive,
                        DepartmentName = e.DepartmentName,
                        SubDepartmentName = e.SubDepartmentName,
                        SubHodEmployeeId = e.RoleTypeId == (int)RoleTypeEnum.SubHOD ? e.EmployeeId : 0,
                        SubHodCode = e.RoleTypeId == (int)RoleTypeEnum.SubHOD ? e.EmployeeCode : "",
                        SubHodName = e.RoleTypeId == (int)RoleTypeEnum.SubHOD ? e.EmployeeName : "",
                        SubHodDesignation = e.RoleTypeId == (int)RoleTypeEnum.SubHOD ? e.Designation : "",
                        SubHodIsActive = e.RoleTypeId == (int)RoleTypeEnum.SubHOD && e.IsActive,
                        ExecEmployeeId = e.RoleTypeId == (int)RoleTypeEnum.Executive ? e.EmployeeId : 0,
                        ExecCode = e.RoleTypeId == (int)RoleTypeEnum.Executive ? e.EmployeeCode : "",
                        ExecName = e.RoleTypeId == (int)RoleTypeEnum.Executive ? e.EmployeeName : "",
                        ExecDesignation = e.RoleTypeId == (int)RoleTypeEnum.Executive ? e.Designation : "",
                        ExecIsActive = e.RoleTypeId == (int)RoleTypeEnum.Executive && e.IsActive
                    });

                adminRows.AddRange(orphanRows);

                existingEmployeeIds = new HashSet<int>(
                    adminRows.SelectMany(r => new[] { r.HodEmployeeId, r.SubHodEmployeeId, r.ExecEmployeeId })
                        .Where(id => id > 0));

                await AddMissingInactiveEmployeeRowsAsync(adminRows, existingEmployeeIds);

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

        private async Task AddMissingInactiveEmployeeRowsAsync(List<MasterFlatAdminRowDto> rows, HashSet<int> existingEmployeeIds)
        {
            using var conn = new SqlConnection(_connectionString);
            var inactiveEmployees = await conn.QueryAsync<dynamic>(@"
SELECT
    e.EmployeeId,
    e.EmployeeCode,
    e.EmployeeName,
    e.Designation,
    e.RoleTypeId,
    e.DateOfJoining,
    e.IsActive,
    e.PrimaryDepartmentId AS DepartmentId,
    d.DepartmentName
FROM [Hie].[Employees] e
LEFT JOIN [Hie].[Departments] d ON d.DepartmentId = e.PrimaryDepartmentId
WHERE e.IsActive = 0
  AND e.EmployeeId NOT IN @ExistingEmployeeIds",
                new { ExistingEmployeeIds = existingEmployeeIds.Any() ? existingEmployeeIds : new HashSet<int> { -1 } });

            foreach (var e in inactiveEmployees)
            {
                int roleTypeId = e.RoleTypeId == null ? 0 : Convert.ToInt32(e.RoleTypeId);
                var row = new MasterFlatAdminRowDto
                {
                    DepartmentId = e.DepartmentId == null ? null : (int?)Convert.ToInt32(e.DepartmentId),
                    DepartmentName = e.DepartmentName ?? "",
                    SubDepartmentName = ""
                };

                if (roleTypeId == (int)RoleTypeEnum.HOD)
                {
                    row.HodEmployeeId = Convert.ToInt32(e.EmployeeId);
                    row.HodCode = e.EmployeeCode ?? "";
                    row.HodName = e.EmployeeName ?? "";
                    row.HodDesignation = e.Designation ?? "";
                    row.HodDateOfJoining = TryGetDate(e.DateOfJoining);
                    row.HodIsActive = false;
                }
                else if (roleTypeId == (int)RoleTypeEnum.SubHOD)
                {
                    row.SubHodEmployeeId = Convert.ToInt32(e.EmployeeId);
                    row.SubHodCode = e.EmployeeCode ?? "";
                    row.SubHodName = e.EmployeeName ?? "";
                    row.SubHodDesignation = e.Designation ?? "";
                    row.SubHodDateOfJoining = TryGetDate(e.DateOfJoining);
                    row.SubHodIsActive = false;
                }
                else if (roleTypeId == (int)RoleTypeEnum.Executive)
                {
                    row.ExecEmployeeId = Convert.ToInt32(e.EmployeeId);
                    row.ExecCode = e.EmployeeCode ?? "";
                    row.ExecName = e.EmployeeName ?? "";
                    row.ExecDesignation = e.Designation ?? "";
                    row.ExecDateOfJoining = TryGetDate(e.DateOfJoining);
                    row.ExecIsActive = false;
                }
                else
                {
                    continue;
                }

                rows.Add(row);
            }
        }

        private async Task AddHodPrimaryDepartmentRowsAsync(List<MasterFlatAdminRowDto> rows)
        {
            var hodIds = rows
                .Where(r => r.HodEmployeeId > 0)
                .Select(r => r.HodEmployeeId)
                .Distinct()
                .ToList();

            if (!hodIds.Any()) return;

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            await EnsureHodDepartmentAssignmentsTableAsync(conn);
            var hodDepartments = await conn.QueryAsync<dynamic>(@"
SELECT
    e.EmployeeId,
    e.EmployeeCode,
    e.EmployeeName,
    e.Designation,
    e.DateOfJoining,
    e.IsActive,
    e.PrimaryDepartmentId AS DepartmentId,
    d.DepartmentName
FROM [Hie].[Employees] e
LEFT JOIN [Hie].[Departments] d ON d.DepartmentId = e.PrimaryDepartmentId
WHERE e.EmployeeId IN @HodIds
  AND e.RoleTypeId = 1
  AND e.PrimaryDepartmentId IS NOT NULL
UNION
SELECT
    e.EmployeeId,
    e.EmployeeCode,
    e.EmployeeName,
    e.Designation,
    e.DateOfJoining,
    e.IsActive,
    hda.DepartmentId,
    d.DepartmentName
FROM [Hie].[HodDepartmentAssignments] hda
INNER JOIN [Hie].[Employees] e ON e.EmployeeId = hda.HodEmployeeId
INNER JOIN [Hie].[Departments] d ON d.DepartmentId = hda.DepartmentId
WHERE hda.HodEmployeeId IN @HodIds
  AND hda.IsActive = 1
  AND e.RoleTypeId = 1",
                new { HodIds = hodIds });

            foreach (var h in hodDepartments)
            {
                int hodId = h.EmployeeId;
                int deptId = h.DepartmentId;
                bool alreadyVisible = rows.Any(r =>
                    r.HodEmployeeId == hodId
                    && (r.DepartmentId ?? 0) == deptId
                    && (r.SubHodEmployeeId > 0 || r.ExecEmployeeId > 0));

                if (alreadyVisible) continue;

                rows.Add(new MasterFlatAdminRowDto
                {
                    HodEmployeeId = hodId,
                    HodCode = h.EmployeeCode ?? "",
                    HodName = h.EmployeeName ?? "",
                    HodDesignation = h.Designation ?? "",
                    HodDateOfJoining = TryGetDate(h.DateOfJoining),
                    HodIsActive = TryGetBool(h.IsActive, true),
                    DepartmentId = deptId,
                    DepartmentName = h.DepartmentName ?? "",
                    SubDepartmentName = "",
                    SubHodEmployeeId = 0,
                    ExecEmployeeId = 0
                });
            }
        }

        private async Task AddSubHodTeamRowsWithoutHodAsync(List<MasterFlatAdminRowDto> rows)
        {
            using var conn = new SqlConnection(_connectionString);
            var teamRows = await conn.QueryAsync<dynamic>(@"
SELECT
    sub.EmployeeId AS SubHodEmployeeId,
    sub.EmployeeCode AS SubHodCode,
    sub.EmployeeName AS SubHodName,
    sub.Designation AS SubHodDesignation,
    sub.DateOfJoining AS SubHodDateOfJoining,
    sub.IsActive AS SubHodIsActive,
    exe.EmployeeId AS ExecEmployeeId,
    exe.EmployeeCode AS ExecCode,
    exe.EmployeeName AS ExecName,
    exe.Designation AS ExecDesignation,
    exe.DateOfJoining AS ExecDateOfJoining,
    exe.IsActive AS ExecIsActive,
    rr.RelationshipId AS ExecRelationshipId,
    rr.DepartmentId,
    d.DepartmentName,
    rr.SubDepartmentId,
    sd.SubDepartmentName
FROM [Hie].[EmployeeReportingRelationships] rr
INNER JOIN [Hie].[Employees] sub
    ON sub.EmployeeId = rr.ReportsToEmployeeId
   AND sub.RoleTypeId = 2
INNER JOIN [Hie].[Employees] exe
    ON exe.EmployeeId = rr.EmployeeId
   AND exe.RoleTypeId = 3
LEFT JOIN [Hie].[Departments] d
    ON d.DepartmentId = rr.DepartmentId
LEFT JOIN [Hie].[SubDepartments] sd
    ON sd.SubDepartmentId = rr.SubDepartmentId
WHERE rr.IsActive = 1
  AND NOT EXISTS
  (
      SELECT 1
      FROM [Hie].[EmployeeReportingRelationships] hodRel
      INNER JOIN [Hie].[Employees] hod
          ON hod.EmployeeId = hodRel.ReportsToEmployeeId
         AND hod.RoleTypeId = 1
      WHERE hodRel.EmployeeId = sub.EmployeeId
        AND hodRel.IsActive = 1
        AND ISNULL(hodRel.DepartmentId, 0) = ISNULL(rr.DepartmentId, 0)
  )");

            foreach (var t in teamRows)
            {
                int subHodId = Convert.ToInt32(t.SubHodEmployeeId);
                int execId = Convert.ToInt32(t.ExecEmployeeId);
                int? departmentId = t.DepartmentId == null ? null : (int?)Convert.ToInt32(t.DepartmentId);
                int? subDepartmentId = t.SubDepartmentId == null ? null : (int?)Convert.ToInt32(t.SubDepartmentId);

                bool alreadyVisible = rows.Any(r =>
                    r.SubHodEmployeeId == subHodId
                    && r.ExecEmployeeId == execId
                    && (r.DepartmentId ?? 0) == (departmentId ?? 0)
                    && (r.SubDepartmentId ?? 0) == (subDepartmentId ?? 0));

                if (alreadyVisible) continue;

                rows.Add(new MasterFlatAdminRowDto
                {
                    DepartmentId = departmentId,
                    DepartmentName = t.DepartmentName ?? "",
                    SubDepartmentId = subDepartmentId,
                    SubDepartmentName = t.SubDepartmentName ?? "",
                    SubHodEmployeeId = subHodId,
                    SubHodCode = t.SubHodCode ?? "",
                    SubHodName = t.SubHodName ?? "",
                    SubHodDesignation = t.SubHodDesignation ?? "",
                    SubHodDateOfJoining = TryGetDate(t.SubHodDateOfJoining),
                    SubHodIsActive = TryGetBool(t.SubHodIsActive, true),
                    ExecEmployeeId = execId,
                    ExecCode = t.ExecCode ?? "",
                    ExecName = t.ExecName ?? "",
                    ExecDesignation = t.ExecDesignation ?? "",
                    ExecDateOfJoining = TryGetDate(t.ExecDateOfJoining),
                    ExecIsActive = TryGetBool(t.ExecIsActive, true),
                    ExecRelationshipId = t.ExecRelationshipId == null ? null : (int?)Convert.ToInt32(t.ExecRelationshipId)
                });
            }
        }

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

            const string deactivateDuplicateExactSql = @"
UPDATE [Hie].[EmployeeReportingRelationships]
SET IsActive = 0,
    IsPrimary = 0,
    EffectiveTo = COALESCE(EffectiveTo, GETDATE())
WHERE EmployeeId = @EmployeeId
  AND ReportsToEmployeeId = @ReportsToEmployeeId
  AND ISNULL(DepartmentId, 0) = ISNULL(@DepartmentId, 0)
  AND ISNULL(SubDepartmentId, 0) = ISNULL(@SubDepartmentId, 0)
  AND IsActive = 1
  AND RelationshipId <> @RelationshipId";

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
                await conn.ExecuteAsync(
                    deactivateDuplicateExactSql,
                    new
                    {
                        EmployeeId = employeeId,
                        ReportsToEmployeeId = reportsToEmployeeId,
                        DepartmentId = departmentId,
                        SubDepartmentId = subDepartmentId,
                        RelationshipId = existingId.Value
                    },
                    tx);
            }
            else
            {
                await conn.ExecuteAsync(insertSql, new
                {
                    EmployeeId = employeeId,
                    ReportsToEmployeeId = reportsToEmployeeId,
                    DepartmentId = departmentId,
                    SubDepartmentId = subDepartmentId
                }, tx);
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
            await conn.OpenAsync();
            await EnsureHodDepartmentAssignmentsTableAsync(conn);
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
    COALESCE(rd.DepartmentName, d.DepartmentName) AS DepartmentName,
    sd.SubDepartmentName,
    CAST(NULL AS DECIMAL(18, 2)) AS Salary,
    e.IsActive
FROM [Hie].[Employees] e
OUTER APPLY (
    SELECT TOP 1 rr.ReportsToEmployeeId, rr.DepartmentId, rr.SubDepartmentId
    FROM [Hie].[EmployeeReportingRelationships] rr
    WHERE rr.EmployeeId = e.EmployeeId
      AND rr.IsActive = 1
    ORDER BY CASE WHEN rr.IsPrimary = 1 THEN 0 ELSE 1 END, rr.RelationshipId DESC
) activeRel
LEFT JOIN [Hie].[Employees] mgr
    ON mgr.EmployeeId = activeRel.ReportsToEmployeeId
LEFT JOIN [Hie].[Departments] d
    ON d.DepartmentId = e.PrimaryDepartmentId
LEFT JOIN [Hie].[Departments] rd
    ON rd.DepartmentId = activeRel.DepartmentId
LEFT JOIN [Hie].[SubDepartments] sd
    ON sd.SubDepartmentId = activeRel.SubDepartmentId
WHERE e.RoleTypeId IN (1, 2, 3)
  AND (
      (
          e.RoleTypeId = 1
          AND e.PrimaryDepartmentId IS NULL
          AND NOT EXISTS
          (
              SELECT 1
              FROM [Hie].[HodDepartmentAssignments] hda
              WHERE hda.HodEmployeeId = e.EmployeeId
                AND hda.IsActive = 1
          )
          AND NOT EXISTS
          (
              SELECT 1
              FROM [Hie].[EmployeeReportingRelationships] childRel
              WHERE childRel.ReportsToEmployeeId = e.EmployeeId
                AND childRel.IsActive = 1
          )
      )
      OR (
          e.RoleTypeId = 2
          AND activeRel.ReportsToEmployeeId IS NULL
          AND NOT EXISTS
          (
              SELECT 1
              FROM [Hie].[EmployeeReportingRelationships] childRel
              INNER JOIN [Hie].[Employees] child
                  ON child.EmployeeId = childRel.EmployeeId
                 AND child.RoleTypeId = 3
              WHERE childRel.ReportsToEmployeeId = e.EmployeeId
                AND childRel.IsActive = 1
          )
      )
      OR (
          e.RoleTypeId = 3
          AND activeRel.ReportsToEmployeeId IS NULL
      )
  )
  AND NOT EXISTS
  (
      SELECT 1
      FROM [Hie].[SalesHierarchy] sh
      WHERE sh.PersonCode = e.EmployeeCode
        AND sh.IsActive = 1
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

        public async Task<EmployeeModalExtrasDto?> GetEmployeeModalExtrasAsync(int employeeId)
        {
            using var conn = new SqlConnection(_connectionString);

            var columns = (await conn.QueryAsync<string>(
                "SELECT [name] FROM sys.columns WHERE object_id = OBJECT_ID(N'[Hie].[Employees]')"))
                .ToList();

            string? genderColumn = FindFirstExistingColumn(columns, "Gender");
            string? qualificationColumn = FindFirstExistingColumn(columns, "Qualification", "Qulaification");
            string? areaColumn = FindFirstExistingColumn(columns, "Area");
            string? sikhNonSikhColumn = FindFirstExistingColumn(
                columns,
                "Sikh / Non-Sikh",
                "Sikh/Non-Sikh",
                "SikhNonSikh",
                "Sikh_Non_Sikh",
                "sikh_no_sikh");

            string sql = $@"
SELECT TOP (1)
    EmployeeId,
    EmployeeCode,
    DateOfJoining,
    {BuildOptionalStringColumnExpression(genderColumn)} AS Gender,
    {BuildOptionalStringColumnExpression(qualificationColumn)} AS Qualification,
    {BuildOptionalStringColumnExpression(areaColumn)} AS Area,
    {BuildOptionalStringColumnExpression(sikhNonSikhColumn)} AS SikhNonSikh
FROM [Hie].[Employees]
WHERE EmployeeId = @EmployeeId";

            return await conn.QueryFirstOrDefaultAsync<EmployeeModalExtrasDto>(sql, new { EmployeeId = employeeId });
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

        private static string? FindFirstExistingColumn(IEnumerable<string> columns, params string[] candidates)
        {
            foreach (var candidate in candidates)
            {
                var match = columns.FirstOrDefault(col => string.Equals(col, candidate, StringComparison.OrdinalIgnoreCase));
                if (!string.IsNullOrWhiteSpace(match))
                    return match;
            }

            return null;
        }

        private static string BuildOptionalStringColumnExpression(string? columnName)
        {
            if (string.IsNullOrWhiteSpace(columnName))
                return "CAST(NULL AS NVARCHAR(255))";

            return $"CONVERT(NVARCHAR(255), [{columnName.Replace("]", "]]")}])";
        }

        private static string? NormalizeSikhNonSikhCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            var normalized = value.Trim();
            var compact = normalized.ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("/", "").Replace("-", "");

            if (compact == "s" || compact == "sikh")
                return "S";

            if (compact == "ns" || compact == "nonsikh" || compact == "notsikh")
                return "N-S";

            return normalized;
        }

        private sealed class EmployeeSikhValueRow
        {
            public int EmployeeId { get; set; }
            public string? SikhNonSikh { get; set; }
        }

        #endregion

        #region Sales Hierarchy

        public async Task<List<SalesHierarchyRowDto>> GetSalesHierarchyFlatAsync(
            string? h1, string? h2, string? h3, string? h4, string? search, bool activeOnly, int companyId)
        {
            const string sql = @"
                SELECT
                    emp.SalesHierarchyId,
                    emp.CompanyId,
                    h1.PersonCode AS H1Code,
                    h1.PersonName AS H1Name,
                    h2.PersonCode AS H2Code,
                    h2.PersonName AS H2Name,
                    h3.PersonCode AS H3Code,
                    h3.PersonName AS H3Name,
                    h4.PersonCode AS H4Code,
                    h4.PersonName AS H4Name,
                    CAST(NULL AS INT) AS EmployeeId,
                    emp.PersonCode AS EmpCode,
                    emp.PersonName AS EmpName,
                    ss.StateName AS State,
                    sg.GroupName,
                    sdg.DesignationName AS Designation,
                    d.DepartmentName AS Department,
                    emp.Mobile,
                    emp.Email,
                    emp.Qualification,
                    emp.Gender,
                    emp.SikhNonSikh,
                    COALESCE(map.Area, sp.Area) AS Area,
                    emp.DateOfJoining,
                    emp.IsActive,
                    emp.CreatedOn,
                    emp.ModifiedOn
                FROM [Hie].[SalesEmployeeHierarchyMap] map
                INNER JOIN [Hie].[SalesHierarchy] emp
                    ON emp.CompanyId = map.CompanyId
                   AND emp.PersonCode = map.EmpPersonCode
                LEFT JOIN [Hie].[SalesHierarchy] h1
                    ON h1.CompanyId = map.CompanyId
                   AND h1.PersonCode = map.H1PersonCode
                LEFT JOIN [Hie].[SalesHierarchy] h2
                    ON h2.CompanyId = map.CompanyId
                   AND h2.PersonCode = map.H2PersonCode
                LEFT JOIN [Hie].[SalesHierarchy] h3
                    ON h3.CompanyId = map.CompanyId
                   AND h3.PersonCode = map.H3PersonCode
                LEFT JOIN [Hie].[SalesHierarchy] h4
                    ON h4.CompanyId = map.CompanyId
                   AND h4.PersonCode = map.H4PersonCode
                LEFT JOIN [Hie].[SalesPersonProfile] sp
                    ON sp.CompanyId = emp.CompanyId
                   AND sp.SalesHierarchyId = emp.SalesHierarchyId
                   AND sp.IsActive = 1
                LEFT JOIN [Hie].[Departments] d
                    ON d.DepartmentId = COALESCE(map.DepartmentId, sp.DepartmentId)
                LEFT JOIN [Hie].[SalesStates] ss
                    ON ss.StateId = COALESCE(map.StateId, sp.StateId)
                LEFT JOIN [Hie].[SalesGroups] sg
                    ON sg.GroupId = COALESCE(map.GroupId, sp.GroupId)
                LEFT JOIN [Hie].[SalesDesignations] sdg
                    ON sdg.DesignationId = COALESCE(map.DesignationId, sp.DesignationId)
                WHERE map.CompanyId = @CompanyId
                  AND map.IsActive = 1
                  AND (@ActiveOnly = 0 OR emp.IsActive = 1)
                  AND (@H1Code IS NULL OR h1.PersonCode = @H1Code OR h1.PersonName = @H1Code)
                  AND (@H2Code IS NULL OR h2.PersonCode = @H2Code OR h2.PersonName = @H2Code)
                  AND (@H3Code IS NULL OR h3.PersonCode = @H3Code OR h3.PersonName = @H3Code)
                  AND (@H4Code IS NULL OR h4.PersonCode = @H4Code OR h4.PersonName = @H4Code)
                  AND (@Search IS NULL
                       OR emp.PersonCode LIKE '%' + @Search + '%'
                       OR emp.PersonName LIKE '%' + @Search + '%'
                       OR h1.PersonName LIKE '%' + @Search + '%'
                       OR h2.PersonName LIKE '%' + @Search + '%'
                       OR h3.PersonName LIKE '%' + @Search + '%'
                       OR h4.PersonName LIKE '%' + @Search + '%'
                       OR ss.StateName LIKE '%' + @Search + '%'
                       OR sg.GroupName LIKE '%' + @Search + '%'
                       OR sdg.DesignationName LIKE '%' + @Search + '%')
                ORDER BY h1.PersonName, h2.PersonName, h3.PersonName, h4.PersonName, emp.PersonName";

            using var conn = new SqlConnection(_connectionString);
            return (await conn.QueryAsync<SalesHierarchyRowDto>(sql, new
            {
                CompanyId = companyId,
                H1Code = string.IsNullOrWhiteSpace(h1) ? null : h1,
                H2Code = string.IsNullOrWhiteSpace(h2) ? null : h2,
                H3Code = string.IsNullOrWhiteSpace(h3) ? null : h3,
                H4Code = string.IsNullOrWhiteSpace(h4) ? null : h4,
                Search = string.IsNullOrWhiteSpace(search) ? null : search.Trim(),
                ActiveOnly = activeOnly
            })).ToList();
        }

        public async Task<SalesHierarchyStatsDto> GetSalesHierarchyStatsAsync(int companyId)
        {
            const string sql = @"
                SELECT
                    COUNT(DISTINCT h1.SalesHierarchyId) AS H1Count,
                    COUNT(DISTINCT h2.SalesHierarchyId) AS H2Count,
                    COUNT(DISTINCT h3.SalesHierarchyId) AS H3Count,
                    COUNT(DISTINCT h4.SalesHierarchyId) AS H4Count,
                    COUNT(DISTINCT emp.SalesHierarchyId) AS TotalEmployees,
                    SUM(CASE WHEN emp.IsActive = 1 THEN 1 ELSE 0 END) AS ActiveCount,
                    SUM(CASE WHEN emp.IsActive = 0 THEN 1 ELSE 0 END) AS InactiveCount
                FROM [Hie].[SalesEmployeeHierarchyMap] map
                INNER JOIN [Hie].[SalesHierarchy] emp
                    ON emp.CompanyId = map.CompanyId
                   AND emp.PersonCode = map.EmpPersonCode
                LEFT JOIN [Hie].[SalesHierarchy] h1
                    ON h1.CompanyId = map.CompanyId
                   AND h1.PersonCode = map.H1PersonCode
                LEFT JOIN [Hie].[SalesHierarchy] h2
                    ON h2.CompanyId = map.CompanyId
                   AND h2.PersonCode = map.H2PersonCode
                LEFT JOIN [Hie].[SalesHierarchy] h3
                    ON h3.CompanyId = map.CompanyId
                   AND h3.PersonCode = map.H3PersonCode
                LEFT JOIN [Hie].[SalesHierarchy] h4
                    ON h4.CompanyId = map.CompanyId
                   AND h4.PersonCode = map.H4PersonCode
                WHERE map.CompanyId = @CompanyId
                  AND map.IsActive = 1";

            using var conn = new SqlConnection(_connectionString);
            return await conn.QueryFirstOrDefaultAsync<SalesHierarchyStatsDto>(sql, new { CompanyId = companyId })
                   ?? new SalesHierarchyStatsDto();
        }

        public async Task<SalesImportResult> ImportSalesHierarchyAsync(
            List<SalesImportRowRequest> rows, int createdBy, int companyId)
        {
            var result = new SalesImportResult { TotalRows = rows.Count };

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            try
            {
                foreach (var row in rows)
                {
                    try
                    {
                        NormalizeSalesImportRow(row);

                        var importResult = await conn.QueryFirstAsync<dynamic>(@"
                            EXEC [Hie].[sp_ImportSalesHierarchyRow]
                                @CompanyId,
                                @H1Code,
                                @H1Name,
                                @H2Code,
                                @H2Name,
                                @H3Code,
                                @H3Name,
                                @H4Code,
                                @H4Name,
                                @EmpCode,
                                @EmpName,
                                @StateName,
                                @GroupName,
                                @DesignationName,
                                @Area,
                                @CreatedBy",
                            new
                            {
                                CompanyId = companyId,
                                row.H1Code,
                                row.H1Name,
                                row.H2Code,
                                row.H2Name,
                                row.H3Code,
                                row.H3Name,
                                row.H4Code,
                                row.H4Name,
                                row.EmpCode,
                                row.EmpName,
                                StateName = row.State,
                                row.GroupName,
                                DesignationName = row.Designation,
                                row.Area,
                                CreatedBy = createdBy
                            });

                        result.RowsUpserted++;
                        result.EmployeesCreated += Convert.ToInt32(importResult.CreatedCount ?? 0);
                        result.EmployeesUpdated += Convert.ToInt32(importResult.UpdatedCount ?? 0);
                    }
                    catch (Exception rowEx)
                    {
                        result.Errors++;
                        result.ErrorDetails.Add($"Row {row.RowNumber} ({row.EmpName}): {rowEx.Message}");
                    }
                }

                _ = LogAuditAsync("BulkImport", "SalesHierarchy", null, null, null,
                    ToJson(new { result.TotalRows, result.RowsUpserted, result.EmployeesCreated }),
                    $"Sales hierarchy import: {result.RowsUpserted} rows upserted", createdBy);
                return result;
            }
            catch { throw; }
        }

        private static async Task<int?> UpsertSalesHierarchyPersonAsync(
            SqlConnection conn,
            System.Data.Common.DbTransaction tx,
            int companyId,
            string? personCode,
            string? personName,
            int changedBy,
            SalesImportResult result)
        {
            if (string.IsNullOrWhiteSpace(personCode) && string.IsNullOrWhiteSpace(personName))
                return null;

            personCode = NormalizeJwplCode(personCode);
            personName = ToTitleCaseName(personName) ?? personCode;
            if (string.IsNullOrWhiteSpace(personName))
                return null;

            var existing = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT TOP 1 SalesHierarchyId, PersonCode, PersonName
                FROM [Hie].[SalesHierarchy]
                WHERE CompanyId = @CompanyId
                  AND (
                        (NULLIF(@PersonCode, '') IS NOT NULL AND PersonCode = @PersonCode)
                     OR (NULLIF(@PersonCode, '') IS NULL AND PersonCode IS NULL AND PersonName = @PersonName)
                  )
                ORDER BY SalesHierarchyId DESC",
                new { CompanyId = companyId, PersonCode = personCode, PersonName = personName }, tx);

            if (existing == null)
            {
                var id = await conn.QueryFirstAsync<int>(@"
                    INSERT INTO [Hie].[SalesHierarchy]
                        (CompanyId, PersonCode, PersonName, IsActive, CreatedBy, CreatedOn)
                    VALUES
                        (@CompanyId, @PersonCode, @PersonName, 1, @ChangedBy, SYSDATETIME());
                    SELECT CAST(SCOPE_IDENTITY() AS INT);",
                    new { CompanyId = companyId, PersonCode = personCode, PersonName = personName, ChangedBy = changedBy }, tx);

                result.EmployeesCreated++;
                return id;
            }

            await conn.ExecuteAsync(@"
                UPDATE [Hie].[SalesHierarchy]
                SET PersonCode = COALESCE(NULLIF(@PersonCode, ''), PersonCode),
                    PersonName = @PersonName,
                    IsActive = 1,
                    ModifiedBy = @ChangedBy,
                    ModifiedOn = SYSDATETIME()
                WHERE SalesHierarchyId = @SalesHierarchyId",
                new
                {
                    SalesHierarchyId = (int)existing.SalesHierarchyId,
                    PersonCode = personCode,
                    PersonName = personName,
                    ChangedBy = changedBy
                }, tx);

            result.EmployeesUpdated++;
            return (int)existing.SalesHierarchyId;
        }

        private static async Task<int> GetSalesHierarchyLevelIdAsync(
            SqlConnection conn,
            System.Data.Common.DbTransaction tx,
            int companyId,
            string levelCode,
            int changedBy)
        {
            levelCode = levelCode.Trim().ToUpperInvariant();
            var levelId = await conn.QueryFirstOrDefaultAsync<int?>(@"
                SELECT SalesHierarchyLevelId
                FROM [Hie].[SalesHierarchyLevels]
                WHERE CompanyId = @CompanyId AND LevelCode = @LevelCode",
                new { CompanyId = companyId, LevelCode = levelCode }, tx);

            if (levelId.HasValue)
                return levelId.Value;

            var levelOrder = levelCode == "EMP"
                ? 999
                : int.TryParse(levelCode.TrimStart('H'), out var parsed) ? parsed : 999;

            return await conn.QueryFirstAsync<int>(@"
                INSERT INTO [Hie].[SalesHierarchyLevels]
                    (CompanyId, LevelCode, LevelName, LevelOrder, IsActive, CreatedBy, CreatedOn)
                VALUES
                    (@CompanyId, @LevelCode, @LevelName, @LevelOrder, 1, @ChangedBy, SYSDATETIME());
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new
                {
                    CompanyId = companyId,
                    LevelCode = levelCode,
                    LevelName = levelCode == "EMP" ? "Employee/Promoter" : $"Hierarchy Level {levelOrder}",
                    LevelOrder = levelOrder,
                    ChangedBy = changedBy
                }, tx);
        }

        private static async Task<int> UpsertSalesHierarchyNodeAsync(
            SqlConnection conn,
            System.Data.Common.DbTransaction tx,
            int companyId,
            int salesHierarchyId,
            string levelCode,
            int? parentNodeId,
            int changedBy)
        {
            var levelId = await GetSalesHierarchyLevelIdAsync(conn, tx, companyId, levelCode, changedBy);

            var activeNode = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                SELECT TOP 1 SalesHierarchyNodeId, SalesHierarchyLevelId, ParentSalesHierarchyNodeId
                FROM [Hie].[SalesHierarchyNodes]
                WHERE CompanyId = @CompanyId
                  AND SalesHierarchyId = @SalesHierarchyId
                  AND IsActive = 1
                  AND EffectiveTo IS NULL
                ORDER BY SalesHierarchyNodeId DESC",
                new { CompanyId = companyId, SalesHierarchyId = salesHierarchyId }, tx);

            if (activeNode != null
                && (int)activeNode.SalesHierarchyLevelId == levelId
                && NullableIntEquals(activeNode.ParentSalesHierarchyNodeId, parentNodeId))
            {
                return (int)activeNode.SalesHierarchyNodeId;
            }

            if (activeNode != null)
            {
                await conn.ExecuteAsync(@"
                    UPDATE [Hie].[SalesHierarchyNodes]
                    SET IsActive = 0,
                        EffectiveTo = CONVERT(date, GETDATE()),
                        ModifiedBy = @ChangedBy,
                        ModifiedOn = SYSDATETIME()
                    WHERE SalesHierarchyNodeId = @SalesHierarchyNodeId",
                    new { SalesHierarchyNodeId = (int)activeNode.SalesHierarchyNodeId, ChangedBy = changedBy }, tx);
            }

            return await conn.QueryFirstAsync<int>(@"
                INSERT INTO [Hie].[SalesHierarchyNodes]
                    (CompanyId, SalesHierarchyId, SalesHierarchyLevelId, ParentSalesHierarchyNodeId,
                     EffectiveFrom, IsActive, CreatedBy, CreatedOn)
                VALUES
                    (@CompanyId, @SalesHierarchyId, @SalesHierarchyLevelId, @ParentSalesHierarchyNodeId,
                     CONVERT(date, GETDATE()), 1, @ChangedBy, SYSDATETIME());
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new
                {
                    CompanyId = companyId,
                    SalesHierarchyId = salesHierarchyId,
                    SalesHierarchyLevelId = levelId,
                    ParentSalesHierarchyNodeId = parentNodeId,
                    ChangedBy = changedBy
                }, tx);
        }

        private static bool NullableIntEquals(object? dbValue, int? expected)
        {
            if (dbValue == null) return !expected.HasValue;
            return Convert.ToInt32(dbValue) == expected.GetValueOrDefault();
        }

        private static async Task UpsertSalesPersonProfileAsync(
            SqlConnection conn,
            System.Data.Common.DbTransaction tx,
            int companyId,
            int salesHierarchyId,
            SalesImportRowRequest row,
            int changedBy)
        {
            var departmentId = await ResolveDepartmentIdAsync(conn, tx);
            var stateId = await ResolveSalesStateIdAsync(conn, tx, row.State);
            var groupId = await ResolveSalesGroupIdAsync(conn, tx, row.GroupName);
            var designationId = await ResolveSalesDesignationIdAsync(conn, tx, row.Designation);

            var existingProfileId = await conn.QueryFirstOrDefaultAsync<int?>(@"
                SELECT TOP 1 SalesPersonProfileId
                FROM [Hie].[SalesPersonProfile]
                WHERE CompanyId = @CompanyId
                  AND SalesHierarchyId = @SalesHierarchyId
                  AND IsActive = 1
                ORDER BY SalesPersonProfileId DESC",
                new { CompanyId = companyId, SalesHierarchyId = salesHierarchyId }, tx);

            if (existingProfileId.HasValue)
            {
                await conn.ExecuteAsync(@"
                    UPDATE [Hie].[SalesPersonProfile]
                    SET DepartmentId = @DepartmentId,
                        StateId = @StateId,
                        GroupId = @GroupId,
                        DesignationId = @DesignationId,
                        ModifiedBy = @ChangedBy,
                        ModifiedOn = SYSDATETIME()
                    WHERE SalesPersonProfileId = @SalesPersonProfileId",
                    new
                    {
                        SalesPersonProfileId = existingProfileId.Value,
                        DepartmentId = departmentId,
                        StateId = stateId,
                        GroupId = groupId,
                        DesignationId = designationId,
                        ChangedBy = changedBy
                    }, tx);
                return;
            }

            await conn.ExecuteAsync(@"
                INSERT INTO [Hie].[SalesPersonProfile]
                    (CompanyId, SalesHierarchyId, DepartmentId, StateId, GroupId, DesignationId,
                     IsActive, CreatedBy, CreatedOn)
                VALUES
                    (@CompanyId, @SalesHierarchyId, @DepartmentId, @StateId, @GroupId, @DesignationId,
                     1, @ChangedBy, SYSDATETIME())",
                new
                {
                    CompanyId = companyId,
                    SalesHierarchyId = salesHierarchyId,
                    DepartmentId = departmentId,
                    StateId = stateId,
                    GroupId = groupId,
                    DesignationId = designationId,
                    ChangedBy = changedBy
                }, tx);
        }

        private static Task<int?> ResolveDepartmentIdAsync(SqlConnection conn, System.Data.Common.DbTransaction tx) =>
            conn.QueryFirstOrDefaultAsync<int?>(@"
                SELECT TOP 1 DepartmentId
                FROM [Hie].[Departments]
                WHERE IsActive = 1 AND DepartmentName = N'Frontend Sales'
                ORDER BY DepartmentId", transaction: tx);

        private static async Task<int?> ResolveSalesStateIdAsync(SqlConnection conn, System.Data.Common.DbTransaction tx, string? stateName)
        {
            if (string.IsNullOrWhiteSpace(stateName)) return null;

            var stateId = await conn.QueryFirstOrDefaultAsync<int?>(@"
                SELECT TOP 1 StateId
                FROM [Hie].[SalesStates]
                WHERE UPPER(LTRIM(RTRIM(StateName))) = @StateName
                ORDER BY StateId",
                new { StateName = stateName.Trim().ToUpperInvariant() }, tx);

            if (stateId.HasValue) return stateId.Value;

            return await conn.QueryFirstAsync<int>(@"
                INSERT INTO [Hie].[SalesStates] (StateName)
                VALUES (@StateName);
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { StateName = stateName.Trim().ToUpperInvariant() }, tx);
        }

        private static async Task<int?> ResolveSalesGroupIdAsync(SqlConnection conn, System.Data.Common.DbTransaction tx, string? groupName)
        {
            if (string.IsNullOrWhiteSpace(groupName)) return null;

            var groupId = await conn.QueryFirstOrDefaultAsync<int?>(@"
                SELECT TOP 1 GroupId
                FROM [Hie].[SalesGroups]
                WHERE UPPER(LTRIM(RTRIM(GroupName))) = @GroupName
                ORDER BY GroupId",
                new { GroupName = groupName.Trim().ToUpperInvariant() }, tx);

            if (groupId.HasValue) return groupId.Value;

            return await conn.QueryFirstAsync<int>(@"
                INSERT INTO [Hie].[SalesGroups] (GroupName)
                VALUES (@GroupName);
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { GroupName = groupName.Trim().ToUpperInvariant() }, tx);
        }

        private static async Task<int?> ResolveSalesDesignationIdAsync(SqlConnection conn, System.Data.Common.DbTransaction tx, string? designationName)
        {
            if (string.IsNullOrWhiteSpace(designationName)) return null;

            var designationId = await conn.QueryFirstOrDefaultAsync<int?>(@"
                SELECT TOP 1 DesignationId
                FROM [Hie].[SalesDesignations]
                WHERE UPPER(LTRIM(RTRIM(DesignationName))) = @DesignationName
                ORDER BY DesignationId",
                new { DesignationName = designationName.Trim().ToUpperInvariant() }, tx);

            if (designationId.HasValue) return designationId.Value;

            return await conn.QueryFirstAsync<int>(@"
                INSERT INTO [Hie].[SalesDesignations] (DesignationName)
                VALUES (@DesignationName);
                SELECT CAST(SCOPE_IDENTITY() AS INT);",
                new { DesignationName = designationName.Trim().ToUpperInvariant() }, tx);
        }

        private static object BuildUpsertParams(SalesImportRowRequest row, int? employeeId, int companyId, int createdBy)
        {
            // Auto-generate codes for hierarchy levels when codes are missing but names are provided
            string GenerateCode(string name, string prefix)
            {
                if (string.IsNullOrWhiteSpace(name)) return null;
                // Create a code from the name: remove special chars, take first 10 chars, uppercase
                var code = System.Text.RegularExpressions.Regex.Replace(name.Trim(), @"[^a-zA-Z0-9\s]", "").Replace(" ", "_").ToUpper();
                return code.Length > 10 ? code.Substring(0, 10) : code;
            }

            return new
            {
                CompanyId = companyId,
                H1Code = !string.IsNullOrWhiteSpace(row.H1Code) ? row.H1Code.Trim() : GenerateCode(row.H1Name, "H1"),
                H1Name = row.H1Name?.Trim(),
                H2Code = !string.IsNullOrWhiteSpace(row.H2Code) ? row.H2Code.Trim() : GenerateCode(row.H2Name, "H2"),
                H2Name = row.H2Name?.Trim(),
                H3Code = !string.IsNullOrWhiteSpace(row.H3Code) ? row.H3Code.Trim() : GenerateCode(row.H3Name, "H3"),
                H3Name = row.H3Name?.Trim(),
                H4Code = !string.IsNullOrWhiteSpace(row.H4Code) ? row.H4Code.Trim() : GenerateCode(row.H4Name, "H4"),
                H4Name = row.H4Name?.Trim(),
                EmployeeId = employeeId,
                EmpCode = row.EmpCode?.Trim(),
                EmpName = row.EmpName?.Trim(),
                State = row.State?.Trim(),
                GroupName = row.GroupName?.Trim(),
                Designation = row.Designation?.Trim(),
                CreatedBy = createdBy
            };
        }

        private static void NormalizeSalesImportRow(SalesImportRowRequest row)
        {
            row.H1Code = NormalizeJwplCode(row.H1Code);
            row.H2Code = NormalizeJwplCode(row.H2Code);
            row.H3Code = NormalizeJwplCode(row.H3Code);
            row.H4Code = NormalizeJwplCode(row.H4Code);
            row.EmpCode = NormalizeJwplCode(row.EmpCode);

            row.H1Name = ToTitleCaseName(row.H1Name);
            row.H2Name = ToTitleCaseName(row.H2Name);
            row.H3Name = ToTitleCaseName(row.H3Name);
            row.H4Name = ToTitleCaseName(row.H4Name);
            row.EmpName = ToTitleCaseName(row.EmpName);

            row.State = NormalizeSalesText(row.State, true);
            row.GroupName = NormalizeSalesText(row.GroupName, true);
            row.Designation = NormalizeSalesText(row.Designation, true);
            row.Area = NormalizeSalesText(row.Area, false);
        }

        private static string? NormalizeJwplCode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var raw = value.Trim().ToUpperInvariant().Replace(" ", "");
            var match = System.Text.RegularExpressions.Regex.Match(raw, @"^([A-Z]+)(\d+)$");
            if (match.Success)
            {
                var prefix = match.Groups[1].Value;
                var digits = match.Groups[2].Value;
                return $"{prefix}{digits.PadLeft(4, '0')}";
            }

            if (System.Text.RegularExpressions.Regex.IsMatch(raw, @"^\d+$"))
                return $"JWPL{raw.PadLeft(4, '0')}";

            return raw;
        }

        private static string? ToTitleCaseName(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var cleaned = System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"\s+", " ").ToLowerInvariant();
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(cleaned);
        }

        private static string? NormalizeSalesText(string? value, bool upper)
        {
            if (string.IsNullOrWhiteSpace(value)) return null;

            var cleaned = System.Text.RegularExpressions.Regex.Replace(value.Trim(), @"\s+", " ");
            return upper ? cleaned.ToUpperInvariant() : cleaned;
        }

        public async Task<int> UpdateMissingSalesHierarchyCodesAsync(int companyId)
        {
            using var conn = new SqlConnection(_connectionString);
            return await conn.ExecuteAsync(@"
                UPDATE [Hie].[SalesHierarchy]
                SET H1Code = CASE WHEN NULLIF(H1Code, '') IS NULL AND NULLIF(H1Name, '') IS NOT NULL
                                  THEN UPPER(REPLACE(REPLACE(REPLACE(H1Name, ' ', '_'), '.', ''), '-', '_'))
                                  ELSE H1Code END,
                    H2Code = CASE WHEN (NULLIF(H2Code, '') IS NULL OR H2Code = H2Name OR H2Code LIKE '% %') AND NULLIF(H2Name, '') IS NOT NULL
                                  THEN UPPER(REPLACE(REPLACE(REPLACE(H2Name, ' ', '_'), '.', ''), '-', '_'))
                                  ELSE H2Code END,
                    H3Code = CASE WHEN (NULLIF(H3Code, '') IS NULL OR H3Code = H3Name OR H3Code LIKE '% %') AND NULLIF(H3Name, '') IS NOT NULL
                                  THEN UPPER(REPLACE(REPLACE(REPLACE(H3Name, ' ', '_'), '.', ''), '-', '_'))
                                  ELSE H3Code END,
                    H4Code = CASE WHEN (NULLIF(H4Code, '') IS NULL OR H4Code = H4Name OR H4Code LIKE '% %') AND NULLIF(H4Name, '') IS NOT NULL
                                  THEN UPPER(REPLACE(REPLACE(REPLACE(H4Name, ' ', '_'), '.', ''), '-', '_'))
                                  ELSE H4Code END,
                    ModifiedOn = GETDATE()
                WHERE CompanyId = @CompanyId
                  AND (NULLIF(H1Code, '') IS NULL AND NULLIF(H1Name, '') IS NOT NULL
                       OR (NULLIF(H2Code, '') IS NULL OR H2Code = H2Name OR H2Code LIKE '% %') AND NULLIF(H2Name, '') IS NOT NULL
                       OR (NULLIF(H3Code, '') IS NULL OR H3Code = H3Name OR H3Code LIKE '% %') AND NULLIF(H3Name, '') IS NOT NULL
                       OR (NULLIF(H4Code, '') IS NULL OR H4Code = H4Name OR H4Code LIKE '% %') AND NULLIF(H4Name, '') IS NOT NULL)",
                new { CompanyId = companyId });
        }

        public async Task<HierarchyApiResponse<bool>> UpdateSalesRowAsync(SalesUpdateRowRequest request, int updatedBy, int companyId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var originalPersonCode = NormalizeJwplCode(request.PersonCode) ?? NormalizeJwplCode(request.EmpCode);
                var target = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                    SELECT TOP (1) SalesHierarchyId, CompanyId
                    FROM [Hie].[SalesHierarchy]
                    WHERE CompanyId = @CompanyId
                      AND (
                            (NULLIF(@PersonCode, N'') IS NOT NULL AND PersonCode = @PersonCode)
                         OR (NULLIF(@PersonCode, N'') IS NULL AND SalesHierarchyId = @SalesHierarchyId)
                      )
                    ORDER BY SalesHierarchyId DESC",
                    new
                    {
                        CompanyId = companyId,
                        PersonCode = originalPersonCode,
                        request.SalesHierarchyId
                    });

                if (target == null) return HierarchyApiResponse<bool>.ErrorResponse("Row not found");
                int targetId = (int)target.SalesHierarchyId;

                var normalized = new
                {
                    Id = targetId,
                    EmpCode = NormalizeJwplCode(request.EmpCode),
                    EmpName = ToTitleCaseName(request.EmpName),
                    Mobile = NormalizeSalesText(request.Mobile, false),
                    Email = NormalizeSalesText(request.Email, false),
                    request.DateOfJoining,
                    State = NormalizeSalesText(request.State, true),
                    GroupName = NormalizeSalesText(request.GroupName, true),
                    Designation = NormalizeSalesText(request.Designation, true),
                    Qualification = NormalizeSalesText(request.Qualification, false),
                    Gender = NormalizeSalesText(request.Gender, false),
                    SikhNonSikh = NormalizeSikhNonSikhCode(request.SikhNonSikh),
                    Area = NormalizeSalesText(request.Area, false),
                    request.IsActive,
                    ModifiedBy = updatedBy
                };

                int affected = await conn.ExecuteAsync(@"
                    UPDATE [Hie].[SalesHierarchy] SET
                        PersonCode    = @EmpCode,
                        PersonName    = COALESCE(NULLIF(@EmpName, N''), PersonName),
                        Mobile        = @Mobile,
                        Email         = @Email,
                        DateOfJoining = @DateOfJoining,
                        Qualification = @Qualification,
                        Gender        = @Gender,
                        SikhNonSikh   = @SikhNonSikh,
                        IsActive      = @IsActive,
                        ModifiedBy    = @ModifiedBy,
                        ModifiedOn    = SYSDATETIME()
                    WHERE SalesHierarchyId = @Id",
                    normalized);

                if (affected == 0) return HierarchyApiResponse<bool>.ErrorResponse("Row not found");

                await conn.ExecuteAsync(@"
                    EXEC [Hie].[sp_UpsertSalesPersonProfile]
                        @CompanyId = @CompanyId,
                        @SalesHierarchyId = @SalesHierarchyId,
                        @StateName = @StateName,
                        @GroupName = @GroupName,
                        @DesignationName = @DesignationName,
                        @Area = @Area,
                        @ChangedBy = @ChangedBy",
                    new
                    {
                        CompanyId = companyId,
                        SalesHierarchyId = targetId,
                        StateName = normalized.State,
                        GroupName = normalized.GroupName,
                        DesignationName = normalized.Designation,
                        Area = normalized.Area,
                        ChangedBy = updatedBy
                    });

                _ = LogAuditAsync("Update", "SalesHierarchy", targetId, null, null,
                    ToJson(request), $"Sales row {targetId} updated", updatedBy);
                return HierarchyApiResponse<bool>.SuccessResponse(true, "Row updated successfully");
            }
            catch (Exception ex) { return HierarchyApiResponse<bool>.ErrorResponse(ex.Message); }
        }

        public async Task<HierarchyApiResponse<bool>> ShiftSalesEmployeeAsync(SalesShiftRequest request, int updatedBy, int companyId)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                var originalPersonCode = NormalizeJwplCode(request.PersonCode);
                var old = await conn.QueryFirstOrDefaultAsync<dynamic>(@"
                    WITH ActiveNodes AS
                    (
                        SELECT *
                        FROM [Hie].[SalesHierarchyNodes]
                        WHERE IsActive = 1 AND EffectiveTo IS NULL
                    )
                    SELECT
                        emp.SalesHierarchyId,
                        emp.CompanyId,
                        h1.PersonCode AS H1Code, h1.PersonName AS H1Name,
                        h2.PersonCode AS H2Code, h2.PersonName AS H2Name,
                        h3.PersonCode AS H3Code, h3.PersonName AS H3Name,
                        h4.PersonCode AS H4Code, h4.PersonName AS H4Name,
                        emp.PersonCode AS EmpCode,
                        emp.PersonName AS EmpName,
                        ss.StateName AS State,
                        sg.GroupName,
                        sdg.DesignationName AS Designation,
                        sp.Area
                    FROM [Hie].[SalesHierarchy] emp
                    LEFT JOIN ActiveNodes empNode
                        ON empNode.CompanyId = emp.CompanyId
                       AND empNode.SalesHierarchyId = emp.SalesHierarchyId
                    LEFT JOIN ActiveNodes h4Node
                        ON h4Node.SalesHierarchyNodeId = empNode.ParentSalesHierarchyNodeId
                    LEFT JOIN [Hie].[SalesHierarchy] h4
                        ON h4.SalesHierarchyId = h4Node.SalesHierarchyId
                    LEFT JOIN ActiveNodes h3Node
                        ON h3Node.SalesHierarchyNodeId = h4Node.ParentSalesHierarchyNodeId
                    LEFT JOIN [Hie].[SalesHierarchy] h3
                        ON h3.SalesHierarchyId = h3Node.SalesHierarchyId
                    LEFT JOIN ActiveNodes h2Node
                        ON h2Node.SalesHierarchyNodeId = h3Node.ParentSalesHierarchyNodeId
                    LEFT JOIN [Hie].[SalesHierarchy] h2
                        ON h2.SalesHierarchyId = h2Node.SalesHierarchyId
                    LEFT JOIN ActiveNodes h1Node
                        ON h1Node.SalesHierarchyNodeId = h2Node.ParentSalesHierarchyNodeId
                    LEFT JOIN [Hie].[SalesHierarchy] h1
                        ON h1.SalesHierarchyId = h1Node.SalesHierarchyId
                    LEFT JOIN [Hie].[SalesPersonProfile] sp
                        ON sp.CompanyId = emp.CompanyId
                       AND sp.SalesHierarchyId = emp.SalesHierarchyId
                       AND sp.IsActive = 1
                    LEFT JOIN [Hie].[SalesStates] ss
                        ON ss.StateId = sp.StateId
                    LEFT JOIN [Hie].[SalesGroups] sg
                        ON sg.GroupId = sp.GroupId
                    LEFT JOIN [Hie].[SalesDesignations] sdg
                        ON sdg.DesignationId = sp.DesignationId
                    WHERE emp.CompanyId = @CompanyId
                      AND (
                            (NULLIF(@PersonCode, N'') IS NOT NULL AND emp.PersonCode = @PersonCode)
                         OR (NULLIF(@PersonCode, N'') IS NULL AND emp.SalesHierarchyId = @Id)
                      )",
                    new
                    {
                        CompanyId = companyId,
                        PersonCode = originalPersonCode,
                        Id = request.SalesHierarchyId
                    });

                if (old == null) return HierarchyApiResponse<bool>.ErrorResponse("Row not found");

                var result = await conn.QueryFirstAsync<dynamic>(@"
                    EXEC [Hie].[sp_ImportSalesHierarchyRow]
                        @CompanyId,
                        @H1Code,
                        @H1Name,
                        @H2Code,
                        @H2Name,
                        @H3Code,
                        @H3Name,
                        @H4Code,
                        @H4Name,
                        @EmpCode,
                        @EmpName,
                        @StateName,
                        @GroupName,
                        @DesignationName,
                        @Area,
                        @CreatedBy",
                    new
                    {
                        CompanyId = (int)old.CompanyId,
                        H1Code = NormalizeJwplCode(request.NewH1Code),
                        H1Name = ToTitleCaseName(request.NewH1Name),
                        H2Code = NormalizeJwplCode(request.NewH2Code),
                        H2Name = ToTitleCaseName(request.NewH2Name),
                        H3Code = NormalizeJwplCode(request.NewH3Code),
                        H3Name = ToTitleCaseName(request.NewH3Name),
                        H4Code = NormalizeJwplCode(request.NewH4Code),
                        H4Name = ToTitleCaseName(request.NewH4Name),
                        EmpCode = (string?)old.EmpCode,
                        EmpName = (string?)old.EmpName,
                        StateName = (string?)old.State,
                        GroupName = (string?)old.GroupName,
                        DesignationName = (string?)old.Designation,
                        Area = (string?)old.Area,
                        CreatedBy = updatedBy
                    });

                _ = LogAuditAsync("Shift", "SalesHierarchy", request.SalesHierarchyId, null,
                    ToJson(old), ToJson(request), $"Sales employee shifted ({(string?)old.EmpCode ?? request.SalesHierarchyId.ToString()})", updatedBy);
                return HierarchyApiResponse<bool>.SuccessResponse(true, "Employee shifted successfully");
            }
            catch (Exception ex) { return HierarchyApiResponse<bool>.ErrorResponse(ex.Message); }
        }

        public async Task<List<AuditLogDto>> GetSalesAuditLogsAsync(AuditLogRequest request)
        {
            try
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
                        SearchTerm = string.IsNullOrWhiteSpace(request.SearchTerm) ? "SalesHierarchy" : request.SearchTerm,
                        request.PageNumber,
                        request.PageSize
                    })).ToList();
            }
            catch { return new List<AuditLogDto>(); }
        }

        public async Task<List<SalesStateDto>> GetSalesStatesAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return (await conn.QueryAsync<SalesStateDto>(
                    "SELECT StateId, StateName, IsActive FROM [Hie].[SalesStates] WHERE IsActive = 1 ORDER BY StateName"
                )).ToList();
            }
            catch { return new List<SalesStateDto>(); }
        }

        public async Task<List<SalesGroupDto>> GetSalesGroupsAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return (await conn.QueryAsync<SalesGroupDto>(
                    "SELECT GroupId, GroupName, IsActive FROM [Hie].[SalesGroups] WHERE IsActive = 1 ORDER BY GroupName"
                )).ToList();
            }
            catch { return new List<SalesGroupDto>(); }
        }

        public async Task<List<SalesDesignationDto>> GetSalesDesignationsAsync()
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return (await conn.QueryAsync<SalesDesignationDto>(
                    "SELECT DesignationId, DesignationName, IsActive FROM [Hie].[SalesDesignations] WHERE IsActive = 1 ORDER BY DesignationName"
                )).ToList();
            }
            catch { return new List<SalesDesignationDto>(); }
        }

        /// <summary>
        /// Returns sales data for org-tree injection.
        /// Chain: H1(HOD by empCode/name) → "Front Sales"(Dept) → H2 → H3 → H4 → Group → Employee
        /// </summary>
        public async Task<object> GetSalesHierarchyForTreeAsync(int companyId)
        {
            using var conn = new SqlConnection(_connectionString);
            const string sql = @"
                SELECT
                       h1.PersonCode AS H1Code,
                       h1.PersonName AS H1Name,
                       h2.PersonCode AS H2Code,
                       h2.PersonName AS H2Name,
                       h3.PersonCode AS H3Code,
                       h3.PersonName AS H3Name,
                       h4.PersonCode AS H4Code,
                       h4.PersonName AS H4Name,
                       emp.PersonCode AS EmpCode,
                       emp.PersonName AS EmpName,
                       sdg.DesignationName AS Designation,
                       ss.StateName AS State,
                       sg.GroupName,
                       CAST(NULL AS INT) AS EmployeeId,
                       emp.DateOfJoining,
                       emp.Gender,
                       emp.Qualification,
                       COALESCE(map.Area, sp.Area) AS Area,
                       emp.SikhNonSikh
                FROM [Hie].[SalesEmployeeHierarchyMap] map
                INNER JOIN [Hie].[SalesHierarchy] emp
                    ON emp.CompanyId = map.CompanyId
                   AND emp.PersonCode = map.EmpPersonCode
                LEFT JOIN [Hie].[SalesHierarchy] h1
                    ON h1.CompanyId = map.CompanyId
                   AND h1.PersonCode = map.H1PersonCode
                LEFT JOIN [Hie].[SalesHierarchy] h2
                    ON h2.CompanyId = map.CompanyId
                   AND h2.PersonCode = map.H2PersonCode
                LEFT JOIN [Hie].[SalesHierarchy] h3
                    ON h3.CompanyId = map.CompanyId
                   AND h3.PersonCode = map.H3PersonCode
                LEFT JOIN [Hie].[SalesHierarchy] h4
                    ON h4.CompanyId = map.CompanyId
                   AND h4.PersonCode = map.H4PersonCode
                LEFT JOIN [Hie].[SalesPersonProfile] sp
                    ON sp.CompanyId = emp.CompanyId
                   AND sp.SalesHierarchyId = emp.SalesHierarchyId
                   AND sp.IsActive = 1
                LEFT JOIN [Hie].[SalesStates] ss
                    ON ss.StateId = COALESCE(map.StateId, sp.StateId)
                LEFT JOIN [Hie].[SalesGroups] sg
                    ON sg.GroupId = COALESCE(map.GroupId, sp.GroupId)
                LEFT JOIN [Hie].[SalesDesignations] sdg
                    ON sdg.DesignationId = COALESCE(map.DesignationId, sp.DesignationId)
                WHERE map.CompanyId = @CompanyId
                  AND map.IsActive = 1
                  AND emp.IsActive = 1
                ORDER BY h1.PersonName, h2.PersonName, h3.PersonName, h4.PersonName, ISNULL(sg.GroupName, N''), emp.PersonName";
            var rows = (await conn.QueryAsync<dynamic>(sql, new { CompanyId = companyId })).ToList();

            var h1Map = new Dictionary<string, SalesH1Node>(StringComparer.OrdinalIgnoreCase);
            var seenEmployees = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var r in rows)
            {
                string h1Code  = ((string?)r.H1Code   ?? "").Trim();
                string h1Name  = ((string?)r.H1Name   ?? "").Trim();
                string h1Key = !string.IsNullOrEmpty(h1Code) ? h1Code : h1Name;
                if (string.IsNullOrEmpty(h1Key))
                {
                    h1Key = "__UNASSIGNED__";
                    h1Name = "Unassigned Sales";
                }

                string empName = ((string?)r.EmpName  ?? "").Trim();
                if (string.IsNullOrEmpty(empName)) continue;

                string h2Name  = ((string?)r.H2Name   ?? "").Trim();
                string h3Name  = ((string?)r.H3Name   ?? "").Trim();
                string h4Name  = ((string?)r.H4Name   ?? "").Trim();
                string h2Code  = ((string?)r.H2Code   ?? "").Trim();
                string h3Code  = ((string?)r.H3Code   ?? "").Trim();
                string h4Code  = ((string?)r.H4Code   ?? "").Trim();
                string grpName = ((string?)r.GroupName ?? "").Trim();
                string empCode = ((string?)r.EmpCode  ?? "").Trim();
                string desig   = ((string?)r.Designation ?? "").Trim();
                string state   = ((string?)r.State    ?? "").Trim();
                string empKey = !string.IsNullOrEmpty(empCode)
                    ? $"C:{empCode}"
                    : $"N:{empName}";
                if (!seenEmployees.Add(empKey)) continue;

                string sikhNonSikh = ((string?)r.SikhNonSikh ?? "").Trim();
                int employeeId = (int?)r.EmployeeId ?? 0;
                DateTime? dateOfJoining = TryGetDate(r.DateOfJoining);
                string gender = ((string?)r.Gender ?? "").Trim();
                string qualification = ((string?)r.Qualification ?? "").Trim();
                string area = ((string?)r.Area ?? "").Trim();

                if (h1Key == "__UNASSIGNED__")
                {
                    if (string.IsNullOrEmpty(h2Name)) h2Name = "Unassigned";
                    if (string.IsNullOrEmpty(h3Name)) h3Name = "Unassigned";
                    if (string.IsNullOrEmpty(h4Name)) h4Name = string.IsNullOrEmpty(desig) ? "Unassigned" : desig;
                }

                if (!h1Map.TryGetValue(h1Key, out var h1Node))
                    h1Map[h1Key] = h1Node = new SalesH1Node(h1Name);

                if (string.IsNullOrEmpty(h2Code) && string.IsNullOrEmpty(h2Name))
                {
                    string directH3Key = !string.IsNullOrEmpty(h3Name) ? h3Name : (string.IsNullOrEmpty(h3Code) ? "_" : h3Code);
                    if (!h1Node.DirectH3Map.TryGetValue(directH3Key, out var directH3Node))
                        h1Node.DirectH3Map[directH3Key] = directH3Node = new SalesH3Node(h3Code, h3Name);

                    AddSalesLeaf(directH3Node, h4Code, h4Name, grpName, employeeId, empCode, empName, desig, state, sikhNonSikh, dateOfJoining, gender, qualification, area);
                    continue;
                }

                string h2Key = !string.IsNullOrEmpty(h2Code) ? h2Code : (string.IsNullOrEmpty(h2Name) ? "_" : h2Name);
                if (!h1Node.H2Map.TryGetValue(h2Key, out var h2Node))
                    h1Node.H2Map[h2Key] = h2Node = new SalesH2Node(h2Code, string.IsNullOrEmpty(h2Name) ? "—" : h2Name);

                string h3Key = !string.IsNullOrEmpty(h3Name) ? h3Name : (string.IsNullOrEmpty(h3Code) ? "_" : h3Code);
                if (!h2Node.H3Map.TryGetValue(h3Key, out var h3Node))
                    h2Node.H3Map[h3Key] = h3Node = new SalesH3Node(h3Code, string.IsNullOrEmpty(h3Name) ? "—" : h3Name);

                AddSalesLeaf(h3Node, h4Code, h4Name, grpName, employeeId, empCode, empName, desig, state, sikhNonSikh, dateOfJoining, gender, qualification, area);
            }

            return h1Map.Select(kvp => new
            {
                h1Code = kvp.Key,
                h1Name = kvp.Value.Name,
                directH3List = kvp.Value.DirectH3Map.Values.Select(ToSalesH3Dto).ToList(),
                h2List = kvp.Value.H2Map.Values.Select(h2 => new
                {
                    h2Code = h2.Code,
                    h2Name = h2.Name,
                    h3List = h2.H3Map.Values.Select(ToSalesH3Dto).ToList()
                }).ToList()
            }).ToList();
        }

        // ── private helper types for GetSalesHierarchyForTreeAsync ──────────────
        private static void AddSalesLeaf(
            SalesH3Node h3Node,
            string h4Code,
            string h4Name,
            string groupName,
            int employeeId,
            string empCode,
            string empName,
            string designation,
            string state,
            string sikhNonSikh,
            DateTime? dateOfJoining,
            string gender,
            string qualification,
            string area)
        {
            string h4Key = !string.IsNullOrEmpty(h4Code) ? h4Code : (string.IsNullOrEmpty(h4Name) ? "_" : h4Name);
            if (!h3Node.H4Map.TryGetValue(h4Key, out var h4Node))
                h3Node.H4Map[h4Key] = h4Node = new SalesH4Node(h4Code, string.IsNullOrEmpty(h4Name) ? "—" : h4Name);

            string groupKey = string.IsNullOrEmpty(groupName) ? "_" : groupName;
            if (!h4Node.GroupMap.TryGetValue(groupKey, out var groupNode))
                h4Node.GroupMap[groupKey] = groupNode = new SalesGroupNode(string.IsNullOrEmpty(groupName) ? "—" : groupName);

            groupNode.Employees.Add(new SalesEmpLeaf(employeeId, empCode, empName, designation, state, sikhNonSikh, dateOfJoining, gender, qualification, area));
        }

        private static object ToSalesH3Dto(SalesH3Node h3) => new
        {
            h3Code = h3.Code,
            h3Name = h3.Name,
            h4List = h3.H4Map.Values.Select(h4 => new
            {
                h4Code = h4.Code,
                h4Name = h4.Name,
                groups = h4.GroupMap.Values.Select(g => new
                {
                    groupName = g.Name,
                    employees = g.Employees.Select(e => new
                    {
                        employeeId = e.EmployeeId,
                        empCode = e.EmpCode,
                        empName = e.EmpName,
                        designation = e.Desig,
                        state = e.State,
                        sikhNonSikh = e.SikhNonSikh,
                        dateOfJoining = e.DateOfJoining,
                        gender = e.Gender,
                        qualification = e.Qualification,
                        area = e.Area
                    }).ToList()
                }).ToList()
            }).ToList()
        };

        private sealed class SalesH1Node    { public string Name; public Dictionary<string,SalesH2Node> H2Map = new(StringComparer.OrdinalIgnoreCase); public Dictionary<string,SalesH3Node> DirectH3Map = new(StringComparer.OrdinalIgnoreCase); public SalesH1Node(string n){Name=n;} }
        private sealed class SalesH2Node    { public string Code; public string Name; public Dictionary<string,SalesH3Node>    H3Map    = new(StringComparer.OrdinalIgnoreCase); public SalesH2Node(string c,string n){Code=c;Name=n;} }
        private sealed class SalesH3Node    { public string Code; public string Name; public Dictionary<string,SalesH4Node>    H4Map    = new(StringComparer.OrdinalIgnoreCase); public SalesH3Node(string c,string n){Code=c;Name=n;} }
        private sealed class SalesH4Node    { public string Code; public string Name; public Dictionary<string,SalesGroupNode> GroupMap = new(StringComparer.OrdinalIgnoreCase); public SalesH4Node(string c,string n){Code=c;Name=n;} }
        private sealed class SalesGroupNode { public string Name; public List<SalesEmpLeaf> Employees = new(); public SalesGroupNode(string n){Name=n;} }
        private sealed class SalesEmpLeaf
        {
            public int EmployeeId;
            public string EmpCode, EmpName, Desig, State, SikhNonSikh, Gender, Qualification, Area;
            public DateTime? DateOfJoining;
            public SalesEmpLeaf(int id, string c, string n, string d, string s, string sns, DateTime? doj, string gender, string qualification, string area)
            {
                EmployeeId = id; EmpCode = c; EmpName = n; Desig = d; State = s; SikhNonSikh = sns;
                DateOfJoining = doj; Gender = gender; Qualification = qualification; Area = area;
            }
        }

        public async Task<List<EmployeeDropdownDto>> GetSalesEmployeeListAsync(int companyId)
        {
            const string sql = @"
                SELECT
                    sh.SalesHierarchyId AS EmployeeId,
                    sh.PersonCode AS EmployeeCode,
                    sh.PersonName AS EmployeeName
                FROM [Hie].[SalesHierarchy] sh
                WHERE sh.CompanyId = @CompanyId
                  AND sh.IsActive = 1
                  AND NULLIF(LTRIM(RTRIM(sh.PersonCode)), '') IS NOT NULL
                  AND (
                        EXISTS (
                            SELECT 1
                            FROM [Hie].[SalesEmployeeHierarchyMap] map
                            WHERE map.CompanyId = sh.CompanyId
                              AND map.EmpPersonCode = sh.PersonCode
                              AND map.IsActive = 1
                        )
                        OR NOT EXISTS (
                            SELECT 1
                            FROM [Hie].[SalesEmployeeHierarchyMap] map
                            WHERE map.CompanyId = sh.CompanyId
                              AND map.IsActive = 1
                              AND (
                                  map.H1PersonCode = sh.PersonCode
                                  OR map.H2PersonCode = sh.PersonCode
                                  OR map.H3PersonCode = sh.PersonCode
                                  OR map.H4PersonCode = sh.PersonCode
                              )
                        )
                  )
                ORDER BY sh.PersonName";
            using var conn = new SqlConnection(_connectionString);
            var result = await conn.QueryAsync<EmployeeDropdownDto>(sql, new { CompanyId = companyId });
            return result.ToList();
        }

        public async Task<HierarchyApiResponse<bool>> CreateSalesEmployeeAsync(
            CreateSalesEmployeeRequest request, int createdBy, int companyId)
        {
            request.EmpCode = NormalizeJwplCode(request.EmpCode) ?? "";
            request.EmpName = ToTitleCaseName(request.EmpName) ?? "";
            request.Mobile = NormalizeSalesText(request.Mobile, false);
            request.Email = NormalizeSalesText(request.Email, false);
            request.State = NormalizeSalesText(request.State, true);
            request.GroupName = NormalizeSalesText(request.GroupName, true);
            request.Designation = NormalizeSalesText(request.Designation, true);
            request.Qualification = NormalizeSalesText(request.Qualification, false);
            request.Gender = NormalizeSalesText(request.Gender, false);
            request.SikhNonSikh = NormalizeSikhNonSikhCode(request.SikhNonSikh);
            request.Area = NormalizeSalesText(request.Area, false);
            request.H1Code = NormalizeJwplCode(request.H1Code);
            request.H1Name = ToTitleCaseName(request.H1Name);
            request.H2Code = NormalizeJwplCode(request.H2Code);
            request.H2Name = ToTitleCaseName(request.H2Name);
            request.H3Code = NormalizeJwplCode(request.H3Code);
            request.H3Name = ToTitleCaseName(request.H3Name);
            request.H4Code = NormalizeJwplCode(request.H4Code);
            request.H4Name = ToTitleCaseName(request.H4Name);

            if (string.IsNullOrWhiteSpace(request.EmpCode))
                return HierarchyApiResponse<bool>.ErrorResponse("Employee code is required.");
            if (string.IsNullOrWhiteSpace(request.EmpName))
                return HierarchyApiResponse<bool>.ErrorResponse("Employee name is required.");

            using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();
            using var tx = await conn.BeginTransactionAsync();

            try
            {
                var id = await conn.ExecuteScalarAsync<int?>(@"
                    SELECT SalesHierarchyId
                    FROM [Hie].[SalesHierarchy]
                    WHERE CompanyId = @CompanyId
                      AND PersonCode = @Code",
                    new { CompanyId = companyId, Code = request.EmpCode.Trim() }, tx);

                int salesHierarchyId;
                if (id.HasValue)
                {
                    await conn.ExecuteAsync(@"
                        UPDATE [Hie].[SalesHierarchy]
                        SET PersonName = @Name,
                            Mobile = @Mobile,
                            Email = @Email,
                            DateOfJoining = @DateOfJoining,
                            Qualification = @Qualification,
                            Gender = @Gender,
                            SikhNonSikh = @SikhNonSikh,
                            IsActive = @IsActive,
                            ModifiedBy = @UserId,
                            ModifiedOn = SYSDATETIME()
                        WHERE SalesHierarchyId = @Id",
                        new
                        {
                            Id = id.Value,
                            Name = request.EmpName.Trim(),
                            request.Mobile,
                            request.Email,
                            request.DateOfJoining,
                            request.Qualification,
                            request.Gender,
                            request.SikhNonSikh,
                            request.IsActive,
                            UserId = createdBy
                        }, tx);
                    salesHierarchyId = id.Value;
                }
                else
                {
                    salesHierarchyId = await conn.ExecuteScalarAsync<int>(@"
                        INSERT INTO [Hie].[SalesHierarchy]
                            (CompanyId, PersonCode, PersonName, Mobile, Email, DateOfJoining,
                             Qualification, Gender, SikhNonSikh,
                             IsActive, CreatedBy, CreatedOn)
                        VALUES
                            (@CompanyId, @Code, @Name, @Mobile, @Email, @DateOfJoining,
                             @Qualification, @Gender, @SikhNonSikh,
                             @IsActive, @UserId, SYSDATETIME());
                        SELECT CAST(SCOPE_IDENTITY() AS INT);",
                        new
                        {
                            CompanyId = companyId,
                            Code = request.EmpCode.Trim(),
                            Name = request.EmpName.Trim(),
                            request.Mobile,
                            request.Email,
                            request.DateOfJoining,
                            request.Qualification,
                            request.Gender,
                            request.SikhNonSikh,
                            request.IsActive,
                            UserId = createdBy
                        }, tx);
                }

                var departmentId = await ResolveDepartmentIdAsync(conn, tx);
                var stateId = await ResolveSalesStateIdAsync(conn, tx, request.State);
                var groupId = await ResolveSalesGroupIdAsync(conn, tx, request.GroupName);
                var designationId = await ResolveSalesDesignationIdAsync(conn, tx, request.Designation);

                var profileId = await conn.ExecuteScalarAsync<int?>(@"
                    SELECT TOP 1 SalesPersonProfileId
                    FROM [Hie].[SalesPersonProfile]
                    WHERE CompanyId = @CompanyId
                      AND SalesHierarchyId = @SalesHierarchyId
                      AND IsActive = 1
                    ORDER BY SalesPersonProfileId DESC",
                    new { CompanyId = companyId, SalesHierarchyId = salesHierarchyId }, tx);

                if (profileId.HasValue)
                {
                    await conn.ExecuteAsync(@"
                        UPDATE [Hie].[SalesPersonProfile]
                        SET DepartmentId = COALESCE(@DepartmentId, DepartmentId),
                            StateId = COALESCE(@StateId, StateId),
                            GroupId = COALESCE(@GroupId, GroupId),
                            DesignationId = COALESCE(@DesignationId, DesignationId),
                            Area = COALESCE(NULLIF(@Area, N''), Area),
                            ModifiedBy = @UserId,
                            ModifiedOn = SYSDATETIME()
                        WHERE SalesPersonProfileId = @ProfileId",
                        new
                        {
                            ProfileId = profileId.Value,
                            DepartmentId = departmentId,
                            StateId = stateId,
                            GroupId = groupId,
                            DesignationId = designationId,
                            request.Area,
                            UserId = createdBy
                        }, tx);
                }
                else if (departmentId.HasValue || stateId.HasValue || groupId.HasValue || designationId.HasValue || !string.IsNullOrWhiteSpace(request.Area))
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO [Hie].[SalesPersonProfile]
                            (CompanyId, SalesHierarchyId, DepartmentId, StateId, GroupId, DesignationId,
                             Area, IsActive, CreatedBy, CreatedOn)
                        VALUES
                            (@CompanyId, @SalesHierarchyId, @DepartmentId, @StateId, @GroupId, @DesignationId,
                             @Area, 1, @UserId, SYSDATETIME())",
                        new
                        {
                            CompanyId = companyId,
                            SalesHierarchyId = salesHierarchyId,
                            DepartmentId = departmentId,
                            StateId = stateId,
                            GroupId = groupId,
                            DesignationId = designationId,
                            request.Area,
                            UserId = createdBy
                        }, tx);
                }

                var hasHierarchySelection =
                    !string.IsNullOrWhiteSpace(request.H1Code) ||
                    !string.IsNullOrWhiteSpace(request.H2Code) ||
                    !string.IsNullOrWhiteSpace(request.H3Code) ||
                    !string.IsNullOrWhiteSpace(request.H4Code);

                var mapExists = await conn.ExecuteScalarAsync<int>(@"
                    SELECT COUNT(1)
                    FROM [Hie].[SalesEmployeeHierarchyMap]
                    WHERE CompanyId = @CompanyId
                      AND EmpPersonCode = @Code",
                    new { CompanyId = companyId, Code = request.EmpCode.Trim() }, tx);

                if (mapExists > 0)
                {
                    await conn.ExecuteAsync(@"
                        UPDATE [Hie].[SalesEmployeeHierarchyMap]
                        SET H1PersonCode = CASE WHEN @HasHierarchySelection = 1 THEN @H1Code ELSE H1PersonCode END,
                            H2PersonCode = CASE WHEN @HasHierarchySelection = 1 THEN @H2Code ELSE H2PersonCode END,
                            H3PersonCode = CASE WHEN @HasHierarchySelection = 1 THEN @H3Code ELSE H3PersonCode END,
                            H4PersonCode = CASE WHEN @HasHierarchySelection = 1 THEN @H4Code ELSE H4PersonCode END,
                            DepartmentId = COALESCE(@DepartmentId, DepartmentId),
                            StateId = COALESCE(@StateId, StateId),
                            Area = COALESCE(NULLIF(@Area, N''), Area),
                            GroupId = COALESCE(@GroupId, GroupId),
                            DesignationId = COALESCE(@DesignationId, DesignationId),
                            IsActive = 1,
                            ModifiedBy = @UserId,
                            ModifiedOn = SYSDATETIME()
                        WHERE CompanyId = @CompanyId
                          AND EmpPersonCode = @Code",
                        new
                        {
                            CompanyId = companyId,
                            Code = request.EmpCode.Trim(),
                            request.H1Code,
                            request.H2Code,
                            request.H3Code,
                            request.H4Code,
                            HasHierarchySelection = hasHierarchySelection ? 1 : 0,
                            DepartmentId = departmentId,
                            StateId = stateId,
                            request.Area,
                            GroupId = groupId,
                            DesignationId = designationId,
                            UserId = createdBy
                        }, tx);
                }
                else
                {
                    await conn.ExecuteAsync(@"
                        INSERT INTO [Hie].[SalesEmployeeHierarchyMap]
                            (CompanyId, EmpPersonCode, H1PersonCode, H2PersonCode, H3PersonCode, H4PersonCode,
                             DepartmentId, StateId, Area, GroupId, DesignationId, IsActive, CreatedBy, CreatedOn)
                        VALUES
                            (@CompanyId, @Code, @H1Code, @H2Code, @H3Code, @H4Code,
                             @DepartmentId, @StateId, @Area, @GroupId, @DesignationId, 1, @UserId, SYSDATETIME())",
                        new
                        {
                            CompanyId = companyId,
                            Code = request.EmpCode.Trim(),
                            request.H1Code,
                            request.H2Code,
                            request.H3Code,
                            request.H4Code,
                            DepartmentId = departmentId,
                            StateId = stateId,
                            request.Area,
                            GroupId = groupId,
                            DesignationId = designationId,
                            UserId = createdBy
                        }, tx);
                }

                var importResult = new SalesImportResult();
                var h1Id = await UpsertSalesHierarchyPersonAsync(conn, tx, companyId, request.H1Code, request.H1Name, createdBy, importResult);
                var h1NodeId = h1Id.HasValue ? await UpsertSalesHierarchyNodeAsync(conn, tx, companyId, h1Id.Value, "H1", null, createdBy) : (int?)null;
                var h2Id = await UpsertSalesHierarchyPersonAsync(conn, tx, companyId, request.H2Code, request.H2Name, createdBy, importResult);
                var h2NodeId = h2Id.HasValue ? await UpsertSalesHierarchyNodeAsync(conn, tx, companyId, h2Id.Value, "H2", h1NodeId, createdBy) : (int?)null;
                var h3Id = await UpsertSalesHierarchyPersonAsync(conn, tx, companyId, request.H3Code, request.H3Name, createdBy, importResult);
                var h3NodeId = h3Id.HasValue ? await UpsertSalesHierarchyNodeAsync(conn, tx, companyId, h3Id.Value, "H3", h2NodeId ?? h1NodeId, createdBy) : (int?)null;
                var h4Id = await UpsertSalesHierarchyPersonAsync(conn, tx, companyId, request.H4Code, request.H4Name, createdBy, importResult);
                var h4NodeId = h4Id.HasValue ? await UpsertSalesHierarchyNodeAsync(conn, tx, companyId, h4Id.Value, "H4", h3NodeId ?? h2NodeId ?? h1NodeId, createdBy) : (int?)null;
                await UpsertSalesHierarchyNodeAsync(conn, tx, companyId, salesHierarchyId, "EMP", h4NodeId ?? h3NodeId ?? h2NodeId ?? h1NodeId, createdBy);

                await tx.CommitAsync();
                var message = mapExists > 0
                    ? "Sales employee already exists; details activated/updated."
                    : hasHierarchySelection ? "Sales employee created successfully." : "Sales employee created as unassigned.";
                return HierarchyApiResponse<bool>.SuccessResponse(true, message);
            }
            catch
            {
                await tx.RollbackAsync();
                throw;
            }
        }

        #endregion
    }
}
