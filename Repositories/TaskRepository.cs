using System;
using System.Collections.Generic;
using System.Linq;
using Sprint2.Interfaces;
using Sprint2.Models;

namespace Sprint2.Repositories
{
    /// <summary>
    /// In-memory repository for managing TodoTask entities.
    /// </summary>
    public class TaskRepository : ITaskRepository
    {
        private static List<TodoTask> _tasks = new List<TodoTask>();
        private static int _nextId = 1;

        /// <summary>
        /// Retrieves all tasks with optional filtering and pagination.
        /// </summary>
        /// <param name="filter">The filter criteria for tasks.</param>
        /// <param name="page">The page number for pagination.</param>
        /// <param name="pageSize">The number of tasks per page.</param>
        /// <returns>A tuple containing the list of tasks and the total count of tasks.</returns>
        public (IEnumerable<TodoTask> Tasks, int TotalCount) GetAll(TaskFilter filter, int page, int pageSize)
        {
            var query = _tasks.AsQueryable();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                query = query.Where(t => t.Description.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase) ||
                                       (t.Category != null && t.Category.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase)) ||
                                       (t.Notes != null && t.Notes.Contains(filter.SearchTerm, StringComparison.OrdinalIgnoreCase)));
            }

            if (filter.Status.HasValue)
            {
                query = query.Where(t => t.Status == filter.Status.Value);
            }

            if (filter.Priority.HasValue)
            {
                query = query.Where(t => t.Priority == filter.Priority.Value);
            }

            if (!string.IsNullOrWhiteSpace(filter.Category))
            {
                query = query.Where(t => t.Category == filter.Category);
            }

            if (filter.IsOverdue.HasValue)
            {
                var today = DateTime.UtcNow.Date;
                query = filter.IsOverdue.Value
                    ? query.Where(t => t.DueDate.HasValue && t.DueDate.Value.Date < today)
                    : query.Where(t => !t.DueDate.HasValue || t.DueDate.Value.Date >= today);
            }

            // Get total count before pagination
            int totalCount = query.Count();

            // Apply pagination
            var tasks = query
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return (tasks, totalCount);
        }

        /// <summary>
        /// Retrieves a task by its ID.
        /// </summary>
        /// <param name="id">The ID of the task to retrieve.</param>
        /// <returns>The task with the specified ID, or null if not found.</returns>
        public TodoTask GetById(int id)
        {
            return _tasks.Find(t => t.Id == id);
        }

        /// <summary>
        /// Adds a new task to the in-memory list.
        /// </summary>
        /// <param name="task">The task to add.</param>
        public void Add(TodoTask task)
        {
            task.Id = _nextId++;
            _tasks.Add(task);
        }

        /// <summary>
        /// Updates an existing task in the in-memory list.
        /// </summary>
        /// <param name="task">The task to update.</param>
        public void Update(TodoTask task)
        {
            var existingTask = _tasks.Find(t => t.Id == task.Id);
            if (existingTask != null)
            {
                existingTask.Description = task.Description;
                existingTask.Status = task.Status;
                existingTask.Priority = task.Priority;
                existingTask.Category = task.Category;
                existingTask.Notes = task.Notes;
                existingTask.DueDate = task.DueDate;
            }
        }

        /// <summary>
        /// Deletes a task from the in-memory list by its ID.
        /// </summary>
        /// <param name="id">The ID of the task to delete.</param>
        public void Delete(int id)
        {
            _tasks.RemoveAll(x => x.Id == id);
        }

        /// <summary>
        /// Retrieves a list of distinct categories from the tasks.
        /// </summary>
        /// <returns>A list of distinct categories.</returns>
        public IEnumerable<string> GetCategories()
        {
            return _tasks
                .Where(t => !string.IsNullOrEmpty(t.Category))
                .Select(t => t.Category)
                .Distinct()
                .OrderBy(c => c);
        }
    }
}
