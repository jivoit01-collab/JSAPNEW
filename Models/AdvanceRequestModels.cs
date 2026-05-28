using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace JSAPNEW.Models
{
    public class AdvanceResponse
    {
        public string? Message { get; set; }
        public bool Success { get; set; }
    }
    public class AdvanceRequestModels
    {
        public string Branch { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string Balance { get; set; }
        public string U_CardCode { get; set; }
        public string Series { get; set; }

    }

    public class VendorExpenseResponse
    {
        public bool success { get; set; }
        public string message { get; set; }
        public int FlowId { get; set; }
        public int ExpenseId { get; set; }
    }

    public class VendorExpenseRequest
    {
        [Required] public int Branch { get; set; }
        [Required] public string Type { get; set; }
        [Required] public string Search { get; set; }
        [Required] public int Department { get; set; }
        [Required] public float Amount { get; set; }
        public string? Purpose { get; set; }
        [Required] public string Remark { get; set; }
        [Required] public string Priority { get; set; }
        [Required] public DateTime ExpectedPayDate { get; set; }
        public DateTime? ExpectedGrpoDate { get; set; }
        [Required] public int UserId { get; set; }
        public string? emiMonth { get; set; }
        public string? po { get; set; }
    }

    public class FileDetails
    {
        public string AttachmentName { get; set; }
        public string AttachmentExtension { get; set; }
        public string AttachmentPath { get; set; }  // folder path only
        public long AttachmentSize { get; set; }    // use long for bytes
    }

    public class ExpenseInsightsModels
    {
        public int pendingExpenses { get; set; }
        public int approvedExpenses { get; set; }
        public int rejectedExpenses { get; set; }
        public int totalExpenses => pendingExpenses + approvedExpenses + rejectedExpenses;
    }
    public class ExpensesModels
    {
        public int Id { get; set; }
        public string company { get; set; }
        public int Branch { get; set; }
        public string Type { get; set; }
        public string Search { get; set; }
        public string partyName { get; set; }
        public int Department { get; set; }
        public string DepartmentName { get; set; }
        public string Amount { get; set; }
        public string Purpose { get; set; }
        public string Remark { get; set; }
        public string Priority { get; set; }
        public string ExpectedPayDate { get; set; }
        public string ExpectedGrpoDate { get; set; }
        public int UserId { get; set; }
        public string CreatedByName { get; set; }
        public string CreatedDate { get; set; }
        public string UpdatedDate { get; set; }
        public string UpdatedByName { get; set; }
        public string UpdatedStatus { get; set; }
        public string emiMonth { get; set; }
        public string po { get; set; }
        public int flowId { get; set; }
        public int approvedAmount { get; set; } // this is for only approved expenses
        public string Status { get; set; } // this is for only total expenses
    }
    public class PayFlowModel
    {
        public int Id { get; set; }
        public int ExpenseId { get; set; }
        public string Status { get; set; }
        public int CurrentStageId { get; set; }
        public int TemplateId { get; set; }
        public int TotalStage { get; set; }
        public int CurrentStage { get; set; }
        public string UpdatedOn { get; set; }
        public string CreatedOn { get; set; }
    }
    public class StageDetailsModel
    {
        public int StageId { get; set; }
        public string StageName { get; set; }
        public int Priority { get; set; }
        public string AssignedTo { get; set; }
        public string ActionStatus { get; set; }
        public string? ActionDate { get; set; }
        public string Description { get; set; }
        public int? ApprovalRequired { get; set; }
        public int? RejectRequired { get; set; }
        public int approvedAmount { get; set; }
    }
    public class ExpenseDetailsResponse
    {
        public ExpensesModels Header { get; set; }
        public PayFlowModel PayFlow { get; set; }
        public List<StageDetailsModel> Stages { get; set; }
        public List<AttachmentModels> Attachments { get; set; }
    }

    public class AttachmentModels
    {
        public int id { get; set; }
        public int expenseId { get; set; }
        public string fileName { get; set; }
        public string fileExtension { get; set; }
        public string filePath { get; set; }
        public decimal fileSize { get; set; }
        public string createdAt { get; set; }
        public string DownloadUrl { get; set; }
    }
    public class ApproveAdvPayRequest
    {
        [Required] public int flowId { get; set; }
        [Required] public int company { get; set; }
        [Required] public int userId { get; set; }
        public string remarks { get; set; } = string.Empty;
        public string action { get; set; } = string.Empty;
    }
    public class RejectAdvPayRequest
    {
        [Required] public int FlowId { get; set; }
        [Required] public int Company { get; set; }
        [Required] public int UserId { get; set; }
        public string Remarks { get; set; }
        public string Action { get; set; }
    }

    public class VendorExpenseUpdateModel
    {
        public int Id { get; set; }
        public int Branch { get; set; }
        public string Type { get; set; }
        public string Search { get; set; }
        public int Department { get; set; }
        public double Amount { get; set; }
        public string Purpose { get; set; }
        public string Remark { get; set; }
        public string Priority { get; set; }
        public DateTime? ExpectedPayDate { get; set; }
        public DateTime? ExpectedGrpoDate { get; set; }
        public string EmiMonth { get; set; }
        public int UpdatedBy { get; set; }
    }

    public class DepartmentsModel
    {
        public int deptId { get; set; }
        public string deptName { get; set; }
    }

    public class GetCustomerBalanceByBranchModel
    {
        public string Branch { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string Balance { get; set; }
        public string U_CardCode { get; set; }
        public string Series { get; set; }
    }
    public class ApprovalIdsModel
    {
        public int userId { get; set; }
        public string loginUser { get; set; }
    }
    public class ExpenseApprovalFlowResult
    {
        public IEnumerable<StageDetailsModel> Stages { get; set; }
        public int CurrentStage { get; set; }
        public int LastApprovedStage { get; set; }
        public string FlowStatus { get; set; } // "Pending", "Approved", "Rejected"
    }

    public class OilBusinessPartnerModel
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string CardType { get; set; }
        public string CurrentAccountBalance { get; set; }
        public int Series { get; set; }
        public string U_WG_CardCode { get; set; }
    }
    public class BevBusinessPartnerModel
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string CardType { get; set; }
        public string CurrentAccountBalance { get; set; }
        public int Series { get; set; }
        public string U_OIL_CardCode { get; set; }
    }
    public class SAPResponseWrapper<T>
    {
        [JsonProperty("value")]
        public List<T> Value { get; set; }
    }
    public class PurchaseOrderLineModel
    {
        public int LineNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemDescription { get; set; }
        public double Quantity { get; set; }
        public double LineTotal { get; set; }
        public double OpenAmount { get; set; }

        // Calculated property
        public double Rate => Quantity != 0 ? LineTotal / Quantity : 0;
        public string U_Remarks { get; set; }
        public decimal RemainingOpenQuantity { get; set; }
        public string MeasureUnit { get; set; }
    }


    public class PurchaseOrderModel
    {
        public int DocEntry { get; set; }
        public int DocNum { get; set; }
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public DateTime DocDate { get; set; }
        public double DocTotal { get; set; }
        public string DocumentStatus { get; set; }
        public string Comments { get; set; }
        public List<PurchaseOrderLineModel> DocumentLines { get; set; }
    }

    public class SapPurchaseOrderResponse
    {
        [JsonProperty("value")]
        public List<PurchaseOrderModel> Value { get; set; }
    }

    public class PoListModel
    {
        public string Po { get; set; }
    }
    public class GetApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public T Data { get; set; }
    }
}
