using JSAPNEW.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface IGIGOService
    {
        Task<GateEntryResponse> InsertGateEntryAsync(GateEntryModel model);
        Task<IEnumerable<gigoModels>> InsertAttachmentAsync(AddAttachmentModel model);
        Task<List<gigoModels>> InsertVehicleDetailAsync(IEnumerable<VehicleDetails> models);
        Task<gigoModels> InsertVendorDocumentAsync(VendorDocument model);
        Task<gigoModels> InsertBSTDocumentAsync(BSTDocument model);
        Task<gigoModels> InsertCustomerDocumentAsync(CustomerDocument model);
        Task<GateEntryMasterResult> InsertGateEntryMasterAsync(GateEntryMasterRequest model);
        Task<List<PurchaseOrderModel>> GetPODataAsync(int company, int docNum);
    }
}
