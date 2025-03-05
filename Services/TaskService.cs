using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Sprint2.Data;
using Sprint2.Interfaces;
using Sprint2.Models;

namespace Sprint2.Services
{
    /// <summary>
    /// Service for managing tasks, providing operations to interact with task data.
    /// </summary>
    public class TaskService : ITaskService
    {
        private readonly ITaskRepository _taskRepository;
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ICacheService _cacheService;

        /// <summary>
        /// Initializes a new instance of the TaskService class.
        /// </summary>
        /// <param name="taskRepository">The repository used for task data operations.</param>
        /// <param name="context">The database context.</param>
        /// <param name="httpContextAccessor">The HTTP context accessor for user information.</param>
        /// <param name="cacheService">The cache service for caching frequently accessed data.</param>
        public TaskService(
            ITaskRepository taskRepository,
            ApplicationDbContext context,
            IHttpContextAccessor httpContextAccessor,
            ICacheService cacheService)
        {
            _taskRepository = taskRepository;
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _cacheService = cacheService;
        }

        private void CreateAuditLog(string action, TodoTask task, TodoTask oldTask = null)
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var userId = user?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var userName = user?.Identity?.Name;

            var changes = string.Empty;
            if (oldTask != null)
            {
                var changedProperties = new Dictionary<string, object>();
                if (oldTask.Description != task.Description)
                    changedProperties["Description"] = task.Description;
                if (oldTask.Category != task.Category)
                    changedProperties["Category"] = task.Category;
                if (oldTask.DueDate != task.DueDate)
                    changedProperties["DueDate"] = task.DueDate;
                if (oldTask.Priority != task.Priority)
                    changedProperties["Priority"] = task.Priority;
                if (oldTask.Status != task.Status)
                    changedProperties["Status"] = task.Status;
                if (oldTask.Notes != task.Notes)
                    changedProperties["Notes"] = task.Notes;

                if (changedProperties.Count > 0)
                    changes = JsonSerializer.Serialize(changedProperties);
            }

            var auditLog = new AuditLog
            {
                EntityName = "TodoTask",
                EntityId = task.Id,
                Action = action,
                Changes = changes,
                UserId = userId,
                UserName = userName,
                Timestamp = DateTime.UtcNow
            };

            _context.AuditLogs.Add(auditLog);
            _context.SaveChanges();
        }

        /// <summary>
        /// Retrieves a paginated list of tasks based on the provided filter.
        /// </summary>
        /// <param name="filter">The filter criteria for tasks.</param>
        /// <param name="page">The page number for pagination.</param>
        /// <param name="pageSize">The number of tasks per page.</param>
        /// <returns>A TaskListViewModel containing the tasks, filter, and pagination info.</returns>
        public TaskListViewModel GetTaskList(TaskFilter filter, int page, int pageSize)
        {
            try
            {
                // Normalizar el filtro para evitar claves de caché innecesarias
                filter = NormalizeFilter(filter);
                
                // Generamos una clave de caché basada en los parámetros
                string cacheKey = GenerateCacheKey(filter, page, pageSize);
                
                // Intentamos obtener del caché
                var cachedResult = _cacheService.GetAsync<TaskListViewModel>(cacheKey).Result;
                if (cachedResult != null)
                {
                    return cachedResult;
                }
                
                // Si no está en caché, obtenemos de la base de datos
                var (tasks, totalCount) = _taskRepository.GetAll(filter, page, pageSize);

                var result = new TaskListViewModel
                {
                    Tasks = tasks,
                    Filter = filter,
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalItems = totalCount
                    }
                };
                
                // Aumentamos el tiempo de caché a 5 minutos para mejorar el rendimiento
                // Las actualizaciones de tareas invalidarán la caché cuando sea necesario
                _cacheService.SetAsync(cacheKey, result, TimeSpan.FromMinutes(5));
                
                return result;
            }
            catch (Exception ex)
            {
                // Log del error
                Console.WriteLine($"Error al obtener la lista de tareas: {ex.Message}");
                
                // Devolver un modelo vacío en caso de error
                return new TaskListViewModel
                {
                    Tasks = new List<TodoTask>(),
                    Filter = filter ?? new TaskFilter(),
                    Pagination = new PaginationInfo
                    {
                        CurrentPage = page,
                        PageSize = pageSize,
                        TotalItems = 0
                    }
                };
            }
        }
        
        /// <summary>
        /// Normaliza el filtro para evitar claves de caché innecesarias
        /// </summary>
        private TaskFilter NormalizeFilter(TaskFilter filter)
        {
            if (filter == null)
                return new TaskFilter();
                
            // Normalizar strings vacíos a null
            if (string.IsNullOrWhiteSpace(filter.SearchTerm))
                filter.SearchTerm = null;
                
            if (string.IsNullOrWhiteSpace(filter.Category))
                filter.Category = null;
                
            return filter;
        }
        
        /// <summary>
        /// Genera una clave de caché basada en los parámetros del filtro
        /// </summary>
        private string GenerateCacheKey(TaskFilter filter, int page, int pageSize)
        {
            return $"tasklist_{filter?.Status}_{filter?.Priority}_{filter?.Category}_{filter?.SearchTerm}_{page}_{pageSize}";
        }

        /// <summary>
        /// Retrieves a task by its ID.
        /// </summary>
        /// <param name="id">The ID of the task to retrieve.</param>
        /// <returns>The task with the specified ID.</returns>
        public TodoTask GetTaskById(int id)
        {
            var cacheKey = $"task_{id}";
            var cachedTask = _cacheService.GetAsync<TodoTask>(cacheKey).Result;
            
            if (cachedTask != null)
            {
                return cachedTask;
            }
            
            var task = _taskRepository.GetById(id);
            
            if (task != null)
            {
                _cacheService.SetAsync(cacheKey, task, TimeSpan.FromMinutes(30));
            }
            
            return task;
        }

        /// <summary>
        /// Creates a new task.
        /// </summary>
        /// <param name="task">The task to create.</param>
        public void CreateTask(TodoTask task)
        {
            _taskRepository.Add(task);
            CreateAuditLog("Create", task);
            
            // Invalidar caché de categorías y listas de tareas
            _cacheService.RemoveAsync("categories");
            InvalidateTaskListCache();
        }
        
        /// <summary>
        /// Invalida todas las claves de caché relacionadas con listas de tareas
        /// </summary>
        private void InvalidateTaskListCache()
        {
            // Usamos un patrón simple para invalidar todas las claves que empiezan con "tasklist_"
            // En una implementación más avanzada, podríamos usar Redis SCAN para encontrar y eliminar
            // claves específicas basadas en patrones
            _cacheService.RemoveAsync("tasklist_*");
        }

        /// <summary>
        /// Updates an existing task.
        /// </summary>
        /// <param name="task">The task to update.</param>
        public void UpdateTask(TodoTask task)
        {
            var oldTask = _taskRepository.GetById(task.Id);
            _taskRepository.Update(task);
            CreateAuditLog("Update", task, oldTask);
            
            // Invalidar caché de la tarea y categorías si cambió la categoría
            _cacheService.RemoveAsync($"task_{task.Id}");
            if (oldTask.Category != task.Category)
            {
                _cacheService.RemoveAsync("categories");
            }
            
            // Invalidar listas de tareas ya que el cambio puede afectar a cualquier lista
            InvalidateTaskListCache();
        }
        
        /// <summary>
        /// Updates only the status of a task.
        /// </summary>
        /// <param name="id">The ID of the task to update.</param>
        /// <param name="newStatus">The new status to set.</param>
        /// <returns>The updated task.</returns>
        public TodoTask UpdateTaskStatus(int id, TodoTaskStatus newStatus)
        {
            // Obtener la tarea directamente de la base de datos
            var task = _context.Tasks.Find(id);
            if (task == null)
            {
                return null;
            }
            
            // Guardar el estado anterior para el registro de auditoría
            var oldStatus = task.Status;
            
            // Actualizar solo el estado
            task.Status = newStatus;
            
            // Guardar los cambios
            _context.SaveChanges();
            
            // Crear registro de auditoría
            var oldTask = new TodoTask { Id = task.Id, Status = oldStatus };
            CreateAuditLog("UpdateStatus", task, oldTask);
            
            // Invalidar caché
            _cacheService.RemoveAsync($"task_{id}");
            InvalidateTaskListCache();
            
            return task;
        }

        /// <summary>
        /// Deletes a task by its ID.
        /// </summary>
        /// <param name="id">The ID of the task to delete.</param>
        public void DeleteTask(int id)
        {
            var task = _taskRepository.GetById(id);
            _taskRepository.Delete(id);
            CreateAuditLog("Delete", task);
            
            // Invalidar caché de la tarea y categorías
            _cacheService.RemoveAsync($"task_{id}");
            _cacheService.RemoveAsync("categories");
            
            // Invalidar listas de tareas
            InvalidateTaskListCache();
        }

        /// <summary>
        /// Retrieves a list of distinct categories from the tasks.
        /// </summary>
        /// <returns>A list of distinct categories.</returns>
        public IEnumerable<string> GetCategories()
        {
            var cacheKey = "categories";
            var cachedCategories = _cacheService.GetAsync<IEnumerable<string>>(cacheKey).Result;
            
            if (cachedCategories != null)
            {
                return cachedCategories;
            }
            
            var categories = _taskRepository.GetCategories();
            
            _cacheService.SetAsync(cacheKey, categories, TimeSpan.FromHours(1));
            
            return categories;
        }
    }
}
