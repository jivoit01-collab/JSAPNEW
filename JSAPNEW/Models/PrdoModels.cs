using System.ComponentModel.DataAnnotations;
using JSAPNEW.Data.Entities;

namespace JSAPNEW.Models
{
    public class PrdoModels
    {
        public string Message { get; set; }
        public bool Success { get; set; }
    }
    public class ApprovedProductionOrder
    {
        public int ProductionOrderId { get; set; }
        public int DocEntry { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Warehouse { get; set; }
        public decimal? PlannedQty_Pcs { get; set; }
        public decimal? PlannedQty_Ltr { get; set; }
        public decimal? PlannedQty_Box { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreatedBy { get; set; }
        public string ApprovalStatus { get; set; }
        public DateTime ApprovalDate { get; set; }
        public string ApprovalRemarks { get; set; }
    }

    public class PendingProductionOrder
    {
        public int ProductionOrderId { get; set; }
        public int DocEntry { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ItemSubGroup { get; set; }
        public string Warehouse { get; set; }
        public decimal? PlannedQty_Pcs { get; set; }
        public decimal? PlannedQty_Ltr { get; set; }
        public decimal? PlannedQty_Box { get; set; }
        public DateTime CreateDate { get; set; }
        public string CreatedBy { get; set; }
        public string Comments { get; set; }
    }

    public class RejectProductionOrder
    {
        public int ProductionOrderId { get; set; }
        public int DocEntry { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Warehouse { get; set; }
        public decimal? PlannedQty_Pcs { get; set; }
        public decimal? PlannedQty_Ltr { get; set; }
        public decimal? PlannedQty_Box { get; set; }
        public DateTime CreateDate { get; set; }
        public string Comments { get; set; }
        public string CreatedBy { get; set; }
        public string ApprovalStatus { get; set; }
        public DateTime ApprovalDate { get; set; }
        public string ApprovalRemarks { get; set; }
    }
    public class ProductionOrderInsightModel
    {
        public int TotalPending { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public int AllRequest => TotalPending + TotalApproved + TotalRejected;
    }

    public class ProductionOrderInsightAllModel
    {
        public int UserID { get; set; }
        public string UserName { get; set; }
        public int TotalPending { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public string Type { get; set; }
    }

    public class ProductionOrderApprovalRequest
    {
        [Required]
        public string DocIds { get; set; }

        [Required]
        public int Company { get; set; }

        [Required]
        public int UserId { get; set; }

        public string? Remarks { get; set; }

    }

    public class ProductionOrderRejectRequest
    {
        [Required]
        public int DocId { get; set; }

        [Required]
        public int Company { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public string Remarks { get; set; }

    }

    public class ProductionOrderResponseDTO
    {
        public ProductionOrderHeaderDTO ProductionOrderHeader { get; set; }
        public List<ProductionOrderDetailDTO> ProductionOrderDetails { get; set; }
    }

    public class ProductionOrderHeaderDTO
    {
        public int ProductionOrderId { get; set; }
        public string DocEntry { get; set; }
        public int TemplateId { get; set; }
        public int TotalStage { get; set; }
        public int CurrentStageId { get; set; }
        public string CurrentStage { get; set; }
        public string CurrentStatus { get; set; }
    }

    public class ProductionOrderDetailDTO
    {
        public string DocEntry { get; set; }
        public string Status { get; set; }
        public string Type { get; set; }
        public string Warehouse { get; set; }
        public DateTime? CreateDate { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ItemSubGroup { get; set; }
        public decimal PlannedQty_Pcs { get; set; }
        public decimal PlannedQty_Ltr { get; set; }
        public decimal PlannedQty_Box { get; set; }
        public string Comments { get; set; }
        public string CreatedBy { get; set; }
    }
    public class ProductionOrderApprovalFlowModel
    {
        public int StageId { get; set; }
        public string StageName { get; set; }
        public int Priority { get; set; }
        public string AssignedTo { get; set; }
        public string ActionStatus { get; set; }
        public DateTime? ActionDate { get; set; }
        public string Description { get; set; }
        public string ApprovalRequired { get; set; }
        public string RejectRequired { get; set; }
    }
    public class ItemLocationStockModel
    {
        public string ItemCode { get; set; }
        public string WhsCode { get; set; }
        public string WhsName { get; set; }
        public int Location { get; set; }
        public string StockQty { get; set; }
    }
    public class AllProductionOrderDetailDTO
    {
        public int ProductionOrderId { get; set; }
        public int DocEntry { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Warehouse { get; set; }
        public decimal? PlannedQty_Pcs { get; set; }
        public decimal? PlannedQty_Ltr { get; set; }
        public decimal? PlannedQty_Box { get; set; }
        public DateTime CreateDate { get; set; }
        public string Comments { get; set; }
        public string CreatedBy { get; set; }
        public string status { get; set; }
    }
}
