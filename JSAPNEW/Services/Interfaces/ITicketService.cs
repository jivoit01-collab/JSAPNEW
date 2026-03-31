using JSAPNEW.Models;
using Microsoft.AspNetCore.Mvc;
using TicketSystem.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface ITicketService
    {
        Task<IEnumerable<TicketResponse>> AssignTicketAsync(TicketAssignmentModel request);
        Task<IEnumerable<TicketResponse>> CreateTicketAsync(TicketCreateModel model, List<IFormFile> files);
        Task<IEnumerable<TicketInsightsModel>> GetUserTicketsInsightsAsync(int userId, string month);
        Task<IEnumerable<OneTicketDetailsModel>> GetOneTicketDetailsAsync(int ticketId);
        Task<IEnumerable<OneTicketDetailsModel>> GetUserTicketsWithStatusAsync(int userId, string statusIds,string month);
        Task<TicketResponse> AddCatalogOneAsync(AddCatalogOneModel request);
        Task<TicketResponse> AddCatalogTwoAsync(AddCatalogTwoModel request);
        Task<IEnumerable<GetRequestModel>> GetRequestNameAsync();
        Task<IEnumerable<TicketResponse>> UpdateTicketStatusAsync(updateticketstatusmodel request);
        Task<IEnumerable<TicketResponse>> AddCommentOnTicketAsync(AddCommentOnTicketModel request);
        Task<IEnumerable<GetRequestModel>> GetRequestTypeAsync();
        Task<List<TicketCommentModel>> GetTicketCommentsAsync(int ticketId);
        Task<List<TicketAttachmentModel>> GetTicketAttachmentsAsync(int ticketId, IUrlHelper urlHelper);
        Task<IEnumerable<GetCatalogOneModel>> GetCatalogOneAsync();
        Task<IEnumerable<GetCatalogTwoModel>> GetCatalogTwoAsync(int oneId);
        Task<IEnumerable<OpenAndReopenTicketsModel>> GetOpenAndReopenTicketsAsync(string MonthYear);
        Task<TicketInsightsFlat> GetTicketInsightsByMonthAsync(string monthYear, int? assignerId);
        Task<IEnumerable<TicketsAssignedByMonth>> GetTicketsAssignedByMonthAsync(int AssignerId, string MonthYear);
        Task<IEnumerable<TicketDetailsModel>> GetTicketDetailsByAssigneeAsync(int assigneeId, string monthYear);
        Task<IEnumerable<TicketDetailsModel>> GetClosedTicketDetailsByAssigneeAsync(int assigneeId, string monthYear);
        Task<IEnumerable<TicketAssignmentInsightsModel>> GetTicketAssignmentInsightsAsync(int assigneeId, string monthYear);
        Task<IEnumerable<GetTicketStatusModel>> GetTicketStatusAsync();
    }
}
