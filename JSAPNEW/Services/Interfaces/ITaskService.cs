using JSAPNEW.Models;

namespace JSAPNEW.Services.Interfaces
{
    public interface ITaskService
    {
        Task<TaskResponseDto> CreateTaskAsync(TaskCreateDto taskDto);
        Task<IEnumerable<TaskResponseDto>> GetAllTasksAsync(TaskFilterDto filterDto);
        Task<TaskResponse> DeleteTaskAsync(string taskId);
        Task<CompleteTaskResponseDto> CompleteTaskAsync(CompleteTaskRequestDto dto);
    }
}
