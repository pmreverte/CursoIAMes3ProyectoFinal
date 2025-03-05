using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using FluentValidation;
using FluentValidation.Results;
using Sprint2.Interfaces;
using Sprint2.Models;
using Sprint2.Validators;

namespace Sprint2.Controllers
{
    /// <summary>
    /// Controller for managing tasks.
    /// </summary>
    [Authorize]
    [AutoValidateAntiforgeryToken]
    public class TasksController : Controller
    {
        private readonly ITaskService _taskService;
        private readonly ILogger<TasksController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IValidator<TodoTask> _taskValidator;
        private readonly ICacheService _cacheService;
        private const int DefaultPageSize = 10;

        /// <summary>
        /// Initializes a new instance of the <see cref="TasksController"/> class.
        /// </summary>
        /// <param name="taskService">The task service.</param>
        /// <param name="logger">The logger.</param>
        /// <param name="userManager">The user manager.</param>
        /// <param name="taskValidator">The FluentValidation validator for TodoTask.</param>
        /// <param name="cacheService">The cache service.</param>
        public TasksController(
            ITaskService taskService, 
            ILogger<TasksController> logger,
            UserManager<ApplicationUser> userManager,
            IValidator<TodoTask> taskValidator,
            ICacheService cacheService)
        {
            _taskService = taskService;
            _logger = logger;
            _userManager = userManager;
            _taskValidator = taskValidator;
            _cacheService = cacheService;
        }

        /// <summary>
        /// Populates the ViewBag with categories for tasks.
        /// </summary>
        private void PopulateCategories()
        {
            ViewBag.Categories = _taskService.GetCategories();
        }

        /// <summary>
        /// Displays a list of tasks with optional filtering and pagination.
        /// </summary>
        /// <param name="filter">The task filter criteria.</param>
        /// <param name="page">The page number for pagination.</param>
        /// <param name="forceReload">Flag to force a reload of the cache.</param>
        /// <returns>The task list view.</returns>
        public IActionResult Index(TaskFilter filter, int page = 1, string forceReload = null)
        {
            try
            {
                // Optimización: Reducir el tamaño de página para cargar más rápido
                int pageSize = 20; // Aumentamos el tamaño de página para mostrar más tareas de una vez
                
                // Si se fuerza la recarga, invalidar la caché
                bool shouldForceReload = !string.IsNullOrEmpty(forceReload) && 
                                        (forceReload.Equals("True", StringComparison.OrdinalIgnoreCase) || 
                                         forceReload.Equals("1"));
                
                if (shouldForceReload)
                {
                    _logger.LogInformation("Forzando recarga de la caché de tareas (forceReload={0})", forceReload);
                    _cacheService.RemoveAsync("tasklist_*");
                    _cacheService.RemoveAsync("categories");
                }
                
                // Cargar las tareas directamente en el servidor
                var viewModel = _taskService.GetTaskList(filter ?? new TaskFilter(), page, pageSize);
                
                // Cargar categorías para los filtros
                ViewBag.Categories = _taskService.GetCategories();
                
                // Indicar que las tareas NO deben cargarse de forma asíncrona en el cliente
                // ya que las estamos cargando directamente en el servidor
                ViewBag.LoadTasksAsync = false;
                
                // Registrar información sobre las tareas cargadas
                _logger.LogInformation("Cargando {Count} tareas directamente en el modelo para la vista", 
                    viewModel.Tasks.Count());
                
                if (viewModel.Tasks.Any())
                {
                    var firstTask = viewModel.Tasks.First();
                    _logger.LogInformation("Primera tarea en el modelo: ID={Id}, Descripción='{Description}', Estado={Status}", 
                        firstTask.Id, firstTask.Description, firstTask.Status);
                }
                
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la lista de tareas");
                TempData["Error"] = "Ha ocurrido un error al cargar las tareas.";
                return View(new TaskListViewModel());
            }
        }
        
        /// <summary>
        /// Endpoint para cargar tareas de forma asíncrona mediante AJAX
        /// </summary>
        /// <param name="filter">Filtro de tareas</param>
        /// <param name="page">Número de página</param>
        /// <returns>JSON con las tareas</returns>
        [HttpGet]
        public IActionResult GetTasksJson(TaskFilter filter, int page = 1)
        {
            try
            {
                _logger.LogInformation("Recibida solicitud GetTasksJson con filtro: {Filter}, página: {Page}", 
                    filter?.ToString() ?? "null", page);
                
                // Usar un tamaño de página mayor para la carga asíncrona
                int pageSize = 20;
                
                // Inicializar el filtro si es null
                filter = filter ?? new TaskFilter();
                
                // Obtener las tareas directamente (sin Task.Run que causa problemas con el contexto)
                var viewModel = _taskService.GetTaskList(filter, page, pageSize);
                
                // Registrar el número de tareas encontradas
                _logger.LogInformation("Se encontraron {Count} tareas", viewModel.Tasks.Count());
                
                // Verificar si hay tareas
                if (!viewModel.Tasks.Any())
                {
                    _logger.LogWarning("No se encontraron tareas que coincidan con los criterios de búsqueda");
                }
                else
                {
                    // Registrar información sobre la primera tarea para depuración
                    var firstTask = viewModel.Tasks.First();
                    _logger.LogInformation("Primera tarea: ID={Id}, Descripción='{Description}', Estado={Status}", 
                        firstTask.Id, firstTask.Description, firstTask.Status);
                }
                
                // Devolver un array simple de tareas para evitar problemas de estructura
                return Json(viewModel.Tasks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener las tareas en formato JSON");
                return Json(new { error = "Error al cargar las tareas: " + ex.Message });
            }
        }

        /// <summary>
        /// Displays the create task view.
        /// </summary>
        /// <returns>The create task view.</returns>
        public IActionResult Create()
        {
            PopulateCategories();
            return View();
        }

        /// <summary>
        /// Handles the creation of a new task.
        /// </summary>
        /// <param name="task">The task to create.</param>
        /// <returns>The task list view if successful; otherwise, the create view with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(TodoTask task)
        {
            try
            {
                // Perform FluentValidation
                ValidationResult validationResult = _taskValidator.Validate(task);
                
                if (validationResult.IsValid)
                {
                    SanitizeTaskProperties(task);
                    _taskService.CreateTask(task);
                    
                    // Invalidar la caché para asegurar que la nueva tarea aparezca en la lista
                    _cacheService.RemoveAsync("tasklist_*");
                    _cacheService.RemoveAsync("categories");
                    
                    // Registrar información sobre la tarea creada
                    _logger.LogInformation($"Tarea creada exitosamente: ID={task.Id}, Descripción='{task.Description}'");
                    
                    // Añadir mensaje de éxito
                    TempData["Success"] = "Tarea creada exitosamente.";
                    
                // Forzar una recarga para actualizar la lista de tareas
                // Esto es necesario para que la tarea eliminada desaparezca de la lista
                return RedirectToAction(nameof(Index), new { forceReload = "True" });
                }
                
                // Add validation errors to ModelState
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
                
                PopulateCategories();
                return View(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al crear la tarea");
                ModelState.AddModelError(string.Empty, "Ha ocurrido un error al crear la tarea.");
                PopulateCategories();
                return View(task);
            }
        }

        /// <summary>
        /// Displays the edit task view for a specific task.
        /// </summary>
        /// <param name="id">The ID of the task to edit.</param>
        /// <returns>The edit task view if the task is found; otherwise, a not found result.</returns>
        public IActionResult Edit(int id)
        {
            try
            {
                var task = _taskService.GetTaskById(id);
                if (task == null)
                {
                    return NotFound();
                }
                PopulateCategories();
                return View(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la tarea para editar");
                TempData["Error"] = "Ha ocurrido un error al cargar la tarea.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Handles the update of an existing task.
        /// </summary>
        /// <param name="task">The task to update.</param>
        /// <returns>The task list view if successful; otherwise, the edit view with errors.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(TodoTask task)
        {
            try
            {
                // Perform FluentValidation
                ValidationResult validationResult = _taskValidator.Validate(task);
                
                if (validationResult.IsValid)
                {
                    SanitizeTaskProperties(task);
                    _taskService.UpdateTask(task);
                    
                    // Registrar información sobre la tarea actualizada
                    _logger.LogInformation($"Tarea actualizada exitosamente: ID={task.Id}, Descripción='{task.Description}'");
                    
                    // Añadir mensaje de éxito
                    TempData["Success"] = "Tarea actualizada exitosamente.";
                    
                // Forzar una recarga para actualizar la lista de tareas
                // Esto es necesario para que la tarea eliminada desaparezca de la lista
                return RedirectToAction(nameof(Index), new { forceReload = "True" });
                }
                
                // Add validation errors to ModelState
                foreach (var error in validationResult.Errors)
                {
                    ModelState.AddModelError(error.PropertyName, error.ErrorMessage);
                }
                
                PopulateCategories();
                return View(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar la tarea");
                ModelState.AddModelError(string.Empty, "Ha ocurrido un error al actualizar la tarea.");
                PopulateCategories();
                return View(task);
            }
        }

        /// <summary>
        /// Displays the delete task confirmation view for a specific task.
        /// </summary>
        /// <param name="id">The ID of the task to delete.</param>
        /// <returns>The delete task view if the task is found; otherwise, a not found result.</returns>
        public IActionResult Delete(int id)
        {
            try
            {
                var task = _taskService.GetTaskById(id);
                if (task == null)
                {
                    return NotFound();
                }
                return View(task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al obtener la tarea para eliminar");
                TempData["Error"] = "Ha ocurrido un error al cargar la tarea.";
                return RedirectToAction(nameof(Index));
            }
        }

        /// <summary>
        /// Handles the deletion of a task.
        /// </summary>
        /// <param name="id">The ID of the task to delete.</param>
        /// <returns>The task list view if successful; otherwise, the task list view with errors.</returns>
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteConfirmed(int id)
        {
            try
            {
                _taskService.DeleteTask(id);
                
                // Registrar información sobre la tarea eliminada
                _logger.LogInformation($"Tarea {id} eliminada exitosamente");
                
                // Añadir mensaje de éxito
                TempData["Success"] = "Tarea eliminada exitosamente.";
                
                // Forzar una recarga para actualizar la lista de tareas
                // Esto es necesario para que la tarea eliminada desaparezca de la lista
                return RedirectToAction(nameof(Index), new { forceReload = "True" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al eliminar la tarea");
                TempData["Error"] = "Ha ocurrido un error al eliminar la tarea.";
                return RedirectToAction(nameof(Index));
            }
        }
        /// <summary>
        /// Sanitizes all string properties of a task to prevent XSS.
        /// </summary>
        /// <param name="task">The task to sanitize.</param>
        private void SanitizeTaskProperties(TodoTask task)
        {
            task.SanitizeAllProperties();
        }
        /// <summary>
        /// Updates the status of a task.
        /// </summary>
        /// <param name="id">The ID of the task to update.</param>
        /// <param name="newStatus">The new status of the task.</param>
        /// <returns>Redirects to the Index action with forceReload=True.</returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateStatus(int id, TodoTaskStatus newStatus)
        {
            try
            {
                _logger.LogInformation($"Actualizando tarea {id} a estado {newStatus}");
                
                // Obtener la tarea para validación y registro
                var task = _taskService.GetTaskById(id);
                if (task == null)
                {
                    _logger.LogWarning($"Tarea {id} no encontrada");
                    TempData["Error"] = "No se encontró la tarea.";
                    return RedirectToAction(nameof(Index));
                }

                _logger.LogInformation($"Estado actual: {task.Status}, Nuevo estado: {newStatus}, IsOverdue: {task.IsOverdue}");
                
                // Validar solo la transición de estado, no toda la tarea
                bool isValidTransition = true;
                
                // Verificar si la transición de estado es válida
                // Solo validamos que las tareas completadas no puedan volver a otro estado
                if (task.IsCompleted && newStatus != TodoTaskStatus.Completed)
                {
                    isValidTransition = false;
                    _logger.LogWarning($"Transición de estado inválida: No se puede cambiar una tarea completada a {newStatus}");
                    TempData["Error"] = "Error de validación: La transición de estado no es válida. Las tareas completadas no pueden volver a pendiente.";
                    return RedirectToAction(nameof(Index));
                }
                
                // Si la transición es válida, actualizar solo el estado
                if (isValidTransition)
                {
                    // Actualizar solo el estado usando el método del servicio
                    var updatedTask = _taskService.UpdateTaskStatus(id, newStatus);
                    if (updatedTask == null)
                    {
                        TempData["Error"] = "No se pudo actualizar la tarea.";
                        return RedirectToAction(nameof(Index));
                    }
                    
                    // Invalidar la caché para asegurar que la tarea actualizada aparezca en la lista
                    _cacheService.RemoveAsync($"task_{id}");
                    _cacheService.RemoveAsync("tasklist_*");
                    _cacheService.RemoveAsync("categories");
                    
                    _logger.LogInformation($"Tarea {id} actualizada exitosamente a estado {newStatus}");
                    
                    // Añadir mensaje de éxito
                    TempData["Success"] = $"Tarea actualizada a {(newStatus == TodoTaskStatus.Completed ? "Completada" : newStatus == TodoTaskStatus.InProgress ? "En Progreso" : "Pendiente")}.";
                    
                    // Forzar una recarga para actualizar la lista de tareas
                    // Esto es necesario para que la tarea actualizada aparezca en la columna correcta
                    return RedirectToAction(nameof(Index), new { forceReload = "True" });
                }
                
                TempData["Error"] = "Error de validación: Transición de estado no válida.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al actualizar el estado de la tarea {id} a {newStatus}");
                TempData["Error"] = "Error al actualizar el estado de la tarea.";
                return RedirectToAction(nameof(Index));
            }
        }
        
        /// <summary>
        /// Obtiene la vista parcial de una tarea para actualizar la UI
        /// </summary>
        /// <param name="id">ID de la tarea</param>
        /// <returns>Vista parcial de la tarea</returns>
        [HttpGet]
        public IActionResult GetTaskPartial(int id)
        {
            try
            {
                var task = _taskService.GetTaskById(id);
                if (task == null)
                {
                    return NotFound();
                }
                
                return PartialView("_TaskCard", task);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error al obtener la vista parcial de la tarea {id}");
                return StatusCode(500, "Error al obtener la tarea");
            }
        }
    }
}
