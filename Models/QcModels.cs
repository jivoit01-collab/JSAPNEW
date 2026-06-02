namespace JSAPNEW.Models
{
    public class QcResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
    public class CreateFormResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int NewFormId { get; set; }
    }
    public class QcDocumentModel
    {
        public string DocumentTypeName { get; set; }
        public int HanaId { get; set; }
        public bool IsMandatory { get; set; }
        public string DocumentPath { get; set; }
    }

    public class CreateFormRequest
    {
        public string FormNumber { get; set; }
        public DateTime? FormDate { get; set; }
        public string Status { get; set; }
        public string? Remarks { get; set; }
        public string CreatedBy { get; set; }

        // Quality Settings
        public int QualityCheckMin { get; set; }
        public int QualityCheckMax { get; set; }
        public decimal? MinValueToPassQC { get; set; }
        public int RandomBoxCheck { get; set; } = 5;

        // Documents
        public List<QcDocumentModel> Documents { get; set; } = new List<QcDocumentModel>();
    }

    public class CreateParameterRequest
    {
        public string ParameterName { get; set; }
        public int FormId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateParameterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? newParameterId { get; set; }
    }

    public class CreateSubParameterRequest
    {
        public int ParameterId { get; set; }
        public string SubParameterName { get; set; }
        public bool IsImageMandatory { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateSubParameterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int? NewSubParameterId { get; set; }
    }

    public class ItemDataModel
    {
        // Existing SQL fields
        public int ItemDataId { get; set; }
        public int DocEntry { get; set; }
        public int DocumentId { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int LineNum { get; set; }
        public int DocNum { get; set; }
        public string Result { get; set; }
        public int FormId { get; set; }

        // 🔽 Additional fields coming from HANA (QcProductionDataModel)
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string Status { get; set; }
        public string ObjType { get; set; }
        public string PlannedQty { get; set; }
        public string Warehouse { get; set; }
        public string Type { get; set; }
        public string BaseQty { get; set; }
        public int ItemType { get; set; }
        public string Box { get; set; }
        public string Litre { get; set; }
        public string Date { get; set; }
    }

    public class FormModel
    {
        public int FormId { get; set; }
        public string FormNumber { get; set; }
        public DateTime? FormDate { get; set; }
        public string Status { get; set; }
        public string Remarks { get; set; }
        public string CreatedBy { get; set; }
        public DateTime? CreatedDate { get; set; }
        public string ModifiedBy { get; set; }
        public DateTime? ModifiedDate { get; set; }
    }

    public class QualitySettingModel
    {
        public int QualitySettingId { get; set; }
        public int FormId { get; set; }
        public decimal? QualityCheckMin { get; set; }
        public decimal? QualityCheckMax { get; set; }
        public decimal? MinValueToPassQC { get; set; }
        public int? RandomBoxCheck { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class QCDocumentModel
    {
        public int DocumentId { get; set; }
        public int FormId { get; set; }
        public string DocumentTypeName { get; set; }
        public string HanaId { get; set; }
        public bool? IsMandatory { get; set; }
        public string DocumentPath { get; set; }
        public DateTime? UploadedDate { get; set; }
        public DateTime? CreatedDate { get; set; }
    }

    public class ParameterDataModel
    {
        public int ParameterId { get; set; }
        public string ParameterName { get; set; }
        public int DisplayOrder { get; set; }
        public bool? IsActive { get; set; }
        public int FormId { get; set; }
        public int Id { get; set; }
        public int ItemDataId { get; set; }
        public string Value { get; set; }
        public string Remark { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
    }

    public class SubParameterDataModel
    {
        public int SubParameterId { get; set; }
        public int ParameterId { get; set; }
        public string SubParameterName { get; set; }
        public bool? IsImageMandatory { get; set; }
        public int SubParamDisplayOrder { get; set; }
        public bool? SubParamIsActive { get; set; }
        public DateTime? SubParamCreatedDate { get; set; }
        public int Id { get; set; }
        public int ItemDataId { get; set; }
        public string ImagePath { get; set; }
        public string DownloadUrl { get; set; }
        public double values { get; set; }
        public string Remark { get; set; }
        public DateTime? CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string ParameterName { get; set; }
        public int ParamDisplayOrder { get; set; }
    }

    public class GetFormDataUsingDocEntryResponse
    {
        public List<ItemDataModel> ItemData { get; set; }
        public FormModel Form { get; set; }
        public List<QualitySettingModel> QualitySettings { get; set; }
        public List<QCDocumentModel> Documents { get; set; }
        public List<ParameterDataModel> Parameters { get; set; }
        public List<SubParameterDataModel> SubParameters { get; set; }
    }

    public class FormParameterModel
    {
        public int ParameterId { get; set; }
        public string ParameterName { get; set; }
        public int? ParameterDisplayOrder { get; set; }
        public bool? ParameterIsActive { get; set; }
        public int? SubParameterId { get; set; }
        public string SubParameterName { get; set; }
        public bool? IsImageMandatory { get; set; }
        public int? SubParameterDisplayOrder { get; set; }
        public bool? SubParameterIsActive { get; set; }
    }
    public class GetFormStructureResponse
    {
        public FormModel Form { get; set; }
        public List<QualitySettingModel> QualitySettings { get; set; }
        public List<QCDocumentModel> Documents { get; set; }
        public List<FormParameterModel> Parameters { get; set; }
    }
    public class ItemDataInsertModel
    {
        public int DocEntry { get; set; }
        public int DocumentId { get; set; }
        public int LineNum { get; set; }
        public int DocNum { get; set; }
        public bool Result { get; set; }
    }

    // Request DTO for the procedure
    public class InsertItemDataRequest
    {
        public int FormId { get; set; }
        public List<ItemDataInsertModel> ItemDataList { get; set; }
    }
    public class ItemParameterDataModel
    {
        public int ItemDataId { get; set; }
        public int ParameterId { get; set; }
        public decimal Value { get; set; }
        public string Remark { get; set; }
    }

    public class InsertItemParameterDataRequest
    {
        public List<ItemParameterDataModel> ItemParamList { get; set; }
    }
    public class SubParameterRequest
    {
        public int ItemDataId { get; set; }
        public int ParameterId { get; set; }
        public int SubParameterId { get; set; }
        public decimal Values { get; set; }
        public string Remark { get; set; }
        public bool HasFile { get; set; }
        public string ImagePath { get; set; } // optional, service fills this
    }

    public class ProductionDocNumModel
    {
        public string DocNum { get; set; }
    }
    public class QcProductionDataModel
    {
        public int DocEntry { get; set; }
        public string DocNum { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string status { get; set; }
        public string objType { get; set; }
        public string plannedQty { get; set; }
        public string Warehouse { get; set; }
        public string Type { get; set; }
        public int LineNum { get; set; }
        public string BaseQty { get; set; }
        public int ItemType { get; set; }
        public string Box { get; set; }
        public string Litre { get; set; }
        public string Date { get; set; }

    }

    public class DocumentInsightModel
    {
        public int totalPending { get; set; }
        public int totalApproved { get; set; }
        public int totalRejected { get; set; }
        public int grandTotal { get; set; }
    }
    public class QCPendingDocumentModel
    {
        public long Id { get; set; }
        public int DocEntry { get; set; }
        public int FormId { get; set; }
        public string DocNum { get; set; }
        public int DocumentId { get; set; }
        public string DocumentName { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }

        public int FlowId { get; set; }
        public string FlowStatus { get; set; }
        public int CurrentStage { get; set; }
        public int TotalStage { get; set; }
        public int TemplateId { get; set; }
        public string TemplateName { get; set; }
        public string CurrentStageName { get; set; }

        public int TotalItems { get; set; }
        public int PassedItems { get; set; }
        public int FailedItems { get; set; }
    }
    public class QCApprovedDocumentModel
    {
        // Header info
        public int Id { get; set; }
        public int DocEntry { get; set; }
        public int FormId { get; set; }
        public string DocNum { get; set; }
        public int DocumentId { get; set; }
        public string DocumentName { get; set; }
        public DateTime DocumentCreatedOn { get; set; }
        public DateTime? DocumentUpdatedOn { get; set; }

        // Flow Status info
        public int FlowId { get; set; }
        public int ApprovedAtStageId { get; set; }
        public DateTime ApprovedOn { get; set; }
        public string ApprovalRemarks { get; set; }

        // Flow details
        public string FlowStatus { get; set; }
        public int CurrentStage { get; set; }
        public int TotalStage { get; set; }
        public DateTime WorkflowCreatedOn { get; set; }
        public DateTime? WorkflowUpdatedOn { get; set; }
        public string HanaStatus { get; set; }

        // Linked names
        public string ApprovedAtStageName { get; set; }
        public string TemplateName { get; set; }
        public string ApprovedByUser { get; set; }

        // Item statistics
        public int TotalItems { get; set; }
        public int PassedItems { get; set; }
        public int FailedItems { get; set; }
    }
    public class QCRejectedDocumentModel
    {
        public int Id { get; set; }
        public int DocEntry { get; set; }
        public int FormId { get; set; }
        public string DocNum { get; set; }
        public int DocumentId { get; set; }
        public string DocumentName { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public int FlowId { get; set; }
        public string FlowStatus { get; set; }
        public int CurrentStage { get; set; }
        public int TotalStage { get; set; }
        public int RejectedAtStageId { get; set; }
        public string RejectedAtStageName { get; set; }
        public DateTime RejectedOn { get; set; }
        public string RejectionRemarks { get; set; }
        public string TemplateName { get; set; }
        public int TotalItems { get; set; }
        public int PassedItems { get; set; }
        public int FailedItems { get; set; }
    }

    public class QcAllDocumentModel
    {
        // 🔹 Common Document Info
        public int Id { get; set; }
        public int DocEntry { get; set; }
        public int FormId { get; set; }
        public string DocNum { get; set; }
        public int DocumentId { get; set; }
        public string DocumentName { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }

        // 🔹 Workflow Info
        public int FlowId { get; set; }
        public string FlowStatus { get; set; }        // Example: "Pending", "Approved", "Rejected"
        public int CurrentStage { get; set; }
        public int TotalStage { get; set; }
        public string TemplateName { get; set; }
        public int TotalItems { get; set; }
        public int PassedItems { get; set; }
        public int FailedItems { get; set; }
        public string status { get; set; }
    }

    public class QCApprovalFlowModel
    {
        public int StageId { get; set; }
        public string StageName { get; set; }
        public int? Priority { get; set; }
        public string AssignedTo { get; set; }
        public int? UserId { get; set; }
        public string ActionStatus { get; set; }
        public DateTime? ActionDate { get; set; }
        public string Description { get; set; }
        public int? ApprovalRequired { get; set; }
        public int? RejectRequired { get; set; }
    }

    public class QcApprovalRequest
    {
        public int FlowId { get; set; }
        public int Company { get; set; }
        public int UserId { get; set; }
        public string? Remarks { get; set; }
    }

    public class QcRejectRequest
    {
        public int FlowId { get; set; }
        public int Company { get; set; }
        public int UserId { get; set; }
        public string Remarks { get; set; }
    }

    public class FormsWithUsersModel
    {
        public int FormId { get; set; }
        public string FormNumber { get; set; }
        public DateTime FormDate { get; set; }
        public string status { get; set; }
        public string remarks { get; set; }
        public string createdBy { get; set; }
        public DateTime createdDate { get; set; }
        public string modifiedByName { get; set; }
        public DateTime modifiedDate { get; set; }
        public DateTime modifiedBy { get; set; }
    }
    public class GetItemDataIdModel
    {
        public int itemDataId { get; set; }
    }

    public class UpdateFormModel
    {
        public int OldFormId { get; set; }
        public string? NewFormNumber { get; set; }
        public DateTime? FormDate { get; set; }
        public string? Status { get; set; }
        public string? Remarks { get; set; }
        public string CreatedBy { get; set; } = string.Empty;

        public int? QualityCheckMin { get; set; }
        public int? QualityCheckMax { get; set; }
        public decimal? MinValueToPassQC { get; set; }
        public int? RandomBoxCheck { get; set; }

        public List<QcDocumentModel>? Documents { get; set; }
        //public List<ParameterWithSubModel>? Parameters { get; set; }
    }

    public class UpdateFormResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int NewFormId { get; set; }
    }

    public class ParameterWithSubModel
    {
        public string ParameterName { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public List<SubParameterWithDetail>? SubParameters { get; set; }
    }

    public class SubParameterWithDetail
    {
        public string SubParameterName { get; set; }
        public bool IsImageMandatory { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

}
