using Dapper;
using JSAPNEW.Data.Entities;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using ServiceStack;
using System.ComponentModel.Design;
using System.Data;

namespace JSAPNEW.Services.Implementation
{
    public class Auth2Service : IAuth2Service
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;
        private readonly INotificationService _notificationService;

        public Auth2Service(IConfiguration configuration, Interfaces.ITokenService tokenService, INotificationService notificationService)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");
            _notificationService = notificationService;
        }

        public async Task<templateDataCloning> GetTemplateDataForCloningAsync(int templateId, int company)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var multi = await conn.QueryMultipleAsync("[dbo].[jsGetTemplateDataForCloning]", new { templateId, company }, commandType: CommandType.StoredProcedure))
                {
                    var result = new templateDataCloning
                    {
                        template = await multi.ReadFirstOrDefaultAsync<templateCloning>(),
                        stages = (await multi.ReadAsync<stageCloning>()).ToList(),
                        approvals = (await multi.ReadAsync<approvalCloning>()).ToList(),
                        queries = (await multi.ReadAsync<queryCloning>()).ToList(),
                        summary = await multi.ReadFirstOrDefaultAsync<summary>()
                    };

                    return result;

                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting template data for cloning", ex);
            }
        }

        public async Task<Response> CloneTemplateWithNewStagesAsync(CloneTemplateModel model)
        {
            var response = new Response();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[dbo].[jsCloneTemplateWithNewStages]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@oldTemplateId", model.OldTemplateId);
                    command.Parameters.AddWithValue("@newTemplateName", model.NewTemplateName ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@stagesJson", model.StagesJson ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@approvalIds", model.ApprovalIds ?? (object)DBNull.Value);
                    command.Parameters.AddWithValue("@newBudgetDate", model.NewBudgetDate);
                    command.Parameters.AddWithValue("@createdBy", model.CreatedBy);
                    command.Parameters.AddWithValue("@company", model.Company);

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.Success = true;
                            response.Message = reader["Message"]?.ToString();
                        }
                        else
                        {
                            response.Success = false;
                            response.Message = "No data returned from stored procedure.";
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

        public async Task<CreateBudget2Response> CreateBudgetWithSubBudgetsAsync(CreateBudget2Request request)
        {
            var response = new CreateBudget2Response();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[bud].[CreateBudgetWithSubBudgets]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@company", request.Company);
                    command.Parameters.AddWithValue("@budgetName", request.BudgetName);
                    command.Parameters.AddWithValue("@description", (object?)request.Description ?? DBNull.Value);
                    command.Parameters.AddWithValue("@totalAmount", request.TotalAmount);
                    command.Parameters.AddWithValue("@isActive", request.IsActive);

                    // Table-Valued Parameter
                    var subBudgetTable = new DataTable();
                    subBudgetTable.Columns.Add("SubBudgetName", typeof(string));
                    subBudgetTable.Columns.Add("Description", typeof(string));

                    if (request.SubBudgets != null && request.SubBudgets.Count > 0)
                    {
                        foreach (var sb in request.SubBudgets)
                        {
                            subBudgetTable.Rows.Add(sb.SubBudgetName, sb.Description);
                        }
                    }

                    var tvpParam = command.Parameters.AddWithValue("@subBudgets", subBudgetTable);
                    tvpParam.SqlDbType = SqlDbType.Structured;
                    tvpParam.TypeName = "bud.SubBudgetTableType";

                    var outputParam = new SqlParameter("@newBudgetId", SqlDbType.Int)
                    {
                        Direction = ParameterDirection.Output
                    };
                    command.Parameters.Add(outputParam);

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.BudgetId = reader["budgetId"] != DBNull.Value ? Convert.ToInt32(reader["budgetId"]) : 0;
                            response.BudgetName = reader["budgetName"]?.ToString();
                            response.CompanyId = reader["companyId"] != DBNull.Value ? Convert.ToInt32(reader["companyId"]) : 0;
                            response.SubBudgetsCreated = reader["subBudgetsCreated"] != DBNull.Value ? Convert.ToInt32(reader["subBudgetsCreated"]) : 0;
                            response.Message = reader["message"]?.ToString();
                            response.Success = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<CreateMonthlyAllocation2Response> CreateMonthlyAllocationsAsync(CreateMonthlyAllocation2Request request)
        {
            var response = new CreateMonthlyAllocation2Response();

            try
            {
                using (var connection = new SqlConnection(_connectionString))
                using (var command = new SqlCommand("[bud].[CreateMonthlyAllocations]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@budgetId", request.BudgetId);
                    command.Parameters.AddWithValue("@allocationMonth", request.AllocationMonth);
                    command.Parameters.AddWithValue("@budgetAllocatedAmount", request.BudgetAllocatedAmount);
                    command.Parameters.AddWithValue("@budgetNotes", (object?)request.BudgetNotes ?? DBNull.Value);

                    // Create table-valued parameter
                    var subBudgetTable = new DataTable();
                    subBudgetTable.Columns.Add("subBudgetId", typeof(int));
                    subBudgetTable.Columns.Add("allocatedAmount", typeof(decimal));
                    subBudgetTable.Columns.Add("notes", typeof(string));

                    if (request.SubBudgetAllocations != null && request.SubBudgetAllocations.Count > 0)
                    {
                        foreach (var sba in request.SubBudgetAllocations)
                        {
                            subBudgetTable.Rows.Add(sba.SubBudgetId, sba.AllocatedAmount, sba.Notes);
                        }
                    }

                    var tvpParam = command.Parameters.AddWithValue("@subBudgetAllocations", subBudgetTable);
                    tvpParam.SqlDbType = SqlDbType.Structured;
                    tvpParam.TypeName = "bud.MonthlyAllocationTableType";

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            response.BudgetId = reader["budgetId"] != DBNull.Value ? Convert.ToInt32(reader["budgetId"]) : 0;
                            response.AllocationMonth = reader["allocationMonth"] != DBNull.Value ? Convert.ToDateTime(reader["allocationMonth"]) : DateTime.MinValue;
                            response.BudgetAllocatedAmount = reader["budgetAllocatedAmount"] != DBNull.Value ? Convert.ToDecimal(reader["budgetAllocatedAmount"]) : 0;
                            response.SubBudgetAllocationsCreated = reader["subBudgetAllocationsCreated"] != DBNull.Value ? Convert.ToInt32(reader["subBudgetAllocationsCreated"]) : 0;
                            response.TotalSubBudgetAmount = reader["totalSubBudgetAmount"] != DBNull.Value ? Convert.ToDecimal(reader["totalSubBudgetAmount"]) : 0;
                            response.Message = reader["message"]?.ToString();
                            response.Success = true;
                        }
                        else
                        {
                            response.Success = false;
                            response.Message = "No response returned from database.";
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = ex.Message;
            }

            return response;
        }

        public async Task<IEnumerable<BudgetList2Model>> GetAllBudgetsAsync(int company, bool isActive)
        {
            var sqlQuery = "EXEC [bud].[GetAllBudgets] @company, @isActive";
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(); // Ensure the connection is opened asynchronously
                return await connection.QueryAsync<BudgetList2Model>(
                sqlQuery,
                    new {company, isActive }
                );
            }
        }

        public async Task<BudgetAndSubBudget2Response> GetBudgetAndSubBudgetDetailsAsync(int budgetId, int subBudgetId)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var multi = await conn.QueryMultipleAsync("[bud].[GetBudgetAndSubBudgetDetails]", new { budgetId, subBudgetId }, commandType: CommandType.StoredProcedure))
                {
                    var result = new BudgetAndSubBudget2Response
                    {
                        BudgetInfo = await multi.ReadFirstOrDefaultAsync<BudgetDetails2>(),
                        SubBudgetInfo = await multi.ReadFirstOrDefaultAsync<SubBudgetDetails2>(),
                        MonthlyComparison = (await multi.ReadAsync<MonthlyAllocationComparison2>()).ToList()
                    };

                    return result;

                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting template data for cloning", ex);
            }
        }

        public async Task<BudgetWithSubBudgetsResponse> GetBudgetWithSubBudgetsAsync(int budgetId)
        {
            var response = new BudgetWithSubBudgetsResponse();

            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var multi = await conn.QueryMultipleAsync("[bud].[GetBudgetWithSubBudgets]",
                    new { budgetId }, commandType: CommandType.StoredProcedure))
                {
                    // Read first result set (Budget)
                    var budget = await multi.ReadFirstOrDefaultAsync<BudgetModel2>();

                    // If no budget found
                    if (budget == null)
                    {
                        return response;
                    }
                    // Read second result set (Sub-Budgets)
                    var subBudgets = (await multi.ReadAsync<SubBudgetModel2>()).ToList();
                    response.Budgets = budget;
                    response.SubBudgets = subBudgets;
                    return response;
                }
            }
            catch (Exception ex)
            {
                return response;
            }
        }

        public async Task<BudgetAttributeResponse> GetDistinctBudgetAttributesAsync(string mode)
        {
            var response = new BudgetAttributeResponse();


            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                var result = await conn.QueryAsync<dynamic>(
                    "[bud].[GetDistinctBudgetAttributes]",
                    new { Mode = mode },
                    commandType: CommandType.StoredProcedure
                );
                response.Data = result.ToList();
            }


            return response;
        }
        public async Task<SubBudgetResponse> GetSubBudgetsByBudgetIdAsync(int budgetId, bool? isActive)
        {
            var response = new SubBudgetResponse();

            using (var conn = new SqlConnection(_connectionString))
            {
                await conn.OpenAsync();

                using (var multi = await conn.QueryMultipleAsync(
                    "[bud].[GetSubBudgetsByBudgetId]",
                    new { budgetId, isActive },
                    commandType: CommandType.StoredProcedure))
                {
                    // Result Set 1: SubBudgets
                    var subBudgets = (await multi.ReadAsync<SubBudgetModelByBudgetId>()).ToList();

                    // If no rows found
                    if (subBudgets == null || subBudgets.Count == 0)
                    {
                        response.SubBudgets = new List<SubBudgetModelByBudgetId>();
                        response.TotalSubBudgets = 0;
                        return response;
                    }

                    // Result Set 2: Count
                    var countResult = await multi.ReadFirstOrDefaultAsync<int>();

                    response.SubBudgets = subBudgets;
                    response.TotalSubBudgets = countResult;
                }
            }


            return response;
        }

        public async Task<IEnumerable<WorkflowActionSummaryModel>> GetWorkflowActionSummaryAsync(int templateId, int company)
        {
            var sqlQuery = "EXEC [bud].[GetWorkflowActionSummary] @templateId, @company";
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(); // Ensure the connection is opened asynchronously
                return await connection.QueryAsync<WorkflowActionSummaryModel>(
                sqlQuery,
                    new { templateId, company }
                );
            }

        }

        public async Task<UpdateMonthlyAllocationsResponse> UpdateMonthlyAllocationsAsync(UpdateMonthlyAllocationsRequest request)
        {
            var response = new UpdateMonthlyAllocationsResponse
            {
                Success = false,
                Message = "Unknown error",
                BudgetId = request.BudgetId,
                AllocationMonth = request.AllocationMonth
            };

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("bud.UpdateMonthlyAllocations", conn)
                {
                    CommandType = CommandType.StoredProcedure,
                    CommandTimeout = 180
                };

                cmd.Parameters.Add(new SqlParameter("@budgetId", SqlDbType.Int) { Value = request.BudgetId });
                cmd.Parameters.Add(new SqlParameter("@allocationMonth", SqlDbType.Date) { Value = request.AllocationMonth.Date });

                if (request.BudgetAllocatedAmount.HasValue)
                    cmd.Parameters.Add(new SqlParameter("@budgetAllocatedAmount", SqlDbType.Decimal) { Precision = 15, Scale = 2, Value = request.BudgetAllocatedAmount.Value });
                else
                    cmd.Parameters.Add(new SqlParameter("@budgetAllocatedAmount", SqlDbType.Decimal) { Precision = 15, Scale = 2, Value = DBNull.Value });

                if (request.BudgetNotes != null)
                    cmd.Parameters.Add(new SqlParameter("@budgetNotes", SqlDbType.NVarChar, -1) { Value = request.BudgetNotes });
                else
                    cmd.Parameters.Add(new SqlParameter("@budgetNotes", SqlDbType.NVarChar, -1) { Value = DBNull.Value });

                cmd.Parameters.Add(new SqlParameter("@updateBudgetNotes", SqlDbType.Bit) { Value = request.UpdateBudgetNotes });

                // TVP param
                var tvp = new SqlParameter("@subBudgetAllocations", SqlDbType.Structured)
                {
                    TypeName = "bud.MonthlyAllocationTableType",
                    Value = ConvertSubBudgetListToDataTable(request.SubBudgetAllocations ?? new List<MonthlyAllocationDto>())
                };
                cmd.Parameters.Add(tvp);

                await conn.OpenAsync();

                using var reader = await cmd.ExecuteReaderAsync();

                // Stored procedure does SELECT ... at the end; read those values
                if (reader != null && await reader.ReadAsync())
                {
                    response.Success = true;
                    response.Message = reader["message"]?.ToString() ?? "Allocations updated";
                    response.BudgetId = reader["budgetId"] != DBNull.Value ? Convert.ToInt32(reader["budgetId"]) : request.BudgetId;
                    response.AllocationMonth = reader["allocationMonth"] != DBNull.Value ? Convert.ToDateTime(reader["allocationMonth"]) : request.AllocationMonth;
                    response.BudgetAllocationUpdated = reader["budgetAllocationUpdated"] != DBNull.Value ? Convert.ToBoolean(reader["budgetAllocationUpdated"]) : false;
                    response.SubBudgetAllocationsUpdated = reader["subBudgetAllocationsUpdated"] != DBNull.Value ? Convert.ToInt32(reader["subBudgetAllocationsUpdated"]) : 0;
                }
                else
                {
                    response.Success = true;
                    response.Message = "Procedure executed but no result set returned.";
                }

                return response;
            }
            catch (SqlException ex)
            {
                return new UpdateMonthlyAllocationsResponse
                {
                    Success = false,
                    Message = $"SQL error: {ex.Message}",
                    BudgetId = request.BudgetId,
                    AllocationMonth = request.AllocationMonth
                };
            }
            catch (Exception ex)
            {
                return new UpdateMonthlyAllocationsResponse
                {
                    Success = false,
                    Message = $"Error: {ex.Message}",
                    BudgetId = request.BudgetId,
                    AllocationMonth = request.AllocationMonth
                };
            }
        }

        private DataTable ConvertSubBudgetListToDataTable(IEnumerable<MonthlyAllocationDto> list)
        {
            var table = new DataTable();
            // IMPORTANT: column names and types must match the TVP definition exactly.
            table.Columns.Add("subBudgetId", typeof(int));
            table.Columns.Add("allocatedAmount", typeof(decimal));
            table.Columns.Add("notes", typeof(string));

            foreach (var item in list)
            {
                var row = table.NewRow();
                row["subBudgetId"] = item.SubBudgetId;
                row["allocatedAmount"] = item.AllocatedAmount;
                row["notes"] = item.Notes ?? (object)DBNull.Value;
                table.Rows.Add(row);
            }

            return table;
        }

        public async Task<BudgetMonthlyAllocationResponse> GetBudgetMonthlyAllocationViewAsync(string budgetName, DateTime allocationMonth)
        {
            try
            {
                using (var conn = new SqlConnection(_connectionString))
                using (var multi = await conn.QueryMultipleAsync("[bud].[GetBudgetMonthlyAllocationView]", new { budgetName, allocationMonth }, commandType: CommandType.StoredProcedure))
                {
                    var result = new BudgetMonthlyAllocationResponse
                    {
                        Budget = await multi.ReadFirstOrDefaultAsync<BudgetDetailViewModel>(),
                        SubBudgets = (await multi.ReadAsync<SubBudgetAllocationViewModel>()).ToList()
                    };

                    return result;

                }
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting template data for cloning", ex);
            }
        }

        public async Task<IEnumerable<AllBudgetModel>> GetAllTypeBudgetAsync()
        {
            try
            {
                using (SqlConnection conn = new SqlConnection(_connectionString))
                {
                    await conn.OpenAsync();

                    var result = await conn.QueryAsync<AllBudgetModel>(
                        "[bud].[jsGetAllTypeBudget]",
                        commandType: CommandType.StoredProcedure
                    );

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw;
            }
        }
        public async Task<IEnumerable<SubBudgetByBudgetModel>> GetSubBudgetByBudgetAsync(string budget)
        {
            await using var conn = new SqlConnection(_connectionString);
            await conn.OpenAsync();

            var result = await conn.QueryAsync<SubBudgetByBudgetModel>(
                "[bud].[jsGetSubBudgetUsingBudget]",
                new { budget },
                commandType: CommandType.StoredProcedure
            );

            return result;
        }

        public async Task<IEnumerable<PendingBudgetAllocation>> GetPendingBudgetAllocationRequestsAsync(int userId, int companyId, string month)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryAsync<PendingBudgetAllocation>(
                    "[bud].[jsGetPendingBudgetAllocationRequests]",
                    new { userId, companyId, month },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting data", ex);
            }

        }

        public async Task<IEnumerable<ApprovedBudgetAllocation>> GetApprovedBudgetAllocationRequestsAsync(int userId, int companyId, string month)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryAsync<ApprovedBudgetAllocation>(
                    "[bud].[jsGetApprovedBudgetAllocationRequests]",
                    new { userId, companyId, month },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting data", ex);
            }

        }

        public async Task<IEnumerable<RejectedBudgetAllocation>> GetRejectedBudgetAllocationRequestsAsync(int userId, int companyId, string month)
        {
            try
            {
                using var conn = new SqlConnection(_connectionString);
                return await conn.QueryAsync<RejectedBudgetAllocation>(
                    "[bud].[jsGetRejectedBudgetAllocationRequests]",
                    new { userId, companyId, month },
                    commandType: CommandType.StoredProcedure);
            }
            catch (Exception ex)
            {
                throw new Exception("Error getting data", ex);
            }

        }

        public async Task<ApproveBudgetAllocationResponse> ApproveBudgetAllocationRequestAsync(ApproveBudgetAllocationRequest request)
        {
            var response = new ApproveBudgetAllocationResponse
            {
                Success = false,
                FlowId = request.FlowId
            };

            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QueryFirstOrDefaultAsync<ApproveBudgetAllocationResponse>(
                    "[bud].[jsApproveBudgetAllocationRequest]",
                    new
                    {
                        flowId = request.FlowId,
                        company = request.Company,
                        userId = request.UserId,
                        remarks = request.Remarks,
                        //action = request.Action
                    },
                    commandType: CommandType.StoredProcedure
                );

                if (result != null)
                {
                    response.Success = true;
                    response.ResultMessage = result.ResultMessage;
                    response.BudgetAllocationRequestId = result.BudgetAllocationRequestId;
                    response.CompanyId = result.CompanyId;
                    response.FlowId = result.FlowId;
                }
                else
                {
                    response.ResultMessage = "No response returned from database.";
                }
            }
            catch (SqlException ex)
            {
                response.Success = false;
                response.ResultMessage = ex.Message;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ResultMessage = ex.Message;
            }

            return response;
        }

        public async Task<RejectBudgetAllocationResponse> RejectBudgetAllocationRequestAsync(RejectBudgetAllocationRequest request)
        {
            var response = new RejectBudgetAllocationResponse
            {
                Success = false,
                FlowId = request.FlowId
            };

            try
            {
                using var conn = new SqlConnection(_connectionString);
                var result = await conn.QueryFirstOrDefaultAsync<RejectBudgetAllocationResponse>(
                    "[bud].[jsRejectBudgetAllocationRequest]",
                    new
                    {
                        flowId = request.FlowId,
                        company = request.Company,
                        userId = request.UserId,
                        remarks = request.Remarks,
                        //action = request.Action
                    },
                    commandType: CommandType.StoredProcedure
                );

                if (result != null)
                {
                    response.Success = true;
                    response.ResultMessage = result.ResultMessage;
                    response.BudgetAllocationRequestId = result.BudgetAllocationRequestId;
                    response.CompanyId = result.CompanyId;
                    response.FlowId = result.FlowId;
                }
                else
                {
                    response.ResultMessage = "No response returned from database.";
                }
            }
            catch (SqlException ex)
            {
                response.Success = false;
                response.ResultMessage = ex.Message;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ResultMessage = ex.Message;
            }

            return response;
        }

        public async Task<CreateBudgetAllocationRequestResponse> CreateBudgetAllocationRequestAsync(CreateBudgetAllocationRequestModel request)
        {
            var response = new CreateBudgetAllocationRequestResponse
            {
                Success = false,
                NewRequestId = 0
            };

            try
            {
                using var conn = new SqlConnection(_connectionString);
                using var cmd = new SqlCommand("[bud].[CreateBudgetAllocationRequest]", conn)
                {
                    CommandType = CommandType.StoredProcedure
                };

                // Input parameters
                cmd.Parameters.Add(new SqlParameter("@budgetAllocationId", SqlDbType.Int) { Value = request.BudgetAllocationId });
                cmd.Parameters.Add(new SqlParameter("@newAmount", SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = request.NewAmount });
                cmd.Parameters.Add(new SqlParameter("@createdBy", SqlDbType.Int) { Value = request.CreatedBy });

                // Output parameters
                var newRequestIdParam = new SqlParameter("@newRequestId", SqlDbType.Int)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(newRequestIdParam);

                var messageParam = new SqlParameter("@message", SqlDbType.NVarChar, 500)
                {
                    Direction = ParameterDirection.Output
                };
                cmd.Parameters.Add(messageParam);

                // Return value parameter
                var returnValueParam = new SqlParameter("@returnValue", SqlDbType.Int)
                {
                    Direction = ParameterDirection.ReturnValue
                };
                cmd.Parameters.Add(returnValueParam);

                await conn.OpenAsync();
                await cmd.ExecuteNonQueryAsync();

                // Read output values
                int returnValue = (int)returnValueParam.Value;
                response.NewRequestId = newRequestIdParam.Value != DBNull.Value ? (int)newRequestIdParam.Value : 0;
                response.Message = messageParam.Value?.ToString();
                response.Success = returnValue == 0; // 0 = Success, -1 = Error
            }
            catch (SqlException ex)
            {
                response.Success = false;
                response.Message = $"SQL Error: {ex.Message}";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.Message = $"Error: {ex.Message}";
            }

            return response;
        }
        public async Task<BudgetAllocationResponse> GetBudgetAllocationRequestDetail(int requestId)
        {
            var response = new BudgetAllocationResponse();

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("[bud].[jsGetBudgetAllocationRequestDetail]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@requestId", requestId);

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        // First result set - Request Detail
                        if (await reader.ReadAsync())
                        {
                            response.RequestDetail = new BudgetAllocationRequestDetail
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("id")),
                                CompanyId = reader.GetInt32(reader.GetOrdinal("companyId")),
                                BudgetId = reader.GetInt32(reader.GetOrdinal("budgetId")),
                                BudgetName = reader.GetString(reader.GetOrdinal("budgetName")),
                                // ... map other fields
                            };
                        }

                        // Second result set - Approval History
                        if (await reader.NextResultAsync())
                        {
                            response.ApprovalHistory = new List<ApprovalStatus>();
                            while (await reader.ReadAsync())
                            {
                                response.ApprovalHistory.Add(new ApprovalStatus
                                {
                                    StatusId = reader.GetInt32(reader.GetOrdinal("statusId")),
                                    // ... map other fields
                                });
                            }
                        }
                    }
                }
            }

            return response;
        }
        public async Task<IEnumerable<AllBudgetAllocation>> GetAllBudgetAllocationRequestsAsync(int userId, int companyId, string month)
        {
            try
            {
                var result = new List<AllBudgetAllocation>();

                using var conn = new SqlConnection(_connectionString);
                await conn.OpenAsync();

                // Get Pending
                var pending = await conn.QueryAsync<PendingBudgetAllocation>(
                    "[bud].[jsGetPendingBudgetAllocationRequests]",
                    new { userId, companyId, month },
                    commandType: CommandType.StoredProcedure);

                var pendingList = pending.ToList();  // Force enumeration
                System.Diagnostics.Debug.WriteLine($"Pending Count: {pendingList.Count}");

                // Get Approved  
                var approved = await conn.QueryAsync<ApprovedBudgetAllocation>(
                    "[bud].[jsGetApprovedBudgetAllocationRequests]",
                    new { userId, companyId, month },
                    commandType: CommandType.StoredProcedure);

                var approvedList = approved.ToList();
                System.Diagnostics.Debug.WriteLine($"Approved Count: {approvedList.Count}");

                // Get Rejected
                var rejected = await conn.QueryAsync<RejectedBudgetAllocation>(
                    "[bud].[jsGetRejectedBudgetAllocationRequests]",
                    new { userId, companyId, month },
                    commandType: CommandType.StoredProcedure);

                var rejectedList = rejected.ToList();
                System.Diagnostics.Debug.WriteLine($"Rejected Count: {rejectedList.Count}");

                foreach (var item in pendingList)
                {
                    result.Add(new AllBudgetAllocation
                    {
                        id = item.id,
                        companyId = item.companyId,
                        budgetId = item.budgetId,
                        budgetName = item.budgetName,
                        allocationId = item.allocationId,
                        allocationMonth = item.alllocationMonth,
                        currentAmount = item.currentAmount,
                        requestedAmount = item.requestedAmount,
                        amountDifference = item.amountDiffeence,
                        createdById = item.createdById,
                        createdBy = item.createdBy,
                        createdOn = item.createdOn,
                        flowId = item.flowId,
                        flowStatus = item.flowStatus,
                        currentStage = item.currentStage,
                        totalStage = item.totalStage,
                        Status = "Pending",
                        remarks = null,
                        actionDate = null
                    });
                }

                foreach (var item in approvedList)
                {
                    result.Add(new AllBudgetAllocation
                    {
                        id = item.id,
                        companyId = item.companyId,
                        budgetId = item.budgetId,
                        budgetName = item.budgetName,
                        allocationId = item.allocationId,
                        allocationMonth = item.alllocationMonth,
                        currentAmount = item.currentAmount,
                        requestedAmount = item.requestedAmount,
                        amountDifference = item.amountDiffeence,
                        createdById = item.createdById,
                        createdBy = item.createdBy,
                        createdOn = item.requestCreatedOn,
                        flowId = item.flowId,
                        flowStatus = item.flowStatus,
                        currentStage = item.currentStage,
                        totalStage = item.totalStage,
                        Status = "Approved",
                        remarks = item.approvalRemarks,
                        actionDate = item.approvalDate
                    });
                }

                foreach (var item in rejectedList)
                {
                    result.Add(new AllBudgetAllocation
                    {
                        id = item.id,
                        companyId = item.companyId,
                        budgetId = item.budgetId,
                        budgetName = item.budgetName,
                        allocationId = item.allocationId,
                        allocationMonth = item.alllocationMonth,
                        currentAmount = item.currentAmount,
                        requestedAmount = item.requestedAmount,
                        amountDifference = item.amountDiffeence,
                        createdById = item.createdById,
                        createdBy = item.createdBy,
                        createdOn = item.requestCreatedOn,
                        flowId = item.flowId,
                        flowStatus = item.flowStatus,
                        currentStage = item.currentStage,
                        totalStage = item.totalStage,
                        Status = "Rejected",
                        remarks = item.rejectionRemarks,
                        actionDate = item.rejectionDate
                    });
                }

                System.Diagnostics.Debug.WriteLine($"Total Result Count: {result.Count}");
                return result.OrderByDescending(x => x.createdOn);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
                throw new Exception("Error getting data: " + ex.Message, ex);
            }
        }

        public async Task<BudgetInsightsMonthlyAllocationModel> GetBudgetMonthlyAllocationInsights(int userId, int company, string month)
        {
            var result = new BudgetInsightsMonthlyAllocationModel();

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("bud.jsGetBudgetInsightMonthlyAllocation", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;

                    command.Parameters.AddWithValue("@userId", userId);
                    command.Parameters.AddWithValue("@company", company);
                    command.Parameters.AddWithValue("@month", month);

                    connection.Open();

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            result.TotalPending = reader.GetInt32(reader.GetOrdinal("TotalPending"));
                            result.TotalApproved = reader.GetInt32(reader.GetOrdinal("TotalApproved"));
                            result.TotalRejected = reader.GetInt32(reader.GetOrdinal("TotalRejected"));
                        }
                    }
                }
            }

            return result;
        }
        public async Task<IEnumerable<budgetAllocationFlowModel>> GetBudgetAllocationFlowAsync(int flowId)
        {
            var result = new List<budgetAllocationFlowModel>();

            using (var connection = new SqlConnection(_connectionString))
            {
                using (var command = new SqlCommand("[bud].[jsGetBudApprovalFlow]", connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@flowId", flowId);

                    await connection.OpenAsync();

                    using (var reader = await command.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())  // Use while for multiple rows
                        {
                            var item = new budgetAllocationFlowModel
                            {
                                stageId = reader.GetInt32(reader.GetOrdinal("stageId")),
                                stageName = reader.IsDBNull(reader.GetOrdinal("stageName")) ? null : reader.GetString(reader.GetOrdinal("stageName")),
                                priority = reader.GetInt32(reader.GetOrdinal("priority")),
                                assignedTo = reader.IsDBNull(reader.GetOrdinal("assignedTo")) ? null : reader.GetString(reader.GetOrdinal("assignedTo")),
                                actionStatus = reader.IsDBNull(reader.GetOrdinal("actionStatus")) ? null : reader.GetString(reader.GetOrdinal("actionStatus")),
                                actionDate = reader.IsDBNull(reader.GetOrdinal("actionDate")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("actionDate")),
                                description = reader.IsDBNull(reader.GetOrdinal("description")) ? null : reader.GetString(reader.GetOrdinal("description")),
                                approvalRequired = reader.GetInt32(reader.GetOrdinal("approvalRequired")),
                                rejectRequired = reader.GetInt32(reader.GetOrdinal("rejectRequired"))
                            };
                            result.Add(item);
                        }
                    }
                }
            }
            return result;
        }

    }
}

