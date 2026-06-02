using System;
using System.ComponentModel.DataAnnotations;
using System.Numerics;
using Newtonsoft.Json;

namespace JSAPNEW.Models
{
    public class WarehouseModel
    {
        public string whsCode { get; set; }
        public string whsName { get; set; }
        public int company { get; set; }
    }
    public class TypeModel
    {
        public string id { get; set; }
        public string type { get; set; }
    }
    public class MaterialModel
    {
        public string itemCode { get; set; }
        public string itemName { get; set; }
        public string itemGroup { get; set; }
        public int itemGroupCode { get; set; }
        public string uom { get; set; }
        public int company { get; set; }
        public string subGroup { get; set; }
        public string unit { get; set; }
        public string isLitre { get; set; }
    }
    public class ItemModel
    {
        public string itemCode { get; set; }
        public string itemName { get; set; }
        public int itemGroupCode { get; set; }
        public string salFactor2 { get; set; }
    }
    public class ResourcesModel
    {
        public string resCode { get; set; }
        public string resName { get; set; }
        public int company { get; set; }

    }
    public class AddBomComponentModel
    {
        [Required]
        public int bomId { get; set; }
        [Required]
        public string type { get; set; }
        [Required]
        public string componentCode { get; set; }
        [Required]
        public int qty { get; set; }
        [Required]
        public int company { get; set; }
        [Required]
        public string wareHouse { get; set; }
        public int updatedBy { get; set; }
        public bool update { get; set; }
    }
    public class RemoveBomComponentModel
    {
        [Required]
        public int bomComId { get; set; }
        public int updatedBy { get; set; }
        public bool update { get; set; }
    }
    public class CreateBomModel
    {
        [Required]
        public string parentCode { get; set; }
        [Required]
        public string type { get; set; }
        [Required]
        public int qty { get; set; }
        [Required]
        public int company { get; set; }
        [Required]
        public int createdBy { get; set; }
    }
    public class PendingBomModel
    {
        public int bomId { get; set; }
        public string parentCode { get; set; }
        public string type { get; set; }
        public int qty { get; set; }
        public int company { get; set; }
        public int bomVersion { get; set; }
        public int createdBy { get; set; }
        public int updatedBy { get; set; }
        public string createdDate { get; set; }
        public string updatedDate { get; set; }
        public string wareHouse { get; set; }
        public string itemGroup { get; set; }
        public int itemGroupCode { get; set; }
        public string itemName { get; set; }
        public string uom { get; set; }
        public string itemType { get; set; }
        public string createdByName { get; set; }
        public string flag { get; set; }
        public string Action { get; set; }

    }
    public class BomMaterialModel
    {
        public int bomComId { get; set; }
        public string type { get; set; }
        public int bomId { get; set; }
        public string componentCode { get; set; }
        public int qty { get; set; }
        public int company { get; set; }
        public string wareHouse { get; set; }
        public string itemGroup { get; set; }
        public string itemGroupCode { get; set; }
        public string itemName { get; set; }
        public string uom { get; set; }
        public string whsName { get; set; }
    }
    public class BomResourceModel
    {
        public int bomComId { get; set; }
        public string type { get; set; }
        public int bomId { get; set; }
        public string componentCode { get; set; }
        public int qty { get; set; }
        public int company { get; set; }
        public string wareHouse { get; set; }
        public string resName { get; set; }
        public string whsName { get; set; }
    }
    public class BomModel
    {
        public int bomId { get; set; }
        public int templateId { get; set; }
        public int stageId { get; set; }
        public int userId { get; set; }
        public string status { get; set; }
        public string createdOn { get; set; }
        public int version { get; set; }
        public string parentCode { get; set; }
        public string type { get; set; }
        public int qty { get; set; }
        public int company { get; set; }
        public int bomVersion { get; set; }
        public string createdBy { get; set; }
        public string updatedBy { get; set; }
        public string createdDate { get; set; }
        public string updatedDate { get; set; }
        public string itemGroup { get; set; }
        public int itemGroupCode { get; set; }
        public string itemName { get; set; }
        public string uom { get; set; }
        public string itemType { get; set; }
        public string createdByName { get; set; }
        public string flag { get; set; }
        public string APIRESPONSE { get; set; }
        public string tag { get; set; }
    }
    public class BomRequest
    {
        public string ParentCode { get; set; }
        public string Type { get; set; }
        public int Qty { get; set; }
        public int Company { get; set; }
        public int CreatedBy { get; set; }
        public string WareHouseCode { get; set; }
        public List<BomComponent> Components { get; set; } = new List<BomComponent>();
        public List<BomFile> Files { get; set; } = new List<BomFile>();
    }

    public class BomComponent
    {
        public string Type { get; set; }
        public string ComponentCode { get; set; }
        public int Qty { get; set; }
        public string Uom { get; set; }
        public int Company { get; set; }
        public string WareHouse { get; set; }
    }

    public class BomFile
    {
        public string Path { get; set; }
        public string FileName { get; set; }
        public string FileExt { get; set; }
        public long FileSize { get; set; }
        public string Description { get; set; }
    }

    public class TotalBomInsightsModel
    {
        public int TotalPendingBOMs { get; set; }
        public int TotalApprovedBOMs { get; set; }
        public int TotalRejectedBOMs { get; set; }
        public int TotalBOMs => TotalPendingBOMs + TotalApprovedBOMs + TotalRejectedBOMs;
    }

    public class FullHeaderDetailModel
    {
        public int bomId { get; set; }
        public string parentCode { get; set; }
        public string type { get; set; }
        public int qty { get; set; }
        public int company { get; set; }
        public int version { get; set; }
        public int createdBy { get; set; }
        public int updatedBy { get; set; }
        public string createdDate { get; set; }
        public string updatedDate { get; set; }
        public string wareHouse { get; set; }
        public string itemName { get; set; }
        public int itemGroupCode { get; set; }
        public string uom { get; set; }
        public string typeName { get; set; }
        public string whsName { get; set; }
        public int flowId { get; set; }
        public string flag { get; set; }
        public string Action { get; set; }
    }

    public class BomAllDataWithDetails
    {
        public int bomId { get; set; }
        public string parentCode { get; set; }
        public string type { get; set; }
        public int qty { get; set; }
        public int company { get; set; }
        public int bomVersion { get; set; }
        public int createdBy { get; set; }
        public int updatedBy { get; set; }
        public string createdDate { get; set; }
        public string updatedDate { get; set; }
        public string wareHouse { get; set; }
        public string itemGroup { get; set; }
        public int itemGroupCode { get; set; }
        public string itemName { get; set; }
        public string uom { get; set; }
        public string itemType { get; set; }
        public string createdByName { get; set; }
        public string flag { get; set; }
        public string Status { get; set; }
    }

    public class BomResponse
    {
        public string Message { get; set; }
        public bool Success { get; set; }
    }

    public class SAPSessionModel
    {
        public string SessionId { get; set; }
        public string RouteId { get; set; }
        public string B1Session { get; set; }
        public DateTime Expiry { get; set; }
    }
    public class SAPLoginModel
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string CompanyDB { get; set; }
    }
    public class CreateBomModel2
    {
        [Required]
        public string parentCode { get; set; }
        [Required]
        public string type { get; set; }
        [Required]
        public int qty { get; set; }
        [Required]
        public int company { get; set; }
        [Required]
        public int createdBy { get; set; }
        [Required]
        public string wareHouse { get; set; }
    }
    public class RemoveBomComponentModel2
    {
        [Required]
        public int bomComId { get; set; }
        [Required]
        public int updatedBy { get; set; }
        [Required]
        public int updateFlag { get; set; }
    }
    public class BomFileModel
    {
        public int fileId { get; set; }
        public string path { get; set; }
        public string fileName { get; set; }
        public string fileExt { get; set; }
        public string createdOn { get; set; }
        public int bomId { get; set; }
        public int version { get; set; }
        public int company { get; set; }
        public int uploadedBy { get; set; }
        public string fileSize { get; set; }
        public string description { get; set; }
        public string DownloadUrl { get; set; }
    }

    public class AddBomFileModel
    {
        [Required]
        public string path { get; set; }

        [Required]
        public string fileName { get; set; }

        [Required]
        public string fileExt { get; set; }

        [Required]
        public int bomId { get; set; }

        [Required]
        public int company { get; set; }

        [Required]
        public int uploadedBy { get; set; }

        [Required]
        public long fileSize { get; set; }

        public string description { get; set; }
    }

    public class BomHeaderModel
    {
        public int bomId { get; set; }
        public string parentCode { get; set; }
        public string type { get; set; }
        public int qty { get; set; }
        public int company { get; set; }
        public int version { get; set; }
        public string createdBy { get; set; }
        public string updatedBy { get; set; }
        public string createdDate { get; set; }
        public string updatedDate { get; set; }
        public string wareHouse { get; set; }
        public string itemGroup { get; set; }
        public int itemGroupCode { get; set; }
        public string itemName { get; set; }
        public string uom { get; set; }
        public string typeName { get; set; }
    }

    public class ApproveModel2
    {
        public int p_docId { get; set; }
        public int p_company { get; set; }
        public int p_userId { get; set; }
        public string p_remarks { get; set; }
        public string p_action { get; set; }

        //public string p_message { get; set; }
    }
    public class GetBomApprove2
    {
        public int bomId { get; set; }
        public int templateId { get; set; }
        public int stageId { get; set; }
        public int userId { get; set; }
        public string status { get; set; }
        public string createdOn { get; set; }
        public int version { get; set; }
        public string parentCode { get; set; }
        public string type { get; set; }
        public int qty { get; set; }
        public int company { get; set; }
        public int bomVersion { get; set; }
        public string createdBy { get; set; }
        public string updatedBy { get; set; }
        public string createdDate { get; set; }
        public string updatedDate { get; set; }
        public string wareHouse { get; set; }
        public string itmsGrpNam { get; set; }
        public string itmsGrpCod { get; set; }
        public string itemName { get; set; }
        public string invntryUom { get; set; }
        public string itemType { get; set; }
        public string createdByName { get; set; }
    }
    public class GetBomReject2
    {
        public int bomId { get; set; }
        public int templateId { get; set; }
        public int stageId { get; set; }
        public int userId { get; set; }
        public string status { get; set; }
        public string createdOn { get; set; }
        public int version { get; set; }
        public string parentCode { get; set; }
        public string type { get; set; }
        public int qty { get; set; }
        public int company { get; set; }
        public int bomVersion { get; set; }
        public string createdBy { get; set; }
        public string updatedBy { get; set; }
        public string createdDate { get; set; }
        public string updatedDate { get; set; }
        public string wareHouse { get; set; }
        public string itmsGrpNam { get; set; }
        public string itmsGrpCod { get; set; }
        public string itemName { get; set; }
        public string invntryUom { get; set; }
        public string itemType { get; set; }
        public string createdByName { get; set; }
        public string flag { get; set; }
    }
    public class RejectModel2
    {
        public int docId { get; set; }
        public int company { get; set; }
        public int userId { get; set; }
        public string remarks { get; set; }
        public string action { get; set; }
    }
    public class AddBomComponentModel2
    {
        [Required]
        public int bomId { get; set; }
        [Required]
        public string type { get; set; }
        [Required]
        public string componentCode { get; set; }
        [Required]
        public int qty { get; set; }
        [Required]
        public int company { get; set; }
        [Required]
        public string wareHouse { get; set; }
        [Required]
        public int updatedBy { get; set; }
        [Required]
        public int updateFlag { get; set; }
    }

    public class BomRequest2
    {
        public string parentCode { get; set; }
        public string type { get; set; }
        public int qty { get; set; }
        public int company { get; set; }
        public int createdBy { get; set; }
        public string wareHouse { get; set; }
        public List<BomComponent2> Components2 { get; set; } = new List<BomComponent2>();
        public List<BomFile2> Files2 { get; set; } = new List<BomFile2>();
    }

    public class BomComponent2
    {
        public int bomId { get; set; }
        public string type { get; set; }
        public string componentCode { get; set; }
        public int qty { get; set; }
        public int company { get; set; }
        public string wareHouse { get; set; }
        public int updatedBy { get; set; }
        public int updateFlag { get; set; }
    }

    public class BomFile2
    {
        public string path { get; set; }
        public string fileName { get; set; }
        public string fileExt { get; set; }
        public int bomId { get; set; }
        public int company { get; set; }
        public int uploadedBy { get; set; }
        public long fileSize { get; set; }
        public string description { get; set; }
    }

    public class BomHeaderData
    {
        public int bomId { get; set; }
        public string parentCode { get; set; }
        public string type { get; set; }
        public int qty { get; set; }
        public int company { get; set; }
        public int version { get; set; }
        public string wareHouse { get; set; }
        public string itmsGrpNam { get; set; }
        public string itmsGrpCod { get; set; }
        public string itemName { get; set; }
        public string invntryUom { get; set; }
        public string typeName { get; set; }
        public string createdDate { get; set; }
    }

    public class BomPendingRequest
    {
        public int bomId { get; set; }
        public string parentCode { get; set; }
        public string type { get; set; }
        public int qty { get; set; }
        public int company { get; set; }
        public int version { get; set; }
        public string wareHouse { get; set; }
        public string itmsGrpNam { get; set; }
        public string itmsGrpCod { get; set; }
        public string itemName { get; set; }
        public string invntryUom { get; set; }
        public int createdBy { get; set; }
        public string itemType { get; set; }
        public string createdDate { get; set; }
        public string createdByName { get; set; }
        public string flag { get; set; }
    }
    public class TotalBomDetails
    {
        public int bomId { get; set; }
        public string parentCode { get; set; }
        public string type { get; set; }
        public int qty { get; set; }
        public int company { get; set; }
        public int version { get; set; }
        public string wareHouse { get; set; }
        public string itmsGrpNam { get; set; }
        public string itmsGrpCod { get; set; }
        public string itemName { get; set; }
        public string invntryUom { get; set; }
        public int createdBy { get; set; }
        public string itemType { get; set; }
        public string createdDate { get; set; }
        public string createdByName { get; set; }
        public string flag { get; set; }
        public string status { get; set; }
    }
    public class BomResponse2
    {
        public string p_message { get; set; }
    }

    public class SapPendingInsertionModel
    {
        public int finalizedId { get; set; }
        public int bomId { get; set; }
        public string templateId { get; set; }
        public int totalStages { get; set; }
        public string currentStageId { get; set; }
        public string currentStage { get; set; }
        public string finalStatus { get; set; }
        public int version { get; set; }
        public DateTime updatedOn { get; set; }
        public DateTime insertedAt { get; set; }

        // Header
        public string parentCode { get; set; }
        public string parentName { get; set; }
        public string bomType { get; set; }          // "PD" or "SL"
        public int bomQty { get; set; }
        public int bomCompany { get; set; }
        public int createdBy { get; set; }
        public int updatedBy { get; set; }
        public DateTime createdDate { get; set; }
        public DateTime updatedDate { get; set; }
        public string headerWareHouse { get; set; }

        // Component
        public int bomComId { get; set; }
        public string componentType { get; set; }    // "IT" or "RS"
        public string componentCode { get; set; }
        public int componentQty { get; set; }
        public int componentCompany { get; set; }
        public string componentWareHouse { get; set; }
        public string componentName { get; set; }
    }
    public class ProductTree
    {
        public string TreeCode { get; set; }
        public string TreeType { get; set; } // "iProductionTree" or "iSalesTree"
        public decimal Quantity { get; set; }
        public string Warehouse { get; set; }
        public string ProductDescription { get; set; }
        public int PriceList { get; set; } = -1;
        public List<ProductTreeLine> ProductTreeLines { get; set; }
    }

    public class ProductTreeLine
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public decimal Quantity { get; set; }
        public int PriceList { get; set; } = -1;
        public string Warehouse { get; set; }
        public string Currency { get; set; } = "INR";
        public string IssueMethod { get; set; } = "im_Manual";
        public string ParentItem { get; set; }
        public string ItemType { get; set; }

    }
    public class SyncResult
    {
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
        public List<int> ProcessedBomIds { get; set; }
    }

    public class BomError
    {
        public string BomId { get; set; }
        public string ErrorMessage { get; set; }
    }

    public class SapBomSyncResult
    {
        public int BomId { get; set; }
        public string TreeCode { get; set; }
        public bool IsSuccess { get; set; }
        public string Message { get; set; }
    }

    public class ApprovalFlowRequest
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
    public class CreatedByBomApprovalFlow
    {
        public int bomId { get; set; }
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
    public class BomByUserIdModel
    {
        public int BomId { get; set; }
        public string Company { get; set; }
        public int Branch { get; set; }
        public string Type { get; set; }
        public string ParentCode { get; set; }
        public string parentName { get; set; }
        public string warehouse { get; set; }
        public string Qty { get; set; }
        public string CreatedByName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string UpdatedByName { get; set; }
        public int FlowId { get; set; }
        public string Status { get; set; }
    }
    // Bom updation model
    public class GetBomUpdationDataModel
    {
        // Header Details
        public string BOM_Code { get; set; }
        public string TreeType { get; set; }
        public string HeaderPriceList { get; set; }
        public string BOM_Quantity { get; set; }
        public string BOM_Name { get; set; }
        public DateTime? HeaderCreateDate { get; set; }
        public DateTime? HeaderUpdateDate { get; set; }

        // Line/Component Details
        public int ChildNum { get; set; }
        public int LineOrder { get; set; }
        public string ItemCode { get; set; }
        public string ItemQuantity { get; set; }
        public string Warehouse { get; set; }
        public string Uom { get; set; }
        public string ItemName { get; set; }
        public string LineComment { get; set; }

        public string ChildType { get; set; }
    }
    public class UpdateBomComponent
    {
        public string Type { get; set; }
        public string ComponentCode { get; set; }
        public int Qty { get; set; }
        public string Uom { get; set; }
        public int Company { get; set; }
        public string WareHouse { get; set; }
    }

    public class UpdateBomFile
    {
        public string Path { get; set; }
        public string FileName { get; set; }
        public string FileExt { get; set; }
        public long? FileSize { get; set; }
        public string Description { get; set; }
    }

    public class UpdateBomHistoryHeader
    {
        public int BomId { get; set; }
        public string ParentCode { get; set; }
        public string Type { get; set; }
        public int Qty { get; set; }
        public int Company { get; set; }
        public int Version { get; set; }
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public string WareHouse { get; set; }
    }

    public class UpdateBomHistoryComponent
    {
        public int BomId { get; set; }
        public string Type { get; set; }
        public string ComponentCode { get; set; }
        public int Qty { get; set; }
        public int Company { get; set; }
        public string WareHouse { get; set; }
        public string Uom { get; set; }
    }

    public class UpdateBomHistoryFile
    {
        public int BomId { get; set; }
        public string Path { get; set; }
        public string FileName { get; set; }
        public string FileExt { get; set; }
        public int FileSize { get; set; }
        public string Description { get; set; }
        public DateTime CreatedOn { get; set; }
        public int Version { get; set; }
        public int Company { get; set; }
        public int UploadedBy { get; set; }
    }

    public class UpdateBomRequest
    {
        public string Action { get; set; }
        public string ParentCode { get; set; }
        public string Type { get; set; }
        public int Qty { get; set; }
        public int Company { get; set; }
        public int CreatedBy { get; set; }
        public string WareHouseCode { get; set; }

        public List<UpdateBomComponent> Components { get; set; }
        public List<UpdateBomFile> Files { get; set; }
        public List<UpdateBomHistoryHeader> HistoryHeader { get; set; }
        public List<UpdateBomHistoryComponent> HistoryComponents { get; set; }
        public List<UpdateBomHistoryFile> HistoryFiles { get; set; }

    }
    public class GetDistinctBom
    {
        public string BOM_Code { get; set; }
        public string BOM_Name { get; set; }
    }

    public class OldBomHeaderModel
    {
        public int BomId { get; set; }
        public string ParentCode { get; set; }
        public string Type { get; set; }
        public int Qty { get; set; }
        public int Version { get; set; }
        public int CreatedBy { get; set; }
        public int UpdatedBy { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public string WareHouse { get; set; }

        // jsFatherMaterial columns
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ItemGroup { get; set; }
        public int ItemGroupCode { get; set; }
        public string UOM { get; set; }
        public int Company { get; set; }
        public string SubGroup { get; set; }
        public string Unit { get; set; }
        public string IsLitre { get; set; }
        public int SalFactor2 { get; set; }
    }

    public class OldITComponentModel
    {
        public int BomId { get; set; }
        public string Type { get; set; }
        public string ComponentCode { get; set; }
        public int Qty { get; set; }
        public string WareHouse { get; set; }

        // jsMaterial columns
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string ItemGroup { get; set; }
        public int ItemGroupCode { get; set; }
        public string UOM { get; set; }
        public int Company { get; set; }
        public string SubGroup { get; set; }
        public string Unit { get; set; }
        public string IsLitre { get; set; }
    }

    public class OldRSComponentModel
    {
        public int BomId { get; set; }
        public string Type { get; set; }
        public string ComponentCode { get; set; }
        public int Qty { get; set; }
        public string WareHouse { get; set; }

        // jsResources columns
        public string ResCode { get; set; }
        public string ResName { get; set; }
        public int Company { get; set; }
    }

    public class OldBomFileModel
    {
        public int BomId { get; set; }
        public string Path { get; set; }
        public string FileName { get; set; }
        public string FileExt { get; set; }
        public long FileSize { get; set; }
        public string Description { get; set; }
        public DateTime CreatedOn { get; set; }
        public int Version { get; set; }
        public int Company { get; set; }
        public int UploadedBy { get; set; }
    }

    public class OldBomPreviewResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }

        public OldBomHeaderModel Header { get; set; }
        public List<OldITComponentModel> ItComponents { get; set; }
        public List<OldRSComponentModel> RsComponents { get; set; }
        public List<OldBomFileModel> Files { get; set; }
    }
}
