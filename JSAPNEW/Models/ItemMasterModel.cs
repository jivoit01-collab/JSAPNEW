using System;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using Newtonsoft.Json;
namespace JSAPNEW.Models
{
    public class HanaCompanySettings
    {
        public string ConnectionString { get; set; }
        public string Schema { get; set; }
    }
    public class ItemMasterModel
    {
        public string Message { get; set; }
        public bool Success { get; set; }
        public string? ApprovalStatus { get; set; }   // "Done" | "Failed"
        public string? SapStatus { get; set; }        // "Success" | "Failed: <reason>" | "Skipped"
        public string? MartStatus { get; set; }       // "Inserted (ID: 512)" | "Skipped" | "Failed: <reason>"
    }
    public class HSNModel
    {
        public int AbsEntry { get; set; }

        public string ChapterID { get; set; }
        public string ChapterName { get; set; }
    }

    public class GetVarietyModel
    {
        public string Variety { get; set; }
    }


    public class GetsubgroupModel
    {
        public string SubGroup { get; set; }
    }
    public class TaxRateModel
    {
        public int TAXRATE { get; set; }
    }
    public class RecieveSKUmodel
    {
        public string SKU { get; set; }
    }

    public class InventoryUOMModel
    {
        public string InvntryUOM { get; set; }
    }
    public class PackingTypeModel
    {
        public string PackingType { get; set; }
    }

    public class PackTypeModel
    {
        public string PackType { get; set; }
    }

    public class PurPackModel
    {
        public string PurPackMsr { get; set; }
    }

    public class SalPackModel
    {
        public string SalPackMsr { get; set; }
    }
    public class SalUnitModel
    {
        public string SalUnitMsr { get; set; }
    }

    public class SKUModel
    {
        public string SKU { get; set; }
    }
    public class UnitModel
    {
        public string Unit { get; set; }
    }


    public class GetFAModel
    {
        public string FaType { get; set; }
    }
    public class BuyUnitModel
    {
        public string BuyUnitMsr { get; set; }
    }

    public class GroupModel
    {
        public int ItmsGrpCod { get; set; }
        public string ItmsGrpNam { get; set; }
    }
    public class BrandModel
    {
        public string Brand { get; set; }
    }
    public class BuyUnitMsrModel
    {
        public string BuyUOM { get; set; }
    }
    public class ApproveItemModel
    {
        [Required]
        public int itemId { get; set; }
        [Required]
        public int company { get; set; }
        [Required]
        public int userId { get; set; }
        public string? remarks { get; set; }
    }

    public class ApprovedItemModel
    {
        public int flowId { get; set; }
        public string initDataId { get; set; }
        public string itemName { get; set; }
        public int itemGroupCode { get; set; }
        public string? itemGroupName { get; set; }
        public string taxRate { get; set; }
        public string chapterId { get; set; }
        public string ChapterName { get; set; }
        public string unit { get; set; }
        public string brand { get; set; }
        public string variety { get; set; }
        public string subGroup { get; set; }
        public string sku { get; set; }
        public string isLitre { get; set; }
        public string grossWeight { get; set; }
        public string mrp { get; set; }
        public string packType { get; set; }
        public string packingType { get; set; }
        public string faType { get; set; }
        public string utype { get; set; }
        public string uom { get; set; }
        public string createDate { get; set; }
        public string createdByName { get; set; }
        public string flag { get; set; }
        public string tag { get; set; }
        public string APIRESPONSE { get; set; }


    }

    public class ItemFullDetailModel
    {
        // jsInitData
        public int flowId { get; set; }
        public int ItemId { get; set; }
        public string ItemName { get; set; }
        public int ItemGroupCode { get; set; }
        public string? itemGroupName { get; set; }
        public string TaxRate { get; set; }
        public string ChapterId { get; set; }
        public string ChapterName { get; set; }
        public string Unit { get; set; }
        public string Brand { get; set; }
        public string Variety { get; set; }
        public string SubGroup { get; set; }
        public string Sku { get; set; }
        public decimal Litre { get; set; }
        public string BoxSize { get; set; } 
        public string UnitSize { get; set; }
        public string UomGroup { get; set; }
        public string IsLitre { get; set; }
        public string GrossWeight { get; set; }
        public string Mrp { get; set; }
        public string PackType { get; set; }
        public string PackingType { get; set; }
        public string FaType { get; set; }
        public string Uom { get; set; }
        public string? Utype { get; set; }
        public string salesUom { get; set; }
        public string invUom { get; set; }
        public string purchaseUom { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? UpdationDate { get; set; }

        // jsSAPData
        public string FranName { get; set; }
        public string PrchseItem { get; set; }
        public string InvItem { get; set; }
        public decimal? NumInBuy { get; set; }
        public string SalUnitMsr { get; set; }
        public decimal? NumInSale { get; set; }
        public string EvalSystem { get; set; }
        public string ThreeType { get; set; }
        public string ManSerNum { get; set; }
        public decimal? SalFactor1 { get; set; }
        public decimal? SalFactor2 { get; set; }
        public decimal? SalFactor3 { get; set; }
        public decimal? SalFactor4 { get; set; }
        public decimal? PurFactor1 { get; set; }
        public decimal? PurFactor2 { get; set; }
        public decimal? PurFactor3 { get; set; }
        public decimal? PurFactor4 { get; set; }
        public string PurPackMsr { get; set; }
        public string PurPackUn { get; set; }
        public string SalPackUn { get; set; }
        public string ManBtchNum { get; set; }
        public string GenEntry { get; set; }
        public string WtLiable { get; set; }
        public string IssueMethod { get; set; }
        public string MngMethod { get; set; }
        public string InvntoryUom { get; set; }
        public int? Series { get; set; }
        public string GstRelevant { get; set; }
        public string GstTaxCtg { get; set; }

        public string? SellItem { get; set; }
        public string? PrcrmntMtd { get; set; }

        public DateTime? SapCreateDate { get; set; }
        public DateTime? SapUpdationDate { get; set; }
    }

    public class PendingItemModel
    {
        public int flowId { get; set; }
        public string InitDataId { get; set; }
        public string ItemName { get; set; }
        public int ItemGroupCode { get; set; }
        public string? itemGroupName { get; set; }
        public string TaxRate { get; set; }
        public string? ChapterId { get; set; }
        public string ChapterName { get; set; }
        public string Unit { get; set; }
        public string Brand { get; set; }
        public string Variety { get; set; }
        public string SubGroup { get; set; }
        public string Sku { get; set; }
        public string IsLitre { get; set; }
        public decimal Litre { get; set; }
        public string SalesUom { get; set; }
        public string InvUom { get; set; }
        public string PurchaseUom { get; set; }
        public string GrossWeight { get; set; }
        public string Mrp { get; set; }
        public string PackType { get; set; }
        public string PackingType { get; set; }
        public string FaType { get; set; }
        public string Uom { get; set; }
        public string utype { get; set; }
        public string createdByName { get; set; }
        public DateTime CreateDate { get; set; }
        public string flag { get; set; }

    }
    public class RejectedItemModel
    {
        public int flowId { get; set; }
        public long InitDataId { get; set; }
        public string ItemName { get; set; }
        public int ItemGroupCode { get; set; }
        public string? itemGroupName { get; set; }
        public string TaxRate { get; set; }
        public string? ChapterId { get; set; }
        public string ChapterName { get; set; }
        public string Unit { get; set; }
        public string Brand { get; set; }
        public string Variety { get; set; }
        public string SubGroup { get; set; }
        public string Sku { get; set; }
        public string IsLitre { get; set; }
        public string GrossWeight { get; set; }
        public string Mrp { get; set; }
        public string PackType { get; set; }
        public string PackingType { get; set; }
        public string FaType { get; set; }
        public string Uom { get; set; }
        public string Utype { get; set; }
        public string CreatedByName { get; set; }
        public string CreateDate { get; set; }
        public string flag { get; set; }
    }

    public class WorkflowInsightModel
    {
        public int PendingWorkflows { get; set; }
        public int ApprovedWorkflows { get; set; }
        public int RejectedWorkflows { get; set; }
        public int TotalWorkflows => PendingWorkflows + ApprovedWorkflows + RejectedWorkflows;
    }
    public class InsertInitDataModel
    {
        public int UserId { get; set; }
        public int Company { get; set; }
        public string? ItemName { get; set; }
        public int? ItemGroupCode { get; set; }
        public string? itemGroupName { get; set; }
        public string? TaxRate { get; set; }
        public string? ChapterId { get; set; }
        public string ChapterName { get; set; }
        public string? Unit { get; set; }
        public string? Brand { get; set; }
        public string? Variety { get; set; }
        public string? SubGroup { get; set; }
        public string? Sku { get; set; }
        public string? IsLitre { get; set; }  // "Y" or "N"
        public decimal? GrossWeight { get; set; }
        public int? Mrp { get; set; }
        public string? PackType { get; set; }
        public string? PackingType { get; set; }
        public string? FaType { get; set; }
        public string? Uom { get; set; }
       // public string? Utype { get; set; }
        public string? SalesUom { get; set; }
        public string? InvUom { get; set; }
        public string? PurchaseUom { get; set; }
    }

    public class InsertSAPDataModel
    {
        public long InitId { get; set; }
        public long? FranName { get; set; }
        public string? PrchseItem { get; set; }
        public string? InvItem { get; set; }
        public decimal? NumInBuy { get; set; }
        public string? SalUnitMsr { get; set; }
        public decimal? NumInSale { get; set; }
        public string? EvalSystem { get; set; }
        public string? ThreeType { get; set; }
        public string? ManSerNum { get; set; }
        public decimal? SalFactor1 { get; set; }
        public decimal? SalFactor2 { get; set; }
        public decimal? SalFactor3 { get; set; }
        public decimal? SalFactor4 { get; set; }
        public decimal? PurFactor1 { get; set; }
        public decimal? PurFactor2 { get; set; }
        public decimal? PurFactor3 { get; set; }
        public decimal? PurFactor4 { get; set; }
        public string? PurPackMsr { get; set; }
        public decimal? PurPackUn { get; set; }
        public decimal? SalPackUn { get; set; }
        public string? ManBtchNum { get; set; }
        public string? GenEntry { get; set; }
        public string? WtLiable { get; set; } = "W";
        public string? IssueMethod { get; set; }
        public string? MngMethod { get; set; }
        public string? InvntoryUom { get; set; }
        public int? Series { get; set; }
        public string? GstRelevant { get; set; } = "A";
        public string? GstTaxCtg { get; set; }
    }

    public class RejectItemModel
    {
        [Required] public int ItemId { get; set; }
        [Required] public int Company { get; set; }
        [Required] public int UserId { get; set; }
        [Required] public string remarks { get; set; }
    }

    public class UpdateInitDataModel
    {
        public long Id { get; set; }  // Required
        public int? Company { get; set; }
        public string? ItemName { get; set; }
        public int? ItemGroupCode { get; set; }
        public string? itemGroupName { get; set; }
        public string? TaxRate { get; set; }
        public string? ChapterId { get; set; }
        public string ChapterName { get; set; }
        public string? Unit { get; set; }
        public string? Brand { get; set; }
        public string? Variety { get; set; }
        public string? SubGroup { get; set; }
        public string? Sku { get; set; }
        public string? IsLitre { get; set; }
        public decimal? GrossWeight { get; set; }
        public int? Mrp { get; set; }
        public string? PackType { get; set; }
        public string? PackingType { get; set; }
        public string? FaType { get; set; }
        public string? Uom { get; set; }
        //public string? Utype { get; set; }
        public string? SalesUom { get; set; }
        public string? InvUom { get; set; }
        public string? PurchaseUom { get; set; }
        public int? Litre { get; set; }
        public decimal? BoxSize { get; set; }
        public decimal? UnitSize { get; set; }
        public string? UomGroup { get; set; }
    }

    public class UpdateSAPDataModel
    {
        public long InitId { get; set; }  // Required
        public long? FranName { get; set; }
        public string? PrchseItem { get; set; }
        public string? InvItem { get; set; }
        public decimal? NumInBuy { get; set; }
        public string? SalUnitMsr { get; set; }
        public decimal? NumInSale { get; set; }
        public string? EvalSystem { get; set; }
        public string? ThreeType { get; set; }
        public string? ManSerNum { get; set; }
        public decimal? SalFactor1 { get; set; }
        public decimal? SalFactor2 { get; set; }
        public decimal? SalFactor3 { get; set; }
        public decimal? SalFactor4 { get; set; }
        public decimal? PurFactor1 { get; set; }
        public decimal? PurFactor2 { get; set; }
        public decimal? PurFactor3 { get; set; }
        public decimal? PurFactor4 { get; set; }
        public string? PurPackMsr { get; set; }
        public decimal? PurPackUn { get; set; }
        public decimal? SalPackUn { get; set; }
        public string? ManBtchNum { get; set; }
        public string? GenEntry { get; set; }
        public string? WtLiable { get; set; }
        public string? IssueMethod { get; set; }
        public string? MngMethod { get; set; }
        public string? InvntoryUom { get; set; }
        public int? Series { get; set; }
        public string? GstRelevant { get; set; }
        public string? GstTaxCtg { get; set; }

        public string? SellItem { get; set; }
        public string? PrcrmntMtd { get; set; }
    }

    public class GetItemByIdModel
    {
        public int itemId { get; set; }
        public string company { get; set; }
        public int branch { get; set; }
        public string itemName { get; set; }
        public int itemGroupCode { get; set; }
        public string? itemGroupName { get; set; }
        public string subGroup { get; set; }
        public string actionByName { get; set; }
        public string createdDate { get; set; }
        public string updatedDate { get; set; }
        public int flowId { get; set; }
        public string status { get; set; } // "Pending", "Approved", "Rejected"
    }

    public class MergedItemModel
    {
        public int flowId { get; set; }
        public string InitDataId { get; set; }
        public string ItemName { get; set; }
        public int ItemGroupCode { get; set; }
        public string? itemGroupName { get; set; }
        public string TaxRate { get; set; }
        public string? ChapterId { get; set; }
        public string ChapterName { get; set; }
        public string Unit { get; set; }
        public string Brand { get; set; }
        public string Variety { get; set; }
        public string SubGroup { get; set; }
        public string Sku { get; set; }
        public string IsLitre { get; set; }
        public string Litre { get; set; }
        public string purchaseUom { get; set; }
        public string salesUom { get; set; }
        public string invUom { get; set; }
        public string GrossWeight { get; set; }
        public string Mrp { get; set; }
        public string PackType { get; set; }
        public string PackingType { get; set; }
        public string FaType { get; set; }
        public string Uom { get; set; }
        public string Utype { get; set; }
        public string createDate { get; set; }
        public string CreatedByName { get; set; }
        public string Status { get; set; } // "Pending", "Approved", "Rejected"
        public string flag { get; set; }
    }
    public class CreatedByDetailModel
    {
        public int id { get; set; }
        public int userId { get; set; }
        public int company { get; set; }
        public string action { get; set; }
        public string itemName { get; set; }
        public int itemGroupCode { get; set; }
        public string itemGroupName { get; set; }
        public int taxRate { get; set; }
        public string chapterId { get; set; }
        public string ChapterName { get; set; }
        public string unit { get; set; }
        public string brand { get; set; }
        public string variety { get; set; }
        public string subGroup { get; set; }
        public string sku { get; set; }
        public string isLitre { get; set; }
        public decimal grossWeight { get; set; }
        public int mrp { get; set; }
        public string packType { get; set; }
        public string packingType { get; set; }
        public string faType { get; set; }
        public string uom { get; set; }
        public string salesUom { get; set; }
        public string invUom { get; set; }
        public string purchaseUom { get; set; }
        public decimal litre { get; set; }
        public decimal boxSize { get; set; }
        public decimal unitSize { get; set; }
        public string uomGroup { get; set; }
        public DateTime createDate { get; set; }
        public DateTime updationDate { get; set; }
        public string u_type { get; set; }
        public int flowId { get; set; }
    }

    // --------------------------- BKDT start --------------------------------

    public class GetUserDetailsModel
    {
        public int USERID { get; set; }
        public string USER_CODE { get; set; }
    }
    public class GetMobjDetailsModel
    {
        public int Code { get; set; }
        public int ObjType { get; set; }
        public string ObjName { get; set; }
    }
    public class BKDTModel
    {
        public string Branch { get; set; }
        public string UserId { get; set; }
        public int TransType { get; set; }
        public string FromDate { get; set; }
        public string ToDate { get; set; }
        public DateTime TimeLimit { get; set; }
        public string Rights { get; set; }
        public string CreatedBy { get; set; }
        public DateTime CreatedOn { get; set; }
        public string DeletedBy { get; set; }
        public DateTime DeletedOn { get; set; }
        //public string Action { get; set; }
    }
    public class GetBKDTinsights
    {
        public int totalPending { get; set; }
        public int totalApproved { get; set; }
        public int totalRejected { get; set; }
        public int grandTotal => totalPending + totalApproved + totalRejected;
    }

    public class BKDTGetDocumentsModels
    {
        public int id { get; set; }
        public int companyId { get; set; }
        public string documentType { get; set; }
        public string branch { get; set; }
        public string action { get; set; }
        public string username { get; set; }
        public string fromDate { get; set; }
        public string toDate { get; set; }
        public string timeLimit { get; set; }
        public int createdById { get; set; }
        public string createdBy { get; set; }
        public string createdOn { get; set; }
        public int flowId { get; set; }
        public string Status { get; set; }
        public string HanaStatus { get; set; }
    }

    public class BKDTDocumentDetailModels
    {
        public int id { get; set; }
        public int companyId { get; set; }
        public string branch { get; set; }
        public string username { get; set; }
        public string documentType { get; set; }
        public string fromDate { get; set; }
        public string toDate { get; set; }
        public string timeLimit { get; set; }
        public string createdOn { get; set; }
        public string action { get; set; }
        public string createdBy { get; set; }
        public string createdByUser { get; set; }
        public int createdByUserId { get; set; }
        public int flowId { get; set; }
    }

    public class CreateDocumentRequest
    {
        public string Branch { get; set; }
        public string Username { get; set; }
        public string DocumentType { get; set; }
        public DateTime FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public DateTime? TimeLimit { get; set; }
        public string Action { get; set; }
        public int CompanyId { get; set; }
        public int? CreatedBy { get; set; }
    }

    public class CreateDocumentResponse
    {
        public int? NewDocumentId { get; set; }
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class ApproveRequestModel
    {
        [Required] public int flowId { get; set; }
        [Required] public int Company { get; set; }
        [Required] public int UserId { get; set; }
        public string? remarks { get; set; }
    }
    public class RejectRequestModel
    {
        [Required] public int flowId { get; set; }
        [Required] public int Company { get; set; }
        [Required] public int UserId { get; set; }
        [Required] public string remarks { get; set; }
    }
    public class BKDTApprovalFlow
    {
        public int stageId { get; set; }
        public string stageName { get; set; }
        public int priority { get; set; }
        public string assignedTo { get; set; }
        public string actionStatus { get; set; }
        public string actionDate { get; set; }
        public string description { get; set; }
        public int approvalRequired { get; set; }
        public int rejectRequired { get; set; }
    }

    public class UserDocumentInsightsModel
    {
        public int PendingRequests { get; set; }
        public int ApprovedRequests { get; set; }
        public int RejectedRequests { get; set; }
        public int TotalRequests => PendingRequests + ApprovedRequests + RejectedRequests;
    }
    public class UserDocumentsByCreatedByAndMonthModel
    {
        public int id { get; set; }
        public int companyId { get; set; }
        public string documentType { get; set; }
        public string branch { get; set; }
        public string action { get; set; }
        public string username { get; set; }
        public string fromDate { get; set; }
        public string toDate { get; set; }
        public string timeLimit { get; set; }
        public int createdById { get; set; }
        public string createdBy { get; set; }
        public string createdOn { get; set; }
        public string status { get; set; }
        public int flowId { get; set; }
    }

    public class DocumentTypeApiResponse
    {
        public bool Success { get; set; }
        public List<DocumentTypeData> Data { get; set; }
    }

    public class DocumentTypeData
    {
        public int Code { get; set; }
        public int ObjType { get; set; }
        public string ObjName { get; set; }
    }

    public class FlowStatus
    {
        public string Status { get; set; }
    }

    public class UpdateHanaStatusRequest
    {
        public int FlowId { get; set; }
        public bool Status { get; set; }
        public string hanastatusText { get; set; }
    }
    // --------------------------- BKDT end --------------------------------

    public class InsertFullItemDataModel
    {
        // InitData
        public int UserId { get; set; }
        public int Company { get; set; }
        public string ItemName { get; set; }
        public int? ItemGroupCode { get; set; }
        public string? itemGroupName { get; set; }
        public string TaxRate { get; set; }
        public string? ChapterId { get; set; }
        public string ChapterName { get; set; }
        public string Unit { get; set; }
        public string Brand { get; set; }
        public string Variety { get; set; }
        public string SubGroup { get; set; }
        public string Sku { get; set; }
        public string IsLitre { get; set; }
        public decimal? Litre { get; set; }
        public decimal? GrossWeight { get; set; }
        public decimal? Mrp { get; set; }
        public string PackType { get; set; }
        public string PackingType { get; set; }
        public string FaType { get; set; }
        public string Uom { get; set; }
        public string SalesUom { get; set; }
        public string InvUom { get; set; }
        public string PurchaseUom { get; set; }
        public decimal BoxSize { get; set; }
        public decimal UnitSize { get; set; }
        public string UomGroup { get; set; }
        // SAPData
        public long? FranName { get; set; }
        public string PrchseItem { get; set; }
        public string InvItem { get; set; }
        public decimal? NumInBuy { get; set; }
        public string SalUnitMsr { get; set; }
        public decimal? NumInSale { get; set; }
        public string EvalSystem { get; set; }
        public string ThreeType { get; set; }
        public string ManSerNum { get; set; }
        public decimal? SalFactor1 { get; set; }
        public decimal? SalFactor2 { get; set; }
        public decimal? SalFactor3 { get; set; }
        public decimal? SalFactor4 { get; set; }
        public decimal? PurFactor1 { get; set; }
        public decimal? PurFactor2 { get; set; }
        public decimal? PurFactor3 { get; set; }
        public decimal? PurFactor4 { get; set; }
        public string PurPackMsr { get; set; }
        public decimal? PurPackUn { get; set; }
        public decimal? SalPackUn { get; set; }
        public string ManBtchNum { get; set; }
        public string GenEntry { get; set; }
        public string WtLiable { get; set; }
        public string IssueMethod { get; set; }
        public string MngMethod { get; set; }
        public string InvntoryUom { get; set; }
        public int? Series { get; set; }
        public string GstRelevant { get; set; }
        public string GstTaxCtg { get; set; }

        public string? SellItem { get; set; }
        public string? PrcrmntMtd { get; set; }
        //public string? Utype { get; set; }

    }
    public class UOMgroupModel
    {
        public string UgpEntry { get; set; }
    }
    public class GetDistinctItemName
    {
        public string itemName { get; set; }
    }

    public class PendingItemApiInsertionsModel
    {
        public int InitId { get; set; }
        public string ItemName { get; set; }
        public string ItemGroupCode { get; set; }
        public string? itemGroupName { get; set; }
        public string TaxRate { get; set; }
        public string ChapterId { get; set; }
        public string ChapterName { get; set; }
        public string Unit { get; set; }
        public string Brand { get; set; }
        public string Variety { get; set; }
        public string SubGroup { get; set; }
        public string Sku { get; set; }
        public string IsLitre { get; set; }
        public decimal? GrossWeight { get; set; }
        public int Mrp { get; set; }
        public string PackType { get; set; }
        public string PackingType { get; set; }
        public string FaType { get; set; }
        public int UserId { get; set; }
        public string Action { get; set; }
        public int Company { get; set; }
        public string Uom { get; set; }
        //public string Utype { get; set; }
        public string SalesUom { get; set; }
        public string InvUom { get; set; }
        public string PurchaseUom { get; set; }
        public decimal? Litre { get; set; }
        public decimal? BoxSize { get; set; }
        public decimal? UnitSize { get; set; }
        public string UomGroup { get; set; }
        public int SAPId { get; set; }
        public int FranName { get; set; }
        public string PrchseItem { get; set; }
        public string InvItem { get; set; }
        public decimal? NumInBuy { get; set; }
        public string SalUnitMsr { get; set; }
        public decimal? NumInSale { get; set; }
        public string EvalSystem { get; set; }
        public string ThreeType { get; set; }
        public string ManSerNum { get; set; }
        public decimal? SalFactor1 { get; set; }
        public decimal? SalFactor2 { get; set; }
        public decimal? SalFactor3 { get; set; }
        public decimal? SalFactor4 { get; set; }
        public decimal? PurFactor1 { get; set; }
        public decimal? PurFactor2 { get; set; }
        public decimal? PurFactor3 { get; set; }
        public decimal? PurFactor4 { get; set; }
        public string PurPackMsr { get; set; }
        public decimal? PurPackUn { get; set; }
        public decimal? SalPackUn { get; set; }
        public string ManBtchNum { get; set; }
        public string GenEntry { get; set; }
        public string WtLiable { get; set; }
        public string IssueMethod { get; set; }
        public string MngMethod { get; set; }
        public string InvntoryUom { get; set; }
        public int? Series { get; set; }
        public string GstRelevant { get; set; }
        public string GstTaxCtg { get; set; }
        public string SellItem { get; set; }
        public string PrcrmntMtd { get; set; }
        public string CreateDate { get; set; }
        public string UpdationDate { get; set; }
        public string SapCreateDate { get; set; }
        public string SapUpdationDate { get; set; }

    }

    public class SapItemSyncResult
    {
        public int ItemId { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public string? MartStatus { get; set; }  // "Inserted (NewInitId: 512)" | "Skipped (...)" | "Failed: ..."
    }

    public class ItemsTree
    {
        public string ItemName { get; set; }
        public string ItemsGroupCode { get; set; }
        public string U_Rev_tax_Rate { get; set; }
        public string U_Tax_Rate { get; set; }
        public string PurchaseItem { get; set; }
        public string InventoryItem { get; set; }
        public string SalesItem { get; set; }
        public int ChapterID { get; set; }
        public string U_Unit { get; set; }
        public string U_Brand { get; set; }
        public string U_Sub_Group { get; set; }
        public string U_Variety { get; set; }
        public string U_SKU { get; set; }
        public string U_IsLitre { get; set; }
        public decimal? U_Gross_Weight { get; set; }
        public int U_MRP { get; set; }
        public string U_PACK_TYPE { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string U_Packing_Type { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? U_FA_TYPE { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string? U_FA_Type { get; set; }
        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        public string SalesUnit { get; set; }
        public string SalesPackagingUnit { get; set; }
        public string InventoryUOM { get; set; }
        public string PurchaseUnit { get; set; }
        public string PurchasePackagingUnit { get; set; }
        public decimal? SalesQtyPerPackUnit { get; set; }
        public decimal? SalesFactor2 { get; set; }
        public int UoMGroupEntry { get; set; }
        public string CostAccountingMethod { get; set; }
        public string WTLiable { get; set; }
        public string IssueMethod { get; set; }
        public string ManageBatchNumbers { get; set; }
        public string ManageSerialNumbers { get; set; } = "tNO";
        public string ForceSelectionOfSerialNumber { get; set; } = "tYES";
        public string SRIAndBatchManageMethod { get; set; } = "bomm_OnEveryTransaction";
        public int? Series { get; set; }
        public string TaxType { get; set; } = "tt_Yes";
        public string GSTRelevnt { get; set; } = "tYES";
        public string GSTTaxCategory { get; set; } = "gtc_Regular";
        public string GLMethod { get; set; }

       // public string ID { get; set; }
       // public decimal? SalPackUn { get; set; }

        public string U_TYPE { get; set; }
    }

    public class LogApiErrorRequest
    {
        public int ReferenceID { get; set; }
        public string ApiName { get; set; }
        public string ErrorMessage { get; set; }
        public string ErrorCode { get; set; }
        public string Payload { get; set; }
        public int CreatedBy { get; set; }
    }

    public class GetIMCApprovalFlowModel
    {
        public int stageId { get; set; }
        public string stageName { get; set; }
        public int priority { get; set; }
        public string assignedTo { get; set; }
        public string actionStatus { get; set; }
        public DateTime actionDate { get; set; }
        public string description { get; set; }
        public int approvalRequired { get; set; }
        public int rejectRequired { get; set; }
    }
}
