using Microsoft.AspNetCore.Http;


namespace JSAPNEW.Models

{
    public class BillDetailDto
    {
        public string AccountName { get; set; }
        public object VchNumber { get; set; }
        public string VoucherDate { get; set; }
        public object BillAmount { get; set; }
        public string SupplierRef { get; set; }
        public string SupplierRefDate { get; set; }
        public string DueDate { get; set; }
        public string PaymentDate { get; set; }
        public string AttachmentPath { get; set; }
        public string Attachment { get; set; }  // "File" or "No File"
        public string MakerRemark { get; set; }
        public string CheckerRemark { get; set; }
        public string CheckerStatus { get; set; }
        public string MakerStatus { get; set; }
        public string PaymentStatus { get; set; }
        public string SerialNumber { get; set; }
        public object TotalQuantity { get; set; }
        public object TotalItemValue { get; set; }
        public object TotalItems { get; set; }
        public bool IsPaymentVerified { get; set; }
    }

    //public class InvoiceItemDto
    //{
    //    public string ProductName { get; set; }
    //    public object Quantity { get; set; }
    //    public object Rate { get; set; }
    //    public object Tax { get; set; }
    //    public object Amount { get; set; }
    //    public string WarehouseName { get; set; }
    //    public string TaxName { get; set; }
    //    public object ItemValue { get; set; }
    //}
    public class InvoiceItemDto
    {
        public string SerialNumber { get; set; }

        public string ProductName { get; set; }

        public string HSNSACID { get; set; }

        public object Quantity { get; set; }

        public object PurchaseRate { get; set; }

        public object DiscountPercent { get; set; }

        public object DiscountAmount { get; set; }

        public object Margin { get; set; }

        public object MRP { get; set; }

 
        public object TaxRate { get; set; }

        public object TaxAmount { get; set; }

        public object Tax { get; set; }

        public object Amount { get; set; }

        public string WarehouseName { get; set; }

        public string TaxName { get; set; }

        public object ItemValue { get; set; }
    }

    public class BillSummaryDto
    {
        public int TotalBills { get; set; }
        public int PendingMaker { get; set; }
        public int ApprovedChecker { get; set; }
        public int TotalPaid { get; set; }
    }
    public class DeleteRequest
    {
        public int VchNumber { get; set; }
    }
    public class DocumentFolderDto
    {
        public int FolderId { get; set; }
        public int? ParentFolderId { get; set; }
        public string FolderName { get; set; }
        public string? Department { get; set; }
        public int ChildFolderCount { get; set; }
        public int FileCount { get; set; }
        public DateTime? LastUpdated { get; set; }
        public bool IsConfidential { get; set; }
        public string PinCode { get; set; } = string.Empty;
    }

    public class DocumentHubFileDto
    {
        public int FileId { get; set; }
        public int FolderId { get; set; }
        public string FileName { get; set; }
        public string StoredFileName { get; set; }
        public string ContentType { get; set; }
        public string UploadedBy { get; set; }
        public int UploadedByUserId { get; set; }
        public DateTime UploadedDate { get; set; }
        public long FileSize { get; set; }
        public int VersionNumber { get; set; }
        public bool IsConfidential { get; set; }
        public bool IsDeleted { get; set; }
        public bool IsFavorite { get; set; }
        public string FileUrl { get; set; } = string.Empty;
        public string Owner { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string PermissionGroup { get; set; } = string.Empty;
        public string AccessLevel { get; set; } = "Public";
        public string Tags { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;

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

        public bool IsConfidential { get; set; }

        public string PinCode { get; set; } = string.Empty;
    }

    public class DocumentHubUploadRequest
    {
        public int FolderId { get; set; }

        public IReadOnlyList<IFormFile> Files { get; set; } = Array.Empty<IFormFile>();

        public bool IsConfidential { get; set; }

        public string ConflictAction { get; set; } = "newversion";

        public string PermissionGroup { get; set; } = "All";

        public string AccessLevel { get; set; } = "Public";

        public string Tags { get; set; } = string.Empty;
    }

    public class DocumentHubUploadResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public List<DocumentHubFileDto> Files { get; set; } = new();
        public List<string> Conflicts { get; set; } = new();
    }

    public class DocumentHubVersionDto
    {
        public int VersionId { get; set; }
        public int FileId { get; set; }
        public int VersionNumber { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string FileType { get; set; } = string.Empty;
        public string UploadedBy { get; set; }
        public int UploadedByUserId { get; set; }
        public DateTime UploadedDate { get; set; }
        public long FileSize { get; set; }
        public string StoredFileName { get; set; } = string.Empty;
    }

    public class DocumentHubActivityDto
    {
        public int ActivityId { get; set; }
        public int? FileId { get; set; }
        public int? FolderId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string Action { get; set; }
        public string Details { get; set; }
        public DateTime ActivityDate { get; set; }
        public string IpAddress { get; set; } = string.Empty;
    }

    public class DocumentHubPermissionDto
    {
        public bool CanView { get; set; }
        public bool CanUpload { get; set; }
        public bool CanDownload { get; set; }
        public bool CanDelete { get; set; }
        public bool CanManageFolders { get; set; }
        public string RoleName { get; set; }
        public bool CanRename { get; set; }
        public bool CanShare { get; set; }

    }

    public class DocumentHubBreadcrumbDto
    {
        public int? FolderId { get; set; }
        public string FolderName { get; set; } = string.Empty;
    }

    public class DocumentHubStatsDto
    {
        public int TotalFiles { get; set; }
        public int TotalFolders { get; set; }
        public int RecentFiles { get; set; }
        public int FavoriteFiles { get; set; }
        public string StorageUsed { get; set; } = "0 B";
    }

    public class DocumentHubSnapshotDto
    {
        public List<DocumentHubFolderDto> Folders { get; set; } = new();
        public List<DocumentHubFileDto> Files { get; set; } = new();
        public List<DocumentHubBreadcrumbDto> Breadcrumb { get; set; } = new();
        public DocumentHubStatsDto Stats { get; set; } = new();
    }

    public class DocumentHubFolderRequest
    {
        public int? ParentFolderId { get; set; }
        public string FolderName { get; set; } = string.Empty;
        public string? Department { get; set; }
    }

    public class DocumentHubFolderPinRequest
    {
        public int FolderId { get; set; }
        public string? CurrentPin { get; set; }
        public string? NewPin { get; set; }
        public string? ConfirmPin { get; set; }
    }

    public class DocumentHubBackupSummaryDto
    {
        public string BackupId { get; set; } = string.Empty;
        public string BackupType { get; set; } = string.Empty;
        public DateTime BackupDate { get; set; }
        public long FileSize { get; set; }
        public string Status { get; set; } = string.Empty;
        public int FolderCount { get; set; }
        public int FileCount { get; set; }
    }
    public class DocumentHubBackupDto
    {
        public string BackupId { get; set; } = string.Empty;
        public string BackupType { get; set; } = string.Empty;
        public DateTime BackupDate { get; set; }
        public string Status { get; set; } = string.Empty;

        public List<DocumentHubFolderDto> Folders { get; set; } = new();
        public List<DocumentHubFileDto> Files { get; set; } = new();
        public List<DocumentHubVersionDto> Versions { get; set; } = new();
        public List<DocumentHubActivityDto> Activities { get; set; } = new();
        public List<DocumentHubBackupFileDto> PhysicalFiles { get; set; } = new();
    }

    public class DocumentHubBackupFileDto
    {
        public string StoredFileName { get; set; } = string.Empty;
        public string BackupRelativePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public bool Exists { get; set; }
    }
}



   
