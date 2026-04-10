using JSAPNEW.Services;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Text.Json;

namespace JSAPNEW.Controllers
{
    public class TaskWebController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly IHierarchyService _hierarchyService;
        private readonly IConfiguration _configuration;

        public TaskWebController(ITaskService taskService, IHierarchyService hierarchyService, IConfiguration configuration)
        {
            _taskService = taskService;
            _hierarchyService = hierarchyService;
            _configuration = configuration;
        }

        #region Session Helpers 

        private int? GetUserId() => HttpContext.Session.GetInt32("userId");

        private bool IsUserLoggedIn()
        {
            var userId = GetUserId();
            return userId.HasValue && userId > 0;
        }

        /// <summary>
        /// Get user info from jsUser table (same approach as HierarchyWebController)
        /// </summary>
        private async Task<(string EmpId, string RoleName, string FirstName, string LastName)> GetUserInfoFromDatabaseAsync(int userId)
        {
            string empId = "", roleName = "", firstName = "", lastName = "";

            try
            {
                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                using var connection = new SqlConnection(connectionString);
                await connection.OpenAsync();

                var query = @"
                    SELECT
                        u.empId, u.firstName, u.lastName,
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
                command.Parameters.AddWithValue("@UserId", userId);

                using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    empId = reader["empId"]?.ToString() ?? "";
                    firstName = reader["firstName"]?.ToString() ?? "";
                    lastName = reader["lastName"]?.ToString() ?? "";
                    roleName = reader["roleName"]?.ToString() ?? "";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching user info: {ex.Message}");
            }

            return (empId, roleName, firstName, lastName);
        }

        private async Task<bool> HasTaskAdminPermissionAsync(int userId)
        {
            try
            {
                var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");
                if (!selectedCompanyId.HasValue || selectedCompanyId.Value <= 0)
                    return false;

                var connectionString = _configuration.GetConnectionString("DefaultConnection");
                await using var connection = new SqlConnection(connectionString);
                await using var command = new SqlCommand("jsGetUserEffectivePermissions", connection)
                {
                    CommandType = System.Data.CommandType.StoredProcedure
                };

                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@companyId", selectedCompanyId.Value);

                await connection.OpenAsync();
                await using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    var moduleName = reader["moduleName"]?.ToString() ?? "";
                    var permissionType = reader["permissionType"]?.ToString() ?? "";

                    if (moduleName.Equals("Daily Task", StringComparison.OrdinalIgnoreCase)
                        && permissionType.Equals("Admin-task", StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error checking task admin permission: {ex.Message}");
            }

            return false;
        }

        #endregion

        #region Views

        /// <summary>Main Task Dashboard - /TaskWeb/Dashboard</summary>
        public async Task<IActionResult> Dashboard()
        {
            if (!IsUserLoggedIn())
                return RedirectToAction("Index", "Login");

            var userId = GetUserId().Value;

            // Step 1: Get basic user info from jsUser (same as HierarchyWebController)
            var userInfo = await GetUserInfoFromDatabaseAsync(userId);
            var empCode = userInfo.EmpId;
            var isAdmin = await HasTaskAdminPermissionAsync(userId);

            // Defaults
            int employeeId = 0;
            int roleTypeId = 3; // Executive default
            string employeeName = !string.IsNullOrEmpty(userInfo.FirstName)
                ? $"{userInfo.FirstName} {userInfo.LastName}".Trim()
                : HttpContext.Session.GetString("username") ?? "User";
            string employeeCode = empCode;

            // Step 2: Get employee details from Employees table via HierarchyService
            if (!string.IsNullOrEmpty(empCode))
            {
                try
                {
                    var currentEmployee = await _hierarchyService.GetEmployeeByCodeAsync(empCode);
                    if (currentEmployee != null)
                    {
                        employeeId = currentEmployee.EmployeeId;
                        roleTypeId = currentEmployee.RoleTypeId;
                        employeeName = !string.IsNullOrEmpty(currentEmployee.EmployeeName)
                            ? currentEmployee.EmployeeName
                            : employeeName;
                        employeeCode = currentEmployee.EmployeeCode ?? empCode;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching employee from hierarchy: {ex.Message}");
                }
            }

            ViewBag.UserId = userId;
            ViewBag.EmployeeId = employeeId;
            ViewBag.RoleTypeId = roleTypeId;
            ViewBag.EmployeeName = employeeName;
            ViewBag.EmployeeCode = employeeCode;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.AdminLabel = isAdmin ? "Task Admin" : string.Empty;
            ViewBag.CompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            // Preload hierarchy for HOD so no extra API call is needed on page load
            if (roleTypeId == 1 && !string.IsNullOrEmpty(employeeCode))
            {
                try
                {
                    var tree = await _hierarchyService.GetHierarchyTreeAsync(employeeCode, isAdmin);
                    var myNode = tree?.FirstOrDefault(n => n.EmployeeCode == employeeCode) ?? tree?.FirstOrDefault();
                    if (myNode != null)
                    {
                        var departments = myNode.SubHODs
                            .GroupBy(s => s.DepartmentId)
                            .Select(dg =>
                            {
                                var deptSubHods = dg.GroupBy(s => s.EmployeeId).Select(sg =>
                                {
                                    var sf = sg.First();
                                    var execs = sg.SelectMany(s => s.Executives)
                                        .GroupBy(e => e.EmployeeId).Select(eg => eg.First())
                                        .Select(e => new { employeeId = e.EmployeeId, employeeName = e.EmployeeName, employeeCode = e.EmployeeCode, designation = e.Designation })
                                        .ToList();
                                    return new { employeeId = sf.EmployeeId, employeeName = sf.EmployeeName, employeeCode = sf.EmployeeCode, designation = sf.Designation, executives = execs };
                                }).ToList();
                                return new { departmentId = dg.Key, departmentName = dg.First().DepartmentName ?? "General", subHods = deptSubHods };
                            }).ToList();

                        int totalSubHods = myNode.SubHODs.Select(s => s.EmployeeId).Distinct().Count();
                        int totalExecs = myNode.SubHODs.SelectMany(s => s.Executives).Select(e => e.EmployeeId).Distinct().Count();

                        ViewBag.HierarchyJson = JsonSerializer.Serialize(new
                        {
                            success = true,
                            subHodCount = totalSubHods,
                            executiveCount = totalExecs,
                            departments
                        });
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error preloading hierarchy: {ex.Message}");
                }
            }

            return View();
        }

        /// <summary>API: Get team members for assign dropdown (Sub HOD / HOD)</summary>
        [HttpGet]
        public async Task<IActionResult> GetTeamMembers([FromQuery] string? scope = null)
        {
            if (!IsUserLoggedIn())
                return Json(new { success = false, data = new List<object>() });

            var userId = GetUserId().Value;
            var userInfo = await GetUserInfoFromDatabaseAsync(userId);
            var empCode = userInfo.EmpId;
            var isAdmin = await HasTaskAdminPermissionAsync(userId);

            var members = new List<object>();
            var wantsAllHods = string.Equals(scope, "allhods", StringComparison.OrdinalIgnoreCase);

            if (isAdmin && wantsAllHods)
            {
                try
                {
                    var connectionString = _configuration.GetConnectionString("DefaultConnection");
                    await using var connection = new SqlConnection(connectionString);
                    await connection.OpenAsync();

                    const string hodQuery = @"
                        SELECT EmployeeId, EmployeeName, EmployeeCode, Designation, RoleTypeId
                        FROM [Hie].[Employees]
                        WHERE RoleTypeId = 1
                        ORDER BY EmployeeName";

                    await using var command = new SqlCommand(hodQuery, connection);
                    await using var reader = await command.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        members.Add(new
                        {
                            employeeId = Convert.ToInt32(reader["EmployeeId"]),
                            employeeName = reader["EmployeeName"]?.ToString() ?? "",
                            employeeCode = reader["EmployeeCode"]?.ToString() ?? "",
                            designation = reader["Designation"]?.ToString() ?? "",
                            role = "HOD",
                            roleTypeId = Convert.ToInt32(reader["RoleTypeId"])
                        });
                    }

                    return Json(new { success = true, data = members });
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching all HOD members: {ex.Message}");
                }
            }

            if (!string.IsNullOrEmpty(empCode))
            {
                try
                {
                    var currentEmployee = await _hierarchyService.GetEmployeeByCodeAsync(empCode);

                    if (currentEmployee != null)
                    {
                        // Get direct reports for this manager
                        var directReports = await _hierarchyService.GetEmployeeDirectReportsAsync(currentEmployee.EmployeeId);
                        if (directReports != null)
                        {
                            foreach (var dr in directReports)
                            {
                                members.Add(new
                                {
                                    employeeId = dr.EmployeeId,
                                    employeeName = dr.EmployeeName,
                                    employeeCode = dr.EmployeeCode,
                                    designation = dr.Designation ?? "",
                                    role = dr.EmployeeRole ?? "",
                                    roleTypeId = string.Equals(dr.EmployeeRole, "HOD", StringComparison.OrdinalIgnoreCase) ? 1
                                        : string.Equals(dr.EmployeeRole, "Sub HOD", StringComparison.OrdinalIgnoreCase) || string.Equals(dr.EmployeeRole, "SubHOD", StringComparison.OrdinalIgnoreCase) ? 2
                                        : 3
                                });
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error fetching team members: {ex.Message}");
                }
            }

            return Json(new { success = true, data = members });
        }

        /// <summary>API: Get hierarchy tree for HOD (Sub HODs + their Executives)</summary>
        [HttpGet]
        public async Task<IActionResult> GetHierarchyTree()
        {
            if (!IsUserLoggedIn())
                return Json(new { success = false });

            var userId = GetUserId().Value;
            var userInfo = await GetUserInfoFromDatabaseAsync(userId);
            var empCode = userInfo.EmpId;
            var isAdmin = await HasTaskAdminPermissionAsync(userId);

            if (string.IsNullOrEmpty(empCode))
                return Json(new { success = false });

            try
            {
                var tree = await _hierarchyService.GetHierarchyTreeAsync(empCode, isAdmin);
                var myNode = tree?.FirstOrDefault(n => n.EmployeeCode == empCode) ?? tree?.FirstOrDefault();
                if (myNode == null)
                    return Json(new { success = false });

                // Group by department, deduplicate members within each dept
                var departments = myNode.SubHODs
                    .GroupBy(s => s.DepartmentId)
                    .Select(dg =>
                    {
                        var deptSubHods = dg.GroupBy(s => s.EmployeeId).Select(sg =>
                        {
                            var sf = sg.First();
                            var execs = sg.SelectMany(s => s.Executives)
                                .GroupBy(e => e.EmployeeId).Select(eg => eg.First())
                                .Select(e => new { employeeId = e.EmployeeId, employeeName = e.EmployeeName, employeeCode = e.EmployeeCode, designation = e.Designation })
                                .ToList();
                            return new { employeeId = sf.EmployeeId, employeeName = sf.EmployeeName, employeeCode = sf.EmployeeCode, designation = sf.Designation, executives = execs };
                        }).ToList();

                        return new
                        {
                            departmentId = dg.Key,
                            departmentName = dg.First().DepartmentName ?? "General",
                            subHods = deptSubHods
                        };
                    }).ToList();

                int totalSubHods = myNode.SubHODs.Select(s => s.EmployeeId).Distinct().Count();
                int totalExecs = myNode.SubHODs.SelectMany(s => s.Executives).Select(e => e.EmployeeId).Distinct().Count();

                return Json(new
                {
                    success = true,
                    subHodCount = totalSubHods,
                    executiveCount = totalExecs,
                    departments
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetHierarchyTree error: {ex.Message}");
                return Json(new { success = false });
            }
        }

        /// <summary>Old Tasks view - kept for backward compat</summary>
        public IActionResult Tasks()
        {
            var userId = HttpContext.Session.GetInt32("userId");
            var selectedCompanyId = HttpContext.Session.GetInt32("selectedCompanyId");

            if (userId == null || selectedCompanyId == null)
                return RedirectToAction("Index", "Login");

            ViewBag.CompanyId = selectedCompanyId;
            ViewBag.UserId = userId;

            return View();
        }

        #endregion
    }
}
