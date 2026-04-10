using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using JSAPNEW.Models;
using JSAPNEW.Services;

namespace JSAPNEW.Controllers
{
    public class HierarchyWebController : Controller
    {
        private readonly IHierarchyService _hierarchyService;
        private readonly IConfiguration _configuration;

        public HierarchyWebController(IHierarchyService hierarchyService, IConfiguration configuration)
        {
            _hierarchyService = hierarchyService;
            _configuration = configuration;
        }

        #region Session & Database Helpers

        private int? GetUserId() => HttpContext.Session.GetInt32("userId");

        private bool IsUserLoggedIn()
        {
            var userId = GetUserId();
            return userId.HasValue && userId > 0;
        }

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

        private bool IsAdmin(string roleName) =>
            roleName == "Admin" || roleName == "Super User";

        /// <summary>
        /// Resolves the user's Hierarchy role by calling jsGetUserEffectivePermissions
        /// across all companies the user belongs to.
        /// Returns "Admin" | "HOD" | "SubHOD" | "None".
        /// </summary>
        private async Task<string> GetHierarchyRoleAsync(int userId)
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

                    if (best != "None") break;
                }

                return best;
            }
            catch { return "None"; }
        }

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

            return false;
        }

        private async Task<bool> HasEmployeeHierarchyPagePermissionAsync(int userId)
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

                        if (permType.Equals("Employee_Master", StringComparison.OrdinalIgnoreCase)
                            || permType.Equals("Hierarchy_Master", StringComparison.OrdinalIgnoreCase)
                            || permType.Equals("Hierarchy_Admin", StringComparison.OrdinalIgnoreCase)
                            || permType.Equals("Hierarchy_HOD", StringComparison.OrdinalIgnoreCase)
                            || permType.Equals("Hierarchy_SubHOD", StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private async Task<bool> HasEmployeeHierarchyEditPermissionAsync(int userId)
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
                    catch { }
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

                        if (permType.Equals("Employee_Master", StringComparison.OrdinalIgnoreCase)
                            || permType.Equals("Hierarchy_Master", StringComparison.OrdinalIgnoreCase)
                            || permType.Equals("Hierarchy_Admin", StringComparison.OrdinalIgnoreCase)
                            || permType.Equals("Hierarchy_HOD", StringComparison.OrdinalIgnoreCase)
                            || permType.Equals("Hierarchy_SubHOD", StringComparison.OrdinalIgnoreCase))
                            return true;
                    }
                }
            }
            catch { }

            return false;
        }

        private void SetViewBagData(string empCode, string roleName, string firstName, string lastName, bool isAdmin, string hierarchyRole = "None")
        {
            ViewBag.CurrentEmployeeCode = empCode;
            ViewBag.IsAdmin = isAdmin;
            ViewBag.HierarchyRole = hierarchyRole;   // "Admin" | "HOD" | "SubHOD" | "None"
            ViewBag.UserName = !string.IsNullOrEmpty(firstName)
                ? $"{firstName} {lastName}".Trim()
                : HttpContext.Session.GetString("username") ?? "User";
            ViewBag.RoleName = roleName;
        }

        #endregion

        #region Dashboard

        /// <summary>URL: /HierarchyWeb/Index</summary>
        public async Task<IActionResult> Index()
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            var hierRole = await GetHierarchyRoleAsync(userId.Value);
            var empCode = userInfo.EmpId;
            var roleName = userInfo.RoleName;
            // If the user has an explicit Hierarchy permission, use it.
            // If no Hierarchy permission is assigned, fall back to the generic admin role.
            var isHierarchyAdmin = hierRole == "Admin"
                || (hierRole == "None" && IsAdmin(roleName));

            SetViewBagData(empCode, roleName, userInfo.FirstName, userInfo.LastName, isHierarchyAdmin, hierRole);

            if (!string.IsNullOrEmpty(empCode))
            {
                try
                {
                    var currentEmployee = await _hierarchyService.GetEmployeeByCodeAsync(empCode);
                    ViewBag.CurrentEmployee = currentEmployee;
                    ViewBag.CurrentRoleTypeId = currentEmployee?.RoleTypeId ?? 0;
                    ViewBag.CurrentRoleName = currentEmployee?.RoleName ?? "";
                    ViewBag.Salary = currentEmployee?.Salary ?? "";
                    ViewBag.ViewSalary = currentEmployee?.ViewSalary ?? false;
                }
                catch (Exception ex)
                {
                    ViewBag.Warning = "Could not load employee info: " + ex.Message;
                }
            }
            else
            {
                ViewBag.Warning = "Employee code not found for your account.";
            }

            try
            {
                var dashboard = await _hierarchyService.GetDashboardSummaryAsync(empCode, isHierarchyAdmin);
                var hierarchyTree = await _hierarchyService.GetHierarchyTreeAsync(empCode, isHierarchyAdmin);
                ViewBag.HierarchyTree = hierarchyTree;
                return View(dashboard ?? new DashboardSummaryDto());
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                if (ex.InnerException != null)
                    ViewBag.Error += $" | {ex.InnerException.Message}";

                return View(new DashboardSummaryDto());
            }
        }

        /// <summary>URL: /HierarchyWeb/HierarchyView</summary>
        public async Task<IActionResult> HierarchyView()
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            var hierRole = await GetHierarchyRoleAsync(userId.Value);
            var empCode = userInfo.EmpId;
            var isHierarchyAdmin = hierRole == "Admin"
                || (hierRole == "None" && IsAdmin(userInfo.RoleName));

            SetViewBagData(empCode, userInfo.RoleName, userInfo.FirstName, userInfo.LastName, isHierarchyAdmin, hierRole);

            if (!string.IsNullOrEmpty(empCode))
            {
                try
                {
                    var currentEmployee = await _hierarchyService.GetEmployeeByCodeAsync(empCode);
                    ViewBag.CurrentEmployee = currentEmployee;
                    ViewBag.CurrentRoleTypeId = currentEmployee?.RoleTypeId ?? 0;
                    ViewBag.CurrentRoleName = currentEmployee?.RoleName ?? "";
                    ViewBag.Salary = currentEmployee?.Salary ?? "";
                    ViewBag.ViewSalary = currentEmployee?.ViewSalary ?? false;
                }
                catch (Exception ex)
                {
                    ViewBag.Warning = "Could not load employee info: " + ex.Message;
                }
            }

            try
            {
                var dashboard = await _hierarchyService.GetDashboardSummaryAsync(empCode, isHierarchyAdmin);
                var hierarchyTree = await _hierarchyService.GetHierarchyTreeAsync(empCode, isHierarchyAdmin);
                ViewBag.HierarchyTree = (object)hierarchyTree;
                return View(dashboard ?? new DashboardSummaryDto());
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error: {ex.Message}";
                if (ex.InnerException != null)
                    ViewBag.Error += $" | {ex.InnerException.Message}";

                return View(new DashboardSummaryDto());
            }
        }

        /// <summary>URL: /HierarchyWeb/HierarchyMaster</summary>
        public async Task<IActionResult> HierarchyMaster()
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            if (!await HasHierarchyMasterPermissionAsync(userId.Value))
            {
                TempData["Error"] = "You do not have permission to access Hierarchy Master.";
                return RedirectToAction("Index");
            }

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            var hierRole = await GetHierarchyRoleAsync(userId.Value);
            var empCode = userInfo.EmpId;
            // Use permission-based admin check, not user role name
            var isAdmin = hierRole == "Admin"
                || (hierRole == "None" && IsAdmin(userInfo.RoleName));

            SetViewBagData(empCode, userInfo.RoleName, userInfo.FirstName, userInfo.LastName, isAdmin, hierRole);

            if (!string.IsNullOrEmpty(empCode))
            {
                try
                {
                    var currentEmployee = await _hierarchyService.GetEmployeeByCodeAsync(empCode);
                    ViewBag.CurrentEmployee = currentEmployee;
                    ViewBag.CurrentRoleTypeId = currentEmployee?.RoleTypeId ?? 0;
                    ViewBag.ViewSalary = currentEmployee?.ViewSalary ?? false;
                }
                catch { }
            }

            return View();
        }

        /// <summary>URL: /HierarchyWeb/EmployeeHierarchy?searchTerm=EMP001&pageNumber=1</summary>
        public async Task<IActionResult> EmployeeHierarchy(string searchTerm = null, int pageNumber = 1)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var canManageHierarchy = await HasEmployeeHierarchyEditPermissionAsync(userId.Value);
            var canViewEmployeeHierarchy = canManageHierarchy || await HasEmployeeHierarchyPagePermissionAsync(userId.Value);

            if (!canViewEmployeeHierarchy)
            {
                TempData["Error"] = "You do not have permission to access Employee Hierarchy.";
                return RedirectToAction("Index");
            }

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            var hierRole = await GetHierarchyRoleAsync(userId.Value);
            var currentEmpCode = userInfo.EmpId;
            var isHierarchyAdmin = hierRole == "Admin"
                || (hierRole == "None" && IsAdmin(userInfo.RoleName));
            var adminKeyStatus = await _hierarchyService.GetAdminKeyStatusAsync(userId.Value);

            SetViewBagData(currentEmpCode, userInfo.RoleName, userInfo.FirstName, userInfo.LastName, isHierarchyAdmin, hierRole);
            ViewBag.CanManageEmployeeHierarchy = canManageHierarchy;
            ViewBag.CanManageAdminKey = adminKeyStatus?.HasPermission ?? false;

            try
            {
                if (!string.IsNullOrEmpty(currentEmpCode))
                {
                    try
                    {
                        var currentEmployee = await _hierarchyService.GetEmployeeByCodeAsync(currentEmpCode);
                        ViewBag.CurrentEmployee = currentEmployee;
                        ViewBag.CurrentRoleTypeId = currentEmployee?.RoleTypeId ?? 0;
                        ViewBag.ViewSalary = currentEmployee?.ViewSalary ?? false;
                    }
                    catch { }
                }

                const int pageSize = 10;
                pageNumber = Math.Max(pageNumber, 1);

                var request = new EmployeeSearchRequest
                {
                    SearchTerm = searchTerm?.Trim(),
                    IsActive = true,
                    PageNumber = 1,
                    PageSize = 100000
                };

                var employees = await _hierarchyService.GetEmployeesAsync(request, currentEmpCode, true) ?? new List<EmployeeDto>();
                var totalRecords = employees.Count;
                var totalPages = Math.Max(1, (int)Math.Ceiling(totalRecords / (double)pageSize));
                pageNumber = Math.Min(pageNumber, totalPages);
                var pagedEmployees = employees
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToList();

                ViewBag.SearchTerm = searchTerm?.Trim() ?? "";
                ViewBag.PageNumber = pageNumber;
                ViewBag.PageSize = pageSize;
                ViewBag.TotalRecords = totalRecords;
                ViewBag.TotalPages = totalPages;
                ViewBag.AllEmployeesJson = System.Text.Json.JsonSerializer.Serialize(employees);

                return View(pagedEmployees);
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error loading employee hierarchy: {ex.Message}";
                ViewBag.SearchTerm = searchTerm?.Trim() ?? "";
                ViewBag.PageNumber = 1;
                ViewBag.PageSize = 10;
                ViewBag.TotalRecords = 0;
                ViewBag.TotalPages = 1;
                return View(new List<EmployeeDto>());
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetEmployeeHierarchyDetails(int id)
        {
            var userId = GetUserId();
            if (userId == null)
                return Json(new { Success = false, Message = "User not logged in" });

            if (!await HasEmployeeHierarchyEditPermissionAsync(userId.Value))
                return Json(new { Success = false, Message = "Access denied" });

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);

            try
            {
                var employee = await _hierarchyService.GetEmployeeByIdAsync(id, userInfo.EmpId, true);
                if (employee == null || employee.EmployeeId <= 0)
                    return Json(new { Success = false, Message = "Employee not found" });

                return Json(new { Success = true, Data = employee });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> UpdateEmployeeHierarchyItem([FromBody] UpdateEmployeeFullRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return Json(new { Success = false, Message = "User not logged in" });

            if (!await HasEmployeeHierarchyEditPermissionAsync(userId.Value))
                return Json(new { Success = false, Message = "Access denied" });

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                return Json(new { Success = false, Message = "Validation failed", Errors = errors });
            }

            try
            {
                var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
                var existing = await _hierarchyService.GetEmployeeByIdAsync(request.EmployeeId, userInfo.EmpId, true);

                if (existing == null || existing.EmployeeId <= 0)
                    return Json(new { Success = false, Message = "Employee not found" });

                var employeeRequest = new EmployeeRequest
                {
                    EmployeeId = request.EmployeeId,
                    EmployeeCode = request.EmployeeCode,
                    EmployeeName = request.EmployeeName,
                    Email = existing.Email,
                    Phone = existing.Phone,
                    Designation = request.Designation,
                    RoleTypeId = existing.RoleTypeId,
                    PrimaryDepartmentId = existing.PrimaryDepartmentId,
                    DateOfJoining = request.DateOfJoining ?? existing.DateOfJoining,
                    CreatedBy = userId.Value,
                    IsActive = request.IsActive
                };

                var result = await _hierarchyService.UpdateEmployeeAsync(employeeRequest);
                return Json(new { Success = result.Success, Message = result.Message });
            }
            catch (Exception ex)
            {
                return Json(new { Success = false, Message = ex.Message });
            }
        }

        #endregion

        #region Employee List & Details

        /// <summary>URL: /HierarchyWeb/Employees</summary>
        public async Task<IActionResult> Employees(string searchTerm = null, int? roleTypeId = null, int? departmentId = null)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            var empCode = userInfo.EmpId;
            var isAdmin = IsAdmin(userInfo.RoleName);

            SetViewBagData(empCode, userInfo.RoleName, userInfo.FirstName, userInfo.LastName, isAdmin);

            try
            {
                var request = new EmployeeSearchRequest
                {
                    SearchTerm = searchTerm,
                    RoleTypeId = roleTypeId,
                    DepartmentId = departmentId,
                    IsActive = true,
                    PageNumber = 1,
                    PageSize = 100
                };

                var employees = await _hierarchyService.GetEmployeesAsync(request, empCode, isAdmin);
                ViewBag.Departments = await _hierarchyService.GetDepartmentsAsync();
                ViewBag.RoleTypes = await _hierarchyService.GetRoleTypesAsync();
                ViewBag.SearchTerm = searchTerm;
                ViewBag.SelectedRoleTypeId = roleTypeId;
                ViewBag.SelectedDepartmentId = departmentId;

                return View(employees ?? new List<EmployeeDto>());
            }
            catch (Exception ex)
            {
                ViewBag.Error = ex.Message;
                return View(new List<EmployeeDto>());
            }
        }

        /// <summary>URL: /HierarchyWeb/EmployeeDetail/5</summary>
        public async Task<IActionResult> EmployeeDetail(int id)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            var empCode = userInfo.EmpId;
            var isAdmin = IsAdmin(userInfo.RoleName);

            SetViewBagData(empCode, userInfo.RoleName, userInfo.FirstName, userInfo.LastName, isAdmin);
            ViewBag.EmployeeId = id;

            try
            {
                var employee = await _hierarchyService.GetEmployeeByIdAsync(id, empCode, isAdmin);
                if (employee == null)
                {
                    TempData["Error"] = "Employee not found";
                    return RedirectToAction("Employees");
                }
                return View(employee);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Employees");
            }
        }

        /// <summary>URL: /HierarchyWeb/MyProfile</summary>
        public async Task<IActionResult> MyProfile()
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            var empCode = userInfo.EmpId;

            SetViewBagData(empCode, userInfo.RoleName, userInfo.FirstName, userInfo.LastName, false);

            if (string.IsNullOrEmpty(empCode))
            {
                TempData["Error"] = "Employee code not found for your account";
                return RedirectToAction("Index");
            }

            try
            {
                var currentEmployee = await _hierarchyService.GetEmployeeByCodeAsync(empCode);
                if (currentEmployee == null)
                {
                    TempData["Error"] = $"Employee '{empCode}' not found in hierarchy";
                    return RedirectToAction("Index");
                }

                var employee = await _hierarchyService.GetEmployeeByIdAsync(currentEmployee.EmployeeId, empCode, false);
                return View("EmployeeDetail", employee);
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Index");
            }
        }

        #endregion

        #region Create / Edit Employee (Admin Only)

        /// <summary>URL: /HierarchyWeb/CreateEmployee</summary>
        public async Task<IActionResult> CreateEmployee()
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            var isAdmin = IsAdmin(userInfo.RoleName);

            if (!isAdmin)
            {
                TempData["Error"] = "Only admins can create employees";
                return RedirectToAction("Employees");
            }

            SetViewBagData(userInfo.EmpId, userInfo.RoleName, userInfo.FirstName, userInfo.LastName, isAdmin);
            ViewBag.Departments = await _hierarchyService.GetDepartmentsAsync();
            ViewBag.RoleTypes = await _hierarchyService.GetRoleTypesAsync();

            return View(new EmployeeRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateEmployee(EmployeeRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            if (!IsAdmin(userInfo.RoleName))
            {
                TempData["Error"] = "Only admins can create employees";
                return RedirectToAction("Employees");
            }

            try
            {
                request.CreatedBy ??= userId.Value;
                var result = await _hierarchyService.CreateEmployeeAsync(request);
                TempData[result.Success ? "Success" : "Error"] = result.Message;
                if (result.Success)
                    return RedirectToAction("Employees");
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            SetViewBagData(userInfo.EmpId, userInfo.RoleName, userInfo.FirstName, userInfo.LastName, true);
            ViewBag.Departments = await _hierarchyService.GetDepartmentsAsync();
            ViewBag.RoleTypes = await _hierarchyService.GetRoleTypesAsync();
            return View(request);
        }

        /// <summary>URL: /HierarchyWeb/EditEmployee/5</summary>
        public async Task<IActionResult> EditEmployee(int id)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            var isAdmin = IsAdmin(userInfo.RoleName);

            if (!isAdmin)
            {
                TempData["Error"] = "Only admins can edit employees";
                return RedirectToAction("Employees");
            }

            try
            {
                var employee = await _hierarchyService.GetEmployeeByIdAsync(id, userInfo.EmpId, true);
                if (employee == null)
                {
                    TempData["Error"] = "Employee not found";
                    return RedirectToAction("Employees");
                }

                SetViewBagData(userInfo.EmpId, userInfo.RoleName, userInfo.FirstName, userInfo.LastName, isAdmin);
                ViewBag.Departments = await _hierarchyService.GetDepartmentsAsync();
                ViewBag.RoleTypes = await _hierarchyService.GetRoleTypesAsync();

                return View(new EmployeeRequest
                {
                    EmployeeId = employee.EmployeeId,
                    EmployeeCode = employee.EmployeeCode,
                    EmployeeName = employee.EmployeeName,
                    Email = employee.Email,
                    Phone = employee.Phone,
                    Designation = employee.Designation,
                    RoleTypeId = employee.RoleTypeId,
                    PrimaryDepartmentId = employee.PrimaryDepartmentId,
                    DateOfJoining = employee.DateOfJoining,
                    IsActive = employee.IsActive
                });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Employees");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditEmployee(EmployeeRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            if (!IsAdmin(userInfo.RoleName))
            {
                TempData["Error"] = "Only admins can edit employees";
                return RedirectToAction("Employees");
            }

            try
            {
                var result = await _hierarchyService.UpdateEmployeeAsync(request);
                TempData[result.Success ? "Success" : "Error"] = result.Message;
                if (result.Success)
                    return RedirectToAction("EmployeeDetail", new { id = request.EmployeeId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            SetViewBagData(userInfo.EmpId, userInfo.RoleName, userInfo.FirstName, userInfo.LastName, true);
            ViewBag.Departments = await _hierarchyService.GetDepartmentsAsync();
            ViewBag.RoleTypes = await _hierarchyService.GetRoleTypesAsync();
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteEmployee(int id)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            if (!IsAdmin(userInfo.RoleName))
            {
                TempData["Error"] = "Only admins can delete employees";
                return RedirectToAction("Employees");
            }

            try
            {
                var result = await _hierarchyService.DeactivateEmployeeAsync(id);
                TempData[result.Success ? "Success" : "Error"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("Employees");
        }

        #endregion

        #region Relationships (Admin Only)

        public async Task<IActionResult> AddRelationship(int employeeId)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            var isAdmin = IsAdmin(userInfo.RoleName);

            if (!isAdmin)
            {
                TempData["Error"] = "Only admins can manage relationships";
                return RedirectToAction("Employees");
            }

            try
            {
                var employee = await _hierarchyService.GetEmployeeByIdAsync(employeeId, userInfo.EmpId, true);

                SetViewBagData(userInfo.EmpId, userInfo.RoleName, userInfo.FirstName, userInfo.LastName, isAdmin);
                ViewBag.Employee = employee;
                ViewBag.Managers = await _hierarchyService.GetAvailableManagersAsync(employeeId);
                ViewBag.ReportingTypes = await _hierarchyService.GetReportingTypesAsync();
                ViewBag.Departments = await _hierarchyService.GetDepartmentsAsync();

                return View(new AddReportingRelationshipRequest { EmployeeId = employeeId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Employees");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRelationship(AddReportingRelationshipRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            if (!IsAdmin(userInfo.RoleName))
            {
                TempData["Error"] = "Only admins can manage relationships";
                return RedirectToAction("Employees");
            }

            try
            {
                var result = await _hierarchyService.AddReportingRelationshipAsync(request);
                TempData[result.Success ? "Success" : "Error"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("EmployeeDetail", new { id = request.EmployeeId });
        }

        public async Task<IActionResult> EditRelationship(int id)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            var isAdmin = IsAdmin(userInfo.RoleName);

            if (!isAdmin)
            {
                TempData["Error"] = "Only admins can manage relationships";
                return RedirectToAction("Employees");
            }

            try
            {
                var relationship = await _hierarchyService.GetRelationshipByIdAsync(id);
                if (relationship == null)
                {
                    TempData["Error"] = "Relationship not found";
                    return RedirectToAction("Employees");
                }

                SetViewBagData(userInfo.EmpId, userInfo.RoleName, userInfo.FirstName, userInfo.LastName, isAdmin);
                ViewBag.Employee = await _hierarchyService.GetEmployeeByIdAsync(relationship.EmployeeId, userInfo.EmpId, true);
                ViewBag.Managers = await _hierarchyService.GetAvailableManagersAsync(relationship.EmployeeId);
                ViewBag.ReportingTypes = await _hierarchyService.GetReportingTypesAsync();
                ViewBag.Departments = await _hierarchyService.GetDepartmentsAsync();
                ViewBag.Relationship = relationship;

                return View(new UpdateReportingRelationshipRequest
                {
                    RelationshipId = relationship.RelationshipId,
                    ReportsToEmployeeId = relationship.ReportsToEmployeeId,
                    ReportingTypeId = relationship.ReportingTypeId,
                    DepartmentId = relationship.DepartmentId,
                    IsPrimary = relationship.IsPrimary,
                    EffectiveFrom = relationship.EffectiveFrom,
                    EffectiveTo = relationship.EffectiveTo,
                    IsActive = relationship.IsActive,
                    Notes = relationship.Notes
                });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Employees");
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRelationship(UpdateReportingRelationshipRequest request)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            if (!IsAdmin(userInfo.RoleName))
            {
                TempData["Error"] = "Only admins can manage relationships";
                return RedirectToAction("Employees");
            }

            try
            {
                var result = await _hierarchyService.UpdateReportingRelationshipAsync(request);
                if (result.Success)
                {
                    TempData["Success"] = result.Message;
                    var relationship = await _hierarchyService.GetRelationshipByIdAsync(request.RelationshipId);
                    return RedirectToAction("EmployeeDetail", new { id = relationship?.EmployeeId });
                }
                TempData["Error"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("EditRelationship", new { id = request.RelationshipId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRelationship(int id, int employeeId)
        {
            var userId = GetUserId();
            if (userId == null)
                return RedirectToAction("Index", "Login");

            var userInfo = await GetUserInfoFromDatabaseAsync(userId.Value);
            if (!IsAdmin(userInfo.RoleName))
            {
                TempData["Error"] = "Only admins can manage relationships";
                return RedirectToAction("Employees");
            }

            try
            {
                var result = await _hierarchyService.RemoveReportingRelationshipAsync(id);
                TempData[result.Success ? "Success" : "Error"] = result.Message;
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("EmployeeDetail", new { id = employeeId });
        }

        #endregion

        #region AJAX

        public async Task<IActionResult> GetSubDepartments(int departmentId)
        {
            try
            {
                var subDepartments = await _hierarchyService.GetSubDepartmentsAsync(departmentId);
                return Json(subDepartments);
            }
            catch
            {
                return Json(new List<SubDepartmentDto>());
            }
        }

        #endregion
    }
}
