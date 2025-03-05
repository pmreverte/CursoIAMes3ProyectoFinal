using System.Collections.Generic;
using Sprint2.Models;

namespace Sprint2.Interfaces
{
/// <summary>
/// Defines the contract for task repository operations.
/// </summary>
public interface ITaskRepository
    {
    /// <summary>
    /// Retrieves a paginated list of tasks based on the specified filter.
    /// </summary>
    /// <param name="filter">The filter criteria for tasks.</param>
    /// <param name="page">The page number for pagination.</param>
    /// <param name="pageSize">The number of tasks per page.</param>
    /// <returns>A tuple containing the list of tasks and the total count.</returns>
    (IEnumerable<TodoTask> Tasks, int TotalCount) GetAll(TaskFilter filter, int page, int pageSize);
    /// <summary>
    /// Retrieves a task by its ID.
    /// </summary>
    /// <param name="id">The ID of the task to retrieve.</param>
    /// <returns>The task with the specified ID.</returns>
    TodoTask GetById(int id);
    /// <summary>
    /// Adds a new task to the repository.
    /// </summary>
    /// <param name="task">The task to add.</param>
    void Add(TodoTask task);
    /// <summary>
    /// Updates an existing task in the repository.
    /// </summary>
    /// <param name="task">The task with updated information.</param>
    void Update(TodoTask task);
    /// <summary>
    /// Deletes a task from the repository by its ID.
    /// </summary>
    /// <param name="id">The ID of the task to delete.</param>
    void Delete(int id);
    /// <summary>
    /// Retrieves a list of task categories.
    /// </summary>
    /// <returns>A list of task categories.</returns>
    IEnumerable<string> GetCategories();
    }
}
