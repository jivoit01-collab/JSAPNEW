using JSAPNEW.Models;
using System;
using System.ComponentModel.DataAnnotations;

namespace TicketSystem.Models
{
    public class TicketAssignmentModel
    {
        [Required]
        public int userId { get; set; }

        [Required]
        public int ticketId { get; set; }

        [Required]
        public int resolverId { get; set; }
    }

    public class TicketCreateModel
    {
        [Required]
        [StringLength(512)]
        public string Issue { get; set; }
        [Required] public string Description { get; set; }
        [Required] public int Company { get; set; }
        [Required] public int PriorityId { get; set; }
        [Required] public int DepartmentId { get; set; }
        [Required] public int CatalogOneId { get; set; }
        [Required] public int CatalogTwoId { get; set; }
        [Required] public int RequestNameId { get; set; }
        //[Required] public int RequestTypeId { get; set; }
        [Required] public string CreatedBy { get; set; }
        // For file uploads
        public List<TicketFile> Files { get; set; } = new List<TicketFile>();
    }

    public class TicketFile
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
    }
    public class TicketInsightsModel
    {
        public string status { get; set; }
        public int count { get; set; }

    }

    public class OneTicketDetailsModel
    {
        public int TicketID { get; set; }
        public string issue { get; set; }
        public string description { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public string CreatedBy { get; set; }
        public string AssignedTo { get; set; }
        public string Department { get; set; }
        public string CatalogOne { get; set; }
        public string CatalogTwo { get; set; }
        public string RequestName { get; set; }
        public int company { get; set; }
        public string createdAt { get; set; }
        public string updateAt { get; set; }
    }

    public class AddCatalogOneModel
    {
        [Required] public int userId { get; set; }
        [Required] public string name { get; set; }
        [Required] public string description { get; set; }
    }

    public class AddCatalogTwoModel
    {
        [Required] public int userId { get; set; }
        [Required] public string name { get; set; }
        [Required] public string description { get; set; }
        [Required] public int cataOneId { get; set; }
    }

    public class GetRequestModel
    {
        public int id { get; set; }
        public string name { get; set; }
    }

    public class updateticketstatusmodel
    {
        [Required]
        public int ticketId { get; set; }
        [Required]
        public int newStatusId { get; set; }
        [Required]
        public int userId { get; set; }
    }

    public class TicketResponse
    {
        public string Message { get; set; }
        public bool Success { get; set; }
    }

    public class AddCommentOnTicketModel
    {
        [Required]
        public int ticketId { get; set; }
        [Required]
        public int userId { get; set; }
        [Required]
        public string comment { get; set; }
    }

    public class GetCatalogOneModel
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string createdAt { get; set; }
    }

    public class GetCatalogTwoModel
    {
        public int id { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public int oneId { get; set; }
        public string createdAt { get; set; }
    }
    public class TicketCommentModel
    {
        public int Id { get; set; }
        public int UserId { get; set; }
        public string LoginUser { get; set; }
        public string Comment { get; set; }
        public DateTime CreatedAt { get; set; }
    }
    public class TicketAttachmentModel
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string FileType { get; set; }
        public DateTime CreatedAt { get; set; }
        public string FileUrl { get; set; }
    }
    public class OpenAndReopenTicketsModel
    {
        public int TicketId { get; set; }
        public string Issue { get; set; }
        public string Description { get; set; }
        public int StatusId { get; set; }
        public string Status { get; set; }
        public int PriorityId { get; set; }
        public string Priority { get; set; }
        public int CreatedById { get; set; }
        public string CreatedBy { get; set; }
        public int? AssignedToId { get; set; }
        public string? AssignedTo { get; set; }
        public int DepartmentId { get; set; }
        public string Department { get; set; }
        public int CatalogOneId { get; set; }
        public string CatalogOne { get; set; }
        public int CatalogTwoId { get; set; }
        public string CatalogTwo { get; set; }
        public int RequestNameId { get; set; }
        public string RequestName { get; set; }
        public int Company { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
    //Model for TicketInsights by month
    public class TicketInsightsFlat
    {
        public int OpenCount { get; set; }
        public int ReopenCount { get; set; }
        public int TotalOpenReopen { get; set; }
        public int? AssignBy { get; set; }
        public int AssignedCount { get; set; }
    }

    public class TicketsAssignedByMonth
    {
        public int TicketId { get; set; }
        public string Issue { get; set; }
        public string Description { get; set; }
        public int StatusId { get; set; }
        public string Status { get; set; }
        public int PriorityId { get; set; }
        public string Priority { get; set; }
        public int CreatedById { get; set; }
        public string CreatedBy { get; set; }
        public int? AssignedToId { get; set; }
        public string? AssignedTo { get; set; }
        public int DepartmentId { get; set; }
        public string Department { get; set; }
        public int CatalogOneId { get; set; }
        public string CatalogOne { get; set; }
        public int CatalogTwoId { get; set; }
        public string CatalogTwo { get; set; }
        public int RequestNameId { get; set; }
        public string RequestName { get; set; }
        public int Company { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int AssignRowId { get; set; }
        public int AssignBy { get; set; }
        public int AssignTo { get; set; }
    }
    public class TicketDetailsModel
    {
        public int TicketID { get; set; }
        public string issue { get; set; }
        public string description { get; set; }
        public string Status { get; set; }
        public string Priority { get; set; }
        public string CreatedBy { get; set; }
        public string AssignedTo { get; set; }
        public string Department { get; set; }
        public string CatalogOne { get; set; }
        public string CatalogTwo { get; set; }
        public string RequestName { get; set; }
        public int company { get; set; }
        public DateTime createdAt { get; set; }
        public DateTime updatedAt { get; set; }
    }

    public class TicketAssignmentInsightsModel
    {
        public int assigneeId { get; set; }
        public int openAssignedCount { get; set; }
        public int closedAssignedCount { get; set; }
        public int totalAssignedCount { get; set; }
        public decimal openPct { get; set; }
        public decimal closedPct { get; set; }
    }
    public class GetTicketStatusModel
    {
        public int id { get; set; }
        public string name { get; set; }
        public string createdAt { get; set; }
    }
}