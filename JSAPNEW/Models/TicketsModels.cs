using System;
using System.Collections.Generic;

namespace JSAPNEW.Models
{
    public class TicResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    public class TicResponse<T> : TicResponse
    {
        public T Data { get; set; }
    }

    public class TApiResponse
    {
        public bool Success { get; set; }
        public int StatusCode { get; set; }
        public string Message { get; set; }
    }

    //endregion

    #region Project Models

    public class CreateProjectModel
    {
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public int CreatedBy { get; set; }
    }

    public class UpdateProjectModel
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class DeleteProjectModel
    {
        public int ProjectId { get; set; }
    }

    public class GetAllProjectsModel
    {
        public bool IncludeInactive { get; set; } = false;
    }

    public class GetProjectByIdModel
    {
        public int ProjectId { get; set; }
    }

    public class ProjectResponse
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedOn { get; set; }
        public int? CreatedBy { get; set; }
        public string CreatedByName { get; set; }
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int AssignedTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int OnHoldTickets { get; set; }
        public int ClosedTickets { get; set; }
    }

    public class CreateProjectResponse : TicResponse
    {
        public int? ProjectId { get; set; }
    }

    #endregion

    #region Ticket Models

    public class CreateTicketModel
    {
        public int ProjectId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; } = "Medium";
        public int FromUserId { get; set; }
    }

    public class UpdateMyTicketModel
    {
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Priority { get; set; }
    }

    public class GetMyTicketsModel
    {
        public int UserId { get; set; }
        public string? Status { get; set; }
        public int? ProjectId { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
    }

    public class GetTicketByIdModel
    {
        public int TicketId { get; set; }
    }

    public class TicketsResponse
    {
        public int TicketId { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public int FromUserId { get; set; }
        public string FromUserName { get; set; }
        public string FromUserEmail { get; set; }
        public int? AssignerUserId { get; set; }
        public string AssignerName { get; set; }
        public int? AssigneeUserId { get; set; }
        public string AssigneeName { get; set; }
        public string AssigneeEmail { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public DateTime? AssignedOn { get; set; }
        public DateTime? ResolvedOn { get; set; }
        public DateTime? ClosedOn { get; set; }
        public string MonthYear { get; set; }
        public int? AgeDays { get; set; }
        public int? AssignedDays { get; set; }
        public int CommentCount { get; set; }
    }

    public class CreateTicketResponse : TicResponse
    {
        public int? TicketId { get; set; }
    }

    #endregion

    #region Assignment Models

    public class GetOpenTicketsModel
    {
        public int? ProjectId { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
    }

    public class AssignTicketModel
    {
        public int TicketId { get; set; }
        public int AssignerUserId { get; set; }
        public int AssigneeUserId { get; set; }
    }

    public class ReassignTicketModel
    {
        public int TicketId { get; set; }
        public int AssignerUserId { get; set; }
        public int NewAssigneeUserId { get; set; }
    }

    public class GetAllTicketsModel
    {
        public string? Status { get; set; }
        public int? ProjectId { get; set; }
        public int? AssigneeUserId { get; set; }
        public int? FromUserId { get; set; }
        public string? Priority { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
    }

    public class UserForAssignmentResponse
    {
        public int UserId { get; set; }
        public string FullName { get; set; }
        public string UserEmail { get; set; }
        public string EmpId { get; set; }
        public int ActiveTickets { get; set; }
    }

    #endregion

    #region Working on Tickets Models

    public class GetAssignedTicketsModel
    {
        public int AssigneeUserId { get; set; }
        public string? Status { get; set; }
        public int? ProjectId { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
    }

    public class StartTicketModel
    {
        public int TicketId { get; set; }
        public int AssigneeUserId { get; set; }
    }

    public class HoldTicketModel
    {
        public int TicketId { get; set; }
        public int AssigneeUserId { get; set; }
        public string HoldReason { get; set; }
    }

    public class ResumeTicketModel
    {
        public int TicketId { get; set; }
        public int AssigneeUserId { get; set; }
    }

    public class CloseTicketModel
    {
        public int TicketId { get; set; }
        public int AssigneeUserId { get; set; }
        public string? ResolutionComment { get; set; }
    }

    public class GetMyWorkloadSummaryModel
    {
        public int AssigneeUserId { get; set; }
    }

    public class WorkloadSummaryResponse
    {
        public int TotalAssigned { get; set; }
        public int PendingToStart { get; set; }
        public int InProgress { get; set; }
        public int OnHold { get; set; }
        public int Closed { get; set; }
        public int CriticalOpen { get; set; }
        public int HighOpen { get; set; }
    }

    #endregion

    #region Comment Models

    public class AddCommentModel
    {
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string Comment { get; set; }
        public bool IsInternal { get; set; } = false;
    }

    public class UpdateCommentModel
    {
        public int CommentId { get; set; }
        public int UserId { get; set; }
        public string Comment { get; set; }
    }

    public class DeleteCommentModel
    {
        public int CommentId { get; set; }
        public int UserId { get; set; }
    }

    public class GetCommentsByTicketIdModel
    {
        public int TicketId { get; set; }
        public bool IncludeInternal { get; set; } = false;
    }

    public class GetCommentByIdModel
    {
        public int CommentId { get; set; }
    }

    public class GetMyCommentsModel
    {
        public int UserId { get; set; }
        public int? TicketId { get; set; }
    }

    public class CommentResponse
    {
        public int CommentId { get; set; }
        public int TicketId { get; set; }
        public string TicketTitle { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string Comment { get; set; }
        public bool IsInternal { get; set; }
        public DateTime CreatedOn { get; set; }
        public DateTime? UpdatedOn { get; set; }
        public bool IsEdited { get; set; }
    }

    public class AddCommentResponse : TicResponse
    {
        public int? CommentId { get; set; }
    }

    #endregion

    #region Timeline Models

    public class GetTicketTimelineModel
    {
        public int TicketId { get; set; }
    }

    public class TicketHistoryResponse
    {
        public int HistoryId { get; set; }
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string Action { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string Description { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    #endregion

    #region Insights Models

    public class GetTicketRaiserInsightsModel
    {
        public int? FromUserId { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
    }

    public class GetAssigneeInsightsModel
    {
        public int? AssigneeUserId { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
    }

    public class GetAssignerInsightsModel
    {
        public int? AssignerUserId { get; set; }
        public int? Month { get; set; }
        public int? Year { get; set; }
    }

    public class UserInsightSummary
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string EmpId { get; set; }
        public string MonthYear { get; set; }
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int AssignedTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int OnHoldTickets { get; set; }
        public int ClosedTickets { get; set; }
        public int CriticalTickets { get; set; }
        public int HighTickets { get; set; }
        public int MediumTickets { get; set; }
        public int LowTickets { get; set; }
    }

    public class AssigneeInsightSummary
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string EmpId { get; set; }
        public string MonthYear { get; set; }
        public int TotalAssigned { get; set; }
        public int PendingToStart { get; set; }
        public int InProgressTickets { get; set; }
        public int OnHoldTickets { get; set; }
        public int ClosedTickets { get; set; }
        public int ActiveTickets { get; set; }
        public int CriticalOpen { get; set; }
        public int HighOpen { get; set; }
        public int? AvgResolutionHours { get; set; }
    }

    public class AssignerInsightSummary
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string UserEmail { get; set; }
        public string EmpId { get; set; }
        public string MonthYear { get; set; }
        public int TotalAssignments { get; set; }
        public int PendingToStart { get; set; }
        public int InProgressTickets { get; set; }
        public int OnHoldTickets { get; set; }
        public int ClosedTickets { get; set; }
        public int ActiveTickets { get; set; }
        public int UniqueAssignees { get; set; }
    }

    public class DateWiseInsight
    {
        public DateTime Date { get; set; }
        public string MonthYear { get; set; }
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int AssignedTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int OnHoldTickets { get; set; }
        public int ClosedTickets { get; set; }
    }

    public class ProjectWiseInsight
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }
        public string MonthYear { get; set; }
        public int TotalTickets { get; set; }
        public int OpenTickets { get; set; }
        public int AssignedTickets { get; set; }
        public int InProgressTickets { get; set; }
        public int OnHoldTickets { get; set; }
        public int ClosedTickets { get; set; }
    }

    public class AssigneeWiseInsight
    {
        public int AssigneeUserId { get; set; }
        public string AssigneeName { get; set; }
        public string AssigneeEmail { get; set; }
        public string MonthYear { get; set; }
        public int TotalAssigned { get; set; }
        public int PendingToStart { get; set; }
        public int InProgressTickets { get; set; }
        public int OnHoldTickets { get; set; }
        public int ClosedTickets { get; set; }
    }

    public class TicketRaiserInsightsResponse
    {
        public List<UserInsightSummary> UserSummary { get; set; }
        public List<DateWiseInsight> DateWise { get; set; }
        public List<ProjectWiseInsight> ProjectWise { get; set; }
    }

    public class AssigneeInsightsResponse
    {
        public List<AssigneeInsightSummary> UserSummary { get; set; }
        public List<DateWiseInsight> DateWiseAssigned { get; set; }
        public List<DateWiseInsight> DateWiseClosed { get; set; }
        public List<ProjectWiseInsight> ProjectWise { get; set; }
    }

    public class AssignerInsightsResponse
    {
        public List<AssignerInsightSummary> UserSummary { get; set; }
        public List<DateWiseInsight> DateWise { get; set; }
        public List<AssigneeWiseInsight> AssigneeWise { get; set; }
        public List<ProjectWiseInsight> ProjectWise { get; set; }
    }

    // ============================================
    // ATTACHMENT MODELS (SIMPLE)
    // ============================================

    public class TAddAttachmentModel
    {
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
    }

    public class DeleteAttachmentModel
    {
        public int AttachmentId { get; set; }
        public int UserId { get; set; }
    }

    public class GetAttachmentsByTicketIdModel
    {
        public int TicketId { get; set; }
    }

    public class GetAttachmentByIdModel
    {
        public int AttachmentId { get; set; }
    }

    public class AttachmentResponse
    {
        public int AttachmentId { get; set; }
        public int TicketId { get; set; }
        public int UserId { get; set; }
        public string UserName { get; set; }
        public string FileName { get; set; }
        public string FilePath { get; set; }
        public DateTime CreatedOn { get; set; }
    }

    public class AddAttachmentResponse : Response
    {
        public int? AttachmentId { get; set; }
    }

    public class DeleteAttachmentResponse : Response
    {
        public string FilePath { get; set; }
    }

    #endregion
}