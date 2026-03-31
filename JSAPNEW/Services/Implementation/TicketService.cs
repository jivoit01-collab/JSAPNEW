using Dapper;
using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;
using ServiceStack;
using TicketSystem.Models;
using Microsoft.AspNetCore.Hosting;
using static System.Runtime.InteropServices.JavaScript.JSType;
using Azure;
using ServiceStack.Web;
using Microsoft.AspNetCore.Mvc;

namespace JSAPNEW.Services.Implementation
{
    public class TicketService : ITicketService
    {
        private readonly IConfiguration _configuration;
        //private readonly Interfaces.ITokenService _tokenService;
        private readonly string _connectionString;
        private object _webHostEnvironment;

        public TicketService(IConfiguration configuration)
        {
            _configuration = configuration;
            // _tokenService = tokenService;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
        }

        public async Task<IEnumerable<TicketResponse>> AssignTicketAsync(TicketAssignmentModel request)
        {
            var response = new List<TicketResponse>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("[tk].[jsAssignTicket]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@userId", request.userId);
                    command.Parameters.AddWithValue("@ticketId", request.ticketId);
                    command.Parameters.AddWithValue("@resolverId", request.resolverId);

                    try
                    {
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();

                        response.Add(new TicketResponse
                        {
                            Success = true,
                            Message = "Ticket assigned successfully"
                        });
                    }
                    catch (SqlException ex)
                    {
                        // Handle specific error codes from the stored procedure
                        if (ex.Number == 50016)
                        {
                            response.Add(new TicketResponse { Success = false, Message = "Ticket ID does not exist!" });
                        }
                        else if (ex.Number == 50017)
                        {
                            response.Add(new TicketResponse { Success = false, Message = "User or Resolver ID does not exist!" });
                        }
                        else if (ex.Number == 50019)
                        {
                            response.Add(new TicketResponse { Success = false, Message = $"Database error: {ex.Message}" });
                        }
                        else
                        {
                            response.Add(new TicketResponse { Success = false, Message = $"{ex.Message}" });
                        }
                    }
                    catch (Exception ex)
                    {
                        response.Add(new TicketResponse { Success = false, Message = $"{ex.Message}" });
                    }
                }
            }
            return response;
        }

        public async Task<IEnumerable<TicketResponse>> CreateTicketAsync(TicketCreateModel model, List<IFormFile> files)
        {
            var response = new List<TicketResponse>();

            try
            {
                using SqlConnection conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                using SqlCommand cmd = new SqlCommand("[tk].[jsCreateTicket]", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Add stored procedure parameters
                cmd.Parameters.AddWithValue("@issue", model.Issue);
                cmd.Parameters.AddWithValue("@description", model.Description);
                cmd.Parameters.AddWithValue("@company", model.Company);
                cmd.Parameters.AddWithValue("@priorityId", model.PriorityId);
                cmd.Parameters.AddWithValue("@createdBy", model.CreatedBy);
                cmd.Parameters.AddWithValue("@departmentId", model.DepartmentId);
                cmd.Parameters.AddWithValue("@catalogOneId", model.CatalogOneId);
                cmd.Parameters.AddWithValue("@catalogTwoId", model.CatalogTwoId);
                cmd.Parameters.AddWithValue("@requestNameId", model.RequestNameId);

                // Create DataTable for attachments (TVP)
                DataTable fileTable = new DataTable();
                fileTable.Columns.Add("filePath", typeof(string));
                fileTable.Columns.Add("fileName", typeof(string));
                fileTable.Columns.Add("fileType", typeof(string));

                foreach (var file in model.Files)
                {
                    fileTable.Rows.Add(file.FilePath, file.FileName, file.FileType);
                }

                var tvpParam = cmd.Parameters.AddWithValue("@AttachmentTable", fileTable);
                tvpParam.SqlDbType = SqlDbType.Structured;

                // Execute SP
                await cmd.ExecuteNonQueryAsync();

                // Return success
                response.Add(new TicketResponse
                {
                    Success = true,
                    Message = "Ticket created successfully.",
                    // TicketId can be retrieved from SP using output parameter or SELECT SCOPE_IDENTITY() if you modify SP
                });
            }
            catch (SqlException ex)
            {
                // Handle known SP error codes
                string message = ex.Number switch
                {
                    50001 => "Required fields are missing.",
                    50002 => "Invalid Priority ID.",
                    50003 => "Invalid CreatedBy User ID.",
                    50004 => "Invalid Department ID.",
                    50005 => "Invalid Catalog One ID.",
                    50006 => "Invalid Catalog Two ID.",
                    50007 => "Invalid Request Name ID.",
                    50010 => "Default status 'Open' not found.",
                    _ => $"Database error: {ex.Message}"
                };

                response.Add(new TicketResponse
                {
                    Success = false,
                    Message = message
                });
            }
            catch (Exception ex)
            {
                response.Add(new TicketResponse
                {
                    Success = false,
                    Message = $"Unexpected error: {ex.Message}"
                });
            }

            return response;
        }

        public async Task<IEnumerable<TicketInsightsModel>> GetUserTicketsInsightsAsync(int userId, string month)
        {
            var sqlQuery = "EXEC [tk].[jsGetUserTicketsInsights] @userId,@month";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<TicketInsightsModel>(
                    sqlQuery,
                   new { userId,month } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<OneTicketDetailsModel>> GetOneTicketDetailsAsync(int ticketId)
        {
            var sqlQuery = "EXEC [tk].[jsGetTicketDetails] @ticketId";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<OneTicketDetailsModel>(
                    sqlQuery,
                    new { ticketId } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<OneTicketDetailsModel>> GetUserTicketsWithStatusAsync(int userId, string statusIds, string month)
        {
            var sqlQuery = "EXEC [tk].[jsGetUserTicketsWithStatus] @userId, @statusIds,@month";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<OneTicketDetailsModel>(
                    sqlQuery,
                    new { userId, statusIds,month } // Parameters for the stored procedure
                );
            }
        }

        public async Task<TicketResponse> AddCatalogOneAsync(AddCatalogOneModel request)
        {
            var response = new TicketResponse
            {
                Success = false
            };

            // Validate inputs before calling stored procedure
            if (string.IsNullOrWhiteSpace(request.name))
            {
                response.Message = "Name is required";
                return response;
            }

            if (string.IsNullOrWhiteSpace(request.description))
            {
                response.Message = "Description is required";
                return response;
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("[tk].[AddCataLogOne]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@userId", request.userId);
                    command.Parameters.AddWithValue("@name", request.name);
                    command.Parameters.AddWithValue("@description", request.description);

                    try
                    {
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();

                        response.Success = true;
                        response.Message = "Catalog item created successfully";
                    }
                    catch (SqlException ex)
                    {
                        // Handle specific error codes from the stored procedure
                        if (ex.Number == 50001)
                        {
                            response.Message = "User ID is not valid";
                        }
                        else if (ex.Number == 50002)
                        {
                            response.Message = "Name is empty";
                        }
                        else if (ex.Number == 50003)
                        {
                            response.Message = "Description is empty";
                        }
                        else
                        {
                            response.Message = $"{ex.Message}";
                        }
                    }
                    catch (Exception ex)
                    {
                        response.Message = $"{ex.Message}";
                    }
                }
            }

            return response;
        }

        public async Task<TicketResponse> AddCatalogTwoAsync(AddCatalogTwoModel request)
        {
            var response = new TicketResponse
            {
                Success = false
            };

            // Validate inputs before calling stored procedure
            if (string.IsNullOrWhiteSpace(request.name))
            {
                response.Message = "Name is required";
                return response;
            }

            if (string.IsNullOrWhiteSpace(request.description))
            {
                response.Message = "Description is required";
                return response;
            }
            if (request.cataOneId <= 0)
            {
                response.Message = "Valid Catalog One ID is required";
                return response;
            }

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();

                using (SqlCommand command = new SqlCommand("[tk].[AddCataLogTwo]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    // Add parameters
                    command.Parameters.AddWithValue("@userId", request.userId);
                    command.Parameters.AddWithValue("@name", request.name);
                    command.Parameters.AddWithValue("@description", request.description);
                    command.Parameters.AddWithValue("@cataOneId", request.cataOneId);

                    try
                    {
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();

                        response.Success = true;
                        response.Message = "Catalog item created successfully";
                    }
                    catch (SqlException ex)
                    {
                        // Handle specific error codes from the stored procedure
                        if (ex.Number == 50001)
                        {
                            response.Message = "User ID is not valid";
                        }
                        else if (ex.Number == 50002)
                        {
                            response.Message = "Name is empty";
                        }
                        else if (ex.Number == 50003)
                        {
                            response.Message = "Description is empty";
                        }
                        else if (ex.Number == 50004)
                        {
                            response.Message = "Catalog One ID does not exist";
                        }
                        else
                        {
                            response.Message = $"{ex.Message}";
                        }
                    }
                    catch (Exception ex)
                    {
                        response.Message = $"{ex.Message}";
                    }
                }
            }

            return response;
        }

        public async Task<IEnumerable<GetRequestModel>> GetRequestNameAsync()
        {
            var sqlQuery = "EXEC [tk].[jsGetRequestName] ";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<GetRequestModel>(
                    sqlQuery // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<TicketResponse>> UpdateTicketStatusAsync(updateticketstatusmodel request)
        {
            var response = new List<TicketResponse>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand("[tk].[jsUpdateTicketStatus]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    // Add parameters
                    command.Parameters.AddWithValue("@ticketId", request.ticketId);
                    command.Parameters.AddWithValue("@newStatusId", request.newStatusId);
                    command.Parameters.AddWithValue("@userId", request.userId);
                    try
                    {
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();

                        response.Add(new TicketResponse
                        {
                            Success = true,
                            Message = "Ticket status updated successfully"
                        });
                    }
                    catch (SqlException ex)
                    {
                        // Handle specific error codes from the stored procedure
                        if (ex.Number == 50013)
                        {
                            response.Add(new TicketResponse
                            {
                                Success = false,
                                Message = "Ticket ID does not exist!"
                            });
                        }
                        else if (ex.Number == 50014)
                        {
                            response.Add(new TicketResponse
                            {
                                Success = false,
                                Message = "Invalid Status ID!"
                            });
                        }
                        else
                        {
                            response.Add(new TicketResponse
                            {
                                Success = false,
                                Message = $"{ex.Message}"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        response.Add(new TicketResponse
                        {
                            Success = false,
                            Message = $"{ex.Message}"
                        });
                    }
                }
            }
            return response;
        }

        public async Task<IEnumerable<TicketResponse>> AddCommentOnTicketAsync(AddCommentOnTicketModel request)
        {
            var response = new List<TicketResponse>();
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                using (SqlCommand command = new SqlCommand("[tk].[jsAddCommentOnTicket]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    // Add parameters
                    command.Parameters.AddWithValue("@ticketId", request.ticketId);
                    command.Parameters.AddWithValue("@comment", request.comment);
                    command.Parameters.AddWithValue("@userId", request.userId);
                    try
                    {
                        // Execute the stored procedure
                        await command.ExecuteNonQueryAsync();

                        response.Add(new TicketResponse
                        {
                            Success = true,
                            Message = "Comment added successfully"
                        });
                    }
                    catch (SqlException ex)
                    {
                        // Handle specific error codes from the stored procedure
                        if (ex.Number == 50001)
                        {
                            response.Add(new TicketResponse
                            {
                                Success = false,
                                Message = "TicketId is not Valid"
                            });
                        }
                        else if (ex.Number == 50002)
                        {
                            response.Add(new TicketResponse
                            {
                                Success = false,
                                Message = "UserId is not Valid"
                            });
                        }
                        else if (ex.Number == 50003)
                        {
                            response.Add(new TicketResponse
                            {
                                Success = false,
                                Message = "Empty String Not Allowed"
                            });
                        }
                        else
                        {
                            response.Add(new TicketResponse
                            {
                                Success = false,
                                Message = $"{ex.Message}"
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        response.Add(new TicketResponse
                        {
                            Success = false,
                            Message = $"{ex.Message}"
                        });
                    }
                }
            }
            return response;
        }

        public async Task<IEnumerable<GetRequestModel>> GetRequestTypeAsync()
        {
            var sqlQuery = "EXEC [tk].[jsGetRequestType] ";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<GetRequestModel>(
                    sqlQuery // Parameters for the stored procedure
                );
            }
        }

        public async Task<List<TicketCommentModel>> GetTicketCommentsAsync(int ticketId)
        {
            var comments = new List<TicketCommentModel>();
            using (SqlConnection conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection")))
            {
                using (SqlCommand cmd = new SqlCommand("[tk].[jsGetTicketComments]", conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.Parameters.AddWithValue("@ticketId", ticketId);

                    await conn.OpenAsync();
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            comments.Add(new TicketCommentModel
                            {
                                Id = reader["id"] != DBNull.Value ? Convert.ToInt32(reader["id"]) : 0,
                                UserId = reader["userId"] != DBNull.Value ? Convert.ToInt32(reader["userId"]) : 0,
                                LoginUser = reader["loginUser"]?.ToString(),
                                Comment = reader["comment"]?.ToString(),
                                CreatedAt = reader["createdAt"] != DBNull.Value ? Convert.ToDateTime(reader["createdAt"]) : DateTime.MinValue
                            });
                        }
                    }
                }
            }
            return comments;
        }

        public async Task<List<TicketAttachmentModel>> GetTicketAttachmentsAsync(int ticketId, IUrlHelper urlHelper)
        {
            using var connection = new SqlConnection(_connectionString);
            await connection.OpenAsync();

            var attachments = (await connection.QueryAsync<TicketAttachmentModel>(
                "[tk].[jsGetTicketAttachments]",
                new { ticketId },
                commandType: CommandType.StoredProcedure
            )).ToList();

            foreach (var file in attachments)
            {
                if (string.IsNullOrEmpty(file.FilePath) || string.IsNullOrEmpty(file.FileName))
                    continue;

                // Clean and format the path
                string cleanFilePath = file.FilePath.Replace("\\", "/").Trim();
                if (cleanFilePath.StartsWith("/"))
                    cleanFilePath = cleanFilePath.Substring(1);

                string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file.FileName);
                string fileExt = Path.GetExtension(file.FileName)?.TrimStart('.') ?? "";

                // Generate download link using a controller action
                file.FileUrl = urlHelper.Action("AdvanceDownloadFile", "File", new
                {
                    filePath = cleanFilePath,
                    fileName = fileNameWithoutExt,
                    fileExt = fileExt
                }, protocol: "http");
            }

            return attachments;
        }


        public async Task<IEnumerable<GetCatalogOneModel>> GetCatalogOneAsync()
        {
            var sqlQuery = "EXEC [tk].[jsGetCatalogOne] ";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<GetCatalogOneModel>(
                    sqlQuery // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<GetCatalogTwoModel>> GetCatalogTwoAsync(int oneId)
        {
            var sqlQuery = "EXEC [tk].[jsGetCatalogTwo] @oneId ";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<GetCatalogTwoModel>(
                    sqlQuery,
                    new { oneId } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<OpenAndReopenTicketsModel>> GetOpenAndReopenTicketsAsync(string MonthYear)
        {

            var sqlQuery = "EXEC [tk].[jsGetOpenAndReopenTickets] @MonthYear ";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<OpenAndReopenTicketsModel>(
                    sqlQuery,
                    new { MonthYear } // Parameters for the stored procedure
                );
            }
        }
        public async Task<TicketInsightsFlat> GetTicketInsightsByMonthAsync(string monthYear, int? assignerId)
        {
            var result = new TicketInsightsFlat();

            using (var conn = new SqlConnection(_connectionString))
            using (var cmd = new SqlCommand("[tk].[jsGetTicketInsightsByMonth]", conn))
            {
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.Parameters.AddWithValue("@MonthYear", (object?)monthYear ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AssignerId", (object?)assignerId ?? DBNull.Value);

                await conn.OpenAsync();
                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    // 1) Open/Reopen summary
                    if (await reader.ReadAsync())
                    {
                        result.OpenCount = reader["OpenCount"] != DBNull.Value ? Convert.ToInt32(reader["OpenCount"]) : 0;
                        result.ReopenCount = reader["ReopenCount"] != DBNull.Value ? Convert.ToInt32(reader["ReopenCount"]) : 0;
                        result.TotalOpenReopen = reader["TotalOpenReopen"] != DBNull.Value ? Convert.ToInt32(reader["TotalOpenReopen"]) : 0;
                    }

                    // 2) Assigned summary (take first row; see note below to SUM)
                    if (await reader.NextResultAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            result.AssignBy = reader["assignBy"] != DBNull.Value ? Convert.ToInt32(reader["assignBy"]) : (int?)null;
                            result.AssignedCount = reader["AssignedCount"] != DBNull.Value ? Convert.ToInt32(reader["AssignedCount"]) : 0;
                        }
                    }
                }
            }

            return result;
        }

        public async Task<IEnumerable<TicketsAssignedByMonth>> GetTicketsAssignedByMonthAsync(int AssignerId, string MonthYear)
        {
            var sqlQuery = "EXEC [tk].[jsGetTicketsAssignedByAssignerMonth] @AssignerId, @MonthYear";

            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<TicketsAssignedByMonth>(
                    sqlQuery,
                    new { AssignerId, MonthYear } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<TicketDetailsModel>> GetTicketDetailsByAssigneeAsync(int assigneeId, string monthYear)
        {
            var sqlQuery = "EXEC [tk].[jsGetTicketDetailsByAssignee] @assigneeId,@monthYear";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<TicketDetailsModel>(
                    sqlQuery,
                    new { assigneeId, monthYear } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<TicketDetailsModel>> GetClosedTicketDetailsByAssigneeAsync(int assigneeId, string monthYear)
        {
            var sqlQuery = "EXEC [tk].[jsGetClosedTicketDetailsByAssignee] @assigneeId,@monthYear";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<TicketDetailsModel>(
                    sqlQuery,
                    new { assigneeId, monthYear } // Parameters for the stored procedure
                );
            }
        }
        public async Task<IEnumerable<TicketAssignmentInsightsModel>> GetTicketAssignmentInsightsAsync(int assigneeId, string monthYear)
        {
            var sqlQuery = "EXEC [tk].[jsGetTicketAssignmentInsights] @assigneeId,@monthYear";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<TicketAssignmentInsightsModel>(
                    sqlQuery,
                    new { assigneeId , monthYear } // Parameters for the stored procedure
                );
            }
        }

        public async Task<IEnumerable<GetTicketStatusModel>> GetTicketStatusAsync()
        {
            var sqlQuery = "EXEC  [tk].[jsGetTicketStatus] ";
            using (var connection = new SqlConnection(_connectionString))
            {
                return await connection.QueryAsync<GetTicketStatusModel>(
                    sqlQuery
                );
            }
        }
    }
}
