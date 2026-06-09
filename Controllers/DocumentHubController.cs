using JSAPNEW.Models;
using JSAPNEW.Services.Implementation;
using JSAPNEW.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace JSAPNEW.Controllers
{
    public class DocumentHubController : Controller
    {
        private readonly IDocumentHubService _service;
        private readonly IWebHostEnvironment _environment;

        public DocumentHubController(
    IDocumentHubService service,
    IWebHostEnvironment environment)
        {
            _service = service;
            _environment = environment;
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

            var permissionPayload = new
            {
                canView = permissions.CanView,
                canUpload = permissions.CanUpload,
                canDownload = permissions.CanDownload,
                canDelete = permissions.CanDelete,
                canManageFolders = permissions.CanManageFolders,
                canRename = permissions.CanRename,
                canShare = permissions.CanShare,
                roleName = permissions.RoleName
            };

            if (folderId.HasValue)
            {
                var folder = (await _service.GetFoldersAsync()).FirstOrDefault(x => x.FolderId == folderId.Value);
                if (folder != null && folder.IsConfidential)
                {
                    var isUnlocked = HttpContext.Session.GetString(FolderAccessSessionKey(folderId.Value)) == "1";
                    if (!isUnlocked)
                    {
                        return Json(new { success = true, folderLockRequired = true, folderId = folderId.Value, folderName = folder.FolderName, permissions = permissionPayload });
                    }
                }
            }

            var data = await _service.GetSnapshotAsync(folderId, filter, search);
            return Json(new { success = true, data, permissions = permissionPayload });
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

            folderName = (folderName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(folderName))
                return Json(new { success = false, message = "Folder name is required." });

            var ok = await _service.RenameFolderAsync(folderId, folderName, CurrentUserId(), CurrentUserName());
            return Json(new { success = ok, message = ok ? "Folder renamed." : "Unable to rename folder." });
        }

        [HttpPost]
        public async Task<IActionResult> DeleteFolder(int folderId)
        {
            if (!await CanDeleteAsync())
                return Forbid();

            var folderContent = await _service.GetSnapshotAsync(folderId, null, null);
            if (folderContent.Files.Any() || folderContent.Folders.Any())
                return Json(new { success = false, message = "Only empty folders can be deleted. Move or delete files and subfolders first." });

            var ok = await _service.DeleteFolderAsync(folderId, CurrentUserId(), CurrentUserName());
            return Json(new { success = ok, message = ok ? "Folder deleted." : "Unable to delete folder." });
        }

        [HttpPost]
        public async Task<IActionResult> MoveFolder(int folderId, int? targetParentFolderId)
        {
            if (!await CanManageFoldersAsync())
                return Forbid();

            var ok = await _service.MoveFolderAsync(folderId, targetParentFolderId, CurrentUserId(), CurrentUserName());
            return Json(new { success = ok, message = ok ? "Folder moved." : "Unable to move folder." });
        }

        [HttpPost]
        public async Task<IActionResult> Upload(int folderId, List<IFormFile> files, string permissionGroup, string accessLevel, string tags, bool isConfidential = false, string conflictAction = "newversion")
        {
            if (!await CanUploadAsync())
                return Forbid();

            accessLevel = accessLevel ?? "Public";
            isConfidential = isConfidential
                || accessLevel.Equals("Confidential", StringComparison.OrdinalIgnoreCase)
                || accessLevel.Equals("Highly Restricted", StringComparison.OrdinalIgnoreCase);

            var result = await _service.UploadFilesAsync(new DocumentHubUploadRequest
            {
                FolderId = folderId,
                Files = files ?? new List<IFormFile>(),
                IsConfidential = isConfidential,
                ConflictAction = conflictAction,
                PermissionGroup = permissionGroup ?? "All",
                AccessLevel = accessLevel,
                Tags = tags ?? string.Empty
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
                return Json(new { success = false, message = "File record was not found." });

            var folderIsLocked = await IsFolderLockedAsync(file.FolderId);
            var folderIsUnlocked = HttpContext.Session.GetString(FolderAccessSessionKey(file.FolderId)) == "1";
            var requiresAccess = file.IsConfidential || folderIsLocked;
            var granted = !requiresAccess
                || HttpContext.Session.GetString(AccessSessionKey(fileId)) == "1"
                || folderIsUnlocked;

            return Json(new { success = true, isConfidential = requiresAccess, accessGranted = granted, file });
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
        public async Task<IActionResult> Preview(int fileId, bool download = false)
        {
            var file = await GetAccessibleFileAsync(fileId, "View");

            if (file == null)
                return Unauthorized();

            var path = PhysicalPath(file.StoredFileName);

            if (!System.IO.File.Exists(path))
                return await MissingPhysicalFileResultAsync(file, download ? "Download" : "View");

            var contentType = GetPreviewContentType(file);
            Response.Headers["Content-Disposition"] = $"{(download ? "attachment" : "inline")}; filename=\"{SafeHeaderFileName(file.FileName)}\"";
            return PhysicalFile(path, contentType);
        }

        [HttpGet]
        public async Task<IActionResult> Download(int fileId)
        {
            if (!await CanDownloadAsync())
                return Forbid();

            var file = await GetAccessibleFileAsync(fileId, "Download");
            if (file == null)
                return Unauthorized();

            var path = PhysicalPath(file.StoredFileName);
            if (!System.IO.File.Exists(path))
                return await MissingPhysicalFileResultAsync(file, "Download");

            return PhysicalFile(path, file.ContentType, file.FileName);
        }

        [HttpPost]
        public async Task<IActionResult> RenameFile(int fileId, string fileName)
        {
            if (!await CanRenameAsync())
                return Forbid();

            fileName = Path.GetFileName(fileName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(fileName))
                return Json(new { success = false, message = "File name is required." });

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

        [HttpPost]
        public async Task<IActionResult> MoveFile(int fileId, int targetFolderId)
        {
            if (!await CanManageFoldersAsync())
                return Forbid();

            var ok = await _service.MoveFileAsync(fileId, targetFolderId, CurrentUserId(), CurrentUserName());
            return Json(new { success = ok, message = ok ? "File moved." : "Unable to move file." });
        }

        [HttpGet]
        public async Task<IActionResult> VersionHistory(int fileId)
        {
            if (!IsLoggedIn())
                return Unauthorized();

            return Json(new { success = true, data = await _service.GetVersionHistoryAsync(fileId) });
        }

        [HttpPost]
        public async Task<IActionResult> RestoreVersion(int fileId, int versionNumber)
        {
            if (!await CanRenameAsync())
                return Forbid();

            var ok = await _service.RestoreVersionAsync(fileId, versionNumber, CurrentUserId(), CurrentUserName());
            return Json(new { success = ok, message = ok ? "Version restored." : "Unable to restore version." });
        }

        [HttpPost]
        public async Task<IActionResult> ConfirmFolderAccess(int folderId, string? pin)
        {
            if (!IsLoggedIn())
                return Unauthorized();

            var ok = await _service.ValidateFolderPinAsync(folderId, pin);
            if (ok)
                HttpContext.Session.SetString(FolderAccessSessionKey(folderId), "1");

            return Json(new { success = ok, message = ok ? "Access granted." : "Incorrect PIN." });
        }

        [HttpPost]
        public async Task<IActionResult> SetFolderPin([FromBody] DocumentHubFolderPinRequest request)
        {
            if (!await CanDeleteAsync())
                return Forbid();

            var pin = request.NewPin?.Trim();
            if (string.IsNullOrWhiteSpace(pin) || pin.Length != 4 || pin != request.ConfirmPin?.Trim())
                return Json(new { success = false, message = "PIN confirmation does not match." });

            var ok = await _service.SetFolderPinAsync(request.FolderId, pin, CurrentUserId(), CurrentUserName());
            return Json(new { success = ok, message = ok ? "Folder PIN saved." : "Unable to save folder PIN." });
        }

        [HttpPost]
        public async Task<IActionResult> ChangeFolderPin([FromBody] DocumentHubFolderPinRequest request)
        {
            if (!await CanDeleteAsync())
                return Forbid();

            var currentPin = request.CurrentPin?.Trim() ?? string.Empty;
            var newPin = request.NewPin?.Trim() ?? string.Empty;
            if (newPin.Length != 4 || newPin != request.ConfirmPin?.Trim())
                return Json(new { success = false, message = "New PIN confirmation does not match." });

            var ok = await _service.ChangeFolderPinAsync(request.FolderId, currentPin, newPin, CurrentUserId(), CurrentUserName());
            return Json(new { success = ok, message = ok ? "Folder PIN changed." : "Current PIN is incorrect." });
        }

        [HttpPost]
        public async Task<IActionResult> RemoveFolderPin([FromBody] DocumentHubFolderPinRequest request)
        {
            if (!await CanDeleteAsync())
                return Forbid();

            var ok = await _service.RemoveFolderPinAsync(request.FolderId, request.CurrentPin?.Trim() ?? string.Empty, CurrentUserId(), CurrentUserName());
            return Json(new { success = ok, message = ok ? "Folder PIN removed." : "Current PIN is incorrect." });
        }

        [HttpGet]
        public async Task<IActionResult> ActivityLog(int? fileId)
        {
            if (!IsLoggedIn())
                return Unauthorized();

            return Json(new { success = true, data = await _service.GetActivityLogAsync(fileId) });
        }

        [HttpGet]
        public async Task<IActionResult> GetBackups()
        {
            try
            {
                if (!await CanManageFoldersAsync())
                    return Forbid();

                var root = BackupRoot();
                if (!Directory.Exists(root))
                    return Json(Array.Empty<DocumentHubBackupSummaryDto>());

                var backups = Directory.GetFiles(root, "*.json")
                    .Select(ReadBackupSummary)
                    .Where(x => x != null)
                    .OrderByDescending(x => x!.BackupDate)
                    .ToList();

                return Json(backups);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetBackupContent(string backupId)
        {
            try
            {
                if (!await CanManageFoldersAsync())
                    return Forbid();

                var backup = ReadBackup(backupId);
                if (backup == null)
                    return Json(new { success = false, message = "Backup was not found or could not be read." });

                return Json(new { success = true, data = backup });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateBackup(string type = "Full")
        {
            try
            {
                if (!await CanManageFoldersAsync())
                    return Forbid();

                Directory.CreateDirectory(BackupRoot());
                var snapshot = await _service.GetSnapshotAsync(null, null, null);
                var backupId = $"DH-{DateTime.UtcNow:yyyyMMddHHmmss}";
                var versions = new List<DocumentHubVersionDto>();
                foreach (var file in snapshot.Files)
                    versions.AddRange(await _service.GetVersionHistoryAsync(file.FileId));

                var backup = new DocumentHubBackupDto
                {
                    BackupId = backupId,
                    BackupType = string.IsNullOrWhiteSpace(type) ? "Full" : type,
                    BackupDate = DateTime.UtcNow,
                    Status = "Completed",
                    Folders = await _service.GetFoldersAsync(),
                    Files = snapshot.Files,
                    Versions = versions,
                    Activities = await _service.GetActivityLogAsync()
                };
                backup.PhysicalFiles = CopyBackupPhysicalFiles(backup.BackupId, backup.Files, backup.Versions);

                await System.IO.File.WriteAllTextAsync(BackupPath(backup.BackupId), JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true }));
                return Json(new { success = true, message = "Backup created.", backupId = backup.BackupId });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public async Task<IActionResult> RestoreBackup(string backupId)
        {
            if (!await CanManageFoldersAsync())
                return Forbid();

            var backup = ReadBackup(backupId);
            if (backup == null)
                return Json(new { success = false, message = "Backup was not found." });

            RestoreBackupPhysicalFiles(
                backup,
                backup.Files.Select(x => x.StoredFileName).Concat(backup.Versions.Select(x => x.StoredFileName)));

            var ok = await _service.RestoreBackupAsync(backup, CurrentUserId(), CurrentUserName());
            return Json(new { success = ok, message = ok ? "Backup restored." : "Unable to restore backup." });
        }

        [HttpPost]
        public async Task<IActionResult> RestoreSingleFolder(string backupId, int folderId)
        {
            if (!await CanManageFoldersAsync())
                return Forbid();

            var backup = ReadBackup(backupId);
            if (backup == null)
                return Json(new { success = false, message = "Backup was not found." });

            var folderIds = GetBackupFolderScope(backup.Folders, folderId);
            var fileIds = backup.Files.Where(x => folderIds.Contains(x.FolderId)).Select(x => x.FileId).ToHashSet();
            RestoreBackupPhysicalFiles(
                backup,
                backup.Files.Where(x => fileIds.Contains(x.FileId)).Select(x => x.StoredFileName)
                    .Concat(backup.Versions.Where(x => fileIds.Contains(x.FileId)).Select(x => x.StoredFileName)));

            var ok = await _service.RestoreBackupFolderAsync(backup, folderId, CurrentUserId(), CurrentUserName());
            return Json(new { success = ok, message = ok ? "Folder restored from backup." : "Unable to restore folder." });
        }

        [HttpPost]
        public async Task<IActionResult> RestoreSingleFile(string backupId, int fileId)
        {
            if (!await CanManageFoldersAsync())
                return Forbid();

            var backup = ReadBackup(backupId);
            if (backup == null)
                return Json(new { success = false, message = "Backup was not found." });

            var file = backup.Files.FirstOrDefault(x => x.FileId == fileId);
            if (file == null)
                return Json(new { success = false, message = "File was not found in this backup." });

            RestoreBackupPhysicalFiles(
                backup,
                new[] { file.StoredFileName }.Concat(backup.Versions.Where(x => x.FileId == fileId).Select(x => x.StoredFileName)));

            var ok = await _service.RestoreBackupFileAsync(backup, fileId, CurrentUserId(), CurrentUserName());
            return Json(new { success = ok, message = ok ? "File restored from backup." : "Unable to restore file." });
        }

        private async Task<DocumentHubFileDto?> GetAccessibleFileAsync(int fileId, string action)
        {
            if (!IsLoggedIn())
                return null;

            var file = await _service.GetFileAsync(fileId);
            if (file == null)
                return null;

            var folderIsLocked = await IsFolderLockedAsync(file.FolderId);
            var hasFileAccess = HttpContext.Session.GetString(AccessSessionKey(fileId)) == "1";
            var hasFolderAccess = HttpContext.Session.GetString(FolderAccessSessionKey(file.FolderId)) == "1";

            if ((file.IsConfidential || folderIsLocked) && !hasFileAccess && !hasFolderAccess)
                return null;

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            await _service.LogActivityAsync(fileId, file.FolderId, CurrentUserId(), CurrentUserName(), action, file.FileName, ip);
            return file;
        }

        private async Task<DocumentHubPermissionDto> GetCurrentPermissionsAsync()
        {
            return await _service.GetPermissionsAsync(CurrentUserId(), HttpContext.Session.GetInt32("selectedCompanyId") ?? 1);
        }

        private async Task<bool> CanUploadAsync() => IsLoggedIn() && (await GetCurrentPermissionsAsync()).CanUpload;

        private async Task<bool> CanDeleteAsync() => IsLoggedIn() && (await GetCurrentPermissionsAsync()).CanDelete;

        private async Task<bool> CanManageFoldersAsync() => IsLoggedIn() && (await GetCurrentPermissionsAsync()).CanManageFolders;

        private async Task<bool> CanRenameAsync() => IsLoggedIn() && (await GetCurrentPermissionsAsync()).CanRename;

        private async Task<bool> CanDownloadAsync() => IsLoggedIn() && (await GetCurrentPermissionsAsync()).CanDownload;

        private bool IsLoggedIn() => CurrentUserId() > 0;

        private int CurrentUserId() => HttpContext.Session.GetInt32("userId") ?? 0;

        private string CurrentUserName() => HttpContext.Session.GetString("username") ?? HttpContext.Session.GetString("userName") ?? "Unknown";

        private static string AccessSessionKey(int fileId) => $"DocumentHubAccess_{fileId}";

        private static string FolderAccessSessionKey(int folderId) => $"DocumentHubFolderAccess_{folderId}";

        private async Task<bool> IsFolderLockedAsync(int folderId)
        {
            var folder = (await _service.GetFoldersAsync()).FirstOrDefault(x => x.FolderId == folderId);
            return folder?.IsConfidential == true;
        }


        private string BackupRoot() =>
      Path.Combine(
          _environment.WebRootPath,
          "uploads",
          "documenthub-backups");

        private string BackupPath(string backupId) => Path.Combine(BackupRoot(), $"{Path.GetFileNameWithoutExtension(backupId)}.json");

        private string BackupFilesRoot(string backupId) => Path.Combine(BackupRoot(), Path.GetFileNameWithoutExtension(backupId), "files");

        private DocumentHubBackupSummaryDto? ReadBackupSummary(string path)
        {
            try
            {
                var backup = JsonSerializer.Deserialize<DocumentHubBackupDto>(System.IO.File.ReadAllText(path), new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                if (backup == null)
                    return null;

                return new DocumentHubBackupSummaryDto
                {
                    BackupId = backup.BackupId,
                    BackupType = backup.BackupType,
                    BackupDate = backup.BackupDate,
                    FileSize = new FileInfo(path).Length + BackupDirectorySize(backup.BackupId),
                    Status = backup.Status,
                    FolderCount = backup.Folders.Count,
                    FileCount = backup.Files.Count
                };
            }
            catch
            {
                return null;
            }
        }

        private DocumentHubBackupDto? ReadBackup(string backupId)
        {
            try
            {
                var path = BackupPath(backupId);
                if (!System.IO.File.Exists(path))
                    return null;

                return JsonSerializer.Deserialize<DocumentHubBackupDto>(
                    System.IO.File.ReadAllText(path),
                    new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            }
            catch
            {
                return null;
            }
        }


        private string UploadRoot() =>
    Path.Combine(
        _environment.WebRootPath,
        "uploads",
        "documenthub");

        private string PhysicalPath(string storedFileName) => Path.Combine(UploadRoot(), Path.GetFileName(storedFileName ?? string.Empty));

        private async Task<IActionResult> MissingPhysicalFileResultAsync(DocumentHubFileDto file, string action)
        {
            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";
            await _service.LogActivityAsync(file.FileId, file.FolderId, CurrentUserId(), CurrentUserName(), "Missing Physical File", $"{action}: {file.FileName}", ip);

            if (TryRestorePhysicalFileFromBackup(file.StoredFileName))
            {
                var restoredPath = PhysicalPath(file.StoredFileName);
                await _service.LogActivityAsync(file.FileId, file.FolderId, CurrentUserId(), CurrentUserName(), "Recovered Physical File", $"{action}: {file.FileName}", ip);

                if (string.Equals(action, "Download", StringComparison.OrdinalIgnoreCase))
                    return PhysicalFile(restoredPath, string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType, file.FileName);

                Response.Headers["Content-Disposition"] = $"inline; filename=\"{SafeHeaderFileName(file.FileName)}\"";
                return PhysicalFile(restoredPath, GetPreviewContentType(file));
            }

            var message = $@"
<!doctype html>
<html>
<head>
    <meta charset=""utf-8"" />
    <title>Document unavailable</title>
    <style>
        body {{ font-family: Arial, sans-serif; background:#f8fafc; color:#0f172a; padding:32px; }}
        .box {{ max-width:620px; margin:40px auto; background:#fff; border:1px solid #e2e8f0; border-radius:10px; padding:24px; box-shadow:0 10px 30px rgba(15,23,42,.08); }}
        h1 {{ font-size:20px; margin:0 0 10px; }}
        p {{ margin:8px 0; color:#475569; line-height:1.5; }}
        code {{ background:#f1f5f9; padding:2px 5px; border-radius:4px; }}
    </style>
</head>
<body>
    <div class=""box"">
        <h1>Document unavailable</h1>
        <p>The file record exists, but the physical uploaded file is not present in application storage.</p>
        <p>File: <code>{System.Net.WebUtility.HtmlEncode(file.FileName)}</code></p>
        <p>Please restore it from a Document Hub backup or upload the document again.</p>
    </div>
</body>
</html>";

            return Content(message, "text/html");
        }

        private bool TryRestorePhysicalFileFromBackup(string storedFileName)
        {
            try
            {
                var safeStoredFileName = Path.GetFileName(storedFileName ?? string.Empty);
                if (string.IsNullOrWhiteSpace(safeStoredFileName) || !Directory.Exists(BackupRoot()))
                    return false;

                var backupFile = Directory
                    .EnumerateFiles(BackupRoot(), safeStoredFileName, SearchOption.AllDirectories)
                    .OrderByDescending(System.IO.File.GetLastWriteTimeUtc)
                    .FirstOrDefault();

                if (string.IsNullOrWhiteSpace(backupFile))
                    return false;

                Directory.CreateDirectory(UploadRoot());
                System.IO.File.Copy(backupFile, PhysicalPath(safeStoredFileName), true);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private List<DocumentHubBackupFileDto> CopyBackupPhysicalFiles(string backupId, IEnumerable<DocumentHubFileDto> files, IEnumerable<DocumentHubVersionDto> versions)
        {
            var backupFiles = new List<DocumentHubBackupFileDto>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            Directory.CreateDirectory(BackupFilesRoot(backupId));

            foreach (var storedFileName in files.Select(f => f.StoredFileName).Concat(versions.Select(v => v.StoredFileName)))
            {
                if (string.IsNullOrWhiteSpace(storedFileName) || !seen.Add(storedFileName))
                    continue;

                var source = PhysicalPath(storedFileName);
                var destinationName = Path.GetFileName(storedFileName);
                var destination = Path.Combine(BackupFilesRoot(backupId), destinationName);
                var exists = System.IO.File.Exists(source);
                long size = 0;

                if (exists)
                {
                    System.IO.File.Copy(source, destination, true);
                    size = new FileInfo(destination).Length;
                }

                backupFiles.Add(new DocumentHubBackupFileDto
                {
                    StoredFileName = storedFileName,
                    BackupRelativePath = Path.Combine(Path.GetFileNameWithoutExtension(backupId), "files", destinationName),
                    FileSize = size,
                    Exists = exists
                });
            }

            return backupFiles;
        }

        private int RestoreBackupPhysicalFiles(DocumentHubBackupDto backup, IEnumerable<string> storedFileNames)
        {
            var restored = 0;
            var names = storedFileNames
                .Select(x => Path.GetFileName(x ?? string.Empty))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (!names.Any())
                return restored;

            Directory.CreateDirectory(UploadRoot());

            foreach (var storedFileName in names)
            {
                var source = FindBackupPhysicalFile(backup, storedFileName);
                if (string.IsNullOrWhiteSpace(source) || !System.IO.File.Exists(source))
                    continue;

                System.IO.File.Copy(source, PhysicalPath(storedFileName), true);
                restored++;
            }

            return restored;
        }

        private string? FindBackupPhysicalFile(DocumentHubBackupDto backup, string storedFileName)
        {
            var physicalFile = backup.PhysicalFiles.FirstOrDefault(x =>
                Path.GetFileName(x.StoredFileName).Equals(storedFileName, StringComparison.OrdinalIgnoreCase)
                || Path.GetFileName(x.BackupRelativePath).Equals(storedFileName, StringComparison.OrdinalIgnoreCase));

            if (physicalFile != null && !string.IsNullOrWhiteSpace(physicalFile.BackupRelativePath))
            {
                var path = Path.Combine(BackupRoot(), physicalFile.BackupRelativePath);
                if (System.IO.File.Exists(path))
                    return path;
            }

            var defaultPath = Path.Combine(BackupFilesRoot(backup.BackupId), storedFileName);
            if (System.IO.File.Exists(defaultPath))
                return defaultPath;

            return Directory.Exists(BackupRoot())
                ? Directory.EnumerateFiles(BackupRoot(), storedFileName, SearchOption.AllDirectories).FirstOrDefault()
                : null;
        }

        private static HashSet<int> GetBackupFolderScope(IEnumerable<DocumentHubFolderDto> folders, int folderId)
        {
            var folderList = folders.ToList();
            var result = new HashSet<int> { folderId };
            var added = true;

            while (added)
            {
                added = false;
                foreach (var folder in folderList)
                {
                    if (folder.ParentFolderId.HasValue && result.Contains(folder.ParentFolderId.Value) && result.Add(folder.FolderId))
                        added = true;
                }
            }

            return result;
        }

        private long BackupDirectorySize(string backupId)
        {
            var root = BackupFilesRoot(backupId);
            return Directory.Exists(root)
                ? Directory.GetFiles(root, "*", SearchOption.AllDirectories).Sum(x => new FileInfo(x).Length)
                : 0;
        }

        private static string GetPreviewContentType(DocumentHubFileDto file)
        {
            return (file.FileType ?? string.Empty).TrimStart('.').ToLowerInvariant() switch
            {
                "doc" => "application/msword",
                "docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "xls" => "application/vnd.ms-excel",
                "xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "csv" => "text/csv",
                _ => string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType
            };
        }

        private static string SafeHeaderFileName(string fileName)
        {
            var safeName = Path.GetFileName(fileName ?? "document");
            return safeName.Replace("\"", "'");
        }

        //private static bool CanPreview(string fileType)
        //{
        //    return fileType.Equals("PDF", StringComparison.OrdinalIgnoreCase)
        //        || fileType.Equals("PNG", StringComparison.OrdinalIgnoreCase)
        //        || fileType.Equals("JPG", StringComparison.OrdinalIgnoreCase)
        //        || fileType.Equals("JPEG", StringComparison.OrdinalIgnoreCase);
        //}
        [HttpPost]
        public async Task<IActionResult> ToggleFavorite(int fileId)
        {
            if (!await CanRenameAsync())
                return Forbid();

            var ok = await _service.ToggleFavoriteAsync(fileId, CurrentUserId(), CurrentUserName());
            return Json(new { success = ok, message = ok ? "Favorite updated." : "Unable to update favorite." });
        }

        [HttpPost]
        public async Task<IActionResult> ResetFolderPin([FromBody] DocumentHubFolderPinRequest request)
        {
            if (!await CanDeleteAsync())
                return Forbid();

            var result = await _service.ResetFolderPinAsync(
                request.FolderId,
                CurrentUserId(),
                CurrentUserName());

            return Json(new
            {
                success = result,
                message = result
                    ? "Folder PIN reset. Set a new PIN when needed."
                    : "Unable to reset folder PIN."
            });
        }

    }

}
