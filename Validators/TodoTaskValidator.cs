using FluentValidation;
using Sprint2.Models;
using System.Text.RegularExpressions;

namespace Sprint2.Validators
{
    /// <summary>
    /// Validator for the TodoTask model using FluentValidation.
    /// </summary>
    public class TodoTaskValidator : AbstractValidator<TodoTask>
    {
    public TodoTaskValidator()
    {
        // Description validation with enhanced rules
        RuleFor(task => task.Description)
            .NotEmpty().WithMessage("La descripción es obligatoria")
            .Length(3, 200).WithMessage("La descripción debe tener entre 3 y 200 caracteres")
            .Must(BeValidText).WithMessage("La descripción contiene caracteres no permitidos")
            .Must(ContainMeaningfulContent).WithMessage("La descripción debe contener información significativa")
            .Must(NotContainCommonPlaceholders).WithMessage("La descripción no debe contener texto genérico como 'tarea', 'pendiente' o 'TODO'");

        // Status validation with transition rules
        RuleFor(task => task.Status)
            .IsInEnum().WithMessage("El estado seleccionado no es válido")
            .Must((task, status) => BeValidStatusTransition(task))
            .WithMessage("La transición de estado no es válida. Las tareas completadas no pueden volver a pendiente");

        // Priority validation with contextual rules
        RuleFor(task => task.Priority)
            .IsInEnum().WithMessage("La prioridad seleccionada no es válida")
            .Must((task, priority) => BeValidPriorityForStatus(task))
            .WithMessage("No se puede cambiar la prioridad de una tarea completada");

        // DueDate validation with enhanced rules
        RuleFor(task => task.DueDate)
            .Must(BeAFutureDate).When(task => task.DueDate.HasValue)
            .WithMessage("La fecha de vencimiento debe ser futura")
            .Must((task, dueDate) => BeValidDueDateForStatus(task))
            .When(task => task.DueDate.HasValue)
            .WithMessage("No se puede establecer una fecha de vencimiento para una tarea completada");

        // Category validation with enhanced rules
        RuleFor(task => task.Category)
            .MaximumLength(50).WithMessage("La categoría no puede exceder los 50 caracteres")
            .Must(BeValidText).When(task => !string.IsNullOrEmpty(task.Category))
            .WithMessage("La categoría contiene caracteres no permitidos")
            .Must(BeValidCategory).When(task => !string.IsNullOrEmpty(task.Category))
            .WithMessage("La categoría debe ser una palabra o frase corta sin caracteres especiales");

        // Notes validation with enhanced rules
        RuleFor(task => task.Notes)
            .MaximumLength(500).WithMessage("Las notas no pueden exceder los 500 caracteres")
            .Must(BeValidText).When(task => !string.IsNullOrEmpty(task.Notes))
            .WithMessage("Las notas contienen caracteres no permitidos")
            .Must(ContainMeaningfulContent).When(task => !string.IsNullOrEmpty(task.Notes))
            .WithMessage("Las notas deben contener información significativa");

        // Complex validation rules
        RuleFor(task => task)
            .Must(HaveReasonableCompletionDate)
            .WithMessage("Las tareas urgentes deben tener una fecha de vencimiento dentro de los próximos 3 días")
            .When(task => task.Priority == Priority.Urgent && task.DueDate.HasValue);

        RuleFor(task => task)
            .Must(HaveNotesForHighPriority)
            .WithMessage("Las tareas de alta prioridad o urgentes deberían incluir notas explicativas")
            .When(task => task.Priority == Priority.High || task.Priority == Priority.Urgent);

        RuleFor(task => task)
            .Must(HaveValidStatusForDueDate)
            .WithMessage("Las tareas vencidas deben marcarse como completadas o reprogramarse")
            .When(task => task.DueDate.HasValue && task.DueDate.Value.Date < DateTime.UtcNow.Date);
        }

        /// <summary>
        /// Validates that the text does not contain potentially dangerous HTML characters.
        /// </summary>
        private bool BeValidText(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            return !Regex.IsMatch(text, @"<|>");
        }

        /// <summary>
        /// Validates that the content is meaningful and not just whitespace or common filler text.
        /// </summary>
        private bool ContainMeaningfulContent(string? text)
        {
            if (string.IsNullOrEmpty(text))
                return true;

            var trimmedText = text.Trim();
            return trimmedText.Length >= 3 && !trimmedText.All(c => char.IsPunctuation(c) || char.IsWhiteSpace(c));
        }

        /// <summary>
        /// Validates that the text doesn't contain common placeholder content.
        /// </summary>
        private bool NotContainCommonPlaceholders(string text)
        {
            var commonPlaceholders = new[] { "tarea", "pendiente", "todo", "test", "prueba" };
            return !commonPlaceholders.Any(p => text.ToLower().Equals(p));
        }

        /// <summary>
        /// Validates that the status transition is valid.
        /// </summary>
        private bool BeValidStatusTransition(TodoTask task)
        {
            // Solo validamos que las tareas completadas no puedan volver a otro estado
            // Las tareas en progreso pueden volver a pendiente sin problema
            if (task.IsCompleted && task.Status != TodoTaskStatus.Completed)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that the priority change is valid for the current status.
        /// </summary>
        private bool BeValidPriorityForStatus(TodoTask task)
        {
            // Prevent priority changes for completed tasks
            return !task.IsCompleted;
        }

        /// <summary>
        /// Validates that the due date is valid for the current status.
        /// </summary>
        private bool BeValidDueDateForStatus(TodoTask task)
        {
            // Prevent setting due dates for completed tasks
            return !task.IsCompleted;
        }

        /// <summary>
        /// Validates that the category format is valid.
        /// </summary>
        private bool BeValidCategory(string category)
        {
            return Regex.IsMatch(category, @"^[\w\s-]{1,50}$");
        }

        /// <summary>
        /// Validates that a date is in the future.
        /// </summary>
        private bool BeAFutureDate(DateTime? date)
        {
            return !date.HasValue || date.Value.Date >= DateTime.UtcNow.Date;
        }

        /// <summary>
        /// Validates that urgent tasks have a due date within the next 3 days.
        /// </summary>
        private bool HaveReasonableCompletionDate(TodoTask task)
        {
            if (task.Priority == Priority.Urgent && task.DueDate.HasValue)
            {
                return (task.DueDate.Value.Date - DateTime.UtcNow.Date).TotalDays <= 3;
            }
            return true;
        }

        /// <summary>
        /// Validates that high priority tasks have explanatory notes.
        /// </summary>
        private bool HaveNotesForHighPriority(TodoTask task)
        {
            if ((task.Priority == Priority.High || task.Priority == Priority.Urgent) && 
                string.IsNullOrWhiteSpace(task.Notes))
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// Validates that overdue tasks are properly handled.
        /// Esta regla se ha modificado para permitir cambios de estado en tareas vencidas.
        /// </summary>
        private bool HaveValidStatusForDueDate(TodoTask task)
        {
            // Permitimos cualquier estado para tareas vencidas
            return true;
        }
    }
}
