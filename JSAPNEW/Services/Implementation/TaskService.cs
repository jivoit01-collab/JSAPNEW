using Dapper;
using JSAPNEW.Models;
using JSAPNEW.Services.Interfaces;
using Microsoft.Data.SqlClient;
using System.Data;

namespace JSAPNEW.Services.Implementation
{
    public class TaskService : ITaskService
    {
        private readonly string _connectionString;

        public TaskService(IConfiguration configuration)
        {
            _connectionString = configuration.GetConnectionString("DefaultConnection");
        }
        public async Task<TaskResponseDto> CreateTaskAsync(TaskCreateDto taskDto)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@task_name", taskDto.TaskName);
                    parameters.Add("@description", taskDto.Description);
                    parameters.Add("@project_name", taskDto.ProjectName);
                    parameters.Add("@module_name", taskDto.ModuleName);
                    parameters.Add("@assigned_to", taskDto.AssignedTo);
                    parameters.Add("@created_by", taskDto.CreatedBy);
                    parameters.Add("@start_date", taskDto.StartDate);
                    parameters.Add("@expected_end_date", taskDto.ExpectedEndDate);
                    parameters.Add("@priority", taskDto.Priority);
                    parameters.Add("@dept_id", taskDto.DeptId);

                    var result = await connection.QueryFirstOrDefaultAsync<TaskResponseDto>(
                        "CreateTask",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    if (result == null)
                        throw new Exception("Task creation failed. Stored procedure returned no result.");

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error creating task: {ex.Message}", ex);
            }
        }
        public async Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync(TaskFilterDto dto)
        {
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    var parameters = new DynamicParameters();

                    parameters.Add("@page", dto.Page);
                    parameters.Add("@limit", dto.Limit);
                    parameters.Add("@status", dto.Status);
                    parameters.Add("@priority", dto.Priority);
                    parameters.Add("@project_name", dto.ProjectName);
                    parameters.Add("@module_name", dto.ModuleName);
                    parameters.Add("@assigned_to", dto.AssignedTo);
                    parameters.Add("@created_by", dto.CreatedBy);
                    parameters.Add("@dept_id", dto.DeptId);
                    parameters.Add("@sort_by", dto.SortBy);
                    parameters.Add("@sort_order", dto.SortOrder);

                    var result = await connection.QueryAsync<TaskResponseDto>(
                        "GetAllTasks",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    return result;
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Error fetching tasks: {ex.Message}", ex);
            }
        }

        public async Task<TaskResponse> DeleteTaskAsync(string taskId)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@task_id", taskId);

                    var result = await connection.QueryFirstOrDefaultAsync<string>(
                        "DeleteTask",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    return new TaskResponse
                    {
                        Success = true,
                        Message = result ?? "Task deleted successfully"
                    };
                }
                catch (SqlException ex)
                {
                    if (ex.Message.Contains("Task not found"))
                    {
                        return new TaskResponse
                        {
                            Success = false,
                            Message = "Task not found"
                        };
                    }

                    throw new Exception("SQL Error: " + ex.Message, ex);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error deleting task: " + ex.Message, ex);
                }
            }
        }

        public async Task<CompleteTaskResponseDto> CompleteTaskAsync(CompleteTaskRequestDto dto)
        {
            using (var connection = new SqlConnection(_connectionString))
            {
                try
                {
                    var parameters = new DynamicParameters();
                    parameters.Add("@task_id", dto.TaskId);
                    parameters.Add("@completion_date", dto.CompletionDate);
                    parameters.Add("@last_modified_by", dto.LastModifiedBy);

                    var result = await connection.QueryFirstOrDefaultAsync<CompleteTaskResponseDto>(
                        "CompleteTask",
                        parameters,
                        commandType: CommandType.StoredProcedure
                    );

                    if (result == null)
                        throw new Exception("No response received from stored procedure.");

                    return result;
                }
                catch (SqlException ex)
                {
                    if (ex.Message.Contains("Task not found"))
                        throw new Exception("Task not found.");

                    throw new Exception("SQL Error: " + ex.Message, ex);
                }
                catch (Exception ex)
                {
                    throw new Exception("Error completing task: " + ex.Message, ex);
                }
            }
        }

    }
}
