using System.Text.Json.Serialization;

namespace JSAPNEW.Models
{
    public class BPmasterModels
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class BPMasterFormData
    {
        public string JsonData { get; set; } = string.Empty;
        public List<IFormFile> Files { get; set; } = new();
    }

    public class BpListModel
    {
        public int flowId { get; set; }
        public int Id { get; set; }
        public int Code { get; set; }
        public int CompanyId { get; set; }
        public string Type { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string PartyName { get; set; } = string.Empty;
        public string ForeignName { get; set; } = string.Empty;
        public string TypeOfBusiness { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string AlternateEmail { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string MainGroup { get; set; } = string.Empty;
        public string Chain { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public bool IsStaff { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string status { get; set; } = string.Empty;
        public int CurrentStage { get; set; }
        public int TotalStage { get; set; }
        public int CurrentStageId { get; set; }
        public string CurrentStageName { get; set; } = string.Empty;
        public bool IsFinalStage { get; set; }
        public string ApiStatusTag { get; set; } = string.Empty;
        public string SapStatus { get; set; } = string.Empty;
        public string ApiMessage { get; set; } = string.Empty;
        public string SapCardCode { get; set; } = string.Empty;
        public int? SapAttachmentEntry { get; set; }
        public string PayloadHash { get; set; } = string.Empty;
        public DateTime? LastAttemptOn { get; set; }
        public int? LastAttemptBy { get; set; }
        public int RetryCount { get; set; }
        public bool CanRetrySap { get; set; }
        public BP_Master Master { get; set; } = new();
        public BP_Tax TaxDetails { get; set; } = new();
        public List<BP_Address> BillingAddresses { get; set; } = new();
        public List<BP_Address> ShippingAddresses { get; set; } = new();
        public List<BP_Bank> BankDetails { get; set; } = new();
        public List<BP_Contact> ContactPersons { get; set; } = new();
        public List<BP_Attachment> Attachments { get; set; } = new();
    }

    public class BpWorkflowModel
    {
        public int FlowId { get; set; }
        public string SapStatus { get; set; } = string.Empty;
        public string ApiMessage { get; set; } = string.Empty;
        public string SapCardCode { get; set; } = string.Empty;
    }

    public class BpListResponseModel
    {
        public BpWorkflowModel Workflow { get; set; } = new();
        public BP_Master Master { get; set; } = new();
        public BP_Tax TaxDetails { get; set; } = new();
        public List<BP_Address> BillingAddresses { get; set; } = new();
        public List<BP_Address> ShippingAddresses { get; set; } = new();
        public List<BP_Bank> BankDetails { get; set; } = new();
        public List<BP_Contact> ContactPersons { get; set; } = new();
        public List<BP_Attachment> Attachments { get; set; } = new();
    }

    public class MergeBpModel : BpListModel
    {
    }

    public class ApprovedBpModel : BpListModel
    {
    }

    public class PendingBpModel : BpListModel
    {
    }

    public class RejectedBPModel : BpListModel
    {
        public string Remark { get; set; } = string.Empty;
    }

    public class InsertBPMasterDataModel
    {
        public string Type { get; set; } = string.Empty;
        public string Company { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string CustomerType { get; set; } = string.Empty;
        public string VendorType { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public string CompanyName { get; set; } = string.Empty;
        public string ForeignName { get; set; } = string.Empty;
        public string ForeignTradeName { get; set; } = string.Empty;
        public string TypeOfBusiness { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string IndustrySector { get; set; } = string.Empty;
        public string ContactFirst { get; set; } = string.Empty;
        public string ContactLast { get; set; } = string.Empty;
        public string ContactTitle { get; set; } = string.Empty;
        public string ContactMobile { get; set; } = string.Empty;
        public string ContactEmail { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string Mobile { get; set; } = string.Empty;
        public string AltContact { get; set; } = string.Empty;
        public string AlternateContact { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string AlternateEmail { get; set; } = string.Empty;
        public string Gstin { get; set; } = string.Empty;
        public string PanNumber { get; set; } = string.Empty;
        public string Pan { get; set; } = string.Empty;
        public string Tan { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR";
        public bool HasMsme { get; set; }
        public string MsmeNo { get; set; } = string.Empty;
        public string MsmeType { get; set; } = string.Empty;
        public string MsmeBType { get; set; } = string.Empty;
        public string Msme { get; set; } = string.Empty;
        public string FssaiNo { get; set; } = string.Empty;
        public string FssaiLicense { get; set; } = string.Empty;
        public string Remarks { get; set; } = string.Empty;
        public string MainGroup { get; set; } = string.Empty;
        public string MainGroupID { get; set; } = string.Empty;
        public string MgrMainGroup { get; set; } = string.Empty;
        public string Chain { get; set; } = string.Empty;
        public string MgrChain { get; set; } = string.Empty;
        public string CreditLimit { get; set; } = string.Empty;
        public string MgrCreditLimit { get; set; } = string.Empty;
        public bool IsStaff { get; set; }
        public int UserId { get; set; }
        public string CompanyByUser { get; set; } = string.Empty;
        public bool SameAsBill { get; set; }
        public string BillAddressName { get; set; } = string.Empty;
        public string BillStreet { get; set; } = string.Empty;
        public string BillBlock { get; set; } = string.Empty;
        public string BillCity { get; set; } = string.Empty;
        public string BillZip { get; set; } = string.Empty;
        public string BillState { get; set; } = string.Empty;
        public string BillCountry { get; set; } = string.Empty;
        public string ShipAddressName { get; set; } = string.Empty;
        public string ShipStreet { get; set; } = string.Empty;
        public string ShipBlock { get; set; } = string.Empty;
        public string ShipCity { get; set; } = string.Empty;
        public string ShipZip { get; set; } = string.Empty;
        public string ShipState { get; set; } = string.Empty;
        public string ShipCountry { get; set; } = string.Empty;
        public List<BPMasterAddress> BillingAddresses { get; set; } = new();
        public List<BPMasterAddress> ShippingAddresses { get; set; } = new();
        public List<BPMasterAddress> AllBillAddresses { get; set; } = new();
        public List<BPMasterAddress> AllShipAddresses { get; set; } = new();
        public List<BPBankAccount> BankAccounts { get; set; } = new();
        [Newtonsoft.Json.JsonIgnore]
        public List<BPAttachment> Attachments { get; set; } = new();
        [Newtonsoft.Json.JsonProperty("attachments")]
        public Newtonsoft.Json.Linq.JToken? AttachmentPayload { get; set; }
    }

    public class BPMasterAddress
    {
        public string AddressType { get; set; } = string.Empty;
        public string AddrName { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string Block { get; set; } = string.Empty;
        public string BlockArea { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Zip { get; set; } = string.Empty;
        public string PinCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Gstin { get; set; } = string.Empty;
        public string AddressName { get; set; } = string.Empty;
    }

    public class BPBankAccount
    {
        public string BankName { get; set; } = string.Empty;
        public string BankCode { get; set; } = string.Empty;
        public string MgrBankCode { get; set; } = string.Empty;
        public string Branch { get; set; } = string.Empty;
        public string AccNo { get; set; } = string.Empty;
        public string Ifsc { get; set; } = string.Empty;
        public string SwiftCode { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
        public bool IsPrimary { get; set; }
    }

    public class BPContactPerson
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string AlternateContact { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string AlternateEmail { get; set; } = string.Empty;
    }

    public class BPAttachment
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string fileType { get; set; } = string.Empty;
    }

    public class BPMasterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int GeneratedCode { get; set; }
    }

    public class DistinctBankNameModel
    {
        public string BankCode { get; set; } = string.Empty;
        public string BankName { get; set; } = string.Empty;
        public string SwiftNo { get; set; } = string.Empty;
        public string CountryCode { get; set; } = string.Empty;
    }

    public class SLPnameModel
    {
        public int SlpCode { get; set; }
        public string SlpName { get; set; } = string.Empty;
    }

    public class ChainModel
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string U_Chain { get; set; } = string.Empty;
    }

    public class GetCountryModel
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class GetMainGroup
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string U_Main_Group { get; set; } = string.Empty;
    }

    public class GetMSMEType
    {
        public string U_MSME_BType { get; set; } = string.Empty;
    }

    public class GetPaymentModel
    {
        public string PymntGroup { get; set; } = string.Empty;
    }

    public class GroupNameResponse
    {
        public int? GroupCode { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string GroupName { get; set; } = string.Empty;
    }

    public class PaymentGroupModel
    {
        public int? GroupNum { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string PymntGroup { get; set; } = string.Empty;
    }

    public class BPStateModel
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
    }

    public class BPOptionsModel
    {
        public List<DistinctBankNameModel> Banks { get; set; } = new();
        public List<GetCountryModel> Countries { get; set; } = new();
        public List<BPStateModel> States { get; set; } = new();
        public List<UniquePANModel> UniquePANs { get; set; } = new();
        public Dictionary<string, string> Errors { get; set; } = new();
    }

    public class BP_Master
    {
        public int Code { get; set; }
        public string Type { get; set; } = string.Empty;
        public bool IsStaff { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ForeignName { get; set; } = string.Empty;
        public string TypeOfBusiness { get; set; } = string.Empty;
        public string Industry { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string AlternateEmail { get; set; } = string.Empty;
        public string Currency { get; set; } = "INR";
        public string Remarks { get; set; } = string.Empty;
        public string MainGroup { get; set; } = string.Empty;
        public string Chain { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public string CompanyByUser { get; set; } = string.Empty;
        public int company { get; set; }
        public int flowId { get; set; }
    }

    public class BP_Tax
    {
        public string Tan { get; set; } = string.Empty;
        public string PanNumber { get; set; } = string.Empty;
        public string FssaiLicense { get; set; } = string.Empty;
        public string Msme { get; set; } = string.Empty;
        public string MsmeType { get; set; } = string.Empty;
        [JsonPropertyName("enterpriseType")]
        public string MsmeBType { get; set; } = string.Empty;
        public string Gstin { get; set; } = string.Empty;
    }

    public class BP_Address
    {
        public string AddressType { get; set; } = string.Empty;
        public string Street { get; set; } = string.Empty;
        public string BlockArea { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PinCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Gstin { get; set; } = string.Empty;
        public string AddressName { get; set; } = string.Empty;
    }

    public class BP_Bank
    {
        public string BankName { get; set; } = string.Empty;
        [JsonPropertyName("branch")]
        public string BranchName { get; set; } = string.Empty;
        [JsonPropertyName("accountNo")]
        public string AccountNumber { get; set; } = string.Empty;
        public string IfscCode { get; set; } = string.Empty;
        public string SwiftCode { get; set; } = string.Empty;
        public string AccountType { get; set; } = string.Empty;
    }

    public class BP_Contact
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Designation { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public string AlternateEmail { get; set; } = string.Empty;
        public string MobileNumber { get; set; } = string.Empty;
        public string AlternateContact { get; set; } = string.Empty;
    }

    public class BP_Attachment
    {
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string ContentType { get; set; } = string.Empty;
        public string fileType { get; set; } = string.Empty;
        public string FileUrl { get; set; } = string.Empty;
    }

    public class SingleBPDataModel
    {
        public BP_Master Master { get; set; } = new();
        public BP_Tax TaxDetails { get; set; } = new();
        public List<BP_Address> BillingAddresses { get; set; } = new();
        public List<BP_Address> ShippingAddresses { get; set; } = new();
        public List<BP_Bank> BankDetails { get; set; } = new();
        public List<BP_Contact> ContactPersons { get; set; } = new();
        public List<BP_Attachment> Attachments { get; set; } = new();
    }

    public class ApproveOrRejectBpRequest
    {
        public int FlowId { get; set; }
        public int Company { get; set; }
        public int UserId { get; set; }
        public string Remarks { get; set; } = string.Empty;
        public string Action { get; set; } = "Approve";
    }

    public class ApproveOrRejectBpResponse
    {
        public bool Success { get; set; } = true;
        public string ResultMessage { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public BpSapErrorInfo? SapError { get; set; }
        public int FlowId { get; set; }
        public int BPCode { get; set; }
        public int BPCompany { get; set; }
        public string ApprovalStatus { get; set; } = string.Empty;
        public string SapStatus { get; set; } = string.Empty;
        public string SapCardCode { get; set; } = string.Empty;
        public int? AttachmentEntry { get; set; }
        public string PayloadHash { get; set; } = string.Empty;
    }

    public class BpApprovalResponseData
    {
        public string approvalStatus { get; set; } = string.Empty;
        public string sapStatus { get; set; } = string.Empty;
        public string sapCardCode { get; set; } = string.Empty;
    }

    public class BPGetCard
    {
        public string CardCode { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string GSTRegnNo { get; set; } = string.Empty;
    }

    public class UniquePANModel
    {
        public string PAN_Number { get; set; } = string.Empty;
    }

    public class GSTMismatchByStateModel
    {
        public string Code { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string GSTCode { get; set; } = string.Empty;
    }

    public class BPCountModel
    {
        public int PendingCount { get; set; }
        public int RejectedCount { get; set; }
        public int ApprovedCount { get; set; }
        public int TotalCount => PendingCount + RejectedCount + ApprovedCount;
    }

    public class GetPricelist
    {
        public int? ListNum { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string ListName { get; set; } = string.Empty;
    }

    public class UidResponse
    {
        public string Message { get; set; } = string.Empty;
    }

    public class GetPanByBranch
    {
        public string Branch { get; set; } = string.Empty;
        public int company { get; set; }
        public string PAN { get; set; } = string.Empty;
    }

    public class SPAData
    {
        public int id { get; set; }
        public string debPayAcct { get; set; } = string.Empty;
        public string wtLabel { get; set; } = string.Empty;
        public string series { get; set; } = string.Empty;
        public string grpCode { get; set; } = string.Empty;
    }

    public class BPMasterUpdateRequest : InsertBPMasterDataModel
    {
        public int Code { get; set; }
        public bool UpdateAddresses { get; set; }
        public bool UpdateBankDetails { get; set; }
        public bool UpdateContacts { get; set; }
        public bool UpdateAttachments { get; set; }
    }

    public class BpSapDataUpdateRequest
    {
        public int Id { get; set; }
        public int MasterId { get; set; }
        public string DebPayAcct { get; set; } = string.Empty;
        public string WtLabel { get; set; } = string.Empty;
        public string Series { get; set; } = string.Empty;
        public string GrpCode { get; set; } = string.Empty;
    }

    public class BPinsightsModel
    {
        public int TotalPending { get; set; }
        public int TotalApproved { get; set; }
        public int TotalRejected { get; set; }
        public int TotalBP => TotalPending + TotalApproved + TotalRejected;
    }

    public class BPApprovalFlowModel
    {
        public int stageId { get; set; }
        public string stageName { get; set; } = string.Empty;
        public int priority { get; set; }
        public string assignedTo { get; set; } = string.Empty;
        public string actionStatus { get; set; } = string.Empty;
        public string actionDate { get; set; } = string.Empty;
        public string description { get; set; } = string.Empty;
        public int approvalRequired { get; set; }
        public int rejectRequired { get; set; }
    }
}
