using Newtonsoft.Json.Linq;

namespace JSAPNEW.Models
{
    public class BpSapPostRequest
    {
        public int FlowId { get; set; }
        public int BpCode { get; set; }
        public int Company { get; set; }
        public int UserId { get; set; }
        public string BpType { get; set; } = string.Empty;
        public SingleBPDataModel BpData { get; set; } = new();
        public SPAData? SapData { get; set; }
    }

    public class BpSapPostResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
        public BpSapErrorInfo? SapError { get; set; }
        public string CardCode { get; set; } = string.Empty;
        public int? AttachmentEntry { get; set; }
        public string PayloadHash { get; set; } = string.Empty;
        public string CardType { get; set; } = string.Empty;
        public JObject? Payload { get; set; }
        public string RawResponse { get; set; } = string.Empty;
    }

    public class BpSapErrorInfo
    {
        public int? code { get; set; }
        public string message { get; set; } = string.Empty;
        public string field { get; set; } = string.Empty;
        public string invalidValue { get; set; } = string.Empty;
        public List<string> validValues { get; set; } = new();
        public string reason { get; set; } = string.Empty;
        public string correctionHint { get; set; } = string.Empty;
    }

    public class BpApiStatusUpdateResult
    {
        public bool ProcedureAvailable { get; set; } = true;
        public string? PreviousTag { get; set; }
        public string Message { get; set; } = string.Empty;
    }

    public class BpControlAccountValidationResult
    {
        public bool IsValid { get; set; }
        public string ErrorCode { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string BpType { get; set; } = string.Empty;
        public string AccountCode { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string Postable { get; set; } = string.Empty;
        public int? GroupMask { get; set; }
    }

    public class BpFlowRuntimeModel
    {
        public int FlowId { get; set; }
        public int BpCode { get; set; }
        public string FlowStatus { get; set; } = string.Empty;
        public int CurrentStage { get; set; }
        public int TotalStage { get; set; }
        public int CurrentStageId { get; set; }
        public int TemplateId { get; set; }
        public int Company { get; set; }
        public string BpType { get; set; } = string.Empty;
        public bool IsFinalStage => TotalStage > 0 && CurrentStage >= TotalStage;
    }
}
