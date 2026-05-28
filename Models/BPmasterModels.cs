using System.Net.Mail;
using System.Security.Cryptography.X509Certificates;

namespace JSAPNEW.Models
{
    public class BPmasterModels
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
    // Models/BPMasterModels.cs
    public class BPMasterFormData
    {
        public string JsonData { get; set; } // metadata as JSON string
        public List<IFormFile> Files { get; set; } // uploaded files
    }

    public class MergeBpModel
    {
        public int flowId { get; set; }
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Type { get; set; }
        public string PartyName { get; set; }
        public string IsStaff { get; set; }
        public string StaffCode { get; set; }
        public string GroupID { get; set; }
        public string MainGroupID { get; set; }
        public string Chain { get; set; }
        public string ContactPerson { get; set; }
        public string MobileNo { get; set; }
        public string PaymentTermID { get; set; }
        public string CreditLimit { get; set; }
        public string PriceList { get; set; }
        public DateTime CreatedOn { get; set; }
        public string status { get; set; }
    }


    public class InsertBPMasterDataModel
    {
        // jsMaster
        public string Type { get; set; }
        public bool IsStaff { get; set; }
        public string StaffCode { get; set; }
        public string Name { get; set; }
        public int Company { get; set; }
        public string GroupID { get; set; }
        public string MainGroupID { get; set; }
        public string? Chain { get; set; }
        public string ContactPerson { get; set; }
        public string MobileNo { get; set; }
        public string? PaymentTermID { get; set; }
        public decimal? CreditLimit { get; set; }
        public string PriceList { get; set; }
        public int UserId { get; set; }
        public string CompanyByUser { get; set; }

        // jsTaxDetails
        public string BuyerTANNo { get; set; }
        public string PanNo { get; set; }
        public string FssaiNo { get; set; }
        public string MsmeNo { get; set; }
        public string MsmeType { get; set; }
        public string MsmeBusinessType { get; set; }

        // Bank
        public string BankName { get; set; }
        public string AccountNo { get; set; }
        public string IfscCode { get; set; }
        public int? BankCountryID { get; set; }
        public string AcctName { get; set; }
        public string Branch { get; set; }
        public string SwiftCode { get; set; }

        public List<BPMasterAddress> Addresses { get; set; }
        public List<BPContactPerson> Contacts { get; set; }
        public List<BPAttachment> Attachments { get; set; }
    }

    public class BPMasterAddress
    {
        public string Email { get; set; }
        public string AddressType { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string StateID { get; set; }
        public string CityID { get; set; }
        public string Pincode { get; set; }
        public string CountryID { get; set; }
        public string GstNo { get; set; }
        public bool IsDefault { get; set; }
        public string AddressUid { get; set; }
    }

    public class BPContactPerson
    {
        public string Designation { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public bool IsPrimary { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Telephone { get; set; }
        public string ContactUid { get; set; }
    }

    public class BPAttachment
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public string fileType { get; set; }
    }

    public class BPMasterResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public int GeneratedCode { get; set; }
    }

    public class DistinctBankNameModel
    {
        public string BankCode { get; set; }
        public string BankName { get; set; }
    }
    public class SLPnameModel
    {
        public int SlpCode { get; set; }
        public string SlpName { get; set; }
    }

    public class ChainModel
    {
        public string U_Chain { get; set; }
    }


    public class GetCountryModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class GetMainGroup
    {
        public string U_Main_Group { get; set; }
    }

    public class GetMSMEType
    {
        public string U_MSME_BType { get; set; }
    }

    public class GetPaymentModel
    {
        public string PymntGroup { get; set; }
    }
    public class GroupNameResponse
    {
        public string GroupName { get; set; }
    }
    public class PaymentGroupModel
    {
        public string PymntGroup { get; set; }
    }
    public class BPStateModel
    {
        public string Code { get; set; }
        public string Name { get; set; }
    }

    public class ApprovedBpModel
    {
        public int flowId { get; set; }
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Type { get; set; }
        public string PartyName { get; set; }
        public string IsStaff { get; set; }
        public string StaffCode { get; set; }
        public string GroupID { get; set; }
        public string MainGroupID { get; set; }
        public string Chain { get; set; }
        public string ContactPerson { get; set; }
        public string MobileNo { get; set; }
        public string PaymentTermID { get; set; }
        public string CreditLimit { get; set; }
        public string PriceList { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class BpOverallStatusModelBPStatusCounts
    {
        public string ReportType { get; set; }
        public int TotalBPs { get; set; }

        public int PendingCount { get; set; }
        public int ApprovedCount { get; set; }
        public int RejectedCount { get; set; }
        public int CompletedCount { get; set; }
        public int OnHoldCount { get; set; }
        public int OtherStatusCount { get; set; }

        public int CustomerCount { get; set; }
        public int VendorCount { get; set; }
        public int StaffCount { get; set; }

        public decimal PendingPercentage { get; set; }
        public decimal ApprovedPercentage { get; set; }
        public decimal RejectedPercentage { get; set; }

        public int Last7DaysCount { get; set; }
        public int LastMonthCount { get; set; }

        public int? AvgProcessingDays { get; set; }
    }

    public class PendingBpModel
    {
        public int flowId { get; set; }
        public int Code { get; set; }
        public int CompanyId { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public bool IsStaff { get; set; }
        public string StaffCode { get; set; }
        public string? GroupID { get; set; }
        public string? MainGroupID { get; set; }
        public string Chain { get; set; }
        public string ContactPerson { get; set; }
        public string MobileNo { get; set; }
        public string? PaymentTermID { get; set; }
        public decimal? CreditLimit { get; set; }
        public string? PriceList { get; set; }
    }

    public class RejectedBPModel
    {
        public int flowId { get; set; }
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Type { get; set; }
        public string PartyName { get; set; }
        public int? Department { get; set; }
        public decimal? Amount { get; set; }
        public string ContactPerson { get; set; }
        public string Remark { get; set; }
        public string StaffCode { get; set; }
        public bool IsStaff { get; set; }
        public string? GroupID { get; set; }
        public string MobileNo { get; set; }
        public string? PaymentTermID { get; set; }
        public string? PriceList { get; set; }
    }

    public class BP_Master
    {
        public int Code { get; set; }
        public string Type { get; set; }
        public bool IsStaff { get; set; }
        public string StaffCode { get; set; }
        public string Name { get; set; }
        public string? GroupID { get; set; }
        public string? MainGroupID { get; set; }
        public string Chain { get; set; }
        public string ContactPerson { get; set; }
        public string MobileNo { get; set; }
        public string? PaymentTermID { get; set; }
        public decimal? CreditLimit { get; set; }
        public string? PriceList { get; set; }
        public string CompanyByUser { get; set; }
        public int company { get; set; }
        public int flowId { get; set; }
    }

    public class BP_Tax
    {
        public string BuyerTANNo { get; set; }
        public string PanNo { get; set; }
        public string FssaiNo { get; set; }
        public string MsmeNo { get; set; }
        public string msmeType { get; set; }
        public string msmeBusinessType { get; set; }
    }

    public class BP_Address
    {
        public string Email { get; set; }
        public string AddressType { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string? StateID { get; set; }
        public string? CityID { get; set; }
        public string Pincode { get; set; }
        public string? CountryID { get; set; }
        public string GstNo { get; set; }
        public string GstType { get; set; }
        public bool IsDefault { get; set; }
        public string AddressUid { get; set; }
    }

    public class BP_Bank
    {
        public string BankName { get; set; }
        public string AccountNo { get; set; }
        public string IfscCode { get; set; }
        public string AcctName { get; set; }
        public string Branch { get; set; }
        public string SwiftCode { get; set; }
        public int? CountryID { get; set; }
    }

    public class BP_Contact
    {
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Designation { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Telephone { get; set; }
        public bool IsPrimary { get; set; }
        public string ContactUid { get; set; }
    }

    public class BP_Attachment
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public long FileSize { get; set; }
        public string ContentType { get; set; }
        public string fileType { get; set; }
        public string FileUrl { get; set; }
    }

    public class SingleBPDataModel
    {
        public BP_Master Master { get; set; }
        public BP_Tax TaxDetails { get; set; }
        public List<BP_Address> Addresses { get; set; }
        public List<BP_Bank> BankDetails { get; set; }
        public List<BP_Contact> ContactPersons { get; set; }
        public List<BP_Attachment> Attachments { get; set; }
    }

    public class ApproveOrRejectBpRequest
    {
        public int FlowId { get; set; }
        public int Company { get; set; }
        public int UserId { get; set; }
    }
    public class ApproveOrRejectBpResponse
    {
        public string ResultMessage { get; set; }
        public int BPCode { get; set; }
        public int BPCompany { get; set; }
    }
    public class BPGetCard
    {
        public string CardCode { get; set; }
        public string CardName { get; set; }
        public string Address { get; set; }
        public string State { get; set; }
        public string GSTRegnNo { get; set; }
    }
    public class UniquePANModel
    {
        public string PAN_Number { get; set; }
    }
    public class GSTMismatchByStateModel
    {
        public string Code { get; set; }
        public string Country { get; set; }
        public string Name { get; set; }
        public string GSTCode { get; set; }
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
        public string ListName { get; set; }
    }
    public class UidResponse
    {
        public string Message { get; set; }
    }
    public class GetPanByBranch
    {
        public string Branch { get; set; }
        public int company { get; set; }
        public string PAN { get; set; }
    }
    public class SPAData
    {
        public int id { get; set; }
        public string debPayAcct { get; set; }
        public string wtLabel { get; set; }
        public string series { get; set; }
        public int grpCode { get; set; }
    }

    public class BPMasterUpdateRequest
    {
        // Required
        public int Code { get; set; }

        // jsMaster fields
        public string Type { get; set; }              // 'V' or 'C'
        public bool? IsStaff { get; set; }
        public string StaffCode { get; set; }
        public string Name { get; set; }
        public int Company { get; set; }
        public string GroupID { get; set; }
        public string MainGroupID { get; set; }
        public string Chain { get; set; }
        public string ContactPerson { get; set; }
        public string MobileNo { get; set; }
        public string PaymentTermID { get; set; }
        public decimal? CreditLimit { get; set; }
        public string PriceList { get; set; }

        // Control params (mandatory in SP)
        public int UserId { get; set; }
        public string CompanyByUser { get; set; }

        // Tax details
        public string BuyerTANNo { get; set; }
        public string PanNo { get; set; }
        public string FssaiNo { get; set; }
        public string MsmeNo { get; set; }
        public string MsmeType { get; set; }
        public string MsmeBusinessType { get; set; }

        // Bank details (single)
        public string BankName { get; set; }
        public string AccountNo { get; set; }
        public string IfscCode { get; set; }
        public int? BankCountryID { get; set; }
        public string AcctName { get; set; }
        public string Branch { get; set; }
        public string SwiftCode { get; set; }

        // Child collections
        public List<BPMasterAddress> Addresses { get; set; }
        public List<BPContactPerson> Contacts { get; set; }
        public List<BPAttachment> Attachments { get; set; }

        // Flags
        public bool UpdateAddresses { get; set; } = false;
        public bool UpdateBankDetails { get; set; } = false;
        public bool UpdateContacts { get; set; } = false;
        public bool UpdateAttachments { get; set; } = false;
    }
    public class BpSapDataUpdateRequest
    {
        public int Id { get; set; }
        public int MasterId { get; set; }
        public string DebPayAcct { get; set; }
        public string WtLabel { get; set; }
        public string Series { get; set; }
        public string GrpCode { get; set; }
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
        public string stageName { get; set; }
        public int priority { get; set; }
        public string assignedTo { get; set; }
        public string actionStatus { get; set; }
        public string actionDate { get; set; }
        public string description { get; set; }
        public int approvalRequired { get; set; }
        public int rejectRequired { get; set; }
    }
}


