
using System.Data;
using Microsoft.Data.SqlClient;
using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;

namespace JSAPNEW.Services.Implementation
{
    public class DocumentHubService : IDocumentHubService
    {

        private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".csv", ".png", ".jpg", ".jpeg"
        };

        private readonly string _connectionString;
        private readonly string _identityConnectionString;
        private readonly IWebHostEnvironment _environment;

        public DocumentHubService(
    IConfiguration config,
    IWebHostEnvironment environment)
        {
            _environment = environment;
            _connectionString =
                config.GetConnectionString("FHConnection") ?? string.Empty;
            _identityConnectionString =
                config.GetConnectionString("DefaultConnection") ?? string.Empty;
        }
        public async Task<DocumentHubPermissionDto> GetPermissionsAsync(int userId, int companyId)
        {
            var role = await GetUserRoleAsync(userId);
            var permissions = new DocumentHubPermissionDto { RoleName = role };

            if (IsAdminRole(role))
            {
                permissions.CanView = true;
                permissions.CanUpload = true;
                permissions.CanDownload = true;
                permissions.CanDelete = true;
                permissions.CanManageFolders = true;
                permissions.CanRename = true;
                permissions.CanShare = true;
            }
            else if (IsPaymentMakerRole(role))
            {
                permissions.CanView = true;
                permissions.CanUpload = true;
                permissions.CanDownload = true;
                permissions.CanDelete = false;
                permissions.CanManageFolders = false;
                permissions.CanRename = false;
                permissions.CanShare = false;
            }
            //else
            //{
            //    permissions.CanView = true;
            //    permissions.CanUpload = false;
            //    permissions.CanDownload = false;
            //    permissions.CanDelete = false;
            //    permissions.CanManageFolders = false;
            //    permissions.CanRename = false;
            //    permissions.CanShare = false;
            //}
            else
            {
                permissions.CanView = true;
                permissions.CanUpload = true;
                permissions.CanDownload = true;
                permissions.CanDelete = (userId == 84 || userId == 135);

                permissions.CanManageFolders = true;
                permissions.CanRename = false;
                permissions.CanShare = false;
            }

            return permissions;
        }

        public async Task<List<DocumentHubFolderDto>> GetFoldersAsync()
        {
            using var con =
                new SqlConnection(_connectionString);

            var data =
                await con.QueryAsync<DocumentHubFolderDto>(
                    "DocumentHub_GetFolders",
                    commandType:
                    CommandType.StoredProcedure);

            return data.ToList();
        }

        public async Task<DocumentHubFolderDto?> CreateFolderAsync(
     DocumentHubFolderRequest request,
     int userId,
     string userName)
        {
            using var con =
                new SqlConnection(_connectionString);

            var folderId =
                await con.ExecuteScalarAsync<int>(
                    "DocumentHub_CreateFolder",
                    new
                    {
                        FolderName = request.FolderName,
                        ParentFolderId = request.ParentFolderId,
                        Department = request.Department,
                        CreatedByUserId = userId,
                        CreatedBy = userName
                    },
                    commandType:
                    CommandType.StoredProcedure);

            return new DocumentHubFolderDto
            {
                FolderId = folderId,
                FolderName = request.FolderName,
                ParentFolderId = request.ParentFolderId,
                Department = request.Department,
                CreatedDate = DateTime.Now
            };
        }

        public async Task<bool> RenameFolderAsync(
    int folderId,
    string folderName,
    int userId,
    string userName)
        {
            using var con =
                new SqlConnection(_connectionString);

            await con.ExecuteAsync(
                "DocumentHub_RenameFolder",
                new
                {
                    FolderId = folderId,
                    FolderName = folderName
                },
                commandType:
                CommandType.StoredProcedure);

            var renamed = await con.ExecuteScalarAsync<int>(@"
SELECT COUNT(1)
FROM DocumentHubFolders
WHERE FolderId = @FolderId
  AND ISNULL(IsDeleted, 0) = 0
  AND FolderName = LTRIM(RTRIM(@FolderName));",
                new { FolderId = folderId, FolderName = folderName });

            if (renamed > 0)
                await LogActivityAsync(null, folderId, userId, userName, "Rename Folder", folderName, null);

            return renamed > 0;
        }
        public async Task<bool> DeleteFolderAsync(
    int folderId,
    int userId,
    string userName)
        {
            using var con =
                new SqlConnection(_connectionString);

            await con.ExecuteAsync(
                "DocumentHub_DeleteFolder",
                new
                {
                    FolderId = folderId
                },
                commandType:
                CommandType.StoredProcedure);

            return true;
        }

        public async Task<DocumentHubUploadResultDto> UploadFilesAsync(DocumentHubUploadRequest request, int userId, string userName)
        {
            var result = new DocumentHubUploadResultDto { Success = true, Message = "Files uploaded successfully." };
            if (request.Files.Count == 0)
                return new DocumentHubUploadResultDto { Success = false, Message = "No files were selected." };

            Directory.CreateDirectory(GetUploadRoot());
            using var con = new SqlConnection(_connectionString);

            var folderExists = await con.ExecuteScalarAsync<int>(@"
SELECT COUNT(1)
FROM DocumentHubFolders
WHERE FolderId = @FolderId AND ISNULL(IsDeleted, 0) = 0;",
                new { request.FolderId });

            if (folderExists == 0)
                return new DocumentHubUploadResultDto { Success = false, Message = "Select a valid folder before uploading." };

            var uploadFileParameterNames = await GetProcedureParameterNamesAsync(con, "DocumentHub_UploadFile");

            foreach (var formFile in request.Files)
            {
                var originalName = Path.GetFileName(formFile.FileName);
                var extension = Path.GetExtension(originalName);
                if (!AllowedExtensions.Contains(extension))
                {
                    result.Conflicts.Add($"{originalName}: unsupported file type.");
                    continue;
                }

                var storedFileName = $"{Guid.NewGuid():N}{extension}";
                var fullPath = Path.Combine(GetUploadRoot(), storedFileName);
                await using (var stream = new FileStream(fullPath, FileMode.CreateNew))
                    await formFile.CopyToAsync(stream);
                var contentType =
    string.IsNullOrWhiteSpace(formFile.ContentType)
        ? GetContentType(extension)
        : formFile.ContentType;

                var fileId =
                   await con.ExecuteScalarAsync<int>(
    "DocumentHub_UploadFile",
    BuildUploadFileParameters(
        uploadFileParameterNames,
        request,
        originalName,
        storedFileName,
        contentType,
        extension.TrimStart('.').ToUpper(),
        formFile.Length,
        userId,
        userName),
    commandType: CommandType.StoredProcedure);

                result.Files.Add(new DocumentHubFileDto
                {
                    FileId = fileId,
                    FolderId = request.FolderId,
                    FileName = originalName,
                    StoredFileName = storedFileName,
                    ContentType = contentType,
                    FileType = extension.TrimStart('.').ToUpper(),
                    UploadedBy = userName,
                    UploadedByUserId = userId,
                    UploadedDate = DateTime.Now,
                    FileSize = formFile.Length,
                    VersionNumber = 1,
                    IsConfidential = request.IsConfidential,
                    PermissionGroup = request.PermissionGroup,
                    AccessLevel = request.AccessLevel,
                    Tags = request.Tags
                });


            }

            if (!result.Files.Any() && result.Conflicts.Any())
                result.Success = false;

            return result;
        }

        private static async Task<HashSet<string>> GetProcedureParameterNamesAsync(SqlConnection con, string procedureName)
        {
            var names = await con.QueryAsync<string>(@"
SELECT REPLACE(p.name, '@', '')
FROM sys.parameters p
WHERE p.object_id = OBJECT_ID(@ProcedureName);",
                new { ProcedureName = procedureName });

            return names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static DynamicParameters BuildUploadFileParameters(
            HashSet<string> procedureParameterNames,
            DocumentHubUploadRequest request,
            string fileName,
            string storedFileName,
            string contentType,
            string fileType,
            long fileSize,
            int userId,
            string userName)
        {
            var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                ["FolderId"] = request.FolderId,
                ["FileName"] = fileName,
                ["StoredFileName"] = storedFileName,
                ["ContentType"] = contentType,
                ["FileType"] = fileType,
                ["FileSize"] = fileSize,
                ["UploadedByUserId"] = userId,
                ["UploadedBy"] = userName,
                ["IsConfidential"] = request.IsConfidential,
                ["PermissionGroup"] = request.PermissionGroup,
                ["AccessLevel"] = request.AccessLevel,
                ["Tags"] = request.Tags,
                ["ConflictAction"] = request.ConflictAction
            };

            var parameters = new DynamicParameters();
            foreach (var name in procedureParameterNames)
            {
                if (values.TryGetValue(name, out var value))
                    parameters.Add(name, value);
            }

            return parameters;
        }

        public async Task<DocumentHubFileDto?> GetFileAsync(int fileId)
        {
            using var con = new SqlConnection(_connectionString);

            return await con.QueryFirstOrDefaultAsync<DocumentHubFileDto>(@"
        SELECT *
        FROM DocumentHubFiles
        WHERE FileId = @FileId
          AND ISNULL(IsDeleted,0) = 0",
                new { FileId = fileId });
        }

        public async Task<bool> RenameFileAsync(
    int fileId,
    string fileName,
    int userId,
    string userName)
        {
            using var con =
                new SqlConnection(_connectionString);

            await con.ExecuteAsync(
                "DocumentHub_RenameFile",
                new
                {
                    FileId = fileId,
                    FileName = fileName
                },
                commandType: CommandType.StoredProcedure);

            var renamed = await con.ExecuteScalarAsync<int>(@"
SELECT COUNT(1)
FROM DocumentHubFiles
WHERE FileId = @FileId
  AND ISNULL(IsDeleted, 0) = 0
  AND FileName = LTRIM(RTRIM(@FileName));",
                new { FileId = fileId, FileName = fileName });

            if (renamed > 0)
                await LogActivityAsync(fileId, null, userId, userName, "Rename File", fileName, null);

            return renamed > 0;
        }

        public async Task<bool> DeleteFileAsync(
     int fileId,
     int userId,
     string userName)
        {
            using var con =
                new SqlConnection(_connectionString);

            await con.ExecuteAsync(
                "DocumentHub_DeleteFile",
                new { FileId = fileId },
                commandType: CommandType.StoredProcedure);

            return true;
        }


        public async Task<bool> RestoreFileAsync(
    int fileId,
    int userId,
    string userName)
        {
            using var con = new SqlConnection(_connectionString);

            await con.ExecuteAsync(@"
        UPDATE DocumentHubFiles
        SET IsDeleted = 0
        WHERE FileId = @FileId",
                new { FileId = fileId });

            return true;
        }

        public async Task<bool> RestoreFolderAsync(
    int folderId,
    int userId,
    string userName)
        {
            using var con = new SqlConnection(_connectionString);

            await con.ExecuteAsync(@"
        UPDATE dbo.DocumentHubFolders
        SET
            IsDeleted = 0,
            ModifiedDate = GETDATE()
        WHERE FolderId = @FolderId",
                new
                {
                    FolderId = folderId
                });

            await LogActivityAsync(
                null,
                folderId,
                userId,
                userName,
                "Restore Folder",
                "Folder restored from trash");

            return true;
        }
        public async Task<bool> PermanentDeleteFileAsync(
            int fileId,
            int userId,
            string userName)
        {
            using var con = new SqlConnection(_connectionString);

            await con.ExecuteAsync(
                "DELETE FROM DocumentHubFileVersions WHERE FileId=@FileId",
                new { FileId = fileId });

            await con.ExecuteAsync(
                "DELETE FROM DocumentHubFiles WHERE FileId=@FileId",
                new { FileId = fileId });

            return true;
        }
        public async Task<bool> PermanentDeleteFolderAsync(
    int folderId,
    int userId,
    string userName)
        {
            using var con = new SqlConnection(_connectionString);

            var rows = await con.ExecuteAsync(@"
        DELETE FROM DocumentHubFolders
        WHERE FolderId = @FolderId",
                new { FolderId = folderId });

            return rows > 0;
        }
        public async Task<bool> MoveFileAsync(int fileId, int targetFolderId, int userId, string userName)
        {
            using var con = new SqlConnection(_connectionString);

            await con.ExecuteAsync(
                "DocumentHub_MoveFile",
                new { FileId = fileId, TargetFolderId = targetFolderId },
                commandType: CommandType.StoredProcedure);

            var moved = await con.ExecuteScalarAsync<int>(@"
SELECT COUNT(1)
FROM DocumentHubFiles f
INNER JOIN DocumentHubFolders d ON d.FolderId = f.FolderId AND ISNULL(d.IsDeleted, 0) = 0
WHERE f.FileId = @FileId
  AND f.FolderId = @TargetFolderId
  AND ISNULL(f.IsDeleted, 0) = 0;",
                new { FileId = fileId, TargetFolderId = targetFolderId });

            if (moved > 0)
                await LogActivityAsync(fileId, targetFolderId, userId, userName, "Move File", null, null);

            return moved > 0;
        }

        public async Task<bool> MoveFolderAsync(int folderId, int? targetParentFolderId, int userId, string userName)
        {
            using var con = new SqlConnection(_connectionString);

            await con.ExecuteAsync(
                "DocumentHub_MoveFolder",
                new { FolderId = folderId, TargetParentFolderId = targetParentFolderId },
                commandType: CommandType.StoredProcedure);

            return true;
        }

        public async Task<bool> ToggleFavoriteAsync(int fileId, int userId, string userName)
        {
            using var con = new SqlConnection(_connectionString);

            var affected = await con.ExecuteAsync(@"
UPDATE DocumentHubFiles
SET IsFavorite = CASE WHEN ISNULL(IsFavorite, 0) = 1 THEN 0 ELSE 1 END
WHERE FileId = @FileId AND ISNULL(IsDeleted, 0) = 0;",
                new { FileId = fileId });

            if (affected > 0)
                await LogActivityAsync(fileId, null, userId, userName, "Toggle Favorite", null, null);

            return affected > 0;
        }


        private string GetUploadRoot()
        {
            return Path.Combine(
       _environment.WebRootPath,
       "uploads",
       "documenthub");
        }

        private static string GetContentType(string extension)
        {
            return extension.ToLowerInvariant() switch
            {
                ".pdf" => "application/pdf",
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                ".xls" => "application/vnd.ms-excel",
                ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                ".csv" => "text/csv",
                _ => "application/octet-stream"
            };
        }

        private async Task<string> GetUserRoleAsync(int userId)
        {
            using var con = new SqlConnection(_identityConnectionString);

            var role = await con.QueryFirstOrDefaultAsync<string>(@"
SELECT TOP 1 r.roleName
FROM jsUserRole ur
INNER JOIN jsRole r ON ur.roleId = r.roleId
WHERE ur.userId = @UserId
ORDER BY CASE
    WHEN r.roleName IN ('Admin', 'Super User', 'Super Admin') THEN 0
    WHEN r.roleName IN ('Payment Maker', 'Maker') THEN 1
    ELSE 2
END, r.roleName;",
                new { UserId = userId });

            return string.IsNullOrWhiteSpace(role) ? "User" : role.Trim();
        }

        public async Task<DocumentHubSnapshotDto> GetSnapshotAsync(
            int? folderId,
            string? filter,
            string? search)
        {
            using var con = new SqlConnection(_connectionString);

            var normalizedFilter = (filter ?? string.Empty).Trim().ToLowerInvariant();
            var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : $"%{search.Trim()}%";

            var folders = await con.QueryAsync<DocumentHubFolderDto>(@"
SELECT
    f.FolderId,
    f.ParentFolderId,
    f.FolderName,
    f.Department,
    f.CreatedDate,
      f.ModifiedDate as LastUpdated,
    ISNULL(f.IsDeleted, 0) AS IsDeleted,
    ISNULL(f.IsConfidential, 0) AS IsConfidential,
    ISNULL(f.PinCode, '') AS PinCode,
    (SELECT COUNT(1) FROM DocumentHubFolders c WHERE c.ParentFolderId = f.FolderId AND ISNULL(c.IsDeleted, 0) = 0) AS ChildFolderCount,
    (SELECT COUNT(1) FROM DocumentHubFiles x WHERE x.FolderId = f.FolderId AND ISNULL(x.IsDeleted, 0) = 0) AS FileCount
FROM DocumentHubFolders f
WHERE ISNULL(f.IsDeleted, 0) = CASE WHEN @Filter = 'trash' THEN 1 ELSE 0 END
  AND (@Search IS NOT NULL OR ((@FolderId IS NULL AND f.ParentFolderId IS NULL) OR f.ParentFolderId = @FolderId))
  AND (@Search IS NULL OR f.FolderName LIKE @Search OR ISNULL(f.Department, '') LIKE @Search)
ORDER BY f.FolderName;",
                new { FolderId = folderId, Filter = normalizedFilter, Search = normalizedSearch });

            var files = await con.QueryAsync<DocumentHubFileDto>(@"
SELECT
    f.FileId,
    f.FolderId,
    f.FileName,
    f.StoredFileName,
    f.ContentType,
    f.FileType,
    f.FileSize,
    f.UploadedBy,
    f.UploadedByUserId,
    f.UploadedDate,
    f.VersionNumber,
    ISNULL(f.IsConfidential, 0) AS IsConfidential,
    ISNULL(f.IsDeleted, 0) AS IsDeleted,
    ISNULL(f.IsFavorite, 0) AS IsFavorite,
    ISNULL(f.Owner, f.UploadedBy) AS Owner,
    ISNULL(f.PermissionGroup, 'All') AS PermissionGroup,
    ISNULL(f.AccessLevel, 'Public') AS AccessLevel,
    ISNULL(f.Tags, '') AS Tags,
    ISNULL(d.FolderName, 'Root') AS Department
FROM DocumentHubFiles f
LEFT JOIN DocumentHubFolders d ON d.FolderId = f.FolderId
WHERE ISNULL(f.IsDeleted, 0) = CASE WHEN @Filter = 'trash' THEN 1 ELSE 0 END
  AND (@Filter <> 'recent' OR f.UploadedDate >= DATEADD(DAY, -30, GETDATE()))
  AND (@Search IS NOT NULL OR @Filter IN ('recent', 'trash') OR @FolderId IS NULL OR f.FolderId = @FolderId)
  AND (@Search IS NULL OR f.FileName LIKE @Search OR ISNULL(f.UploadedBy, '') LIKE @Search OR ISNULL(f.Tags, '') LIKE @Search)
ORDER BY f.UploadedDate DESC;",
                new { FolderId = folderId, Filter = normalizedFilter, Search = normalizedSearch });

            var breadcrumb = await GetBreadcrumbAsync(con, folderId);
            var stats = await con.QueryFirstAsync<DocumentHubStatsDto>(@"
SELECT
    (SELECT COUNT(1) FROM DocumentHubFiles WHERE ISNULL(IsDeleted, 0) = 0) AS TotalFiles,
    (SELECT COUNT(1) FROM DocumentHubFolders WHERE ISNULL(IsDeleted, 0) = 0) AS TotalFolders,
    (SELECT COUNT(1) FROM DocumentHubFiles WHERE ISNULL(IsDeleted, 0) = 0 AND UploadedDate >= DATEADD(DAY, -30, GETDATE())) AS RecentFiles,
    (SELECT COUNT(1) FROM DocumentHubFiles WHERE ISNULL(IsDeleted, 0) = 0 AND ISNULL(IsFavorite, 0) = 1) AS FavoriteFiles,
    CAST((SELECT ISNULL(SUM(FileSize), 0) FROM DocumentHubFiles WHERE ISNULL(IsDeleted, 0) = 0) AS varchar(30)) AS StorageUsed;");

            stats.StorageUsed = FormatBytes(stats.StorageUsed);

            return new DocumentHubSnapshotDto
            {
                Folders = folders.ToList(),
                Files = files.ToList(),
                Breadcrumb = breadcrumb,
                Stats = stats
            };
        }

        public async Task<List<DocumentHubVersionDto>> GetVersionHistoryAsync(int fileId)
        {
            using var con = new SqlConnection(_connectionString);

            var data = await con.QueryAsync<DocumentHubVersionDto>(@"
SELECT
    v.VersionId,
    v.FileId,
    v.VersionNumber,
    f.FileName,
    v.StoredFileName,
    f.ContentType,
    f.FileType,
    v.FileSize,
    v.UploadedBy,
    f.UploadedByUserId,
    v.UploadedDate
FROM DocumentHubFileVersions v
LEFT JOIN DocumentHubFiles f ON f.FileId = v.FileId
WHERE v.FileId = @FileId
ORDER BY v.VersionNumber DESC;",
                new { FileId = fileId });

            return data.ToList();
        }

        public async Task<bool> RestoreVersionAsync(int fileId, int versionNumber, int userId, string userName)
        {
            using var con = new SqlConnection(_connectionString);

            await con.ExecuteAsync(
                "DocumentHub_RestoreVersion",
                new { FileId = fileId, VersionNumber = versionNumber, UserId = userId, UserName = userName },
                commandType: CommandType.StoredProcedure);

            return true;
        }

        public async Task<List<DocumentHubActivityDto>> GetActivityLogAsync(int? fileId = null)
        {
            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();

            var columns = await GetTableColumnNamesAsync(con, null, "DocumentHubActivityLog");
            var userIdSelect = columns.Contains("UserId") ? "UserId" : "CAST(0 AS int) AS UserId";
            var ipAddressSelect = columns.Contains("IpAddress") ? "IpAddress" : "CAST('' AS nvarchar(100)) AS IpAddress";

            var data = await con.QueryAsync<DocumentHubActivityDto>($@"
SELECT TOP 100 ActivityId, FileId, FolderId, {userIdSelect}, UserName, Action, Details, {ipAddressSelect}, ActivityDate
FROM DocumentHubActivityLog
WHERE @FileId IS NULL OR FileId = @FileId
ORDER BY ActivityDate DESC;",
                new { FileId = fileId });

            return data.ToList();
        }

        public async Task<bool> ValidateConfidentialAccessAsync(
            int fileId,
            int userId,
            string userName,
            string? usernameConfirmation,
            string? pin)
        {
            using var con = new SqlConnection(_connectionString);

            var file = await con.QueryFirstOrDefaultAsync<DocumentHubFileDto>(
                "SELECT FileId, FolderId, UploadedBy, ISNULL(IsConfidential, 0) AS IsConfidential FROM DocumentHubFiles WHERE FileId = @FileId",
                new { FileId = fileId });

            if (file == null)
                return false;

            if (!file.IsConfidential)
                return true;

            if (!string.IsNullOrWhiteSpace(usernameConfirmation)
                && usernameConfirmation.Trim().Equals(userName.Trim(), StringComparison.OrdinalIgnoreCase))
                return true;

            if (!string.IsNullOrWhiteSpace(pin))
            {
                var folderPin = await con.QueryFirstOrDefaultAsync<string>(
                    "SELECT PinCode FROM DocumentHubFolders WHERE FolderId = @FolderId AND ISNULL(IsDeleted, 0) = 0",
                    new { file.FolderId });

                return !string.IsNullOrWhiteSpace(folderPin) && folderPin == pin.Trim();
            }

            return false;
        }

        public async Task<bool> ValidateFolderPinAsync(int folderId, string? pin)
        {
            using var con = new SqlConnection(_connectionString);
            var savedPin = await con.QueryFirstOrDefaultAsync<string>(
                "SELECT PinCode FROM DocumentHubFolders WHERE FolderId = @FolderId AND ISNULL(IsDeleted, 0) = 0",
                new { FolderId = folderId });

            return !string.IsNullOrWhiteSpace(savedPin) && savedPin == pin;
        }

        public async Task<bool> SetFolderPinAsync(int folderId, string pin, int userId, string userName)
        {
            using var con = new SqlConnection(_connectionString);
            await con.ExecuteAsync(
                "DocumentHub_SetFolderPin",
                new { FolderId = folderId, Pin = pin },
                commandType: CommandType.StoredProcedure);
            return true;
        }

        public async Task<bool> ChangeFolderPinAsync(int folderId, string currentPin, string newPin, int userId, string userName)
        {
            if (!await ValidateFolderPinAsync(folderId, currentPin))
                return false;

            return await SetFolderPinAsync(folderId, newPin, userId, userName);
        }

        public async Task<bool> RemoveFolderPinAsync(int folderId, string currentPin, int userId, string userName)
        {
            if (!await ValidateFolderPinAsync(folderId, currentPin))
                return false;

            using var con = new SqlConnection(_connectionString);
            await con.ExecuteAsync(
                "DocumentHub_RemoveFolderPin",
                new { FolderId = folderId },
                commandType: CommandType.StoredProcedure);
            return true;
        }

        public async Task LogActivityAsync(
            int? fileId,
            int? folderId,
            int userId,
            string userName,
            string action,
            string? details = null,
            string? ipAddress = null)
        {
            using var con = new SqlConnection(_connectionString);

            await con.ExecuteAsync(
                "DocumentHub_LogActivity",
                new { FileId = fileId, FolderId = folderId, UserId = userId, UserName = userName, Action = action, Details = details, IpAddress = ipAddress },
                commandType: CommandType.StoredProcedure);
        }

        private static bool IsAdminRole(string role)
        {
            return role.Equals("Admin", StringComparison.OrdinalIgnoreCase)
                || role.Equals("Super User", StringComparison.OrdinalIgnoreCase)
                || role.Equals("Super Admin", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsPaymentMakerRole(string role)
        {
            return role.Equals("Payment Maker", StringComparison.OrdinalIgnoreCase)
                || role.Equals("Maker", StringComparison.OrdinalIgnoreCase);
        }


        private async Task<List<DocumentHubBreadcrumbDto>> GetBreadcrumbAsync(SqlConnection con, int? folderId)
        {
            var folders = (await con.QueryAsync<DocumentHubFolderDto>(@"
SELECT FolderId, ParentFolderId, FolderName
FROM DocumentHubFolders
WHERE ISNULL(IsDeleted, 0) = 0;")).ToDictionary(x => x.FolderId);

            var stack = new Stack<DocumentHubBreadcrumbDto>();
            var current = folderId;
            while (current.HasValue && folders.TryGetValue(current.Value, out var folder))
            {
                stack.Push(new DocumentHubBreadcrumbDto { FolderId = folder.FolderId, FolderName = folder.FolderName });
                current = folder.ParentFolderId;
            }

            var result = new List<DocumentHubBreadcrumbDto> { new DocumentHubBreadcrumbDto { FolderId = null, FolderName = "Home" } };
            result.AddRange(stack);
            return result;
        }

        private static string FormatBytes(string value)
        {
            if (!long.TryParse(value, out var bytes))
                return "0 B";
            if (bytes < 1024)
                return bytes + " B";
            if (bytes < 1048576)
                return (bytes / 1024d).ToString("0.0") + " KB";
            if (bytes < 1073741824)
                return (bytes / 1048576d).ToString("0.0") + " MB";
            return (bytes / 1073741824d).ToString("0.0") + " GB";
        }
        public async Task<bool> ResetFolderPinAsync(
    int folderId,
    int userId,
    string userName)
        {
            using var con = new SqlConnection(_connectionString);

            await con.ExecuteAsync(
                "DocumentHub_RemoveFolderPin",
                new { FolderId = folderId },
                commandType: CommandType.StoredProcedure);

            var reset = await con.ExecuteScalarAsync<int>(@"
SELECT COUNT(1)
FROM DocumentHubFolders
WHERE FolderId = @FolderId
  AND ISNULL(IsDeleted, 0) = 0
  AND ISNULL(IsConfidential, 0) = 0
  AND ISNULL(PinCode, '') = '';",
                new { FolderId = folderId });

            if (reset > 0)
                await LogActivityAsync(null, folderId, userId, userName, "Reset Folder PIN", null, null);

            return reset > 0;
        }

        public async Task<bool> RestoreBackupAsync(DocumentHubBackupDto backup, int userId, string userName)
        {
            if (backup == null)
                return false;

            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            using var tx = con.BeginTransaction();

            try
            {
                await con.ExecuteAsync("DELETE FROM DocumentHubActivityLog;", transaction: tx);
                await con.ExecuteAsync("DELETE FROM DocumentHubFileVersions;", transaction: tx);
                await con.ExecuteAsync("DELETE FROM DocumentHubFiles;", transaction: tx);
                await con.ExecuteAsync("UPDATE DocumentHubFolders SET ParentFolderId = NULL;", transaction: tx);
                await con.ExecuteAsync("DELETE FROM DocumentHubFolders;", transaction: tx);

                await InsertFoldersAsync(con, tx, backup.Folders);
                await InsertFilesAsync(con, tx, backup.Files);
                await InsertVersionsAsync(con, tx, backup.Versions, backup.Files, userId, userName, preserveVersionIds: true);
                await InsertActivitiesAsync(con, tx, backup.Activities, userId, userName);

                await con.ExecuteAsync(
                    "DocumentHub_LogActivity",
                    new { FileId = (int?)null, FolderId = (int?)null, UserId = userId, UserName = userName, Action = "Restore Backup", Details = backup.BackupId, IpAddress = (string?)null },
                    tx,
                    commandType: CommandType.StoredProcedure);

                tx.Commit();
                return true;
            }
            catch
            {
                tx.Rollback();
                return false;
            }
        }

        public async Task<bool> RestoreBackupFolderAsync(DocumentHubBackupDto backup, int folderId, int userId, string userName)
        {
            if (backup == null || !backup.Folders.Any(x => x.FolderId == folderId))
                return false;

            var folderIds = GetFolderRestoreScope(backup.Folders, folderId);
            var files = backup.Files.Where(x => folderIds.Contains(x.FolderId)).ToList();
            var fileIds = files.Select(x => x.FileId).ToHashSet();
            var versions = backup.Versions.Where(x => fileIds.Contains(x.FileId)).ToList();
            var folders = backup.Folders.Where(x => folderIds.Contains(x.FolderId)).ToList();

            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            using var tx = con.BeginTransaction();

            try
            {
                await UpsertFoldersAsync(con, tx, folders);
                await UpsertFilesAsync(con, tx, files);
                await ReplaceVersionsAsync(con, tx, versions, files, userId, userName);

                await con.ExecuteAsync(
                    "DocumentHub_LogActivity",
                    new { FileId = (int?)null, FolderId = folderId, UserId = userId, UserName = userName, Action = "Restore Folder", Details = backup.BackupId, IpAddress = (string?)null },
                    tx,
                    commandType: CommandType.StoredProcedure);

                tx.Commit();
                return true;
            }
            catch
            {
                tx.Rollback();
                return false;
            }
        }

        public async Task<bool> RestoreBackupFileAsync(DocumentHubBackupDto backup, int fileId, int userId, string userName)
        {
            if (backup == null)
                return false;

            var file = backup.Files.FirstOrDefault(x => x.FileId == fileId);
            if (file == null)
                return false;


            var folderIds = GetFolderAncestorScope(backup.Folders, file.FolderId);
            var folders = backup.Folders.Where(x => folderIds.Contains(x.FolderId)).ToList();
            var versions = backup.Versions.Where(x => x.FileId == fileId).ToList();

            using var con = new SqlConnection(_connectionString);
            await con.OpenAsync();
            using var tx = con.BeginTransaction();

            //    try
            //    {
            //        await UpsertFoldersAsync(con, tx, folders);
            //        await UpsertFilesAsync(con, tx, new[] { file });
            //        await ReplaceVersionsAsync(con, tx, versions, new[] { file }, userId, userName);

            //        await con.ExecuteAsync(
            //            "DocumentHub_LogActivity",
            //            new { FileId = fileId, FolderId = file.FolderId, UserId = userId, UserName = userName, Action = "Restore File", Details = backup.BackupId, IpAddress = (string?)null },
            //            tx,
            //            commandType: CommandType.StoredProcedure);

            //        tx.Commit();
            //        return true;
            //    }
            //    catch
            //    {
            //        //tx.Rollback();
            //        //return false;
            //        tx.Rollback();
            //        throw;
            //    }
            //}
            try
            {
                var exists = await con.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM DocumentHubFiles WHERE FileId = @FileId",
                    new { FileId = fileId },
                    tx);

                if (exists > 0)
                {
                    await con.ExecuteAsync(
                        "UPDATE DocumentHubFiles SET IsDeleted = 0 WHERE FileId = @FileId",
                        new { FileId = fileId },
                        tx);

                    tx.Commit();
                    return true;
                }

                await UpsertFoldersAsync(con, tx, folders);
                await UpsertFilesAsync(con, tx, new[] { file });
                await ReplaceVersionsAsync(con, tx, versions, new[] { file }, userId, userName);

                await con.ExecuteAsync(
                    "DocumentHub_LogActivity",
                    new
                    {
                        FileId = fileId,
                        FolderId = file.FolderId,
                        UserId = userId,
                        UserName = userName,
                        Action = "Restore File",
                        Details = backup.BackupId,
                        IpAddress = (string?)null
                    },
                    tx,
                    commandType: CommandType.StoredProcedure);

                tx.Commit();
                return true;
            }
            catch
            {
                tx.Rollback();
                return false;
            }

        }

        private static async Task InsertFoldersAsync(SqlConnection con, SqlTransaction tx, IEnumerable<DocumentHubFolderDto> folders)
        {
            var orderedFolders = OrderFoldersForInsert(folders).ToList();
            if (!orderedFolders.Any())
                return;

            await con.ExecuteAsync("SET IDENTITY_INSERT DocumentHubFolders ON;", transaction: tx);
            foreach (var folder in orderedFolders)
            {
                await con.ExecuteAsync(@"
INSERT INTO DocumentHubFolders
    (FolderId, ParentFolderId, FolderName, Department, CreatedDate, LastUpdated, IsDeleted, IsConfidential, PinCode)
VALUES
    (@FolderId, @ParentFolderId, @FolderName, @Department, @CreatedDate, @LastUpdated, @IsDeleted, @IsConfidential, @PinCode);",
                    NormalizeFolder(folder), tx);
            }
            await con.ExecuteAsync("SET IDENTITY_INSERT DocumentHubFolders OFF;", transaction: tx);
        }

        private static async Task UpsertFoldersAsync(SqlConnection con, SqlTransaction tx, IEnumerable<DocumentHubFolderDto> folders)
        {
            foreach (var folder in OrderFoldersForInsert(folders))
            {
                var exists = await con.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM DocumentHubFolders WHERE FolderId = @FolderId;",
                    new { folder.FolderId },
                    tx);

                if (exists > 0)
                {
                    await con.ExecuteAsync(@"
UPDATE DocumentHubFolders
SET ParentFolderId = @ParentFolderId,
    FolderName = @FolderName,
    Department = @Department,
    LastUpdated = @LastUpdated,
    IsDeleted = @IsDeleted,
    IsConfidential = @IsConfidential,
    PinCode = @PinCode
WHERE FolderId = @FolderId;",
                        NormalizeFolder(folder), tx);
                }
                else
                {
                    await con.ExecuteAsync("SET IDENTITY_INSERT DocumentHubFolders ON;", transaction: tx);
                    await con.ExecuteAsync(@"
INSERT INTO DocumentHubFolders
    (FolderId, ParentFolderId, FolderName, Department, CreatedDate, LastUpdated, IsDeleted, IsConfidential, PinCode)
VALUES
    (@FolderId, @ParentFolderId, @FolderName, @Department, @CreatedDate, @LastUpdated, @IsDeleted, @IsConfidential, @PinCode);",
                        NormalizeFolder(folder), tx);
                    await con.ExecuteAsync("SET IDENTITY_INSERT DocumentHubFolders OFF;", transaction: tx);
                }
            }
        }

        private static async Task InsertFilesAsync(SqlConnection con, SqlTransaction tx, IEnumerable<DocumentHubFileDto> files)
        {
            var targetFiles = files.ToList();
            if (!targetFiles.Any())
                return;

            await con.ExecuteAsync("SET IDENTITY_INSERT DocumentHubFiles ON;", transaction: tx);
            foreach (var file in targetFiles)
                await con.ExecuteAsync(FileInsertSql, NormalizeFile(file), tx);
            await con.ExecuteAsync("SET IDENTITY_INSERT DocumentHubFiles OFF;", transaction: tx);
        }

        private static async Task UpsertFilesAsync(SqlConnection con, SqlTransaction tx, IEnumerable<DocumentHubFileDto> files)
        {
            foreach (var file in files)
            {
                var exists = await con.ExecuteScalarAsync<int>(
                    "SELECT COUNT(1) FROM DocumentHubFiles WHERE FileId = @FileId;",
                    new { file.FileId },
                    tx);

                if (exists > 0)
                    await con.ExecuteAsync(FileUpdateSql, NormalizeFile(file), tx);
                else
                {
                    await con.ExecuteAsync("SET IDENTITY_INSERT DocumentHubFiles ON;", transaction: tx);
                    await con.ExecuteAsync(FileInsertSql, NormalizeFile(file), tx);
                    await con.ExecuteAsync("SET IDENTITY_INSERT DocumentHubFiles OFF;", transaction: tx);
                }
            }
        }

        private static async Task InsertVersionsAsync(SqlConnection con, SqlTransaction tx, IEnumerable<DocumentHubVersionDto> versions, IEnumerable<DocumentHubFileDto> files, int userId, string userName, bool preserveVersionIds = false)
        {
            var targetVersions = versions.ToList();
            if (!targetVersions.Any())
                return;

            var fileMap = files.ToDictionary(x => x.FileId);
            var columns = await GetTableColumnNamesAsync(con, tx, "DocumentHubFileVersions");
            var insertVersionId = preserveVersionIds && columns.Contains("VersionId");

            if (insertVersionId)
                await con.ExecuteAsync("SET IDENTITY_INSERT DocumentHubFileVersions ON;", transaction: tx);

            foreach (var version in targetVersions)
                await InsertVersionAsync(con, tx, columns, NormalizeVersion(version, fileMap, userId, userName), insertVersionId);

            if (insertVersionId)
                await con.ExecuteAsync("SET IDENTITY_INSERT DocumentHubFileVersions OFF;", transaction: tx);
        }

        private static async Task ReplaceVersionsAsync(SqlConnection con, SqlTransaction tx, IEnumerable<DocumentHubVersionDto> versions, IEnumerable<DocumentHubFileDto> files, int userId, string userName)
        {
            var fileList = files.ToList();
            if (!fileList.Any())
                return;

            var fileIds = fileList.Select(x => x.FileId).ToArray();
            await con.ExecuteAsync("DELETE FROM DocumentHubFileVersions WHERE FileId IN @FileIds;", new { FileIds = fileIds }, tx);
            await InsertVersionsAsync(con, tx, versions, fileList, userId, userName);
        }

        private static async Task InsertActivitiesAsync(SqlConnection con, SqlTransaction tx, IEnumerable<DocumentHubActivityDto> activities, int userId, string userName)
        {
            var targetActivities = activities.ToList();
            if (!targetActivities.Any())
                return;

            var tableColumns = await GetTableColumnNamesAsync(con, tx, "DocumentHubActivityLog");
            var insertActivityId = tableColumns.Contains("ActivityId");

            if (insertActivityId)
                await con.ExecuteAsync("SET IDENTITY_INSERT DocumentHubActivityLog ON;", transaction: tx);

            foreach (var activity in targetActivities)
                await InsertActivityAsync(con, tx, tableColumns, new
                {
                    activity.ActivityId,
                    activity.FileId,
                    activity.FolderId,
                    UserId = activity.UserId > 0 ? activity.UserId : userId,
                    UserName = string.IsNullOrWhiteSpace(activity.UserName) ? userName : activity.UserName,
                    Action = string.IsNullOrWhiteSpace(activity.Action) ? "Restored Activity" : activity.Action,
                    activity.Details,
                    activity.IpAddress,
                    ActivityDate = activity.ActivityDate == default ? DateTime.UtcNow : activity.ActivityDate
                }, insertActivityId);

            if (insertActivityId)
                await con.ExecuteAsync("SET IDENTITY_INSERT DocumentHubActivityLog OFF;", transaction: tx);
        }

        private static object NormalizeFolder(DocumentHubFolderDto folder) => new
        {
            folder.FolderId,
            folder.ParentFolderId,
            FolderName = string.IsNullOrWhiteSpace(folder.FolderName) ? "Restored Folder" : folder.FolderName,
            folder.Department,
            CreatedDate = folder.CreatedDate == default ? DateTime.UtcNow : folder.CreatedDate,
            LastUpdated = folder.LastUpdated,
            folder.IsDeleted,
            folder.IsConfidential,
            PinCode = folder.PinCode ?? string.Empty
        };

        private static object NormalizeFile(DocumentHubFileDto file) => new
        {
            file.FileId,
            file.FolderId,
            FileName = string.IsNullOrWhiteSpace(file.FileName) ? Path.GetFileName(file.StoredFileName) : file.FileName,
            file.StoredFileName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            FileType = string.IsNullOrWhiteSpace(file.FileType) ? Path.GetExtension(file.FileName).TrimStart('.').ToUpperInvariant() : file.FileType,
            file.FileSize,
            UploadedBy = string.IsNullOrWhiteSpace(file.UploadedBy) ? "Restored" : file.UploadedBy,
            UploadedByUserId = file.UploadedByUserId > 0 ? file.UploadedByUserId : 0,
            UploadedDate = file.UploadedDate == default ? DateTime.UtcNow : file.UploadedDate,
            VersionNumber = file.VersionNumber > 0 ? file.VersionNumber : 1,
            file.IsConfidential,
            file.IsDeleted,
            file.IsFavorite,
            Owner = string.IsNullOrWhiteSpace(file.Owner) ? file.UploadedBy : file.Owner,
            PermissionGroup = string.IsNullOrWhiteSpace(file.PermissionGroup) ? "All" : file.PermissionGroup,
            AccessLevel = string.IsNullOrWhiteSpace(file.AccessLevel) ? "Public" : file.AccessLevel,
            Tags = file.Tags ?? string.Empty
        };

        private static object NormalizeVersion(DocumentHubVersionDto version, Dictionary<int, DocumentHubFileDto> fileMap, int userId, string userName)
        {
            fileMap.TryGetValue(version.FileId, out var file);
            var fileName = string.IsNullOrWhiteSpace(version.FileName) ? file?.FileName : version.FileName;
            var contentType = string.IsNullOrWhiteSpace(version.ContentType) ? file?.ContentType : version.ContentType;
            var fileType = string.IsNullOrWhiteSpace(version.FileType) ? file?.FileType : version.FileType;

            return new
            {
                version.VersionId,
                version.FileId,
                VersionNumber = version.VersionNumber > 0 ? version.VersionNumber : 1,
                FileName = string.IsNullOrWhiteSpace(fileName) ? "Restored File" : fileName,
                version.StoredFileName,
                ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType,
                FileType = string.IsNullOrWhiteSpace(fileType) ? Path.GetExtension(fileName ?? string.Empty).TrimStart('.').ToUpperInvariant() : fileType,
                version.FileSize,
                UploadedBy = string.IsNullOrWhiteSpace(version.UploadedBy) ? userName : version.UploadedBy,
                UploadedByUserId = version.UploadedByUserId > 0 ? version.UploadedByUserId : userId,
                UploadedDate = version.UploadedDate == default ? DateTime.UtcNow : version.UploadedDate
            };
        }

        private static HashSet<int> GetFolderRestoreScope(IEnumerable<DocumentHubFolderDto> folders, int folderId)
        {
            var folderList = folders.ToList();
            var result = GetFolderAncestorScope(folderList, folderId);
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

        private static HashSet<int> GetFolderAncestorScope(IEnumerable<DocumentHubFolderDto> folders, int folderId)
        {
            var map = folders.ToDictionary(x => x.FolderId);
            var result = new HashSet<int>();
            var current = folderId;
            while (map.TryGetValue(current, out var folder) && result.Add(folder.FolderId))
            {
                if (!folder.ParentFolderId.HasValue)
                    break;
                current = folder.ParentFolderId.Value;
            }
            return result;
        }

        private static IEnumerable<DocumentHubFolderDto> OrderFoldersForInsert(IEnumerable<DocumentHubFolderDto> folders)
        {
            var folderList = folders.ToList();
            var map = folderList.ToDictionary(x => x.FolderId);
            var emitted = new HashSet<int>();

            foreach (var folder in folderList)
                foreach (var orderedFolder in EmitFolder(folder, map, emitted))
                    yield return orderedFolder;
        }

        private static IEnumerable<DocumentHubFolderDto> EmitFolder(DocumentHubFolderDto folder, Dictionary<int, DocumentHubFolderDto> map, HashSet<int> emitted)
        {
            if (emitted.Contains(folder.FolderId))
                yield break;

            if (folder.ParentFolderId.HasValue && map.TryGetValue(folder.ParentFolderId.Value, out var parent))
            {
                foreach (var parentFolder in EmitFolder(parent, map, emitted))
                    yield return parentFolder;
            }

            if (emitted.Add(folder.FolderId))
                yield return folder;
        }

        private static async Task<HashSet<string>> GetTableColumnNamesAsync(SqlConnection con, SqlTransaction? tx, string tableName)
        {
            var names = await con.QueryAsync<string>(@"
SELECT c.name
FROM sys.columns c
WHERE c.object_id = OBJECT_ID(@TableName);",
                new { TableName = tableName },
                tx);

            return names.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static async Task InsertActivityAsync(SqlConnection con, SqlTransaction tx, HashSet<string> tableColumns, object activityValues, bool insertActivityId)
        {
            var preferredColumns = new[]
            {
                "ActivityId",
                "FileId",
                "FolderId",
                "UserId",
                "UserName",
                "Action",
                "Details",
                "IpAddress",
                "ActivityDate"
            };

            var valueType = activityValues.GetType();
            var columns = new List<string>();
            var parameters = new DynamicParameters();

            foreach (var column in preferredColumns)
            {
                if (!tableColumns.Contains(column))
                    continue;

                if (column.Equals("ActivityId", StringComparison.OrdinalIgnoreCase) && !insertActivityId)
                    continue;

                var property = valueType.GetProperty(column);
                if (property == null)
                    continue;

                columns.Add(column);
                parameters.Add(column, property.GetValue(activityValues));
            }

            if (!columns.Any())
                return;

            var columnList = string.Join(", ", columns);
            var valueList = string.Join(", ", columns.Select(x => "@" + x));
            await con.ExecuteAsync(
                $"INSERT INTO DocumentHubActivityLog ({columnList}) VALUES ({valueList});",
                parameters,
                tx);
        }

        private static async Task InsertVersionAsync(SqlConnection con, SqlTransaction tx, HashSet<string> tableColumns, object versionValues, bool insertVersionId)
        {
            var preferredColumns = new[]
            {
                "VersionId",
                "FileId",
                "VersionNumber",
                "FileName",
                "StoredFileName",
                "ContentType",
                "FileType",
                "FileSize",
                "UploadedBy",
                "UploadedByUserId",
                "UploadedDate"
            };

            var valueType = versionValues.GetType();
            var columns = new List<string>();
            var parameters = new DynamicParameters();

            foreach (var column in preferredColumns)
            {
                if (!tableColumns.Contains(column))
                    continue;

                if (column.Equals("VersionId", StringComparison.OrdinalIgnoreCase) && !insertVersionId)
                    continue;

                var property = valueType.GetProperty(column);
                if (property == null)
                    continue;

                columns.Add(column);
                parameters.Add(column, property.GetValue(versionValues));
            }

            if (!columns.Any())
                return;

            var columnList = string.Join(", ", columns);
            var valueList = string.Join(", ", columns.Select(x => "@" + x));
            await con.ExecuteAsync(
                $"INSERT INTO DocumentHubFileVersions ({columnList}) VALUES ({valueList});",
                parameters,
                tx);
        }

        private const string FileInsertSql = @"
INSERT INTO DocumentHubFiles
    (FileId, FolderId, FileName, StoredFileName, ContentType, FileType, FileSize, UploadedBy, UploadedByUserId, UploadedDate, VersionNumber, IsConfidential, IsDeleted, IsFavorite, Owner, PermissionGroup, AccessLevel, Tags)
VALUES
    (@FileId, @FolderId, @FileName, @StoredFileName, @ContentType, @FileType, @FileSize, @UploadedBy, @UploadedByUserId, @UploadedDate, @VersionNumber, @IsConfidential, @IsDeleted, @IsFavorite, @Owner, @PermissionGroup, @AccessLevel, @Tags);";

        private const string FileUpdateSql = @"
UPDATE DocumentHubFiles
SET FolderId = @FolderId,
    FileName = @FileName,
    StoredFileName = @StoredFileName,
    ContentType = @ContentType,
    FileType = @FileType,
    FileSize = @FileSize,
    UploadedBy = @UploadedBy,
    UploadedByUserId = @UploadedByUserId,
    UploadedDate = @UploadedDate,
    VersionNumber = @VersionNumber,
    IsConfidential = @IsConfidential,
    IsDeleted = @IsDeleted,
    IsFavorite = @IsFavorite,
    Owner = @Owner,
    PermissionGroup = @PermissionGroup,
    AccessLevel = @AccessLevel,
    Tags = @Tags
WHERE FileId = @FileId;";
    }
}
