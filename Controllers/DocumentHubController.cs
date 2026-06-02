using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Controllers
{
    public class DocumentHubController : Controller
    {
        private readonly IDocumentHubService _service;

        public DocumentHubController(IConfiguration configuration, IWebHostEnvironment environment)
        {
            _service = new DocumentHubService(configuration, environment);
        }

        public IActionResult Index() => RedirectToAction(nameof(DocumentHubPage));

        public async Task<IActionResult> DocumentHubPage()
        {
            if (!IsLoggedIn())
                return RedirectToAction("Index", "Login");

            var permissions = await GetCurrentPermissionsAsync();
            if (!permissions.CanView)
                return RedirectToAction("Index", "DashboardWeb");

            ViewBag.DocumentHubPermissions = permissions;
            return View("~/Views/Documenthub/DocumentHubPage.cshtml");
        }

        [HttpGet]
        public async Task<IActionResult> GetHub(int? folderId, string? filter, string? search)
        {
            if (!IsLoggedIn())
                return Unauthorized();

            var permissions = await GetCurrentPermissionsAsync();
            if (!permissions.CanView)
                return Forbid();

            var data = await _service.GetSnapshotAsync(folderId, filter, search);
            return Json(new { success = true, data, permissions });
        }

        [HttpGet]
        public async Task<IActionResult> GetFolders()
        {
            if (!IsLoggedIn())
                return Unauthorized();

            return Json(await _service.GetFoldersAsync());
        }

        [HttpPost]
        public async Task<IActionResult> CreateFolder([FromBody] DocumentHubFolderRequest request)
        {
            if (!await CanManageFoldersAsync())
                return Forbid();

            var folder = await _service.CreateFolderAsync(request, CurrentUserId(), CurrentUserName());
            return Json(new { success = folder != null, data = folder, message = folder == null ? "Folder name is required." : "Folder created." });
        }

        [HttpPost]
        public async Task<IActionResult> RenameFolder(int folderId, string folderName)
        {
            if (!await CanManageFoldersAsync())
                return Forbid();

            var ok = await _service.RenameFolderAsync(folderId, folderName, CurrentUserId(), CurrentUserName());
            return Json(new { success = ok, message = ok ? "Folder renamed." : "Unable to rename folder." });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFolder(int folderId)
        {
            if (!await CanDeleteAsync())
                return Forbid();

            var ok = await _service.DeleteFolderAsync(folderId, CurrentUserId(), CurrentUserName());
            return Json(new { success = ok, message = ok ? "Folder deleted." : "Unable to delete folder." });
        }

        [HttpPost]
        public async Task<IActionResult> Upload(int folderId, List<IFormFile> files, bool isConfidential = false, string conflictAction = "newversion")
        {
            if (!await CanUploadAsync())
                return Forbid();

            var result = await _service.UploadFilesAsync(new DocumentHubUploadRequest
            {
                FolderId = folderId,
                Files = files ?? new List<IFormFile>(),
                IsConfidential = isConfidential,
                ConflictAction = conflictAction
            }, CurrentUserId(), CurrentUserName());

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> FileAccessInfo(int fileId)
        {
            if (!IsLoggedIn())
                return Unauthorized();

            var file = await _service.GetFileAsync(fileId);
            if (file == null)
                return NotFound();

            var granted = !file.IsConfidential || HttpContext.Session.GetString(AccessSessionKey(fileId)) == "1";
            return Json(new { success = true, isConfidential = file.IsConfidential, accessGranted = granted, file });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmAccess(int fileId, string? usernameConfirmation, string? pin)
        {
            if (!IsLoggedIn())
                return Unauthorized();

            var ok = await _service.ValidateConfidentialAccessAsync(fileId, CurrentUserId(), CurrentUserName(), usernameConfirmation, pin);
            if (ok)
                HttpContext.Session.SetString(AccessSessionKey(fileId), "1");

            return Json(new { success = ok, message = ok ? "Access confirmed." : "Access confirmation failed." });
        }

        [HttpGet]
        public async Task<IActionResult> Preview(int fileId)
        {
            var file = await GetAccessibleFileAsync(fileId, "View");
            if (file == null)
                return Unauthorized();

            var path = PhysicalPath(file.StoredFileName);
            if (!CanPreview(file.FileType))
                return RedirectToAction(nameof(Download), new { fileId });

            if (!System.IO.File.Exists(path))
                return MockPreview(file);

            return PhysicalFile(path, file.ContentType);
        }

        [HttpGet]
        public async Task<IActionResult> Download(int fileId)
        {
            var file = await GetAccessibleFileAsync(fileId, "Download");
            if (file == null)
                return Unauthorized();

            var path = PhysicalPath(file.StoredFileName);
            if (!System.IO.File.Exists(path))
                return MockDownload(file);

            return PhysicalFile(path, file.ContentType, file.FileName);
        }

        [HttpPost]
        public async Task<IActionResult> RenameFile(int fileId, string fileName)
        {
            if (!await CanUploadAsync())
                return Forbid();

            var ok = await _service.RenameFileAsync(fileId, fileName, CurrentUserId(), CurrentUserName());
            return Json(new { success = ok, message = ok ? "File renamed." : "Unable to rename file." });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFile(int fileId)
        {
            if (!await CanDeleteAsync())
                return Forbid();

            var ok = await _service.DeleteFileAsync(fileId, CurrentUserId(), CurrentUserName());
            return Json(new { success = ok, message = ok ? "File deleted." : "Unable to delete file." });
        }

        [HttpGet]
        public async Task<IActionResult> VersionHistory(int fileId)
        {
            if (!IsLoggedIn())
                return Unauthorized();

            return Json(new { success = true, data = await _service.GetVersionHistoryAsync(fileId) });
        }

        [HttpGet]
        public async Task<IActionResult> ActivityLog(int? fileId)
        {
            if (!IsLoggedIn())
                return Unauthorized();

            return Json(new { success = true, data = await _service.GetActivityLogAsync(fileId) });
        }

        private async Task<DocumentHubFileDto?> GetAccessibleFileAsync(int fileId, string action)
        {
            if (!IsLoggedIn())
                return null;

            var file = await _service.GetFileAsync(fileId);
            if (file == null)
                return null;

            if (file.IsConfidential && HttpContext.Session.GetString(AccessSessionKey(fileId)) != "1")
                return null;

            await _service.LogActivityAsync(fileId, file.FolderId, CurrentUserId(), CurrentUserName(), action, file.FileName);
            return file;
        }

        private async Task<DocumentHubPermissionDto> GetCurrentPermissionsAsync()
        {
            return await _service.GetPermissionsAsync(CurrentUserId(), HttpContext.Session.GetInt32("selectedCompanyId") ?? 1);
        }

        private async Task<bool> CanUploadAsync() => IsLoggedIn() && (await GetCurrentPermissionsAsync()).CanUpload;

        private async Task<bool> CanDeleteAsync() => IsLoggedIn() && (await GetCurrentPermissionsAsync()).CanDelete;

        private async Task<bool> CanManageFoldersAsync() => IsLoggedIn() && (await GetCurrentPermissionsAsync()).CanManageFolders;

        private bool IsLoggedIn() => CurrentUserId() > 0;

        private int CurrentUserId() => HttpContext.Session.GetInt32("userId") ?? 0;

        private string CurrentUserName() => HttpContext.Session.GetString("username") ?? HttpContext.Session.GetString("userName") ?? "Unknown";

        private static string AccessSessionKey(int fileId) => $"DocumentHubAccess_{fileId}";

        private string PhysicalPath(string storedFileName) => Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "documenthub", storedFileName);

        private static bool CanPreview(string fileType)
        {
            return fileType.Equals("PDF", StringComparison.OrdinalIgnoreCase)
                || fileType.Equals("PNG", StringComparison.OrdinalIgnoreCase)
                || fileType.Equals("JPG", StringComparison.OrdinalIgnoreCase)
                || fileType.Equals("JPEG", StringComparison.OrdinalIgnoreCase);
        }

        private IActionResult MockPreview(DocumentHubFileDto file)
        {
            if (file.FileType.Equals("PDF", StringComparison.OrdinalIgnoreCase))
            {
                var html = $@"
<!doctype html>
<html>
<head><meta charset='utf-8'><title>{System.Net.WebUtility.HtmlEncode(file.FileName)}</title></head>
<body style='margin:0;font-family:Segoe UI,Arial;background:#f8fafc;color:#0f172a'>
<div style='height:100vh;display:flex;align-items:center;justify-content:center'>
  <div style='width:72%;max-width:760px;background:white;border:1px solid #e2e8f0;box-shadow:0 18px 60px rgba(15,23,42,.12);padding:48px'>
    <div style='color:#dc2626;font-size:42px;margin-bottom:16px'>PDF</div>
    <h1 style='font-size:28px;margin:0 0 12px'>{System.Net.WebUtility.HtmlEncode(file.FileName)}</h1>
    <p style='font-size:15px;line-height:1.6;color:#475569'>Mock Document Hub preview for test mode. Uploaded physical PDFs will render directly in this viewer.</p>
    <p style='font-size:13px;color:#64748b'>Version {file.VersionNumber} • {file.UploadedBy} • {file.UploadedDate:dd-MMM-yyyy}</p>
  </div>
</div>
</body>
</html>";
                return Content(html, "text/html");
            }

            var svg = $@"
<svg xmlns='http://www.w3.org/2000/svg' width='1200' height='720' viewBox='0 0 1200 720'>
  <rect width='1200' height='720' fill='#eef6ff'/>
  <rect x='210' y='140' width='780' height='440' rx='24' fill='#ffffff' stroke='#cbd5e1'/>
  <text x='600' y='310' text-anchor='middle' font-family='Segoe UI, Arial' font-size='42' font-weight='700' fill='#0b2c4d'>Image Preview</text>
  <text x='600' y='370' text-anchor='middle' font-family='Segoe UI, Arial' font-size='24' fill='#475569'>{System.Security.SecurityElement.Escape(file.FileName)}</text>
</svg>";
            return Content(svg, "image/svg+xml");
        }

        private IActionResult MockDownload(DocumentHubFileDto file)
        {
            var content = $"Document Hub test-mode file\r\n\r\nName: {file.FileName}\r\nType: {file.FileType}\r\nVersion: {file.VersionNumber}\r\nUploaded By: {file.UploadedBy}\r\nUploaded Date: {file.UploadedDate:O}\r\n";
            return File(System.Text.Encoding.UTF8.GetBytes(content), file.ContentType, file.FileName);
        }
    }
}
