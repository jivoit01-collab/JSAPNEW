using JSAPNEW.Models;
using TicketSystem.Models;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azure;

namespace JSAPNEW.Services.Interfaces
{
    public interface ITicketsService
    {
        #region Project Management

        Task<CreateProjectResponse> CreateProjectAsync(CreateProjectModel model);
        Task<TicResponse> UpdateProjectAsync(UpdateProjectModel model);
        Task<TicResponse> DeleteProjectAsync(DeleteProjectModel model);
        Task<TicResponse<List<ProjectResponse>>> GetAllProjectsAsync(GetAllProjectsModel model);
        Task<TicResponse<ProjectResponse>> GetProjectByIdAsync(GetProjectByIdModel model);

        #endregion

        #region Ticket Creation

        Task<CreateTicketResponse> CreateTicketAsync(CreateTicketModel model);
        Task<TicResponse<List<TicketsResponse>>> GetMyTicketsAsync(GetMyTicketsModel model);
        Task<TicResponse<TicketsResponse>> GetTicketByIdAsync(GetTicketByIdModel model);
        Task<TicResponse> UpdateMyTicketAsync(UpdateMyTicketModel model);

        #endregion

        #region Ticket Assignment

        Task<TicResponse<List<TicketsResponse>>> GetOpenTicketsAsync(GetOpenTicketsModel model);
        Task<TicResponse> AssignTicketAsync(AssignTicketModel model);
        Task<TicResponse> ReassignTicketAsync(ReassignTicketModel model);
        Task<TicResponse<List<TicketsResponse>>> GetAllTicketsAsync(GetAllTicketsModel model);
        Task<TicResponse<List<UserForAssignmentResponse>>> GetUsersForAssignmentAsync();

        #endregion

        #region Working on Tickets

        Task<TicResponse<List<TicketsResponse>>> GetAssignedTicketsAsync(GetAssignedTicketsModel model);
        Task<TicResponse> StartTicketAsync(StartTicketModel model);
        Task<TicResponse> HoldTicketAsync(HoldTicketModel model);
        Task<TicResponse> ResumeTicketAsync(ResumeTicketModel model);
        Task<TicResponse> CloseTicketAsync(CloseTicketModel model);
        Task<TicResponse<WorkloadSummaryResponse>> GetMyWorkloadSummaryAsync(GetMyWorkloadSummaryModel model);

        #endregion

        #region Comments

        Task<AddCommentResponse> AddCommentAsync(AddCommentModel model);
        Task<TicResponse> UpdateCommentAsync(UpdateCommentModel model);
        Task<TicResponse> DeleteCommentAsync(DeleteCommentModel model);
        Task<TicResponse<List<CommentResponse>>> GetCommentsByTicketIdAsync(GetCommentsByTicketIdModel model);
        Task<TicResponse<CommentResponse>> GetCommentByIdAsync(GetCommentByIdModel model);
        Task<TicResponse<List<CommentResponse>>> GetMyCommentsAsync(GetMyCommentsModel model);

        #endregion

        #region Timeline

        Task<TicResponse<List<TicketHistoryResponse>>> GetTicketTimelineAsync(GetTicketTimelineModel model);

        #endregion

        #region Insights

        Task<TicResponse<TicketRaiserInsightsResponse>> GetTicketRaiserInsightsAsync(GetTicketRaiserInsightsModel model);
        Task<TicResponse<AssigneeInsightsResponse>> GetAssigneeInsightsAsync(GetAssigneeInsightsModel model);
        Task<TicResponse<AssignerInsightsResponse>> GetAssignerInsightsAsync(GetAssignerInsightsModel model);

        #endregion
        // Add to ITicketsService

        Task<AddAttachmentResponse> AddAttachmentAsync(TAddAttachmentModel model);
        Task<DeleteAttachmentResponse> DeleteAttachmentAsync(DeleteAttachmentModel model);
        Task<TicResponse<List<AttachmentResponse>>> GetAttachmentsByTicketIdAsync(GetAttachmentsByTicketIdModel model);
        Task<TicResponse<AttachmentResponse>> GetAttachmentByIdAsync(GetAttachmentByIdModel model);
    }
}