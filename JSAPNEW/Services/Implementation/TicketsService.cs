using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using TicketSystem.Models;
using System;
using System.Collections.Generic;
using Azure;
using Dapper;

namespace JSAPNEW.Services.Implementation
{
    public class TicketsService : ITicketsService
    {
        private readonly string _connectionString;

        public TicketsService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }

        #region Project Management

        public async Task<CreateProjectResponse> CreateProjectAsync(CreateProjectModel model)
        {
            var response = new CreateProjectResponse();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[CreateProject]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@projectName", model.ProjectName);
                    command.Parameters.AddWithValue("@description", model.Description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@createdBy", model.CreatedBy);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = Convert.ToBoolean(reader["success"]);
                            response.Message = reader["message"]?.ToString();
                            response.ProjectId = reader["projectId"] != DBNull.Value ? Convert.ToInt32(reader["projectId"]) : null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse> UpdateProjectAsync(UpdateProjectModel model)
        {
            var response = new TicResponse();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[UpdateProject]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@projectId", model.ProjectId);
                    command.Parameters.AddWithValue("@projectName", model.ProjectName);
                    command.Parameters.AddWithValue("@description", model.Description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@isActive", model.IsActive);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = Convert.ToBoolean(reader["success"]);
                            response.Message = reader["message"]?.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse> DeleteProjectAsync(DeleteProjectModel model)
        {
            var response = new TicResponse();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[DeleteProject]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@projectId", model.ProjectId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = Convert.ToBoolean(reader["success"]);
                            response.Message = reader["message"]?.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse<List<ProjectResponse>>> GetAllProjectsAsync(GetAllProjectsModel model)
        {
            var response = new TicResponse<List<ProjectResponse>> { Data = new List<ProjectResponse>() };
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetAllProjects]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@includeInactive", model.IncludeInactive);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            response.Data.Add(new ProjectResponse
                            {
                                ProjectId = Convert.ToInt32(reader["projectId"]),
                                ProjectName = reader["projectName"]?.ToString(),
                                Description = reader["description"]?.ToString(),
                                IsActive = Convert.ToBoolean(reader["isActive"]),
                                CreatedOn = Convert.ToDateTime(reader["createdOn"]),
                                CreatedBy = reader["createdBy"] != DBNull.Value ? Convert.ToInt32(reader["createdBy"]) : null,
                                CreatedByName = reader["createdByName"]?.ToString(),
                                TotalTickets = Convert.ToInt32(reader["totalTickets"]),
                                OpenTickets = Convert.ToInt32(reader["openTickets"])
                            });
                        }
                    }
                    response.Success = true;
                    response.Message = "Projects retrieved successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse<ProjectResponse>> GetProjectByIdAsync(GetProjectByIdModel model)
        {
            var response = new TicResponse<ProjectResponse>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetProjectById]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@projectId", model.ProjectId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            // Check if it's an error response
                            if (reader.FieldCount == 2 && HasColumn(reader, "success"))
                            {
                                response.Success = Convert.ToBoolean(reader["success"]);
                                response.Message = reader["message"]?.ToString();
                            }
                            else
                            {
                                response.Data = new ProjectResponse
                                {
                                    ProjectId = Convert.ToInt32(reader["projectId"]),
                                    ProjectName = reader["projectName"]?.ToString(),
                                    Description = reader["description"]?.ToString(),
                                    IsActive = Convert.ToBoolean(reader["isActive"]),
                                    CreatedOn = Convert.ToDateTime(reader["createdOn"]),
                                    CreatedBy = reader["createdBy"] != DBNull.Value ? Convert.ToInt32(reader["createdBy"]) : null,
                                    CreatedByName = reader["createdByName"]?.ToString(),
                                    TotalTickets = Convert.ToInt32(reader["totalTickets"]),
                                    OpenTickets = Convert.ToInt32(reader["openTickets"]),
                                    AssignedTickets = Convert.ToInt32(reader["assignedTickets"]),
                                    InProgressTickets = Convert.ToInt32(reader["inProgressTickets"]),
                                    OnHoldTickets = Convert.ToInt32(reader["onHoldTickets"]),
                                    ClosedTickets = Convert.ToInt32(reader["closedTickets"])
                                };
                                response.Success = true;
                                response.Message = "Project retrieved successfully";
                            }
                        }
                        else
                        {
                            response.Success = false;
                            response.Message = "Project not found";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        #endregion

        #region Ticket Creation

        public async Task<CreateTicketResponse> CreateTicketAsync(CreateTicketModel model)
        {
            var response = new CreateTicketResponse();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[CreateTicket]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@projectId", model.ProjectId);
                    command.Parameters.AddWithValue("@title", model.Title);
                    command.Parameters.AddWithValue("@description", model.Description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@priority", model.Priority ?? "Medium");
                    command.Parameters.AddWithValue("@fromUserId", model.FromUserId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = Convert.ToBoolean(reader["success"]);
                            response.Message = reader["message"]?.ToString();
                            response.TicketId = reader["ticketId"] != DBNull.Value ? Convert.ToInt32(reader["ticketId"]) : null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse<List<TicketsResponse>>> GetMyTicketsAsync(GetMyTicketsModel model)
        {
            var response = new TicResponse<List<TicketsResponse>> { Data = new List<TicketsResponse>() };
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetMyTickets]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@userId", model.UserId);
                    command.Parameters.AddWithValue("@status", model.Status ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@projectId", model.ProjectId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@month", model.Month ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@year", model.Year ?? (object)DBNull.Value);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            response.Data.Add(MapTicketResponse(reader));
                        }
                    }
                    response.Success = true;
                    response.Message = "Tickets retrieved successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse<TicketsResponse>> GetTicketByIdAsync(GetTicketByIdModel model)
        {
            var response = new TicResponse<TicketsResponse>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetTicketById]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ticketId", model.TicketId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            if (reader.FieldCount == 2 && HasColumn(reader, "success"))
                            {
                                response.Success = Convert.ToBoolean(reader["success"]);
                                response.Message = reader["message"]?.ToString();
                            }
                            else
                            {
                                response.Data = MapTicketResponse(reader);
                                response.Success = true;
                                response.Message = "Ticket retrieved successfully";
                            }
                        }
                        else
                        {
                            response.Success = false;
                            response.Message = "Ticket not found";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse> UpdateMyTicketAsync(UpdateMyTicketModel model)
        {
            var response = new TicResponse();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[UpdateMyTicket]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ticketId", model.TicketId);
                    command.Parameters.AddWithValue("@userId", model.UserId);
                    command.Parameters.AddWithValue("@title", model.Title);
                    command.Parameters.AddWithValue("@description", model.Description ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@priority", model.Priority ?? (object)DBNull.Value);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = Convert.ToBoolean(reader["success"]);
                            response.Message = reader["message"]?.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        #endregion

        #region Ticket Assignment

        public async Task<TicResponse<List<TicketsResponse>>> GetOpenTicketsAsync(GetOpenTicketsModel model)
        {
            var response = new TicResponse<List<TicketsResponse>> { Data = new List<TicketsResponse>() };
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetOpenTickets]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@projectId", model.ProjectId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@month", model.Month ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@year", model.Year ?? (object)DBNull.Value);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            response.Data.Add(new TicketsResponse
                            {
                                TicketId = Convert.ToInt32(reader["ticketId"]),
                                ProjectId = Convert.ToInt32(reader["projectId"]),
                                ProjectName = reader["projectName"]?.ToString(),
                                Title = reader["title"]?.ToString(),
                                Description = reader["description"]?.ToString(),
                                Status = reader["status"]?.ToString(),
                                Priority = reader["priority"]?.ToString(),
                                FromUserId = Convert.ToInt32(reader["fromUserId"]),
                                FromUserName = reader["fromUserName"]?.ToString(),
                                FromUserEmail = reader["fromUserEmail"]?.ToString(),
                                CreatedOn = Convert.ToDateTime(reader["createdOn"]),
                                MonthYear = reader["monthYear"]?.ToString(),
                                AgeDays = reader["ageDays"] != DBNull.Value ? Convert.ToInt32(reader["ageDays"]) : null
                            });
                        }
                    }
                    response.Success = true;
                    response.Message = "Open tickets retrieved successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse> AssignTicketAsync(AssignTicketModel model)
        {
            var response = new TicResponse();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[AssignTicket]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ticketId", model.TicketId);
                    command.Parameters.AddWithValue("@assignerUserId", model.AssignerUserId);
                    command.Parameters.AddWithValue("@assigneeUserId", model.AssigneeUserId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = Convert.ToBoolean(reader["success"]);
                            response.Message = reader["message"]?.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse> ReassignTicketAsync(ReassignTicketModel model)
        {
            var response = new TicResponse();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[ReassignTicket]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ticketId", model.TicketId);
                    command.Parameters.AddWithValue("@assignerUserId", model.AssignerUserId);
                    command.Parameters.AddWithValue("@newAssigneeUserId", model.NewAssigneeUserId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = Convert.ToBoolean(reader["success"]);
                            response.Message = reader["message"]?.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse<List<TicketsResponse>>> GetAllTicketsAsync(GetAllTicketsModel model)
        {
            var response = new TicResponse<List<TicketsResponse>> { Data = new List<TicketsResponse>() };
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetAllTickets]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@status", model.Status ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@projectId", model.ProjectId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@assigneeUserId", model.AssigneeUserId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@fromUserId", model.FromUserId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@priority", model.Priority ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@month", model.Month ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@year", model.Year ?? (object)DBNull.Value);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            response.Data.Add(MapTicketResponse(reader));
                        }
                    }
                    response.Success = true;
                    response.Message = "Tickets retrieved successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse<List<UserForAssignmentResponse>>> GetUsersForAssignmentAsync()
        {
            var response = new TicResponse<List<UserForAssignmentResponse>> { Data = new List<UserForAssignmentResponse>() };
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetUsersForAssignment]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            response.Data.Add(new UserForAssignmentResponse
                            {
                                UserId = Convert.ToInt32(reader["userId"]),
                                FullName = reader["fullName"]?.ToString(),
                                UserEmail = reader["userEmail"]?.ToString(),
                                EmpId = reader["empId"]?.ToString(),
                                ActiveTickets = Convert.ToInt32(reader["activeTickets"])
                            });
                        }
                    }
                    response.Success = true;
                    response.Message = "Users retrieved successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        #endregion

        #region Working on Tickets

        public async Task<TicResponse<List<TicketsResponse>>> GetAssignedTicketsAsync(GetAssignedTicketsModel model)
        {
            var response = new TicResponse<List<TicketsResponse>> { Data = new List<TicketsResponse>() };
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetAssignedTickets]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@assigneeUserId", model.AssigneeUserId);
                    command.Parameters.AddWithValue("@status", model.Status ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@projectId", model.ProjectId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@month", model.Month ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@year", model.Year ?? (object)DBNull.Value);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            response.Data.Add(MapTicketResponse(reader));
                        }
                    }
                    response.Success = true;
                    response.Message = "Assigned tickets retrieved successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse> StartTicketAsync(StartTicketModel model)
        {
            var response = new TicResponse();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[StartTicket]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ticketId", model.TicketId);
                    command.Parameters.AddWithValue("@assigneeUserId", model.AssigneeUserId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = Convert.ToBoolean(reader["success"]);
                            response.Message = reader["message"]?.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse> HoldTicketAsync(HoldTicketModel model)
        {
            var response = new TicResponse();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[HoldTicket]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ticketId", model.TicketId);
                    command.Parameters.AddWithValue("@assigneeUserId", model.AssigneeUserId);
                    command.Parameters.AddWithValue("@holdReason", model.HoldReason ?? (object)DBNull.Value);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = Convert.ToBoolean(reader["success"]);
                            response.Message = reader["message"]?.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse> ResumeTicketAsync(ResumeTicketModel model)
        {
            var response = new TicResponse();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[ResumeTicket]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ticketId", model.TicketId);
                    command.Parameters.AddWithValue("@assigneeUserId", model.AssigneeUserId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = Convert.ToBoolean(reader["success"]);
                            response.Message = reader["message"]?.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse> CloseTicketAsync(CloseTicketModel model)
        {
            var response = new TicResponse();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[CloseTicket]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ticketId", model.TicketId);
                    command.Parameters.AddWithValue("@assigneeUserId", model.AssigneeUserId);
                    command.Parameters.AddWithValue("@resolutionComment", model.ResolutionComment ?? (object)DBNull.Value);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = Convert.ToBoolean(reader["success"]);
                            response.Message = reader["message"]?.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse<WorkloadSummaryResponse>> GetMyWorkloadSummaryAsync(GetMyWorkloadSummaryModel model)
        {
            var response = new TicResponse<WorkloadSummaryResponse>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetMyWorkloadSummary]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@assigneeUserId", model.AssigneeUserId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Data = new WorkloadSummaryResponse
                            {
                                TotalAssigned = Convert.ToInt32(reader["totalAssigned"]),
                                PendingToStart = Convert.ToInt32(reader["pendingToStart"]),
                                InProgress = Convert.ToInt32(reader["inProgress"]),
                                OnHold = Convert.ToInt32(reader["onHold"]),
                                Closed = Convert.ToInt32(reader["closed"]),
                                CriticalOpen = Convert.ToInt32(reader["criticalOpen"]),
                                HighOpen = Convert.ToInt32(reader["highOpen"])
                            };
                            response.Success = true;
                            response.Message = "Workload summary retrieved successfully";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        #endregion

        #region Comments

        public async Task<AddCommentResponse> AddCommentAsync(AddCommentModel model)
        {
            var response = new AddCommentResponse();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[AddComment]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ticketId", model.TicketId);
                    command.Parameters.AddWithValue("@userId", model.UserId);
                    command.Parameters.AddWithValue("@comment", model.Comment);
                    command.Parameters.AddWithValue("@isInternal", model.IsInternal);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = Convert.ToBoolean(reader["success"]);
                            response.Message = reader["message"]?.ToString();
                            response.CommentId = reader["commentId"] != DBNull.Value ? Convert.ToInt32(reader["commentId"]) : null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse> UpdateCommentAsync(UpdateCommentModel model)
        {
            var response = new TicResponse();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[UpdateComment]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@commentId", model.CommentId);
                    command.Parameters.AddWithValue("@userId", model.UserId);
                    command.Parameters.AddWithValue("@comment", model.Comment);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = Convert.ToBoolean(reader["success"]);
                            response.Message = reader["message"]?.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse> DeleteCommentAsync(DeleteCommentModel model)
        {
            var response = new TicResponse();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[DeleteComment]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@commentId", model.CommentId);
                    command.Parameters.AddWithValue("@userId", model.UserId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = Convert.ToBoolean(reader["success"]);
                            response.Message = reader["message"]?.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse<List<CommentResponse>>> GetCommentsByTicketIdAsync(GetCommentsByTicketIdModel model)
        {
            var response = new TicResponse<List<CommentResponse>> { Data = new List<CommentResponse>() };
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetCommentsByTicketId]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ticketId", model.TicketId);
                    command.Parameters.AddWithValue("@includeInternal", model.IncludeInternal);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Check for error response first
                        if (await reader.ReadAsync())
                        {
                            if (reader.FieldCount == 2 && HasColumn(reader, "success"))
                            {
                                response.Success = Convert.ToBoolean(reader["success"]);
                                response.Message = reader["message"]?.ToString();
                                return response;
                            }

                            // First row is data
                            response.Data.Add(MapCommentResponse(reader));

                            while (await reader.ReadAsync())
                            {
                                response.Data.Add(MapCommentResponse(reader));
                            }
                        }
                    }
                    response.Success = true;
                    response.Message = "Comments retrieved successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse<CommentResponse>> GetCommentByIdAsync(GetCommentByIdModel model)
        {
            var response = new TicResponse<CommentResponse>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetCommentById]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@commentId", model.CommentId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            if (reader.FieldCount == 2 && HasColumn(reader, "success"))
                            {
                                response.Success = Convert.ToBoolean(reader["success"]);
                                response.Message = reader["message"]?.ToString();
                            }
                            else
                            {
                                response.Data = MapCommentResponse(reader);
                                response.Success = true;
                                response.Message = "Comment retrieved successfully";
                            }
                        }
                        else
                        {
                            response.Success = false;
                            response.Message = "Comment not found";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse<List<CommentResponse>>> GetMyCommentsAsync(GetMyCommentsModel model)
        {
            var response = new TicResponse<List<CommentResponse>> { Data = new List<CommentResponse>() };
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetMyComments]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@userId", model.UserId);
                    command.Parameters.AddWithValue("@ticketId", model.TicketId ?? (object)DBNull.Value);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            response.Data.Add(new CommentResponse
                            {
                                CommentId = Convert.ToInt32(reader["commentId"]),
                                TicketId = Convert.ToInt32(reader["ticketId"]),
                                TicketTitle = reader["ticketTitle"]?.ToString(),
                                Comment = reader["comment"]?.ToString(),
                                IsInternal = Convert.ToBoolean(reader["isInternal"]),
                                CreatedOn = Convert.ToDateTime(reader["createdOn"]),
                                UpdatedOn = reader["updatedOn"] != DBNull.Value ? Convert.ToDateTime(reader["updatedOn"]) : null,
                                IsEdited = Convert.ToBoolean(reader["isEdited"])
                            });
                        }
                    }
                    response.Success = true;
                    response.Message = "Comments retrieved successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        #endregion

        #region Timeline

        public async Task<TicResponse<List<TicketHistoryResponse>>> GetTicketTimelineAsync(GetTicketTimelineModel model)
        {
            var response = new TicResponse<List<TicketHistoryResponse>> { Data = new List<TicketHistoryResponse>() };
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetTicketTimeline]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ticketId", model.TicketId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Check for error response
                        if (await reader.ReadAsync())
                        {
                            if (reader.FieldCount == 2 && HasColumn(reader, "success"))
                            {
                                response.Success = Convert.ToBoolean(reader["success"]);
                                response.Message = reader["message"]?.ToString();
                                return response;
                            }

                            // First row is data
                            response.Data.Add(MapHistoryResponse(reader));

                            while (await reader.ReadAsync())
                            {
                                response.Data.Add(MapHistoryResponse(reader));
                            }
                        }
                    }
                    response.Success = true;
                    response.Message = "Timeline retrieved successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        #endregion

        #region Insights

        public async Task<TicResponse<TicketRaiserInsightsResponse>> GetTicketRaiserInsightsAsync(GetTicketRaiserInsightsModel model)
        {
            var response = new TicResponse<TicketRaiserInsightsResponse>
            {
                Data = new TicketRaiserInsightsResponse
                {
                    UserSummary = new List<UserInsightSummary>(),
                    DateWise = new List<DateWiseInsight>(),
                    ProjectWise = new List<ProjectWiseInsight>()
                }
            };

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetTicketRaiserInsights]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@fromUserId", model.FromUserId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@month", model.Month ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@year", model.Year ?? (object)DBNull.Value);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Result Set 1: User Summary
                        while (await reader.ReadAsync())
                        {
                            response.Data.UserSummary.Add(new UserInsightSummary
                            {
                                UserId = Convert.ToInt32(reader["userId"]),
                                UserName = reader["userName"]?.ToString(),
                                UserEmail = reader["userEmail"]?.ToString(),
                                EmpId = reader["empId"]?.ToString(),
                                MonthYear = reader["monthYear"]?.ToString(),
                                TotalTickets = Convert.ToInt32(reader["totalTickets"]),
                                OpenTickets = Convert.ToInt32(reader["openTickets"]),
                                AssignedTickets = Convert.ToInt32(reader["assignedTickets"]),
                                InProgressTickets = Convert.ToInt32(reader["inProgressTickets"]),
                                OnHoldTickets = Convert.ToInt32(reader["onHoldTickets"]),
                                ClosedTickets = Convert.ToInt32(reader["closedTickets"]),
                                CriticalTickets = Convert.ToInt32(reader["criticalTickets"]),
                                HighTickets = Convert.ToInt32(reader["highTickets"]),
                                MediumTickets = Convert.ToInt32(reader["mediumTickets"]),
                                LowTickets = Convert.ToInt32(reader["lowTickets"])
                            });
                        }

                        // Result Set 2: Date Wise
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                response.Data.DateWise.Add(new DateWiseInsight
                                {
                                    Date = Convert.ToDateTime(reader["date"]),
                                    MonthYear = reader["monthYear"]?.ToString(),
                                    TotalTickets = Convert.ToInt32(reader["totalTickets"]),
                                    OpenTickets = Convert.ToInt32(reader["openTickets"]),
                                    AssignedTickets = Convert.ToInt32(reader["assignedTickets"]),
                                    InProgressTickets = Convert.ToInt32(reader["inProgressTickets"]),
                                    OnHoldTickets = Convert.ToInt32(reader["onHoldTickets"]),
                                    ClosedTickets = Convert.ToInt32(reader["closedTickets"])
                                });
                            }
                        }

                        // Result Set 3: Project Wise
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                response.Data.ProjectWise.Add(new ProjectWiseInsight
                                {
                                    ProjectId = Convert.ToInt32(reader["projectId"]),
                                    ProjectName = reader["projectName"]?.ToString(),
                                    MonthYear = reader["monthYear"]?.ToString(),
                                    TotalTickets = Convert.ToInt32(reader["totalTickets"]),
                                    OpenTickets = Convert.ToInt32(reader["openTickets"]),
                                    AssignedTickets = Convert.ToInt32(reader["assignedTickets"]),
                                    InProgressTickets = Convert.ToInt32(reader["inProgressTickets"]),
                                    OnHoldTickets = Convert.ToInt32(reader["onHoldTickets"]),
                                    ClosedTickets = Convert.ToInt32(reader["closedTickets"])
                                });
                            }
                        }
                    }
                    response.Success = true;
                    response.Message = "Insights retrieved successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse<AssigneeInsightsResponse>> GetAssigneeInsightsAsync(GetAssigneeInsightsModel model)
        {
            var response = new TicResponse<AssigneeInsightsResponse>
            {
                Data = new AssigneeInsightsResponse
                {
                    UserSummary = new List<AssigneeInsightSummary>(),
                    DateWiseAssigned = new List<DateWiseInsight>(),
                    DateWiseClosed = new List<DateWiseInsight>(),
                    ProjectWise = new List<ProjectWiseInsight>()
                }
            };

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetAssigneeInsights]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@assigneeUserId", model.AssigneeUserId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@month", model.Month ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@year", model.Year ?? (object)DBNull.Value);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Result Set 1: User Summary
                        while (await reader.ReadAsync())
                        {
                            response.Data.UserSummary.Add(new AssigneeInsightSummary
                            {
                                UserId = Convert.ToInt32(reader["userId"]),
                                UserName = reader["userName"]?.ToString(),
                                UserEmail = reader["userEmail"]?.ToString(),
                                EmpId = reader["empId"]?.ToString(),
                                MonthYear = reader["monthYear"]?.ToString(),
                                TotalAssigned = Convert.ToInt32(reader["totalAssigned"]),
                                PendingToStart = Convert.ToInt32(reader["pendingToStart"]),
                                InProgressTickets = Convert.ToInt32(reader["inProgressTickets"]),
                                OnHoldTickets = Convert.ToInt32(reader["onHoldTickets"]),
                                ClosedTickets = Convert.ToInt32(reader["closedTickets"]),
                                ActiveTickets = Convert.ToInt32(reader["activeTickets"]),
                                CriticalOpen = Convert.ToInt32(reader["criticalOpen"]),
                                HighOpen = Convert.ToInt32(reader["highOpen"]),
                                AvgResolutionHours = reader["avgResolutionHours"] != DBNull.Value ? Convert.ToInt32(reader["avgResolutionHours"]) : null
                            });
                        }

                        // Result Set 2: Date Wise Assigned
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                response.Data.DateWiseAssigned.Add(new DateWiseInsight
                                {
                                    Date = Convert.ToDateTime(reader["date"]),
                                    MonthYear = reader["monthYear"]?.ToString(),
                                    TotalTickets = Convert.ToInt32(reader["totalAssigned"]),
                                    AssignedTickets = Convert.ToInt32(reader["pendingToStart"]),
                                    InProgressTickets = Convert.ToInt32(reader["inProgressTickets"]),
                                    OnHoldTickets = Convert.ToInt32(reader["onHoldTickets"]),
                                    ClosedTickets = Convert.ToInt32(reader["closedTickets"])
                                });
                            }
                        }

                        // Result Set 3: Date Wise Closed
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                response.Data.DateWiseClosed.Add(new DateWiseInsight
                                {
                                    Date = Convert.ToDateTime(reader["date"]),
                                    MonthYear = reader["monthYear"]?.ToString(),
                                    ClosedTickets = Convert.ToInt32(reader["closedTickets"])
                                });
                            }
                        }

                        // Result Set 4: Project Wise
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                response.Data.ProjectWise.Add(new ProjectWiseInsight
                                {
                                    ProjectId = Convert.ToInt32(reader["projectId"]),
                                    ProjectName = reader["projectName"]?.ToString(),
                                    MonthYear = reader["monthYear"]?.ToString(),
                                    TotalTickets = Convert.ToInt32(reader["totalAssigned"]),
                                    AssignedTickets = Convert.ToInt32(reader["pendingToStart"]),
                                    InProgressTickets = Convert.ToInt32(reader["inProgressTickets"]),
                                    OnHoldTickets = Convert.ToInt32(reader["onHoldTickets"]),
                                    ClosedTickets = Convert.ToInt32(reader["closedTickets"])
                                });
                            }
                        }
                    }
                    response.Success = true;
                    response.Message = "Insights retrieved successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse<AssignerInsightsResponse>> GetAssignerInsightsAsync(GetAssignerInsightsModel model)
        {
            var response = new TicResponse<AssignerInsightsResponse>
            {
                Data = new AssignerInsightsResponse
                {
                    UserSummary = new List<AssignerInsightSummary>(),
                    DateWise = new List<DateWiseInsight>(),
                    AssigneeWise = new List<AssigneeWiseInsight>(),
                    ProjectWise = new List<ProjectWiseInsight>()
                }
            };

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetAssignerInsights]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@assignerUserId", model.AssignerUserId ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@month", model.Month ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@year", model.Year ?? (object)DBNull.Value);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // Result Set 1: User Summary
                        while (await reader.ReadAsync())
                        {
                            response.Data.UserSummary.Add(new AssignerInsightSummary
                            {
                                UserId = Convert.ToInt32(reader["userId"]),
                                UserName = reader["userName"]?.ToString(),
                                UserEmail = reader["userEmail"]?.ToString(),
                                EmpId = reader["empId"]?.ToString(),
                                MonthYear = reader["monthYear"]?.ToString(),
                                TotalAssignments = Convert.ToInt32(reader["totalAssignments"]),
                                PendingToStart = Convert.ToInt32(reader["pendingToStart"]),
                                InProgressTickets = Convert.ToInt32(reader["inProgressTickets"]),
                                OnHoldTickets = Convert.ToInt32(reader["onHoldTickets"]),
                                ClosedTickets = Convert.ToInt32(reader["closedTickets"]),
                                ActiveTickets = Convert.ToInt32(reader["activeTickets"]),
                                UniqueAssignees = Convert.ToInt32(reader["uniqueAssignees"])
                            });
                        }

                        // Result Set 2: Date Wise
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                response.Data.DateWise.Add(new DateWiseInsight
                                {
                                    Date = Convert.ToDateTime(reader["date"]),
                                    MonthYear = reader["monthYear"]?.ToString(),
                                    TotalTickets = Convert.ToInt32(reader["totalAssignments"]),
                                    AssignedTickets = Convert.ToInt32(reader["pendingToStart"]),
                                    InProgressTickets = Convert.ToInt32(reader["inProgressTickets"]),
                                    OnHoldTickets = Convert.ToInt32(reader["onHoldTickets"]),
                                    ClosedTickets = Convert.ToInt32(reader["closedTickets"])
                                });
                            }
                        }

                        // Result Set 3: Assignee Wise
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                response.Data.AssigneeWise.Add(new AssigneeWiseInsight
                                {
                                    AssigneeUserId = Convert.ToInt32(reader["assigneeUserId"]),
                                    AssigneeName = reader["assigneeName"]?.ToString(),
                                    AssigneeEmail = reader["assigneeEmail"]?.ToString(),
                                    MonthYear = reader["monthYear"]?.ToString(),
                                    TotalAssigned = Convert.ToInt32(reader["totalAssigned"]),
                                    PendingToStart = Convert.ToInt32(reader["pendingToStart"]),
                                    InProgressTickets = Convert.ToInt32(reader["inProgressTickets"]),
                                    OnHoldTickets = Convert.ToInt32(reader["onHoldTickets"]),
                                    ClosedTickets = Convert.ToInt32(reader["closedTickets"])
                                });
                            }
                        }

                        // Result Set 4: Project Wise
                        if (await reader.NextResultAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                response.Data.ProjectWise.Add(new ProjectWiseInsight
                                {
                                    ProjectId = Convert.ToInt32(reader["projectId"]),
                                    ProjectName = reader["projectName"]?.ToString(),
                                    MonthYear = reader["monthYear"]?.ToString(),
                                    TotalTickets = Convert.ToInt32(reader["totalAssignments"]),
                                    AssignedTickets = Convert.ToInt32(reader["pendingToStart"]),
                                    InProgressTickets = Convert.ToInt32(reader["inProgressTickets"]),
                                    OnHoldTickets = Convert.ToInt32(reader["onHoldTickets"]),
                                    ClosedTickets = Convert.ToInt32(reader["closedTickets"])
                                });
                            }
                        }
                    }
                    response.Success = true;
                    response.Message = "Insights retrieved successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        #endregion

        #region Helper Methods

        private bool HasColumn(SqlDataReader reader, string columnName)
        {
            for (int i = 0; i < reader.FieldCount; i++)
            {
                if (reader.GetName(i).Equals(columnName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private TicketsResponse MapTicketResponse(SqlDataReader reader)
        {
            var ticket = new TicketsResponse
            {
                TicketId = Convert.ToInt32(reader["ticketId"]),
                ProjectId = Convert.ToInt32(reader["projectId"]),
                ProjectName = reader["projectName"]?.ToString(),
                Title = reader["title"]?.ToString(),
                Description = reader["description"]?.ToString(),
                Status = reader["status"]?.ToString(),
                Priority = reader["priority"]?.ToString(),
                FromUserId = Convert.ToInt32(reader["fromUserId"]),
                CreatedOn = Convert.ToDateTime(reader["createdOn"])
            };

            // Optional fields
            if (HasColumn(reader, "fromUserName"))
                ticket.FromUserName = reader["fromUserName"]?.ToString();
            if (HasColumn(reader, "fromUserEmail"))
                ticket.FromUserEmail = reader["fromUserEmail"]?.ToString();
            if (HasColumn(reader, "assignerUserId") && reader["assignerUserId"] != DBNull.Value)
                ticket.AssignerUserId = Convert.ToInt32(reader["assignerUserId"]);
            if (HasColumn(reader, "assignerName"))
                ticket.AssignerName = reader["assignerName"]?.ToString();
            if (HasColumn(reader, "assigneeUserId") && reader["assigneeUserId"] != DBNull.Value)
                ticket.AssigneeUserId = Convert.ToInt32(reader["assigneeUserId"]);
            if (HasColumn(reader, "assigneeName"))
                ticket.AssigneeName = reader["assigneeName"]?.ToString();
            if (HasColumn(reader, "assigneeEmail"))
                ticket.AssigneeEmail = reader["assigneeEmail"]?.ToString();
            if (HasColumn(reader, "updatedOn") && reader["updatedOn"] != DBNull.Value)
                ticket.UpdatedOn = Convert.ToDateTime(reader["updatedOn"]);
            if (HasColumn(reader, "assignedOn") && reader["assignedOn"] != DBNull.Value)
                ticket.AssignedOn = Convert.ToDateTime(reader["assignedOn"]);
            if (HasColumn(reader, "resolvedOn") && reader["resolvedOn"] != DBNull.Value)
                ticket.ResolvedOn = Convert.ToDateTime(reader["resolvedOn"]);
            if (HasColumn(reader, "closedOn") && reader["closedOn"] != DBNull.Value)
                ticket.ClosedOn = Convert.ToDateTime(reader["closedOn"]);
            if (HasColumn(reader, "monthYear"))
                ticket.MonthYear = reader["monthYear"]?.ToString();
            if (HasColumn(reader, "ageDays") && reader["ageDays"] != DBNull.Value)
                ticket.AgeDays = Convert.ToInt32(reader["ageDays"]);
            if (HasColumn(reader, "assignedDays") && reader["assignedDays"] != DBNull.Value)
                ticket.AssignedDays = Convert.ToInt32(reader["assignedDays"]);
            if (HasColumn(reader, "commentCount"))
                ticket.CommentCount = Convert.ToInt32(reader["commentCount"]);

            return ticket;
        }

        private CommentResponse MapCommentResponse(SqlDataReader reader)
        {
            return new CommentResponse
            {
                CommentId = Convert.ToInt32(reader["commentId"]),
                TicketId = Convert.ToInt32(reader["ticketId"]),
                UserId = Convert.ToInt32(reader["userId"]),
                UserName = reader["userName"]?.ToString(),
                UserEmail = reader["userEmail"]?.ToString(),
                Comment = reader["comment"]?.ToString(),
                IsInternal = Convert.ToBoolean(reader["isInternal"]),
                CreatedOn = Convert.ToDateTime(reader["createdOn"]),
                UpdatedOn = reader["updatedOn"] != DBNull.Value ? Convert.ToDateTime(reader["updatedOn"]) : null,
                IsEdited = Convert.ToBoolean(reader["isEdited"])
            };
        }

        private TicketHistoryResponse MapHistoryResponse(SqlDataReader reader)
        {
            return new TicketHistoryResponse
            {
                HistoryId = Convert.ToInt32(reader["historyId"]),
                TicketId = Convert.ToInt32(reader["ticketId"]),
                UserId = Convert.ToInt32(reader["userId"]),
                UserName = reader["userName"]?.ToString(),
                UserEmail = reader["userEmail"]?.ToString(),
                Action = reader["action"]?.ToString(),
                OldValue = reader["oldValue"]?.ToString(),
                NewValue = reader["newValue"]?.ToString(),
                Description = reader["description"]?.ToString(),
                CreatedOn = Convert.ToDateTime(reader["createdOn"])
            };
        }
        // Add to TicketsService

        public async Task<AddAttachmentResponse> AddAttachmentAsync(TAddAttachmentModel model)
        {
            var response = new AddAttachmentResponse();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[AddAttachment]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ticketId", model.TicketId);
                    command.Parameters.AddWithValue("@userId", model.UserId);
                    command.Parameters.AddWithValue("@fileName", model.FileName);
                    command.Parameters.AddWithValue("@filePath", model.FilePath);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = Convert.ToBoolean(reader["success"]);
                            response.Message = reader["message"]?.ToString();
                            response.AttachmentId = reader["attachmentId"] != DBNull.Value
                                ? Convert.ToInt32(reader["attachmentId"]) : null;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<DeleteAttachmentResponse> DeleteAttachmentAsync(DeleteAttachmentModel model)
        {
            var response = new DeleteAttachmentResponse();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[DeleteAttachment]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@attachmentId", model.AttachmentId);
                    command.Parameters.AddWithValue("@userId", model.UserId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = Convert.ToBoolean(reader["success"]);
                            response.Message = reader["message"]?.ToString();
                            response.FilePath = reader["filePath"]?.ToString();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse<List<AttachmentResponse>>> GetAttachmentsByTicketIdAsync(GetAttachmentsByTicketIdModel model)
        {
            var response = new TicResponse<List<AttachmentResponse>> { Data = new List<AttachmentResponse>() };
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetAttachmentsByTicketId]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@ticketId", model.TicketId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            response.Data.Add(new AttachmentResponse
                            {
                                AttachmentId = Convert.ToInt32(reader["attachmentId"]),
                                TicketId = Convert.ToInt32(reader["ticketId"]),
                                UserId = Convert.ToInt32(reader["userId"]),
                                UserName = reader["userName"]?.ToString(),
                                FileName = reader["fileName"]?.ToString(),
                                FilePath = reader["filePath"]?.ToString(),
                                CreatedOn = Convert.ToDateTime(reader["createdOn"])
                            });
                        }
                    }
                    response.Success = true;
                    response.Message = "Attachments retrieved successfully";
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }

        public async Task<TicResponse<AttachmentResponse>> GetAttachmentByIdAsync(GetAttachmentByIdModel model)
        {
            var response = new TicResponse<AttachmentResponse>();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[tic].[GetAttachmentById]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@attachmentId", model.AttachmentId);

                    await connection.OpenAsync();
                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Data = new AttachmentResponse
                            {
                                AttachmentId = Convert.ToInt32(reader["attachmentId"]),
                                TicketId = Convert.ToInt32(reader["ticketId"]),
                                UserId = Convert.ToInt32(reader["userId"]),
                                UserName = reader["userName"]?.ToString(),
                                FileName = reader["fileName"]?.ToString(),
                                FilePath = reader["filePath"]?.ToString(),
                                CreatedOn = Convert.ToDateTime(reader["createdOn"])
                            };
                            response.Success = true;
                            response.Message = "Attachment retrieved successfully";
                        }
                        else
                        {
                            response.Success = false;
                            response.Message = "Attachment not found";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = "Error: " + ex.Message;
            }
            return response;
        }
        #endregion
    }
}