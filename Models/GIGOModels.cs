using System.ComponentModel.DataAnnotations;
using TicketSystem.Models;

namespace JSAPNEW.Models
{
    public class gigoModels
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
    public class GateEntryModel
    {
        [Required] public string EntryType { get; set; }
        [Required] public DateTime EntryDate { get; set; }
        [Required] public string PartyID { get; set; }
        [Required] public string DocumentType { get; set; }
        [Required] public string Remarks { get; set; }
        [Required] public int CreatedBy { get; set; }
    }
    public class GateEntryResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? NewGateEntryID { get; set; }
    }

    public class AttachmentItem
    {
        public string FileName { get; set; }
        public string FileType { get; set; }
        public long FileSize { get; set; }
        public int UploadedBy { get; set; }
    }

    public class AddAttachmentModel
    {
        [Required] public int GateEntryID { get; set; }
        [Required] public int UploadedBy { get; set; }
        public List<AttachmentItem> Attachments { get; set; }
    }

    public class VendorDocument
    {
        public int GateEntryID { get; set; }
        public List<VendorDocumentItem> Items { get; set; }
    }

    // UPDATED: GateEntryMasterRequest with separate vehicle and general attachments
    public class GateEntryMasterRequest
    {
        public string? EntryType { get; set; }
        public DateTime EntryDate { get; set; }
        public string? PartyID { get; set; }
        public string? DocumentType { get; set; }
        public string? Remarks { get; set; }
        public int CreatedBy { get; set; }

        // Vehicle Information
        public string? VehicleNo { get; set; }
        public string? VRefNo { get; set; }
        public string? DriverName { get; set; }
        public string? DriverNumber { get; set; }
        public string? VendorBiltyNo { get; set; }
        public string? TransporterName { get; set; }
        public string? DocumentRemarks { get; set; }

        // FIXED: Separate attachment properties
        public VehicleAttachmentInfo? VehicleAttachment { get; set; }  // Single vehicle attachment
        public List<GeneralAttachmentInfo>? GeneralAttachments { get; set; }  // Multiple general attachments

        // Document Arrays
        public List<VendorDocumentItem>? VendorDocuments { get; set; }
        public List<CustomerDocumentItem>? CustomerDocuments { get; set; }
        public List<BSTDocumentItem>? BSTDocuments { get; set; }

        // REMOVED: Old Attachments property (replaced by VehicleAttachment + GeneralAttachments)
        // public List<AttachmentInfo>? Attachments { get; set; }  // ❌ REMOVE THIS
    }

    // NEW: Separate classes for different attachment types
    public class VehicleAttachmentInfo
    {
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public long FileSize { get; set; }
        public int UploadedBy { get; set; }
        public string AttachmentCategory { get; set; } = "VEHICLE_DOCUMENT";
    }

    public class GeneralAttachmentInfo
    {
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public long FileSize { get; set; }
        public int UploadedBy { get; set; }
        public string AttachmentCategory { get; set; } = "GENERAL_DOCUMENT";
        public int Index { get; set; }  // For ordering
    }

    // Keep existing classes unchanged
    public class VendorDocumentItem
    {
        public string? DocType { get; set; }
        public string? PONumber { get; set; }
        public string? ItemCode { get; set; }
        public string? ItemName { get; set; }
        public decimal POQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal OpenQty { get; set; }
        public string? Remarks { get; set; }
        public int CreatedBy { get; set; }
    }
    public class VehicleDetails
    {
        public int GateEntryID { get; set; }
        public string? VehicleNo { get; set; }
        public string? VRefNo { get; set; }
        public string? DriverName { get; set; }
        public string? DriverNumber { get; set; }
        public string? VendorBiltyNo { get; set; }
        public string? TransporterName { get; set; }
        public string? DocumentRemarks { get; set; }
        public int CreatedBy { get; set; }
        public string? FileName { get; set; }
        public string? FileType { get; set; }
        public int FileSize { get; set; }
        public DateTime UploadedOn { get; set; }
    }

    public class GateEntryMasterResult : gigoModels
    {
        public int GateEntryID { get; set; }
    }

    public class VendorDocumentDto
    {
        public string DocType { get; set; } = "";
        public string PONumber { get; set; } = "";
        public string ItemCode { get; set; } = "";
        public string ItemName { get; set; } = "";
        public decimal POQty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal OpenQty { get; set; }
        public string Remarks { get; set; } = "";
        public int CreatedBy { get; set; }
    }

    public class CustomerDocumentDto
    {
        public string DocType { get; set; } = "";
        public string DocId { get; set; } = "";
        public string ItemCode { get; set; } = "";
        public string ItemName { get; set; } = "";
        public decimal Qty { get; set; }
        public decimal ReceivedQty { get; set; }
        public decimal OpenQty { get; set; }
        public string Remarks { get; set; } = "";
        public int CreatedBy { get; set; }
    }

    public class BSTDocumentDto
    {
        public string DocType { get; set; } = "";
        public string DocId { get; set; } = "";
        public string ItemCode { get; set; } = "";
        public string FromLocation { get; set; } = "";
        public string FromWarehouse { get; set; } = "";
        public string ToWarehouse { get; set; } = "";
        public decimal Qty { get; set; }
        public decimal QtyReceived { get; set; }
        public decimal OpenQty { get; set; }
        public string Remarks { get; set; } = "";
        public int CreatedBy { get; set; }
    }

    public class AttachmentDto
    {
        public string FileName { get; set; } = "";
        public string FileType { get; set; } = "";
        public long FileSize { get; set; }
        public int UploadedBy { get; set; }
    }

    public class CustomerDocument
    {
        public int GateEntryID { get; set; }
        public List<CustomerDocumentItem> Items { get; set; } = new();
    }

    public class CustomerDocumentItem
    {
        public string DocType { get; set; } = "";
        public string DocId { get; set; } = "";
        public string ItemCode { get; set; } = "";
        public decimal Qty { get; set; }
        public decimal ReceivedQty { get; set; }
        public string Remarks { get; set; } = "";
        public int CreatedBy { get; set; }
    }

    public class BSTDocument
    {
        public int GateEntryID { get; set; }
        public List<BSTDocumentItem> Items { get; set; } = new();
    }

    public class BSTDocumentItem
    {
        public string DocType { get; set; } = "";
        public string DocId { get; set; } = "";
        public string ItemCode { get; set; } = "";
        public string FromWarehouse { get; set; } = "";
        public string ToWarehouse { get; set; } = "";
        public decimal Qty { get; set; }
        public decimal QtyReceived { get; set; }
        public string Remarks { get; set; } = "";
        public int CreatedBy { get; set; }
    }

}