using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services;

namespace JSAPNEW.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HierarchyController : ControllerBase
    {
        private readonly IHierarchyService _hierarchyService;
        private readonly IConfiguration _configuration;

        public HierarchyController(IHierarchyService hierarchyService, IConfiguration configuration)
        {
            _hierarchyService = hierarchyService;
            _configuration = configuration;
        }

        #region Helper Methods

        private int? GetUserId() => HttpContext.Session.GetInt32("userId");

        private bool IsUserLoggedIn()
        {
            var userId = GetUserId();
            return userId.HasValue && userId > 0;
        }

        private async Task<(string EmpId, string RoleName, bool IsAdmin)> GetUserInfoFromDatabaseAsync()
        {
            var userId = GetUserId();
            if (!userId.HasValue || userId <= 0)
                return ("", "", false);

            string empId = "", roleName = "";
            bool isAdmin = false;

            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT
                        u.empId,
                        CASE
                            WHEN EXISTS (
                                SELECT 1 FROM jsUserRole ur2
                                JOIN jsRole r2 ON ur2.roleId = r2.roleId
                                WHERE ur2.userId = u.userId
                                AND r2.roleName IN ('Admin', 'Super User')
                            ) THEN 'Super User'
                            ELSE ISNULL((
                                SELECT TOP 1 r.roleName
                                FROM jsUserRole ur
                                JOIN jsRole r ON ur.roleId = r.roleId
                                WHERE ur.userId = u.userId
                            ), 'User')
                        END AS roleName
                    FROM jsUser u
                    WHERE u.userId = @UserId";

                using var command = new SqlCommand(query, connection);
                command.Parameters.AddWithValue("@UserId", userId.Value);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    empId = reader["empId"]?.ToString() ?? "";
                    roleName = reader["roleName"]?.ToString() ?? "";
                    isAdmin = roleName == "Admin" || roleName == "Super User";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user info: {ex.Message}");
            }

            return (empId, roleName, isAdmin);
        }

        private async Task<EmployeeDto?> GetCurrentHierarchyEmployeeAsync()
        {
            var userInfo = await GetUserInfoFromDatabaseAsync();
            if (string.IsNullOrWhiteSpace(userInfo.EmpId)) return null;
            try
            {
                var cs = _configuration.GetConnectionString("DefaultConnection");
                using var conn = new SqlConnection(cs);
                return await conn.QueryFirstOrDefaultAsync<EmployeeDto>(@"
SELECT TOP (1)
    EmployeeId,
    EmployeeCode,
    EmployeeName,
    Designation,
    RoleTypeId,
    PrimaryDepartmentId,
    DateOfJoining,
    IsActive,
    CAST(NULL AS NVARCHAR(50)) AS Salary
FROM [Hie].[Employees]
WHERE EmployeeCode = @EmployeeCode",
                    new { EmployeeCode = userInfo.EmpId });
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Resolves the user's Hierarchy role by calling jsGetUserEffectivePermissions
        /// across all companies the user belongs to (reads companyList from session).
        /// Returns "Admin" | "HOD" | "SubHOD" | "None".
        /// Also returns any error message for diagnostics.
        /// </summary>
        private async Task<(string Role, string Error)> GetHierarchyRoleInternalAsync(int userId)
        {
            try
            {
                // Build list of companyIds to try: selected first, then all others from session
                var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId") ?? 1;
                var companyIds = new List<int> { selectedCompanyId };

                var companyListJson = HttpContext.Session.GetString("companyList");
                if (!string.IsNullOrEmpty(companyListJson))
                {
                    try
                    {
                        var companies = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(companyListJson);
                        if (companies != null)
                        {
                            foreach (var c in companies)
                            {
                                if (c.TryGetProperty("id", out var idProp) && idProp.TryGetInt32(out int cid) && !companyIds.Contains(cid))
                                    companyIds.Add(cid);
                            }
                        }
                    }
                    catch { /* ignore parse errors */ }
                }

                var cs = _configuration.GetConnectionString("DefaultConnection");
                string best = "None";
                int bestRank = 99;

                foreach (var companyId in companyIds)
                {
                    using var conn = new SqlConnection(cs);
                    using var cmd = new SqlCommand("jsGetUserEffectivePermissions", conn)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@companyId", companyId);

                    await conn.OpenAsync();
                    using var reader = await cmd.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        var modName = reader["moduleName"]?.ToString() ?? "";
                        var permType = reader["permissionType"]?.ToString() ?? "";

                        if (!modName.Equals("Employee Hierarchy", StringComparison.OrdinalIgnoreCase)) continue;

                        var (role, rank) = permType switch
                        {
                            "Hierarchy_Admin" => ("Admin", 1),
                            "Hierarchy_Master" => ("Admin", 1),
                            "Hierarchy_HOD" => ("HOD", 2),
                            "Hierarchy_SubHOD" => ("SubHOD", 3),
                            _ => ("None", 99)
                        };

                        if (rank < bestRank) { best = role; bestRank = rank; }
                    }

                    if (best != "None") break; // found a hierarchy permission — no need to try other companies
                }

                return (best, "");
            }
            catch (Exception ex)
            {
                return ("None", ex.Message);
            }
        }

        private async Task<string> GetHierarchyRoleAsync(int userId)
            => (await GetHierarchyRoleInternalAsync(userId)).Role;

        private async Task<bool> HasHierarchyMasterPermissionAsync(int userId)
        {
            try
            {
                var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId") ?? 1;
                var companyIds = new List<int> { selectedCompanyId };

                var companyListJson = HttpContext.Session.GetString("companyList");
                if (!string.IsNullOrEmpty(companyListJson))
                {
                    try
                    {
                        var companies = System.Text.Json.JsonSerializer.Deserialize<List<System.Text.Json.JsonElement>>(companyListJson);
                        if (companies != null)
                        {
                            foreach (var c in companies)
                            {
                                if (c.TryGetProperty("id", out var idProp) && idProp.TryGetInt32(out int cid) && !companyIds.Contains(cid))
                                    companyIds.Add(cid);
                            }
                        }
                    }
                    catch { /* ignore parse errors */ }
                }

                var cs = _configuration.GetConnectionString("DefaultConnection");
                foreach (var companyId in companyIds)
                {
                    using var conn = new SqlConnection(cs);
                    using var cmd = new SqlCommand("jsGetUserEffectivePermissions", conn)
                    {
                        CommandType = System.Data.CommandType.StoredProcedure
                    };
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@companyId", companyId);

                    await conn.OpenAsync();
                    using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var modName = reader["moduleName"]?.ToString() ?? "";
                        var permType = reader["permissionType"]?.ToString() ?? "";

                        if (!modName.Equals("Employee Hierarchy", StringComparison.OrdinalIgnoreCase))
                            continue;

                        if (permType.Equals("Hierarchy_Master", StringComparison.OrdinalIgnoreCase)
                            || permType.Equals("Hierarchy_Admin", StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }
            catch { }

            // Fallback: allow system admins (Admin / Super User role) even if
            // no explicit Hierarchy permission is found in the SP.
            var userInfo = await GetUserInfoFromDatabaseAsync();
            return userInfo.IsAdmin;
        }

        #endregion

        #region Hierarchy Tree

        /// <summary>GET /api/Hierarchy/GetHierarchyTree</summary>
        [HttpGet("GetHierarchyTree")]
        public async Task<IActionResult> GetHierarchyTree()
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                var userId = GetUserId()!.Value;
                var userInfo = await GetUserInfoFromDatabaseAsync();
                var (hierRole, _) = await GetHierarchyRoleInternalAsync(userId);

                // Explicit hierarchy permission takes priority over generic admin role.
                bool isHierarchyAdmin = hierRole == "Admin"
                    || (hierRole == "None" && userInfo.IsAdmin);

                if (string.IsNullOrEmpty(userInfo.EmpId) && !isHierarchyAdmin)
                    return Ok(new { Success = false, Message = "Employee code not found for user" });

                // Always fetch the full tree — the SP's @IsAdmin filter is unreliable.
                // We apply role-based filtering here in C# after loading.
                var fullTree = await _hierarchyService.GetHierarchyTreeAsync(userInfo.EmpId, true);

                List<HODTreeNodeDto> result;

                if (isHierarchyAdmin)
                {
                    // Hierarchy_Admin → full tree
                    result = fullTree;
                }
                else if (hierRole == "HOD")
                {
                    // Hierarchy_HOD → show only this user's HOD node + everything below it
                    result = fullTree
                        .Where(h => string.Equals(h.EmployeeCode, userInfo.EmpId, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else if (hierRole == "SubHOD")
                {
                    // Hierarchy_SubHOD → show only the HOD node containing this SubHOD,
                    // with SubHODs and Departments filtered to only their own section.
                    var myHod = fullTree.FirstOrDefault(h =>
                        h.SubHODs != null &&
                        h.SubHODs.Any(s => string.Equals(s.EmployeeCode, userInfo.EmpId, StringComparison.OrdinalIgnoreCase)));

                    if (myHod != null)
                    {
                        // Find the exact SubHOD rows that belong to this user
                        var mySubHodRows = myHod.SubHODs
                            .Where(s => string.Equals(s.EmployeeCode, userInfo.EmpId, StringComparison.OrdinalIgnoreCase))
                            .ToList();

                        // Collect dept+subdept keys that this SubHOD appears in
                        var myDeptSubDeptKeys = mySubHodRows
                            .Select(s => $"{s.DepartmentId}|{s.SubDepartmentId}")
                            .ToHashSet(StringComparer.OrdinalIgnoreCase);

                        // Keep only this SubHOD's rows + phantom rows in their same dept/subdept
                        myHod.SubHODs = myHod.SubHODs
                            .Where(s =>
                                string.Equals(s.EmployeeCode, userInfo.EmpId, StringComparison.OrdinalIgnoreCase) ||
                                (s.EmployeeId == 0 && myDeptSubDeptKeys.Contains($"{s.DepartmentId}|{s.SubDepartmentId}")))
                            .ToList();

                        // Filter Departments to only the ones the SubHOD belongs to
                        var myDeptIds = mySubHodRows.Select(s => s.DepartmentId).ToHashSet();
                        myHod.Departments = myHod.Departments
                            .Where(d => myDeptIds.Contains(d.DepartmentId))
                            .ToList();

                        result = new List<HODTreeNodeDto> { myHod };
                    }
                    else
                    {
                        result = new List<HODTreeNodeDto>();
                    }
                }
                else
                {
                    result = new List<HODTreeNodeDto>();
                }

                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/Hierarchy/DebugMyRole
        /// Diagnostic — returns userId, companyId, hierarchy role detected, and any error.
        /// Use this to verify the permission lookup is working correctly.
        /// </summary>
        [HttpGet("DebugMyRole")]
        public async Task<IActionResult> DebugMyRole()
        {
            if (!IsUserLoggedIn())
                return Ok(new { Success = false, Message = "Not logged in" });

            var userId = GetUserId()!.Value;
            var userInfo = await GetUserInfoFromDatabaseAsync();
            var selectedCo = HttpContext.Session.GetInt32("selectedCompanyId") ?? -1;
            var companyList = HttpContext.Session.GetString("companyList") ?? "(not in session)";
            var (role, err) = await GetHierarchyRoleInternalAsync(userId);
            var isHierarchyAdmin = role == "Admin" || (role == "None" && userInfo.IsAdmin);

            return Ok(new
            {
                UserId = userId,
                EmpId = userInfo.EmpId,
                GenericRole = userInfo.RoleName,
                SelectedCompanyId = selectedCo,
                CompanyList = companyList,
                HierarchyRole = role,
                IsHierarchyAdmin = isHierarchyAdmin,
                SpError = err
            });
        }

        #endregion

        #region Employee Endpoints

        /// <summary>GET /api/Hierarchy/SearchEmployees</summary>
        [HttpGet("SearchEmployees")]
        public async Task<IActionResult> SearchEmployees(
            [FromQuery] string searchTerm = null,
            [FromQuery] int? roleTypeId = null,
            [FromQuery] int? departmentId = null,
            [FromQuery] bool? isActive = true,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                var userId = GetUserId()!.Value;
                var userInfo = await GetUserInfoFromDatabaseAsync();
                var canManageHierarchy = await HasHierarchyMasterPermissionAsync(userId);

                var request = new EmployeeSearchRequest
                {
                    SearchTerm = searchTerm,
                    RoleTypeId = roleTypeId,
                    DepartmentId = departmentId,
                    IsActive = isActive,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };

                var employees = await _hierarchyService.GetEmployeesAsync(request, userInfo.EmpId, canManageHierarchy || userInfo.IsAdmin);
                return Ok(new { Success = true, Data = employees });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>GET /api/Hierarchy/GetEmployee/{id}</summary>
        [HttpGet("GetEmployee/{id}")]
        public async Task<IActionResult> GetEmployee(int id)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                var userInfo = await GetUserInfoFromDatabaseAsync();
                var employee = await _hierarchyService.GetEmployeeByIdAsync(id, userInfo.EmpId, userInfo.IsAdmin);

                if (employee == null || employee.EmployeeId == 0)
                    return Ok(new { Success = false, Message = "Employee not found or access denied" });

                return Ok(new { Success = true, Data = employee });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>GET /api/Hierarchy/GetCurrentEmployee</summary>
        [HttpGet("GetCurrentEmployee")]
        public async Task<IActionResult> GetCurrentEmployee()
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                var userInfo = await GetUserInfoFromDatabaseAsync();

                if (string.IsNullOrEmpty(userInfo.EmpId))
                    return Ok(new { Success = false, Message = "Employee code not found for user" });

                var employee = await _hierarchyService.GetEmployeeByCodeAsync(userInfo.EmpId);

                if (employee == null)
                    return Ok(new { Success = false, Message = "Employee not found in hierarchy" });

                return Ok(new { Success = true, Data = employee });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>POST /api/Hierarchy/CreateEmployee (Admin only)</summary>
        [HttpPost("CreateEmployee")]
        public async Task<IActionResult> CreateEmployee([FromBody] EmployeeRequest request)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                var userInfo = await GetUserInfoFromDatabaseAsync();
                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value))
                    return Ok(new { Success = false, Message = "Access denied. Admin permission required." });

                request.CreatedBy ??= GetUserId();
                var result = await _hierarchyService.CreateEmployeeAsync(request);
                return Ok(new { Success = result.Success, Message = result.Message, Data = result.Data });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>PUT /api/Hierarchy/UpdateEmployee (Admin only)</summary>
        [HttpPut("UpdateEmployee")]
        public async Task<IActionResult> UpdateEmployee([FromBody] EmployeeRequest request)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                var userInfo = await GetUserInfoFromDatabaseAsync();
                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value))
                    return Ok(new { Success = false, Message = "Access denied. Admin permission required." });

                var result = await _hierarchyService.UpdateEmployeeAsync(request);
                return Ok(new { Success = result.Success, Message = result.Message, Data = result.Data });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>DELETE /api/Hierarchy/DeactivateEmployee/{id} (Admin only)</summary>
        [HttpDelete("DeactivateEmployee/{id}")]
        public async Task<IActionResult> DeactivateEmployee(int id)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                var userInfo = await GetUserInfoFromDatabaseAsync();
                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value))
                    return Ok(new { Success = false, Message = "Access denied. Admin permission required." });

                var result = await _hierarchyService.DeactivateEmployeeAsync(id);
                return Ok(new { Success = result.Success, Message = result.Message });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>GET /api/Hierarchy/GetAvailableManagers/{employeeId}</summary>
        [HttpGet("GetAvailableManagers/{employeeId}")]
        public async Task<IActionResult> GetAvailableManagers(int employeeId)
        {
            try
            {
                var managers = await _hierarchyService.GetAvailableManagersAsync(employeeId);
                return Ok(new { Success = true, Data = managers });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        #endregion

        #region Relationship Endpoints

        /// <summary>GET /api/Hierarchy/GetEmployeeReportingTo/{employeeId}</summary>
        [HttpGet("GetEmployeeReportingTo/{employeeId}")]
        public async Task<IActionResult> GetEmployeeReportingTo(int employeeId, [FromQuery] bool includeInactive = false)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                var userInfo = await GetUserInfoFromDatabaseAsync();
                var reportingTo = await _hierarchyService.GetEmployeeReportingToAsync(employeeId, includeInactive);
                return Ok(new { Success = true, Data = reportingTo });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>GET /api/Hierarchy/GetDirectReports/{managerId}</summary>
        [HttpGet("GetDirectReports/{managerId}")]
        public async Task<IActionResult> GetDirectReports(int managerId, [FromQuery] bool includeInactive = false)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                var userInfo = await GetUserInfoFromDatabaseAsync();
                var directReports = await _hierarchyService.GetEmployeeDirectReportsAsync(managerId, includeInactive);
                return Ok(new { Success = true, Data = directReports });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>GET /api/Hierarchy/GetRelationship/{id}</summary>
        [HttpGet("GetRelationship/{id}")]
        public async Task<IActionResult> GetRelationship(int id)
        {
            try
            {
                var relationship = await _hierarchyService.GetRelationshipByIdAsync(id);
                if (relationship == null)
                    return Ok(new { Success = false, Message = "Relationship not found" });

                return Ok(new { Success = true, Data = relationship });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>POST /api/Hierarchy/AddRelationship (Admin only)</summary>
        [HttpPost("AddRelationship")]
        public async Task<IActionResult> AddRelationship([FromBody] AddReportingRelationshipRequest request)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value))
                    return Ok(new { Success = false, Message = "Access denied. Admin permission required." });

                request.CreatedBy ??= (GetUserId() ?? 0).ToString();
                var result = await _hierarchyService.AddReportingRelationshipAsync(request);
                return Ok(new { Success = result.Success, Message = result.Message, RelationshipId = result.RelationshipId });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>PUT /api/Hierarchy/UpdateRelationship (Admin only)</summary>
        [HttpPut("UpdateRelationship")]
        public async Task<IActionResult> UpdateRelationship([FromBody] UpdateReportingRelationshipRequest request)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value))
                    return Ok(new { Success = false, Message = "Access denied. Admin permission required." });

                var result = await _hierarchyService.UpdateReportingRelationshipAsync(request);
                return Ok(new { Success = result.Success, Message = result.Message });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>DELETE /api/Hierarchy/RemoveRelationship/{id} (Admin only)</summary>
        [HttpDelete("RemoveRelationship/{id}")]
        public async Task<IActionResult> RemoveRelationship(int id)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value))
                    return Ok(new { Success = false, Message = "Access denied. Admin permission required." });

                var result = await _hierarchyService.RemoveReportingRelationshipAsync(id);
                return Ok(new { Success = result.Success, Message = result.Message });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>POST /api/Hierarchy/SetPrimaryRelationship/{id} (Admin only)</summary>
        [HttpPost("SetPrimaryRelationship/{id}")]
        public async Task<IActionResult> SetPrimaryRelationship(int id)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value))
                    return Ok(new { Success = false, Message = "Access denied. Admin permission required." });

                var result = await _hierarchyService.SetPrimaryRelationshipAsync(id);
                return Ok(new { Success = result.Success, Message = result.Message });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        #endregion

        #region Master Data Endpoints

        [HttpGet("GetDepartments")]
        public async Task<IActionResult> GetDepartments([FromQuery] bool includeSubDepartments = false)
        {
            try
            {
                var departments = await _hierarchyService.GetDepartmentsAsync(includeSubDepartments);
                return Ok(new { Success = true, Data = departments });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetSubDepartments/{departmentId}")]
        public async Task<IActionResult> GetSubDepartments(int departmentId)
        {
            try
            {
                var subDepartments = await _hierarchyService.GetSubDepartmentsAsync(departmentId);
                return Ok(new { Success = true, Data = subDepartments });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetDepartmentsForHOD/{hodId}")]
        public async Task<IActionResult> GetDepartmentsForHOD(int hodId)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                var departments = await _hierarchyService.GetDepartmentsForHODAsync(hodId);
                return Ok(new { Success = true, Data = departments });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetRoleTypes")]
        public async Task<IActionResult> GetRoleTypes()
        {
            try
            {
                var roleTypes = await _hierarchyService.GetRoleTypesAsync();
                return Ok(new { Success = true, Data = roleTypes });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        [HttpGet("GetReportingTypes")]
        public async Task<IActionResult> GetReportingTypes()
        {
            try
            {
                var reportingTypes = await _hierarchyService.GetReportingTypesAsync();
                return Ok(new { Success = true, Data = reportingTypes });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        #endregion

        #region Export Endpoint

        /// <summary>GET /api/Hierarchy/ExportHierarchy</summary>
        [HttpGet("ExportHierarchy")]
        public async Task<IActionResult> ExportHierarchy()
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                var userInfo = await GetUserInfoFromDatabaseAsync();
                var data = await _hierarchyService.GetHierarchyForExportAsync(userInfo.EmpId, userInfo.IsAdmin);
                return Ok(new { Success = true, Data = data });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        #endregion

        #region Import / Master Flat

        [HttpGet("GetMasterFlat")]
        public async Task<IActionResult> GetMasterFlat()
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                var userInfo = await GetUserInfoFromDatabaseAsync();
                var rows = await _hierarchyService.GetMasterFlatAsync(userInfo.EmpId, userInfo.IsAdmin);
                return Ok(new { Success = true, Data = rows });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost("ProcessImport")]
        public async Task<IActionResult> ProcessImport([FromBody] List<ImportRowRequest> rows)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value))
                    return Ok(new { Success = false, Message = "Access denied. Admin permission required." });

                if (rows == null || !rows.Any())
                    return Ok(new { Success = false, Message = "No data to import." });

                var summary = await _hierarchyService.ProcessImportAsync(rows);
                return Ok(new { Success = true, Data = summary });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }


        #endregion

        #region Admin Master Grid Endpoints

        /// <summary>GET /api/Hierarchy/GetMasterFlatAdmin — Admin-only single-SP flat grid data.</summary>
        [HttpGet("GetMasterFlatAdmin")]
        public async Task<IActionResult> GetMasterFlatAdmin()
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                var userInfo = await GetUserInfoFromDatabaseAsync();
                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value))
                    return Ok(new { Success = false, Message = "Access denied. Admin permission required." });

                var rows = await _hierarchyService.GetMasterFlatAdminAsync(userInfo.EmpId);
                return Ok(new { Success = true, Data = rows });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        /// <summary>POST /api/Hierarchy/UpdateEmployeeBasic — Inline grid cell save (Name/Desig/Salary/DOJ/Status).</summary>
        [HttpPost("UpdateEmployeeBasic")]
        public async Task<IActionResult> UpdateEmployeeBasic([FromBody] UpdateEmployeeBasicRequest request)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                var userInfo = await GetUserInfoFromDatabaseAsync();
                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value))
                    return Ok(new { Success = false, Message = "Access denied. Admin permission required." });

                if (!ModelState.IsValid)
                    return Ok(new { Success = false, Message = "Invalid request data" });

                var result = await _hierarchyService.UpdateEmployeeBasicAsync(request);
                return Ok(result);
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        #endregion

        #region Excel Import Endpoint

        /// <summary>POST /api/Hierarchy/ImportFromExcel — Bulk upsert from HR Excel JSON.</summary>
        [HttpPost("ImportFromExcel")]
        public async Task<IActionResult> ImportFromExcel([FromBody] List<ImportExcelRowRequest> rows)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "User not logged in" });

                var userInfo = await GetUserInfoFromDatabaseAsync();
                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value))
                    return Ok(new { Success = false, Message = "Access denied. Admin only." });

                if (rows == null || !rows.Any())
                    return Ok(new { Success = false, Message = "No rows to import." });

                // Pass logged-in userId (INT) as CreatedBy — matches DB column type
                var result = await _hierarchyService.ImportFromExcelAsync(rows, GetUserId() ?? 0);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex)
            {
                return Ok(new { Success = false, Message = ex.Message });
            }
        }

        #endregion

        /// <summary>POST /api/Hierarchy/UpdateEmployeeFull — Full edit (Admin only)</summary>
        [HttpPost("UpdateEmployeeFull")]
        public async Task<IActionResult> UpdateEmployeeFull([FromBody] UpdateEmployeeFullRequest request)
        {
            try
            {
                if (!IsUserLoggedIn()) return Ok(new { Success = false, Message = "Not logged in" });
                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value)) return Ok(new { Success = false, Message = "Admin only" });
                if (!ModelState.IsValid)
                    return Ok(new { Success = false, Message = "Validation failed", Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

                var userId = GetUserId() ?? 0;
                var result = await _hierarchyService.UpdateEmployeeFullAsync(request, userId);
                return Ok(new { Success = result.Success, Message = result.Message });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        [HttpPost("SyncHODDepartments")]
        public async Task<IActionResult> SyncHODDepartments([FromBody] SyncHodDepartmentsRequest request)
        {
            try
            {
                if (!IsUserLoggedIn()) return Ok(new { Success = false, Message = "Not logged in" });
                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value)) return Ok(new { Success = false, Message = "Admin only" });
                if (!ModelState.IsValid)
                    return Ok(new { Success = false, Message = "Validation failed", Errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage) });

                var result = await _hierarchyService.SyncHodDepartmentsAsync(request, GetUserId() ?? 0);
                return Ok(new { Success = result.Success, Message = result.Message });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        [HttpPost("GetDepartmentChangeImpact")]
        public async Task<IActionResult> GetDepartmentChangeImpact([FromBody] DepartmentChangeImpactRequest request)
        {
            try
            {
                if (!IsUserLoggedIn()) return Ok(new { Success = false, Message = "Not logged in" });
                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value)) return Ok(new { Success = false, Message = "Admin only" });

                var result = await _hierarchyService.GetDepartmentChangeImpactAsync(request);
                return Ok(new { Success = true, Data = result });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        [HttpPost("BulkAssignTeam")]
        public async Task<IActionResult> BulkAssignTeam([FromBody] BulkAssignTeamRequest request)
        {
            try
            {
                if (!IsUserLoggedIn()) return Ok(new { Success = false, Message = "Not logged in" });
                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value)) return Ok(new { Success = false, Message = "Admin only" });

                var userId = GetUserId() ?? 0;
                var result = await _hierarchyService.BulkAssignTeamAsync(request, userId);
                return Ok(new { Success = result.Success, Message = result.Message, Data = result.Data });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        /// <summary>GET /api/Hierarchy/GetActiveHODs — For edit modal dropdown</summary>
        [HttpGet("GetActiveHODs")]
        public async Task<IActionResult> GetActiveHODs()
        {
            try
            {
                if (!IsUserLoggedIn()) return Ok(new { Success = false });
                var list = await _hierarchyService.GetActiveHODsAsync();
                return Ok(new { Success = true, Data = list });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        /// <summary>GET /api/Hierarchy/GetSubHODsForEdit?hodId=X — For edit modal dropdown</summary>
        [HttpGet("GetSubHODsForEdit")]
        public async Task<IActionResult> GetSubHODsForEdit([FromQuery] int? hodId)
        {
            try
            {
                if (!IsUserLoggedIn()) return Ok(new { Success = false });
                var list = await _hierarchyService.GetSubHODsForEditAsync(hodId);
                return Ok(new { Success = true, Data = list });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        /// <summary>POST /api/Hierarchy/AddDepartment</summary>
        [HttpPost("AddDepartment")]
        public async Task<IActionResult> AddDepartment([FromBody] AddDepartmentRequest request)
        {
            try
            {
                if (!IsUserLoggedIn()) return Ok(new { Success = false, Message = "Not logged in" });
                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value)) return Ok(new { Success = false, Message = "Admin only" });
                var userId = GetUserId() ?? 0;
                var result = await _hierarchyService.AddDepartmentAsync(request, userId);
                return Ok(new { Success = result.Success, Message = result.Message, Data = result.Data });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        /// <summary>POST /api/Hierarchy/AddSubDepartment</summary>
        [HttpPost("AddSubDepartment")]
        public async Task<IActionResult> AddSubDepartment([FromBody] AddSubDepartmentRequest request)
        {
            try
            {
                if (!IsUserLoggedIn()) return Ok(new { Success = false, Message = "Not logged in" });
                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value)) return Ok(new { Success = false, Message = "Admin only" });
                var userId = GetUserId() ?? 0;
                var result = await _hierarchyService.AddSubDepartmentAsync(request, userId);
                return Ok(new { Success = result.Success, Message = result.Message, Data = result.Data });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        /// <summary>POST /api/Hierarchy/UpdateDepartment</summary>
        [HttpPost("UpdateDepartment")]
        public async Task<IActionResult> UpdateDepartment([FromBody] UpdateDepartmentRequest request)
        {
            try
            {
                if (!IsUserLoggedIn()) return Ok(new { Success = false, Message = "Not logged in" });
                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value)) return Ok(new { Success = false, Message = "Admin only" });
                var userId = GetUserId() ?? 0;
                var result = await _hierarchyService.UpdateDepartmentAsync(request, userId);
                return Ok(new { Success = result.Success, Message = result.Message, Data = result.Data });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        /// <summary>POST /api/Hierarchy/UpdateSubDepartment</summary>
        [HttpPost("UpdateSubDepartment")]
        public async Task<IActionResult> UpdateSubDepartment([FromBody] UpdateSubDepartmentRequest request)
        {
            try
            {
                if (!IsUserLoggedIn()) return Ok(new { Success = false, Message = "Not logged in" });
                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value)) return Ok(new { Success = false, Message = "Admin only" });
                var userId = GetUserId() ?? 0;
                var result = await _hierarchyService.UpdateSubDepartmentAsync(request, userId);
                return Ok(new { Success = result.Success, Message = result.Message, Data = result.Data });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        // ════════════════════════════════════════════════════════════════
        // HierarchyController.cs mein add karo
        // Last closing } se PEHLE paste karo (AddSubDepartment ke baad)
        // ════════════════════════════════════════════════════════════════

        #region Salary Password

        /// <summary>POST /api/Hierarchy/VerifySalaryPassword</summary>
        [HttpPost("VerifySalaryPassword")]
        public async Task<IActionResult> VerifySalaryPassword([FromBody] VerifySalaryPasswordRequest request)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "Not logged in" });

                var userInfo = await GetUserInfoFromDatabaseAsync();

                // Find employeeId from empCode
                var emp = await _hierarchyService.GetEmployeeByCodeAsync(userInfo.EmpId);
                if (emp == null)
                    return Ok(new { Success = false, Message = "Employee not found" });

                var isValid = await _hierarchyService.VerifySalaryPasswordAsync(emp.EmployeeId, request.Password);
                return Ok(new { Success = isValid, Message = isValid ? "Verified" : "Incorrect password" });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        /// <summary>POST /api/Hierarchy/SetSalaryPassword — Admin sets salary password for HOD</summary>
        [HttpPost("SetSalaryPassword")]
        public async Task<IActionResult> SetSalaryPassword([FromBody] SetSalaryPasswordRequest request)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "Not logged in" });

                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value))
                    return Ok(new { Success = false, Message = "Admin only" });

                var ok = await _hierarchyService.SetSalaryPasswordAsync(request.EmployeeId, request.Password);
                return Ok(new { Success = ok, Message = ok ? "Password set successfully" : "Failed to set password" });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        /// <summary>POST /api/Hierarchy/VerifySalaryAccess — Two-factor: own password + admin master key</summary>
        [HttpPost("VerifySalaryAccess")]
        public async Task<IActionResult> VerifySalaryAccess([FromBody] VerifySalaryAccessRequest request)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "Not logged in" });

                var userInfo = await GetUserInfoFromDatabaseAsync();
                var emp = await _hierarchyService.GetEmployeeByCodeAsync(userInfo.EmpId);
                if (emp == null)
                    return Ok(new { Success = false, Message = "Employee record not found" });

                var result = await _hierarchyService.VerifySalaryAccessAsync(emp.EmployeeId, request.OwnPassword, request.AdminKey);
                return Ok(new { Success = result.Success, Message = result.Message });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        /// <summary>POST /api/Hierarchy/SetAdminSalaryKey — Authorised user sets/changes global salary master key</summary>
        [HttpPost("SetAdminSalaryKey")]
        public async Task<IActionResult> SetAdminSalaryKey([FromBody] SetAdminSalaryKeyRequest request)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "Not logged in" });

                var userId = GetUserId() ?? 0;
                var result = await _hierarchyService.SetAdminSalaryKeyAsync(request.OldKey, request.AdminKey, userId);
                return Ok(new { Success = result.Success, Message = result.Message });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        /// <summary>GET /api/Hierarchy/GetSalaryDbCredentials - Returns salary DB server/database/user after admin key verification.</summary>
        [HttpGet("GetSalaryDbCredentials")]
        public async Task<IActionResult> GetSalaryDbCredentials([FromQuery] string adminKey)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "Not logged in" });

                if (!await _hierarchyService.VerifyAdminKeyAsync(adminKey ?? ""))
                    return Ok(new { Success = false, Message = "Admin key is incorrect" });

                var credentials = await _hierarchyService.GetSalaryDbCredentialAsync();
                return Ok(new { Success = credentials != null, Data = credentials, Message = credentials == null ? "Salary database credentials are not configured" : "Success" });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        /// <summary>POST /api/Hierarchy/UpdateSalaryDbCredentials - Updates salary DB server/database/user after admin key verification.</summary>
        [HttpPost("UpdateSalaryDbCredentials")]
        public async Task<IActionResult> UpdateSalaryDbCredentials([FromBody] UpdateSalaryDbCredentialRequest request)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "Not logged in" });

                if (!await _hierarchyService.VerifyAdminKeyAsync(request?.AdminKey ?? ""))
                    return Ok(new { Success = false, Message = "Admin key is incorrect" });

                var userId = GetUserId() ?? 0;
                var currentEmployee = await GetCurrentHierarchyEmployeeAsync();
                var result = await _hierarchyService.UpdateSalaryDbCredentialAsync(request, userId, currentEmployee?.EmployeeId);
                return Ok(new { Success = result.Success, Message = result.Message });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        /// <summary>GET /api/Hierarchy/GetAdminKeyStatus — Returns HasPermission + KeyExists for current user</summary>
        [HttpGet("GetAdminKeyStatus")]
        public async Task<IActionResult> GetAdminKeyStatus()
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, HasPermission = false, KeyExists = false });

                var userId = GetUserId() ?? 0;
                var status = await _hierarchyService.GetAdminKeyStatusAsync(userId);
                return Ok(new { Success = true, HasPermission = status.HasPermission, KeyExists = status.KeyExists });
            }
            catch (Exception ex) { return Ok(new { Success = false, HasPermission = false, KeyExists = false, Message = ex.Message }); }
        }

        /// <summary>GET /api/Hierarchy/HasAdminSalaryKey — Check if admin key has been configured</summary>
        [HttpGet("HasAdminSalaryKey")]
        public async Task<IActionResult> HasAdminSalaryKey()
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, HasKey = false });

                var hasKey = await _hierarchyService.HasAdminSalaryKeySetAsync();
                return Ok(new { Success = true, HasKey = hasKey });
            }
            catch (Exception ex) { return Ok(new { Success = false, HasKey = false, Message = ex.Message }); }
        }

        /// <summary>POST /api/Hierarchy/VerifyAdminKey — Verifies only the admin key (no employee password)</summary>
        [HttpPost("VerifyAdminKey")]
        public async Task<IActionResult> VerifyAdminKey([FromBody] VerifyAdminKeyRequest request)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "Not logged in" });

                var ok = await _hierarchyService.VerifyAdminKeyAsync(request?.AdminKey ?? "");
                return Ok(new { Success = ok, Message = ok ? "Admin key verified" : "Admin key is incorrect" });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        /// <summary>POST /api/Hierarchy/SetSalaryPermission — Set ViewSalary flag for given employees</summary>
        [HttpPost("SetSalaryPermission")]
        public async Task<IActionResult> SetSalaryPermission([FromBody] SetSalaryPermissionRequest request)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "Not logged in" });

                var userId = GetUserId() ?? 0;
                var result = await _hierarchyService.SetSalaryPermissionAsync(request?.EmployeeIds ?? new List<int>(), userId);
                return Ok(new { Success = result.Success, Message = result.Message });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        // ══════════════════════════════════════════════════════════════
        // SALARY SESSION  — password NEVER logged, stored, or serialised
        // ══════════════════════════════════════════════════════════════

        private const string SalarySessionKey = "SalarySession";

        /// <summary>
        /// POST /api/Hierarchy/UnlockSalarySession
        /// Calls the Tankha Payee API with the configured username plus
        /// user-supplied password/action, then returns salary data directly.
        /// </summary>
        [HttpPost("UnlockSalarySession")]
        public async Task<IActionResult> UnlockSalarySession([FromBody] UnlockSalaryRequest request)
        {
            if (!IsUserLoggedIn())
                return Ok(new { Success = false, Message = "Not logged in" });

            if (string.IsNullOrWhiteSpace(request?.Action))
                return Ok(new { Success = false, Message = "Action is required" });

            var apiUrl = _configuration["TankhaPayeeApi:Url"];
            if (string.IsNullOrWhiteSpace(apiUrl))
                return Ok(new { Success = false, Message = "Tankha Payee API URL is not configured." });

            var apiUsername = _configuration["TankhaPayeeApi:Username"];
            if (string.IsNullOrWhiteSpace(apiUsername))
                apiUsername = request?.Username;

            if (string.IsNullOrWhiteSpace(apiUsername))
                return Ok(new { Success = false, Message = "Tankha Payee API username is not configured." });

            var apiPassword = !string.IsNullOrWhiteSpace(request?.Password)
                ? request.Password
                : _configuration["TankhaPayeeApi:Password"];

            if (string.IsNullOrWhiteSpace(apiPassword))
                return Ok(new { Success = false, Message = "Tankha Payee API password is not configured." });

            var userId = GetUserId() ?? 0;
            var currentEmployee = await GetCurrentHierarchyEmployeeAsync();
            var action = request.Action.Trim();
            var requestedActions = action.Equals("both", StringComparison.OrdinalIgnoreCase)
                ? new[] { "belowfourtyemployee", "equalandabovefourty" }
                : new[] { action };

            var salaryMap = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
            var apiRows = new List<Dictionary<string, object>>();
            var recordsByAction = new Dictionary<string, List<Dictionary<string, object>>>(StringComparer.OrdinalIgnoreCase);
            int count;
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                var encoded = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes($"{apiUsername}:{apiPassword}"));
                client.DefaultRequestHeaders.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", encoded);

                // Discard credentials immediately after building the header
                request.Username = null;
                request.Password = null;
                apiPassword = null;

                foreach (var apiAction in requestedActions)
                {
                    var body = System.Text.Json.JsonSerializer.Serialize(new { action = apiAction });
                    var content = new StringContent(body, System.Text.Encoding.UTF8, "application/json");
                    var response = await client.PostAsync(apiUrl, content);

                    if (!response.IsSuccessStatusCode)
                        return Ok(new { Success = false, Message = $"API returned status {(int)response.StatusCode} for {SalaryActionText(apiAction)}." });

                    var json = await response.Content.ReadAsStringAsync();
                    using var doc = System.Text.Json.JsonDocument.Parse(json);
                    var root = doc.RootElement;

                    // Response may be a root array or wrapped in commonData/data/Data.
                    System.Text.Json.JsonElement arr = default;
                    if (root.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        arr = root;
                    }
                    else if (TryGetPropertyIgnoreCase(root, "commonData", out var commonData) && commonData.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        arr = commonData;
                    }
                    else if (TryGetPropertyIgnoreCase(root, "data", out var data) && data.ValueKind == System.Text.Json.JsonValueKind.Array)
                    {
                        arr = data;
                    }
                    else
                    {
                        return Ok(new { Success = false, Message = $"Unexpected response format from Tankha Payee API for {SalaryActionText(apiAction)}." });
                    }

                    var actionRows = new List<Dictionary<string, object>>();
                    foreach (var item in arr.EnumerateArray())
                    {
                        string empCode = null;
                        decimal ctcAmount = 0;
                        var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);

                        if (item.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            foreach (var property in item.EnumerateObject())
                                row[property.Name] = ReadJsonValue(property.Value);
                        }

                        if (TryGetPropertyIgnoreCase(item, "empcode", out var ec))
                            empCode = ec.ValueKind == System.Text.Json.JsonValueKind.String ? ec.GetString() : ec.ToString();

                        if (TryGetPropertyIgnoreCase(item, "ctc_amount", out var ctc))
                            ctcAmount = ReadDecimal(ctc);

                        if (!string.IsNullOrWhiteSpace(empCode))
                            salaryMap[empCode.Trim()] = ctcAmount;

                        if (row.Count > 0)
                        {
                            actionRows.Add(row);
                            apiRows.Add(row);
                        }
                    }
                    recordsByAction[apiAction] = actionRows;
                }
                count = salaryMap.Count;
            }
            catch
            {
                request.Username = null;
                request.Password = null;
                return Ok(new { Success = false, Message = "API call failed. Please verify your credentials and try again." });
            }

            _ = LogSalaryEventAsync(userId, currentEmployee?.EmployeeId, "SalaryUnlock",
                $"Salary data loaded directly from Tankha Payee API (action: {action}). {count} records loaded.");

            var sessionMinutes = _configuration.GetValue<int?>("TankhaPayeeApi:SessionMinutes") ?? 30;
            if (sessionMinutes <= 0) sessionMinutes = 30;
            var expiresAt = DateTime.UtcNow.AddMinutes(sessionMinutes);
            var sessionPayload = System.Text.Json.JsonSerializer.Serialize(new
            {
                Salaries = salaryMap,
                Records = apiRows,
                RecordsByAction = recordsByAction,
                ExpiresAt = expiresAt,
                Action = action,
                Count = count
            });
            HttpContext.Session.SetString(SalarySessionKey, sessionPayload);

            return Ok(new
            {
                Success = true,
                Salaries = salaryMap,
                Records = apiRows,
                RecordsByAction = recordsByAction,
                Count = count,
                ExpiresAt = expiresAt,
                Action = action,
                Message = $"Salary data loaded for {count} employees."
            });

            static bool TryGetPropertyIgnoreCase(System.Text.Json.JsonElement element, string name, out System.Text.Json.JsonElement value)
            {
                value = default;
                if (element.ValueKind != System.Text.Json.JsonValueKind.Object)
                    return false;

                foreach (var property in element.EnumerateObject())
                {
                    if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
                    {
                        value = property.Value;
                        return true;
                    }
                }

                return false;
            }

            static string SalaryActionText(string value)
            {
                return string.Equals(value, "equalandabovefourty", StringComparison.OrdinalIgnoreCase)
                    ? "Equal and Above 40 Employees"
                    : "Below 40 Employees";
            }

            static decimal ReadDecimal(System.Text.Json.JsonElement element)
            {
                if (element.ValueKind == System.Text.Json.JsonValueKind.Number && element.TryGetDecimal(out var number))
                    return number;

                if (element.ValueKind == System.Text.Json.JsonValueKind.String &&
                    decimal.TryParse(element.GetString(), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var textNumber))
                    return textNumber;

                return 0;
            }

            static object ReadJsonValue(System.Text.Json.JsonElement element)
            {
                return element.ValueKind switch
                {
                    System.Text.Json.JsonValueKind.String => element.GetString(),
                    System.Text.Json.JsonValueKind.Number when element.TryGetDecimal(out var number) => number,
                    System.Text.Json.JsonValueKind.True => true,
                    System.Text.Json.JsonValueKind.False => false,
                    System.Text.Json.JsonValueKind.Null => null,
                    _ => element.ToString()
                };
            }
        }

        /// <summary>GET /api/Hierarchy/GetSalarySessionData — Returns live salary map if session is valid</summary>
        [HttpGet("GetSalarySessionData")]
        public async Task<IActionResult> GetSalarySessionData()
        {
            if (!IsUserLoggedIn())
                return Ok(new { Success = false, Message = "Not logged in" });

            var sessionJson = HttpContext.Session.GetString(SalarySessionKey);
            if (string.IsNullOrEmpty(sessionJson))
                return Ok(new { Success = false, Message = "No active salary session" });

            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(sessionJson);
                var root      = doc.RootElement;
                var expiresAt = root.GetProperty("ExpiresAt").GetDateTime();

                if (DateTime.UtcNow > expiresAt)
                {
                    HttpContext.Session.Remove(SalarySessionKey);
                    return Ok(new { Success = false, Message = "Salary session expired", Expired = true });
                }

                var salariesEl  = root.GetProperty("Salaries");
                var salaryDict  = new Dictionary<string, decimal>();
                foreach (var prop in salariesEl.EnumerateObject())
                    salaryDict[prop.Name] = prop.Value.GetDecimal();

                var records = new List<Dictionary<string, object>>();
                if (root.TryGetProperty("Records", out var recordsEl) && recordsEl.ValueKind == System.Text.Json.JsonValueKind.Array)
                {
                    foreach (var item in recordsEl.EnumerateArray())
                    {
                        var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                        if (item.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            foreach (var property in item.EnumerateObject())
                                row[property.Name] = property.Value.ValueKind switch
                                {
                                    System.Text.Json.JsonValueKind.String => property.Value.GetString(),
                                    System.Text.Json.JsonValueKind.Number when property.Value.TryGetDecimal(out var number) => number,
                                    System.Text.Json.JsonValueKind.True => true,
                                    System.Text.Json.JsonValueKind.False => false,
                                    System.Text.Json.JsonValueKind.Null => null,
                                    _ => property.Value.ToString()
                                };
                        }
                        if (row.Count > 0) records.Add(row);
                    }
                }

                var recordsByAction = new Dictionary<string, List<Dictionary<string, object>>>(StringComparer.OrdinalIgnoreCase);
                if (root.TryGetProperty("RecordsByAction", out var recordsByActionEl) && recordsByActionEl.ValueKind == System.Text.Json.JsonValueKind.Object)
                {
                    foreach (var actionProp in recordsByActionEl.EnumerateObject())
                    {
                        var actionRows = new List<Dictionary<string, object>>();
                        if (actionProp.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            foreach (var item in actionProp.Value.EnumerateArray())
                            {
                                var row = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
                                if (item.ValueKind == System.Text.Json.JsonValueKind.Object)
                                {
                                    foreach (var property in item.EnumerateObject())
                                        row[property.Name] = property.Value.ValueKind switch
                                        {
                                            System.Text.Json.JsonValueKind.String => property.Value.GetString(),
                                            System.Text.Json.JsonValueKind.Number when property.Value.TryGetDecimal(out var number) => number,
                                            System.Text.Json.JsonValueKind.True => true,
                                            System.Text.Json.JsonValueKind.False => false,
                                            System.Text.Json.JsonValueKind.Null => null,
                                            _ => property.Value.ToString()
                                        };
                                }
                                if (row.Count > 0) actionRows.Add(row);
                            }
                        }
                        recordsByAction[actionProp.Name] = actionRows;
                    }
                }

                var userId = GetUserId() ?? 0;
                var currentEmployee = await GetCurrentHierarchyEmployeeAsync();
                _ = LogSalaryEventAsync(userId, currentEmployee?.EmployeeId, "SalaryView",
                    $"Salary data viewed. {salaryDict.Count} employee salary records returned. Session expires {expiresAt:HH:mm:ss} UTC.");

                var action = root.TryGetProperty("Action", out var actionEl) ? actionEl.GetString() : "";
                var count = root.TryGetProperty("Count", out var countEl) && countEl.TryGetInt32(out var storedCount)
                    ? storedCount
                    : salaryDict.Count;

                return Ok(new { Success = true, Salaries = salaryDict, Records = records, RecordsByAction = recordsByAction, ExpiresAt = expiresAt, Action = action, Count = count });
            }
            catch
            {
                HttpContext.Session.Remove(SalarySessionKey);
                return Ok(new { Success = false, Message = "Session data error" });
            }
        }

        /// <summary>POST /api/Hierarchy/ClearSalarySession — Clears salary data from session</summary>
        [HttpPost("ClearSalarySession")]
        public async Task<IActionResult> ClearSalarySession()
        {
            if (!IsUserLoggedIn())
                return Ok(new { Success = false, Message = "Not logged in" });

            var userId = GetUserId() ?? 0;
            var currentEmployee = await GetCurrentHierarchyEmployeeAsync();
            HttpContext.Session.Remove(SalarySessionKey);
            _ = LogSalaryEventAsync(userId, currentEmployee?.EmployeeId, "SalaryLock", "Salary session cleared by user.");
            return Ok(new { Success = true, Message = "Salary session cleared" });
        }

        private async Task LogSalaryEventAsync(int userId, int? employeeId, string action, string note)
        {
            try
            {
                await _hierarchyService.LogAuditAsync(
                    action,
                    "SalarySession",
                    employeeId,
                    employeeId,
                    null,
                    null,
                    note,
                    userId);
            }
            catch { /* audit log failure must never affect the main response */ }
        }

        /// <summary>GET /api/Hierarchy/GetEmployeesForSalaryPermission — Returns employees with ViewSalary flag</summary>
        [HttpGet("GetEmployeesForSalaryPermission")]
        public async Task<IActionResult> GetEmployeesForSalaryPermission()
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "Not logged in" });

                var employees = await _hierarchyService.GetEmployeesForSalaryPermissionAsync();
                return Ok(new { Success = true, Data = employees });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        #endregion

        #region Audit Log

        /// <summary>GET /api/Hierarchy/GetAuditLogs</summary>
        [HttpGet("GetAuditLogs")]
        public async Task<IActionResult> GetAuditLogs(
            [FromQuery] int? employeeId = null,
            [FromQuery] string actionType = null,
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] string searchTerm = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 50)
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "Not logged in" });

                var request = new AuditLogRequest
                {
                    EmployeeId = employeeId,
                    ActionType = actionType,
                    FromDate = fromDate,
                    ToDate = toDate,
                    SearchTerm = searchTerm,
                    PageNumber = pageNumber,
                    PageSize = pageSize
                };
                var logs = await _hierarchyService.GetAuditLogsAsync(request);
                return Ok(new { Success = true, Data = logs });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        #endregion

        #region Orphan Employees

        /// <summary>GET /api/Hierarchy/GetOrphanEmployees — Active employees with no reporting relationship</summary>
        [HttpGet("GetOrphanEmployees")]
        public async Task<IActionResult> GetOrphanEmployees()
        {
            try
            {
                if (!IsUserLoggedIn())
                    return Ok(new { Success = false, Message = "Not logged in" });

                var list = await _hierarchyService.GetOrphanEmployeesAsync();
                return Ok(new { Success = true, Data = list });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        #endregion

        #region Custom Fields (Dynamic Columns)

        [HttpGet("GetCustomFields")]
        public async Task<IActionResult> GetCustomFields()
        {
            try
            {
                var fields = await _hierarchyService.GetCustomFieldsAsync();
                return Ok(new { Success = true, Data = fields });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        [HttpPost("AddCustomField")]
        public async Task<IActionResult> AddCustomField([FromBody] AddCustomFieldRequest request)
        {
            try
            {
                if (!IsUserLoggedIn()) return Ok(new { Success = false, Message = "Not logged in" });
                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value)) return Ok(new { Success = false, Message = "Admin only" });
                var result = await _hierarchyService.AddCustomFieldAsync(request, GetUserId() ?? 0);
                return Ok(new { Success = result.Success, Message = result.Message, Data = result.Data });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        [HttpDelete("RemoveCustomField/{fieldId}")]
        public async Task<IActionResult> RemoveCustomField(int fieldId)
        {
            try
            {
                if (!IsUserLoggedIn()) return Ok(new { Success = false, Message = "Not logged in" });
                if (!await HasHierarchyMasterPermissionAsync(GetUserId()!.Value)) return Ok(new { Success = false, Message = "Admin only" });
                var result = await _hierarchyService.RemoveCustomFieldAsync(fieldId);
                return Ok(new { Success = result.Success, Message = result.Message });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        [HttpGet("GetEmployeeCustomValues")]
        public async Task<IActionResult> GetEmployeeCustomValues([FromQuery] int? employeeId = null)
        {
            try
            {
                var values = await _hierarchyService.GetEmployeeCustomValuesAsync(employeeId);
                return Ok(new { Success = true, Data = values });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        [HttpGet("GetEmployeeModalExtras")]
        public async Task<IActionResult> GetEmployeeModalExtras([FromQuery] int employeeId)
        {
            try
            {
                var extras = await _hierarchyService.GetEmployeeModalExtrasAsync(employeeId);
                return Ok(new { Success = true, Data = extras });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        [HttpPost("SetEmployeeCustomValue")]
        public async Task<IActionResult> SetEmployeeCustomValue([FromBody] SetCustomValueRequest request)
        {
            try
            {
                if (!IsUserLoggedIn()) return Ok(new { Success = false, Message = "Not logged in" });
                var result = await _hierarchyService.SetEmployeeCustomValueAsync(request);
                return Ok(new { Success = result.Success, Message = result.Message });
            }
            catch (Exception ex) { return Ok(new { Success = false, Message = ex.Message }); }
        }

        #endregion
    }
}
