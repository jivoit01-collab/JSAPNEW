namespace JSAPNEW.Models
{
    public class DocumentDispatchModels
    {
        public string DatabaseName { get; set; }
        public string DocEntry { get; set; }
        public string DocNum { get; set; }
        public string DocDate { get; set; }
        public string CreateDate { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string DocTotal { get; set; }
        public string link { get; set; }
    }
    public class SaveDocumentAttachmentModel
    {
        public string BundleId { get; set; }
        public string Attachment { get; set; }
        public string DocId { get; set; }
        public string DocType { get; set; }
        public string Branch { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string Status { get; set; }
    }

    public class DocumentAttachmentModel
    {
        public int id { get; set; }
        public string BundleId { get; set; }
        public string Attachment { get; set; }
        public string DocId { get; set; }
        public string DocType { get; set; }
        public string Branch { get; set; }
        public int CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string status { get; set; }
        public string createdByName { get; set; }
        public string receivedByName { get; set; }
    }

    public class HanaDocumentDispatchModels
    {
        public string p_docId { get; set; }
        public string p_docType { get; set; }
        public string p_branch { get; set; }
    }
    public class DispatchResponse
    {
        public string Message { get; set; }
        public bool Success { get; set; }
    }

    public class UpdateDocumentModel
    {
        public int id { get; set; }
        public string status { get; set; }
        public int receivedBy { get; set; }
        public string docId { get; set; }
        public string? rejectedReason { get; set; }
        public string? BundleStatus { get; set; }
    }

    public class SaveBundleStatusModel
    {
        public string bundleId { get; set; }
        public string status { get; set; }
        public int createdBy { get; set; }
    }
    public class DocumentModel
    {
        public int id { get; set; }
        public string bundleId { get; set; }
        public string docId { get; set; }
        public string docType { get; set; }
        public string branch { get; set; }
        public string attachment { get; set; }
        public string status { get; set; }
        public string rejectedReason { get; set; }
        public int createdBy { get; set; }
        public DateTime createdOn { get; set; }
        public string receivedBy { get; set; }
        public DateTime receivedOn { get; set; }
        public string createdByName { get; set; }
        public string receivedByName { get; set; }

    }

    public class PendingDocumentModel
    {
        public int id { get; set; }
        public string bundleId { get; set; }
        public string docId { get; set; }
        public string docType { get; set; }
        public string branch { get; set; }
        public string attachment { get; set; }
        public string status { get; set; }
        public string rejectedReason { get; set; }
        public int createdBy { get; set; }
        public DateTime createdOn { get; set; }
        public string receivedBy { get; set; }
        public DateTime receivedOn { get; set; }
        public string loginUser { get; set; }


    }
    public class RejectDocumentModel
    {
        public int id { get; set; }
        public string bundleId { get; set; }
        public string docId { get; set; }
        public string docType { get; set; }
        public string branch { get; set; }
        public string attachment { get; set; }
        public string status { get; set; }
        public string rejectedReason { get; set; }
        public int createdBy { get; set; }
        public DateTime createdOn { get; set; }
        public string receivedBy { get; set; }
        public DateTime receivedOn { get; set; }
        public string ReceivedName { get; set; }

    }
}
