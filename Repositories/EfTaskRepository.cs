using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Sprint2.Data;
using Sprint2.Interfaces;
using Sprint2.Models;

namespace Sprint2.Repositories
{
    /// <summary>
    /// Repository for managing TodoTask entities using Entity Framework Core.
    /// </summary>
    public class EfTaskRepository : ITaskRepository
    {
        private readonly ApplicationDbContext _context;

        /// <summary>
        /// Initializes a new instance of the EfTaskRepository class.
        /// </summary>
        /// <param name="context">The database context to be used for data operations.</param>
        public EfTaskRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Retrieves all tasks with optional filtering and pagination.
        /// </summary>
        /// <param name="filter">The filter criteria for tasks.</param>
        /// <param name="page">The page number for pagination.</param>
        /// <param name="pageSize">The number of tasks per page.</param>
        /// <returns>A tuple containing the list of tasks and the total count of tasks.</returns>
        public (IEnumerable<TodoTask> Tasks, int TotalCount) GetAll(TaskFilter filter, int page, int pageSize)
        {
            // Usar AsNoTracking para mejorar el rendimiento de consultas de solo lectura
            var query = _context.Tasks.AsNoTracking();

            // Apply filters
            if (!string.IsNullOrWhiteSpace(filter.SearchTerm))
            {
                var searchTerm = filter.SearchTerm.ToLower();
                query = query.Where(t => t.Description.ToLower().Contains(searchTerm) ||
                                       (t.Category != null && t.Category.ToLower().Contains(searchTerm)) ||
                                       (t.Notes != null && t.Notes.ToLower().Contains(searchTerm)));
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

            // Ordenar la consulta
            var orderedQuery = query
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate);

            // Optimización: Obtener el total y los resultados en una sola consulta
            var pagedQuery = orderedQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            // Ejecutar la consulta y obtener los resultados
            var tasks = pagedQuery.ToList();
            
            // Obtener el total de manera más eficiente
            int totalCount = query.Count();

            return (tasks, totalCount);
        }

        /// <summary>
        /// Retrieves a task by its ID.
        /// </summary>
        /// <param name="id">The ID of the task to retrieve.</param>
        /// <returns>The task with the specified ID, or a new TodoTask if not found.</returns>
        public TodoTask GetById(int id)
        {
            // Usar Find para búsquedas por clave primaria (más eficiente)
            return _context.Tasks.Find(id) ?? new TodoTask();
        }

        /// <summary>
        /// Adds a new task to the database.
        /// </summary>
        /// <param name="task">The task to add.</param>
        public void Add(TodoTask task)
        {
            _context.Tasks.Add(task);
            _context.SaveChanges();
        }

        /// <summary>
        /// Updates an existing task in the database.
        /// </summary>
        /// <param name="task">The task to update.</param>
        public void Update(TodoTask task)
        {
            _context.Tasks.Update(task);
            _context.SaveChanges();
        }

        /// <summary>
        /// Deletes a task from the database by its ID.
        /// </summary>
        /// <param name="id">The ID of the task to delete.</param>
        public void Delete(int id)
        {
            var task = _context.Tasks.Find(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                _context.SaveChanges();
            }
        }

        /// <summary>
        /// Retrieves a list of distinct categories from the tasks.
        /// </summary>
        /// <returns>A list of distinct categories.</returns>
        public IEnumerable<string> GetCategories()
        {
            // Usar AsNoTracking para mejorar el rendimiento
            return _context.Tasks
                .AsNoTracking()
                .Where(t => !string.IsNullOrEmpty(t.Category))
                .Select(t => t.Category!)
                .Distinct()
                .OrderBy(c => c)
                .ToList();
        }
    }
}
