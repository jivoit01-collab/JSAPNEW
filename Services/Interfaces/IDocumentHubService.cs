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
        Task<bool> MoveFileAsync(int fileId, int targetFolderId, int userId, string userName);
        Task<bool> MoveFolderAsync(int folderId, int? targetParentFolderId, int userId, string userName);
        Task<bool> ToggleFavoriteAsync(int fileId, int userId, string userName);
        Task<List<DocumentHubVersionDto>> GetVersionHistoryAsync(int fileId);
        Task<bool> RestoreVersionAsync(int fileId, int versionNumber, int userId, string userName);
        Task<List<DocumentHubActivityDto>> GetActivityLogAsync(int? fileId = null);
        Task<bool> ValidateConfidentialAccessAsync(int fileId, int userId, string userName, string? usernameConfirmation, string? pin);
        Task<bool> ValidateFolderPinAsync(int folderId, string? pin);
        Task<bool> SetFolderPinAsync(int folderId, string pin, int userId, string userName);
        Task<bool> ChangeFolderPinAsync(int folderId, string currentPin, string newPin, int userId, string userName);
        Task<bool> RemoveFolderPinAsync(int folderId, string currentPin, int userId, string userName);
        Task<bool> ResetFolderPinAsync(int folderId, int userId, string userName);
        Task<bool> RestoreBackupAsync(DocumentHubBackupDto backup, int userId, string userName);
        Task<bool> RestoreBackupFolderAsync(DocumentHubBackupDto backup, int folderId, int userId, string userName);
        Task<bool> RestoreBackupFileAsync(DocumentHubBackupDto backup, int fileId, int userId, string userName);

        Task LogActivityAsync( int? fileId,int? folderId,int userId, string userName, string action, string? details = null,string? ipAddress = null);
    }

}

