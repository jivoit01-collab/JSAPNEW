using System.ComponentModel.DataAnnotations;
using System.Globalization;

namespace JSAPNEW.Models
{
    public class InventoryAuditModels
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string OnHand { get; set; }
        public string StockValue { get; set; }
        public string SalPackMsr { get; set; }
        public string Warehouse { get; set; }
        public int LocationCode { get; set; }
        public string LocationName { get; set; }
        public string U_Sub_Group { get; set; }
        public string U_Variety { get; set; }
        public string U_Unit { get; set; }
        public int ItmsGrpCod { get; set; }
        public string ItmsGrpNam { get; set; }
        public string SalPackUn { get; set; }
        public string U_IsLitre { get; set; }
    }
    public class InventoryAuditParamModels
    {
        public string p_warehouses { get; set; }
        public string p_units { get; set; }
        public string p_itemGroups { get; set; }
        public string p_subGroups { get; set; }
        public string p_itemCodes { get; set; }
        public string p_locations { get; set; }        
        public int company { get; set; }
    }
    public class UnitModels
    {
        public string U_Unit { get; set; }
    }
    public class SubGroupModels
    {
        public string U_Sub_Group { get; set; }
    }
    public class OITMmodels
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string FrgnName { get; set; }
        public int ItmsGrpCod { get; set; }
        public string OnHand { get; set; }
        public string SalPackMsr { get; set; }
        public string SalPackUn { get; set; }
        public string U_IsLitre { get; set; }
    }
    public class LocationModels
    {
        public int LocationCode { get; set; }
        public string LocationName { get; set; }
    }
    public class WarehouseModels
    {
        public string Warehouse { get; set; }
    }
    public class StockCountItem
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string WarehouseName { get; set; }
        public string ItemGroupCode { get; set; }
        public decimal SystemCount { get; set; }
        public decimal LastCount { get; set; }
        public decimal PhysicalCount { get; set; }
        public decimal StockValue { get; set; }
        public string itmsGrpNam { get; set; }        // *** ADD THIS ***
        public string u_Sub_Group { get; set; }      // *** ADD THIS ***
    }
    public class InsertStockCountDataBulkRequest
    {
        public string LotNumber { get; set; }
        public string Unit { get; set; }
        public string LocationCode { get; set; }
        public string LocationName { get; set; }
        public string Warehouses { get; set; }        // comma-separated
        public string ItemGroups { get; set; }        // comma-separated as CODE:NAME
        public string ItemSubGroups { get; set; }     // comma-separated
        public decimal DifferenceQty { get; set; }    // *** ADD THIS ***
        public decimal DifferenceValue { get; set; }  // *** ADD THIS ***
        public string Status { get; set; } = "Pending";
        public string UserId { get; set; }
        public string Company { get; set; }
        public List<StockCountItem> Items { get; set; }
    }
    public class InventoryApiResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }
    public class GenerateUsernameRequest
    {
        public int UserId { get; set; }
    }
    public class GenerateUsernameResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public string UniqueUsername { get; set; }
    }
    public class LotNumberModels
    {
        public string LotNumber { get; set; }
    }
    public class StockCountRequest
    {
        public string unit { get; set; }
        public string location { get; set; }
        public string warehouse { get; set; }
        public string itemGroup { get; set; }
        public string itemSubGroup { get; set; }
        public string status { get; set; }
    }
    public class StockCountHeaderDto
    {
        public string LotNumber { get; set; }
        public int sessionId { get; set; }
        public string sessionName { get; set; }
        public string Unit { get; set; }
        public int LocationCode { get; set; }
        public string Location { get; set; }
        public string Status { get; set; }
        public int CreatedBy { get; set; }
        public DateTime? CreatedOn { get; set; }
        public string UpdatedBy { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public string Company { get; set; }
        public string loginUser { get; set; }
    }
    public class StockCountItemGroupDto
    {
        public string LotNumber { get; set; }
        public int ItemGroupId { get; set; }
        public string ItemGroupCode { get; set; }
        public string ItemGroupName { get; set; }
    }
    public class StockCountItemSubGroupDto
    {
        public string LotNumber { get; set; }
        public int ItemSubGroupId { get; set; }
        public string ItemSubGroupName { get; set; }
    }
    public class StockCountWarehouseDto
    {
        public string LotNumber { get; set; }
        public int WarehouseId { get; set; }
        public string WarehouseName { get; set; }
    }
    public class StockCountResponseDto
    {
        public List<StockCountHeaderDto> Headers { get; set; }
        public List<StockCountItemGroupDto> ItemGroups { get; set; }
        public List<StockCountItemSubGroupDto> ItemSubGroups { get; set; }
        public List<StockCountWarehouseDto> Warehouse { get; set; }
    }
    public class WarehouseDto
    {
        public string WarehouseName { get; set; }
    }
    public class ItemGroupDto
    {
        public string ItemGroupName { get; set; }
        public int ItemGroupCode { get; set; }
    }
    public class ItemSubGroupDto
    {
        public string ItemSubGroupName { get; set; }
    }
    public class StockCountItemDto
    {
        public int ItemId { get; set; }
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string LotNumber { get; set; }
        public string WarehouseId { get; set; }
        public decimal SystemCount { get; set; }
        public decimal LastCount { get; set; }
        public decimal PhysicalCount { get; set; }
        public decimal StockValue { get; set; }
        public string ItmsGrpNam { get; set; }
        public string U_Sub_Group { get; set; }
        public int userId { get; set; }
        public string isLitre { get; set; }
        public decimal salPackun { get; set; }
    }
    public class SessionDto
    {
        public int sessionId { get; set; }
        public int isActive { get; set; }
        public int UserId { get; set; }
    }
    // Wrapper model for returning everything together
    public class GetItemsUsingLotNumberResult
    {
        public List<StockCountHeaderDto> Headers { get; set; }
        public List<WarehouseDto> Warehouses { get; set; }
        public List<ItemGroupDto> ItemGroups { get; set; }
        public List<ItemSubGroupDto> ItemSubGroups { get; set; }
        public List<StockCountItemDto> Items { get; set; }
        public List<SessionDto> Session { get; set; }
    }
    public class UpdatePhysicalCountDto
    {
        public int ItemId { get; set; }
        public decimal NewPhysicalCount { get; set; }
        //public decimal HeaderDifferenceQty { get; set; }
       // public decimal HeaderDifferenceValue { get; set; }
        public string UpdatedBy { get; set; }
        public string Notes { get; set; }
        public string Status { get; set; }
    }

    //// again start
    public class UserAssignmentModel
    {
        public int UserId { get; set; }
        public string RoleInSession { get; set; } = "Counter"; // default
    }
    public class CreateBulkStockCountRequest
    {
        // Session parameters
        public string SessionName { get; set; }
        public string? Description { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int CreatedBy { get; set; }
        public List<UserAssignmentModel> UserAssignments { get; set; } = new();

        // StockCountHeader parameters
        public string LotNumber { get; set; }
        public string Unit { get; set; }
        public string LocationCode { get; set; }
        public string LocationName { get; set; }
        public decimal? DifferenceQty { get; set; }
        public decimal? DifferenceValue { get; set; }
        public string Status { get; set; } = "Pending";
        public string Company { get; set; }

        // Bulk data parameters
        public string Warehouses { get; set; }
        public string ItemGroups { get; set; }
        public string ItemSubGroups { get; set; }
    }
    public class CreateBulkStockCountResponse
    {
        public int SessionId { get; set; }
        public string SessionName { get; set; }
        public string StatusMessage { get; set; }
        public int UsersAssigned { get; set; }
        public int StockCountHeaderInserted { get; set; }
        public int WarehouseAssignmentsInserted { get; set; }
        public int ItemGroupAssignmentsInserted { get; set; }
        public int ItemSubGroupAssignmentsInserted { get; set; }
        public string InsertedLotNumber { get; set; }
    }
    public class ActiveSessionDetail
    {
        public int SessionId { get; set; }
        public string SessionName { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string RoleInSession { get; set; }
        public DateTime AssignedDate { get; set; }
        public string CreatedByUsername { get; set; }
        public string AssignedByUsername { get; set; }
        public int? SessionDurationDays { get; set; }
        public string SessionStatus { get; set; }
        public string LotNumber { get; set; }
    }
    public class ActiveSessionSummary
    {
        public int RequestedUserId { get; set; }
        public int TotalActiveSessions { get; set; }
        public string StatusMessage { get; set; }
    }
    public class UserActiveSessionsResponse
    {
        public List<ActiveSessionDetail> Sessions { get; set; }
        public ActiveSessionSummary Summary { get; set; }
    }
    public class InactiveSessionDetail
    {
        public int SessionId { get; set; }
        public string SessionName { get; set; }
        public string LotNumber { get; set; }
        public string sessionStatus { get; set; }
        public string Description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime LastModifiedDate { get; set; }
        public string RoleInSession { get; set; }
        public DateTime AssignedDate { get; set; }
        public string CreatedByUsername { get; set; }
        public string AssignedByUsername { get; set; }
        public string LastModifiedByUsername { get; set; }
        public int? SessionDurationDays { get; set; }
        public string InactiveReason { get; set; }
        public int DaysSinceLastModification { get; set; }
    }
    public class InactiveSessionSummary
    {
        public int RequestedUserId { get; set; }
        public int TotalInactiveSessions { get; set; }
        public int DeactivatedSessions { get; set; }
        public int UserRemovedSessions { get; set; }
        public string StatusMessage { get; set; }
    }
    public class UserInactiveSessionsResponse
    {
        public List<InactiveSessionDetail> Sessions { get; set; }
        public InactiveSessionSummary Summary { get; set; }
    }
    public class DeactivateSessionRequest
    {
        public int SessionId { get; set; }
        public int DeactivatedBy { get; set; }
        public string DeactivationReason { get; set; }
    }
    public class DeactivateSessionResponse
    {
        public int SessionId { get; set; }
        public string SessionName { get; set; }
        public int DeactivatedBy { get; set; }
        public string DeactivatedByUsername { get; set; }
        public DateTime DeactivationDate { get; set; }
        public string DeactivationReason { get; set; }
        public string StatusMessage { get; set; }
        public int TotalUsersAffected { get; set; }
        public int TotalFiltersDeactivated { get; set; }
    }
    public class StockCountItemModel
    {
        public string ItemCode { get; set; }
        public string ItemName { get; set; }
        public string WarehouseName { get; set; }
        public int ItemGroupCode { get; set; }
        public decimal? SystemCount { get; set; }
        public decimal? LastCount { get; set; }
        public decimal? PhysicalCount { get; set; }
        public decimal? StockValue { get; set; }
        public string itmsGrpNam { get; set; }
        public string u_Sub_Group { get; set; }
        public int UserId { get; set; }
        public string isLitre { get; set; }
        public decimal salPackun { get; set; }
    }

    // Request model
    public class InsertStockCountItemsRequest
    {
        public string LotNumber { get; set; }
        public List<StockCountItemModel> Items { get; set; }
        public int CreatedBy { get; set; }
        public bool AllowDuplicates { get; set; } = true;
        public string ActionType { get; set; } = "INSERT";
    }
    public class UpdateUserSessionStatusRequest
    {
        public int SessionId { get; set; }
        public int UserId { get; set; }
    }

    public class InsertSingleStockCountItemRequest
    {
        [Required] public string LotNumber { get; set; }
        [Required] public string ItemCode { get; set; }
        [Required] public string ItemName { get; set; }
        public string WarehouseName { get; set; }
        public string ItemGroupCode { get; set; }  
        public decimal SystemCount { get; set; }
        public decimal LastCount { get; set; }
        public decimal PhysicalCount { get; set; }
        public decimal StockValue { get; set; }
        public string itmsGrpNam { get; set; }
        public string u_Sub_Group { get; set; }
        public int UserId { get; set; }
        public decimal salPackun { get; set; }
        public string isLitre { get; set; }
        public int? CreatedBy { get; set; }  
        public bool AllowDuplicates { get; set; } = true;
        public string ActionType { get; set; } = "INSERT";
    }
    public class StockCountReportModel
    {
        public int ID { get; set; }
        public int SessionId { get; set; }
        public string LotNumber { get; set; }
        public string Warehouse { get; set; }
        public string ItemGroupCode { get; set; }
        public string ItemCode { get; set; }
        public string Unit { get; set; }
        public string LocationName { get; set; }
        public string ItemName { get; set; }
        public string ItemGroupName { get; set; }
        public string SubGroupName { get; set; }
        public decimal SystemQty { get; set; }
        public decimal PhysicalQty { get; set; }
        public decimal DiffQty { get; set; }
        public decimal TotalStockValue { get; set; }
        public decimal DiffValue { get; set; }
        public decimal DiffLitre { get; set; }
        public decimal SalPackUn { get; set; }
        public string IsLitreFlag { get; set; }
    }

    public class AllSessionModel
    {
        public int SessionId { get; set; }
        public string SessionName { get; set; }
        public int CreatedBy { get; set; }
        public string description { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime startDate { get; set; }
        public DateTime endDate { get; set; }
        public string LotNumber { get; set; }
    }

    public class SessionUserModel
    {
        public int SessionId { get; set; }
        public int UserId { get; set; }
        public string RoleInSession { get; set; }
        public string isActive { get; set; }
        public DateTime AssignedDate { get; set; }
        public string LoginUser { get; set; }
    }
}
