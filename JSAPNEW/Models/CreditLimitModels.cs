namespace JSAPNEW.Models
{
    public class CreditLimitApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
    public sealed class OpenCslmRequest
    {
        public int company { get; set; }
        public string CardCode { get; set; }
        public decimal CurrentLimit { get; set; }
        public decimal NewLimit { get; set; }
        public DateTime ValidTill { get; set; }
        public int CreatedBy { get; set; }
        public decimal Balance { get; set; }
    }
    public sealed class OpenCslmResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = "";
        public int ResultId { get; set; }
    }
    public sealed class GetCustomerCardModel
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string CardType { get; set; }
        public string Balance { get; set; }
        public string DebtLine { get; set; }
        public string CreditLine { get; set; }
    }
    public class CreateDocumentDto
    {
        public string BranchId { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerValue { get; set; }
        public double CurrentBalance { get; set; }
        public double CurrentCreditLimit { get; set; }
        public double NewCreditLimit { get; set; }
        public string ValidTill { get; set; }
        public int CompanyId { get; set; } 
        public int? CreatedBy { get; set; }
    }
    public class CreateDocumentDtoV2
    {
        public string BranchId { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerValue { get; set; }
        public double CurrentBalance { get; set; }
        public double CurrentCreditLimit { get; set; }
        public double NewCreditLimit { get; set; }
        public string ValidTill { get; set; }
        public int CompanyId { get; set; } = 1;
        public int? CreatedBy { get; set; }
        // ===== Attachments =====
        public List<IFormFile> Attachments { get; set; }
    }
    public class CreditLimitAttachmentDto
    {
        public long CreditDocumentId { get; set; }
        public string FileName { get; set; }
        public string Extension { get; set; }
        public string FilePath { get; set; }
        public string UploadedBy { get; set; }
    }
    public class CreateDocumentResultV2
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int CreditDocumentId { get; set; }
    }
    public class CreateDocumentResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? NewDocumentId { get; set; }
    }
    public class CLDocumentRequest
    {
        public int userId { get; set; }
        public int companyId { get; set; }
        public string month { get; set; }
    }
    public class ApprovedDocumentDto
    {
        public int Id { get; set; }
        public string BranchId { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerValue { get; set; }
        public double CustomerBalance { get; set; }
        public double CurrentCreditLimit { get; set; }
        public double NewCreditLimit { get; set; }
        public string ValidTill { get; set; }
        public int CreatedById { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int FlowId { get; set; }
        public string hanaStatusText { get; set; }
    }
    public class PendingDocumentDto
    {
        public int Id { get; set; }
        public string BranchId { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerValue { get; set; }
        public double CustomerBalance { get; set; }
        public double CurrentCreditLimit { get; set; }
        public double NewCreditLimit { get; set; }
        public string ValidTill { get; set; }
        public int CreatedById { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int FlowId { get; set; }
    }
    public class RejectedDocumentDto
    {
        public int Id { get; set; }
        public string BranchId { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerValue { get; set; }
        public double CustomerBalance { get; set; }
        public double CurrentCreditLimit { get; set; }
        public double NewCreditLimit { get; set; }
        public string ValidTill { get; set; }
        public int CreatedById { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int FlowId { get; set; }
    }
    public class AllDocumentDto
    {
        public int Id { get; set; }
        public string BranchId { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerValue { get; set; }
        public double CustomerBalance { get; set; }
        public double CurrentCreditLimit { get; set; }
        public double NewCreditLimit { get; set; }
        public string ValidTill { get; set; }
        public int CreatedById { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public int FlowId { get; set; }
        public string Status { get; set; }
    }
    public class CreditDocumentInsightResponse
    {
        public int TotalPending { get; set; }
        public int totalApproved { get; set; }
        public int totalRejected { get; set; }
        public int GrandTotal => TotalPending + totalApproved + totalRejected;
    }
    public class UserDocumentInsightsRequest
    {
        public string createdBy { get; set; }
        public string monthYear { get; set; }
    }
    public class UserDocumentInsightsResponse
    {
        public int TotalRequest => approvedRequests + rejectedRequests + PendingRequest;
        public int PendingRequest { get; set; }
        public int approvedRequests { get; set; }
        public int rejectedRequests { get; set; }
    }
    public class DocumentDetailDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string BranchId { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string CustomerValue { get; set; }
        public double CurrentBalance { get; set; }
        public double CurrentCreditLimit { get; set; }
        public double NewCreditLimit { get; set; }
        public string ValidTill { get; set; }
        public int? CreatedBy { get; set; }
        public string CreatedByUser { get; set; }
        public int? CreatedByUserId { get; set; }
        public int flowId { get; set; }
    }
    public class CreditLimitApprovalFlowDto
    {
        public int StageId { get; set; }
        public string StageName { get; set; }
        public int Priority { get; set; }
        public string AssignedTo { get; set; }
        public string ActionStatus { get; set; }
        public DateTime? ActionDate { get; set; }
        public string Description { get; set; }
        public int? ApprovalRequired { get; set; }
        public int? RejectRequired { get; set; }
    }

    public class UserDocumentRequest
    {
        public string createdBy { get; set; }
        public string monthYear { get; set; }
        public string status { get; set; }
    }
    public class UserDocumentDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerName { get; set; }
        public string BranchId { get; set; }
        public string CustomerValue { get; set; }
        public double CurremtBalance { get; set; }   // note: typo kept same as SP alias
        public double CurrentCreditLimit { get; set; }
        public double NewCreditLimit { get; set; }
        public string ValidTill { get; set; }
        public int CreatedById { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string Status { get; set; }
        public int flowId { get; set; }
    }
    public class ApproveDocumentRequest
    {
        public int FlowId { get; set; }
        public int Company { get; set; }
        public int UserId { get; set; }
        public string Remarks { get; set; }
        public string Action { get; set; } = "Approve";
    }
    public class RejectDocumentRequest
    {
        public int FlowId { get; set; }
        public int Company { get; set; }
        public int UserId { get; set; }
        public string Remarks { get; set; }
        public string Action { get; set; } = "Reject";
    }
    public class FlowStatusRequest
    {
        public string status { get; set; }
    }

    public class CreditLimitUpdateHanaStatus
    {
        public int FlowId { get; set; }
        public bool Status { get; set; }
        public string hanaStatusText { get; set; }
    }
    public class CreditLimitDocumentResponse
    {
        public bool Success { get; set; }
        public List<CreditLimitDocument> Data { get; set; }
    }

    public class CreditLimitDocument
    {
        public int Id { get; set; }
        public string BranchId { get; set; }
        public string CustomerCode { get; set; }
        public double NewCreditLimit { get; set; }
    }
    public class AfterCreatedRequestSendNotificationToUser
    {
        public int userId { get; set; }
        public string username { get; set; }
    }
    public class CreditDocumentDetailModel
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public int BranchId { get; set; }
        public string CustomerCode { get; set; }
        public string CustomerValue { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal CurrentCreditLimit { get; set; }
        public decimal NewCreditLimit { get; set; }
        public DateTime? ValidTill { get; set; }
        public int CreatedBy { get; set; }
        public string CreatedByUser { get; set; }
        public int CreatedByUserId { get; set; }
        public int FlowId { get; set; }

        public List<CreditDocumentAttachmentModel> Attachments { get; set; }
    }
    public class CreditDocumentAttachmentModel
    {
        public int attachmentId { get; set; }
        public int CreditDocumentId { get; set; }
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileExtension { get; set; }
        public int UploadedBy { get; set; }
        public DateTime UploadedOn { get; set; }
        public string DownloadUrl { get; set; }
    }
}
