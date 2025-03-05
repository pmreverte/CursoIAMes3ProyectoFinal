using System;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
// Note: We keep the Display attributes for UI purposes but remove validation attributes

namespace Sprint2.Models
{
    /// <summary>
    /// Represents a task in the to-do list with various properties and validation.
    /// </summary>
    public class TodoTask
    {
        /// <summary>
        /// Gets or sets the unique identifier for the task.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Gets or sets the description of the task.
        /// </summary>
        [Display(Name = "Descripción")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the status of the task.
        /// </summary>
        [Display(Name = "Estado")]
        public TodoTaskStatus Status { get; set; } = TodoTaskStatus.Pending;

        /// <summary>
        /// Gets or sets the creation date of the task.
        /// </summary>
        [Display(Name = "Fecha de Creación")]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Gets or sets the due date of the task.
        /// </summary>
        [Display(Name = "Fecha de Vencimiento")]
        [DataType(DataType.Date)]
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Gets or sets the priority of the task.
        /// </summary>
        [Display(Name = "Prioridad")]
        public Priority Priority { get; set; } = Priority.Medium;

        /// <summary>
        /// Gets or sets the category of the task.
        /// </summary>
        [Display(Name = "Categoría")]
        public string? Category { get; set; }

        /// <summary>
        /// Gets or sets the notes for the task.
        /// </summary>
        [Display(Name = "Notas")]
        public string? Notes { get; set; }

        /// <summary>
        /// Gets a value indicating whether the task is completed.
        /// </summary>
        [Display(Name = "Completada")]
        public bool IsCompleted => Status == TodoTaskStatus.Completed;

        /// <summary>
        /// Gets a value indicating whether the task is overdue.
        /// </summary>
        [Display(Name = "Vencida")]
        public bool IsOverdue => DueDate.HasValue && DueDate.Value.Date < DateTime.UtcNow.Date;

        /// <summary>
        /// Initializes a new instance of the <see cref="TodoTask"/> class.
        /// </summary>
        public TodoTask() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="TodoTask"/> class with a description.
        /// </summary>
        /// <param name="description">The description of the task.</param>
        public TodoTask(string description)
        {
            Description = SanitizeInput(description);
        }

        /// <summary>
        /// Sanitizes all string properties to prevent XSS.
        /// </summary>
        public void SanitizeAllProperties()
        {
            Description = SanitizeInput(Description);
            
            if (!string.IsNullOrEmpty(Category))
                Category = SanitizeInput(Category);
            
            if (!string.IsNullOrEmpty(Notes))
                Notes = SanitizeInput(Notes);
        }

        /// <summary>
        /// Sanitizes input to prevent XSS by removing dangerous HTML and scripts.
        /// </summary>
        /// <param name="input">The input string to sanitize.</param>
        /// <returns>The sanitized string.</returns>
        private string SanitizeInput(string input)
        {
            if (string.IsNullOrEmpty(input))
                return string.Empty;

            // Remove potentially dangerous HTML tags
            var sanitized = Regex.Replace(input, @"<[^>]*>", string.Empty);
            
            // Remove potentially dangerous scripts
            sanitized = Regex.Replace(sanitized, @"javascript:", "blocked:", RegexOptions.IgnoreCase);
            sanitized = Regex.Replace(sanitized, @"on\w+\s*=", "blocked=", RegexOptions.IgnoreCase);
            
            // Remove potentially dangerous URLs
            sanitized = Regex.Replace(sanitized, @"data:text/html", "blocked:", RegexOptions.IgnoreCase);
            
            // Encode special characters
            sanitized = System.Web.HttpUtility.HtmlEncode(sanitized);
            
            return sanitized;
        }
        
        /// <summary>
        /// Returns a string representation of the task for logging purposes.
        /// </summary>
        /// <returns>A string representation of the task.</returns>
        public override string ToString()
        {
            return $"Task[Id={Id}, Description='{Description}', Status={Status}, Priority={Priority}, " +
                   $"Category='{Category ?? "None"}', DueDate={DueDate?.ToString("yyyy-MM-dd") ?? "None"}, " +
                   $"IsOverdue={IsOverdue}, IsCompleted={IsCompleted}]";
        }
    }

    /// <summary>
    /// Represents the status of a to-do task.
    /// </summary>
    public enum TodoTaskStatus
    {
        [Display(Name = "Pendiente")]
        Pending,
        [Display(Name = "En Progreso")]
        InProgress,
        [Display(Name = "Completada")]
        Completed
    }

    /// <summary>
    /// Represents the priority level of a to-do task.
    /// </summary>
    public enum Priority
    {
        [Display(Name = "Baja")]
        Low,
        [Display(Name = "Media")]
        Medium,
        [Display(Name = "Alta")]
        High,
        [Display(Name = "Urgente")]
        Urgent
    }

}
