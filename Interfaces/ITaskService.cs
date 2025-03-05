using System.Collections.Generic;
using Sprint2.Models;

namespace Sprint2.Interfaces
{
/// <summary>
/// Defines the contract for task service operations.
/// </summary>
public interface ITaskService
    {
    /// <summary>
    /// Retrieves a paginated list of tasks based on the specified filter.
    /// </summary>
    /// <param name="filter">The filter criteria for tasks.</param>
    /// <param name="page">The page number for pagination.</param>
    /// <param name="pageSize">The number of tasks per page.</param>
    /// <returns>A TaskListViewModel containing the list of tasks and pagination information.</returns>
    TaskListViewModel GetTaskList(TaskFilter filter, int page, int pageSize);
    /// <summary>
    /// Retrieves a task by its ID.
    /// </summary>
    /// <param name="id">The ID of the task to retrieve.</param>
    /// <returns>The task with the specified ID.</returns>
    TodoTask GetTaskById(int id);
    /// <summary>
    /// Creates a new task.
    /// </summary>
    /// <param name="task">The task to create.</param>
    void CreateTask(TodoTask task);
    /// <summary>
    /// Updates an existing task.
    /// </summary>
    /// <param name="task">The task with updated information.</param>
    void UpdateTask(TodoTask task);
    /// <summary>
    /// Updates only the status of a task.
    /// </summary>
    /// <param name="id">The ID of the task to update.</param>
    /// <param name="newStatus">The new status to set.</param>
    /// <returns>The updated task.</returns>
    TodoTask UpdateTaskStatus(int id, TodoTaskStatus newStatus);
    /// <summary>
    /// Deletes a task by its ID.
    /// </summary>
    /// <param name="id">The ID of the task to delete.</param>
    void DeleteTask(int id);
    /// <summary>
    /// Retrieves a list of task categories.
    /// </summary>
    /// <returns>A list of task categories.</returns>
    IEnumerable<string> GetCategories();
    }
}
