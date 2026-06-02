using JSAPNEW.Models;
using Microsoft.AspNetCore.Http;

namespace JSAPNEW.Services.Interfaces
{
    public interface IDocumentHubService
    {
        Task<DocumentHubPermissionDto> GetPermissionsAsync(int userId, int companyId);
        Task<DocumentHubSnapshotDto> GetSnapshotAsync(int? folderId, string? filter, string? search);
        Task<List<DocumentHubFolderDto>> GetFoldersAsync();
        Task<DocumentHubFolderDto?> CreateFolderAsync(DocumentHubFolderRequest request, int userId, string userName);
        Task<bool> RenameFolderAsync(int folderId, string folderName, int userId, string userName);
        Task<bool> DeleteFolderAsync(int folderId, int userId, string userName);
        Task<DocumentHubUploadResultDto> UploadFilesAsync(DocumentHubUploadRequest request, int userId, string userName);
        Task<DocumentHubFileDto?> GetFileAsync(int fileId);
        Task<bool> RenameFileAsync(int fileId, string fileName, int userId, string userName);
        Task<bool> DeleteFileAsync(int fileId, int userId, string userName);
        Task<List<DocumentHubVersionDto>> GetVersionHistoryAsync(int fileId);
        Task<List<DocumentHubActivityDto>> GetActivityLogAsync(int? fileId = null);
        Task<bool> ValidateConfidentialAccessAsync(int fileId, int userId, string userName, string? usernameConfirmation, string? pin);
        Task LogActivityAsync(int? fileId, int? folderId, int userId, string userName, string action, string? details = null);
    }
}

namespace JSAPNEW.Models
{
    public class DocumentHubPermissionDto
    {
        public bool CanView { get; set; }
        public bool CanUpload { get; set; }
        public bool CanDelete { get; set; }
        public bool CanManageFolders { get; set; }
    }

    public class DocumentHubSnapshotDto
    {
        public List<DocumentHubFolderDto> Folders { get; set; } = new();
        public List<DocumentHubFileDto> Files { get; set; } = new();
        public List<DocumentHubBreadcrumbDto> Breadcrumb { get; set; } = new();
        public DocumentHubStatsDto Stats { get; set; } = new();
    }

    public class DocumentHubStatsDto
    {
        public int TotalFiles { get; set; }
        public int TotalFolders { get; set; }
        public int RecentFiles { get; set; }
        public int FavoriteFiles { get; set; }
    }

    public class DocumentHubFolderDto
    {
        public int FolderId { get; set; }
        public int? ParentFolderId { get; set; }
        public string FolderName { get; set; } = string.Empty;
        public string? Department { get; set; }
        public int ItemCount { get; set; }
        public int ChildFolderCount { get; set; }
        public int FileCount { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? LastUpdated { get; set; }
        public bool IsDeleted { get; set; }
    }

    public class DocumentHubFileDto
    {
        public int FileId { get; set; }
        public int FolderId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = "application/octet-stream";
        public string FileType { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public int UploadedByUserId { get; set; }
        public DateTime UploadedDate { get; set; }
        public long FileSize { get; set; }
        public int VersionNumber { get; set; }
        public bool IsConfidential { get; set; }
        public bool IsFavorite { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public bool IsDeleted { get; set; }
    }

    public class DocumentHubBreadcrumbDto
    {
        public int? FolderId { get; set; }
        public string FolderName { get; set; } = string.Empty;
    }

    public class DocumentHubFolderRequest
    {
        public int? ParentFolderId { get; set; }
        public string FolderName { get; set; } = string.Empty;
        public string? Department { get; set; }
    }

    public class DocumentHubUploadRequest
    {
        public int FolderId { get; set; }
        public IReadOnlyList<IFormFile> Files { get; set; } = Array.Empty<IFormFile>();
        public bool IsConfidential { get; set; }
        public string ConflictAction { get; set; } = "newversion";
    }

    public class DocumentHubUploadResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<DocumentHubFileDto> Files { get; set; } = new();
        public List<string> Conflicts { get; set; } = new();
    }

    public class DocumentHubVersionDto
    {
        public int VersionId { get; set; }
        public int FileId { get; set; }
        public int VersionNumber { get; set; }
        public string StoredFileName { get; set; } = string.Empty;
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }
        public long FileSize { get; set; }
    }

    public class DocumentHubActivityDto
    {
        public int ActivityId { get; set; }
        public int? FileId { get; set; }
        public int? FolderId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public DateTime ActivityDate { get; set; }
        public string Action { get; set; } = string.Empty;
        public string? Details { get; set; }
    }
}
