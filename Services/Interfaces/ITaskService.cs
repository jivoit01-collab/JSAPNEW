using JSAPNEW.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface ITaskService
    {
        // Core CRUD
        Task<TaskResponseDto> CreateTaskAsync(TaskCreateDto taskDto);
        Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync(TaskFilterDto filterDto);
        Task<TaskResponse> DeleteTaskAsync(DeleteTaskRequestDto dto);
        Task<CompleteTaskResponseDto> CompleteTaskAsync(CompleteTaskRequestDto dto);

        // Dashboard
        Task<TaskDashboardResponseDto> GetTaskDashboardAsync(TaskDashboardRequestDto dto);
        Task<TaskReportResponseDto> GetTaskReportAsync(TaskReportRequestDto dto);

        // Progress Updates
        Task<TaskProgressResponseDto> AddProgressUpdateAsync(TaskProgressCreateDto dto);
        Task<IEnumerable<TaskProgressResponseDto>> GetProgressUpdatesAsync(string taskId);
        Task<TaskDetailsResponseDto?> GetTaskDetailsAsync(string taskId);

        // Reassign
        Task<TaskResponse> ReassignTaskAsync(ReassignTaskRequestDto dto);
    }
}
