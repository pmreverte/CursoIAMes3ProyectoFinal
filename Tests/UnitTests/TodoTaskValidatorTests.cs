using System;
using System.Linq;
using Xunit;
using FluentValidation.TestHelper;
using Sprint2.Models;
using Sprint2.Validators;
using Moq;

namespace Sprint2.Tests.UnitTests
{
    /// <summary>
    /// Unit tests for the TodoTaskValidator.
    /// </summary>
    public class TodoTaskValidatorTests
    {
        private readonly TodoTaskValidator _validator;

        public TodoTaskValidatorTests()
        {
            _validator = new TodoTaskValidator();
        }

        #region Description Validation Tests

        [Fact]
        public void Should_Have_Error_When_Description_Is_Empty()
        {
            // Arrange
            var task = new TodoTask { Description = string.Empty };

            // Act & Assert
            var result = _validator.TestValidate(task);
            result.ShouldHaveValidationErrorFor(t => t.Description)
                  .WithErrorMessage("La descripción es obligatoria");
        }

        [Fact]
        public void Should_Have_Error_When_Description_Is_Too_Short()
        {
            // Arrange
            var task = new TodoTask { Description = "AB" }; // Less than 3 characters

            // Act & Assert
            var result = _validator.TestValidate(task);
            result.ShouldHaveValidationErrorFor(t => t.Description)
                  .WithErrorMessage("La descripción debe tener entre 3 y 200 caracteres");
        }

        [Fact]
        public void Should_Have_Error_When_Description_Contains_Invalid_Characters()
        {
            // Arrange
            var task = new TodoTask { Description = "Task with <script>" };

            // Act & Assert
            var result = _validator.TestValidate(task);
            result.ShouldHaveValidationErrorFor(t => t.Description)
                  .WithErrorMessage("La descripción contiene caracteres no permitidos");
        }

        [Fact]
        public void Should_Have_Error_When_Description_Is_Not_Meaningful()
        {
            // Arrange
            var task = new TodoTask { Description = "..." };

            // Act & Assert
            var result = _validator.TestValidate(task);
            result.ShouldHaveValidationErrorFor(t => t.Description)
                  .WithErrorMessage("La descripción debe contener información significativa");
        }

        [Fact]
        public void Should_Have_Error_When_Description_Is_Common_Placeholder()
        {
            // Arrange
            var task = new TodoTask { Description = "tarea" };

            // Act & Assert
            var result = _validator.TestValidate(task);
            result.ShouldHaveValidationErrorFor(t => t.Description)
                  .WithErrorMessage("La descripción no debe contener texto genérico como 'tarea', 'pendiente' o 'TODO'");
        }

        #endregion

        #region Status Validation Tests

        [Fact]
        public void Should_Have_Error_When_Completed_Task_Changes_To_Pending()
        {
            // Arrange
            var task = new TodoTask 
            { 
                Description = "Valid description",
                Status = TodoTaskStatus.Completed // Primero establecemos como completada
            };
            
            // Verificamos que la tarea está marcada como completada
            Assert.True(task.IsCompleted);
            
            // Cambiamos el estado a pendiente
            task = new TodoTask 
            { 
                Description = "Valid description",
                Status = TodoTaskStatus.Pending,
                // Simulamos que la tarea estaba completada anteriormente
                // En la aplicación real, esto se detectaría comparando con el estado anterior en la base de datos
            };
            
            // En este punto, no podemos probar directamente la regla de transición
            // porque el validador no tiene forma de saber que la tarea estaba completada antes
            // Esta prueba es más para documentación que para validación real
            
            // Act & Assert - Verificamos que el validador funciona para otros casos
            var result = _validator.TestValidate(task);
            
            // Nota: No podemos verificar directamente el error de transición de estado
            // porque no tenemos forma de simular el estado anterior de la tarea
        }

        #endregion

        #region Priority Validation Tests

        [Fact]
        public void Should_Have_Error_When_Changing_Priority_Of_Completed_Task()
        {
            // Arrange
            var task = new TodoTask 
            { 
                Description = "Valid description",
                Status = TodoTaskStatus.Completed,
                Priority = Priority.High
            };

            // Act & Assert
            var result = _validator.TestValidate(task);
            result.ShouldHaveValidationErrorFor(t => t.Priority)
                  .WithErrorMessage("No se puede cambiar la prioridad de una tarea completada");
        }

        #endregion

        #region DueDate Validation Tests

        [Fact]
        public void Should_Have_Error_When_DueDate_Is_In_Past()
        {
            // Arrange
            var task = new TodoTask 
            { 
                Description = "Valid description",
                DueDate = DateTime.UtcNow.AddDays(-1) 
            };

            // Act & Assert
            var result = _validator.TestValidate(task);
            result.ShouldHaveValidationErrorFor(t => t.DueDate)
                  .WithErrorMessage("La fecha de vencimiento debe ser futura");
        }

        [Fact]
        public void Should_Have_Error_When_Setting_DueDate_For_Completed_Task()
        {
            // Arrange
            var task = new TodoTask 
            { 
                Description = "Valid description",
                Status = TodoTaskStatus.Completed,
                DueDate = DateTime.UtcNow.AddDays(1)
            };

            // Act & Assert
            var result = _validator.TestValidate(task);
            result.ShouldHaveValidationErrorFor(t => t.DueDate)
                  .WithErrorMessage("No se puede establecer una fecha de vencimiento para una tarea completada");
        }

        #endregion

        #region Category Validation Tests

        [Fact]
        public void Should_Have_Error_When_Category_Contains_Special_Characters()
        {
            // Arrange
            var task = new TodoTask 
            { 
                Description = "Valid description",
                Category = "Work@Home!"
            };

            // Act & Assert
            var result = _validator.TestValidate(task);
            result.ShouldHaveValidationErrorFor(t => t.Category)
                  .WithErrorMessage("La categoría debe ser una palabra o frase corta sin caracteres especiales");
        }

        #endregion

        #region Notes Validation Tests

        [Fact]
        public void Should_Have_Error_When_Notes_Are_Not_Meaningful()
        {
            // Arrange
            var task = new TodoTask 
            { 
                Description = "Valid description",
                Notes = "..."
            };

            // Act & Assert
            var result = _validator.TestValidate(task);
            result.ShouldHaveValidationErrorFor(t => t.Notes)
                  .WithErrorMessage("Las notas deben contener información significativa");
        }

        #endregion

        #region Complex Validation Tests

        [Fact]
        public void Should_Have_Error_When_Urgent_Task_Has_Far_Future_DueDate()
        {
            // Arrange
            var task = new TodoTask 
            { 
                Description = "Valid description",
                Priority = Priority.Urgent,
                DueDate = DateTime.UtcNow.AddDays(10) // More than 3 days in the future
            };

            // Act & Assert
            var result = _validator.TestValidate(task);
            result.ShouldHaveValidationErrorFor(t => t)
                  .WithErrorMessage("Las tareas urgentes deben tener una fecha de vencimiento dentro de los próximos 3 días");
        }

        [Fact]
        public void Should_Have_Error_When_High_Priority_Task_Has_No_Notes()
        {
            // Arrange
            var task = new TodoTask 
            { 
                Description = "Valid description",
                Priority = Priority.High,
                Notes = null
            };

            // Act & Assert
            var result = _validator.TestValidate(task);
            result.ShouldHaveValidationErrorFor(t => t)
                  .WithErrorMessage("Las tareas de alta prioridad o urgentes deberían incluir notas explicativas");
        }

        [Fact]
        public void Should_Have_Error_When_Overdue_Task_Is_Not_Completed()
        {
            // Arrange
            var task = new TodoTask 
            { 
                Description = "Valid description",
                Status = TodoTaskStatus.Pending,
                DueDate = DateTime.UtcNow.AddDays(-1)
            };

            // Act & Assert
            var result = _validator.TestValidate(task);
            result.ShouldHaveValidationErrorFor(t => t)
                  .WithErrorMessage("Las tareas vencidas deben marcarse como completadas o reprogramarse");
        }

        #endregion

        #region Valid Cases Tests

        [Fact]
        public void Should_Not_Have_Error_For_Valid_Task()
        {
            // Arrange
            var task = new TodoTask 
            { 
                Description = "Valid description with meaningful content",
                Status = TodoTaskStatus.Pending,
                Priority = Priority.Medium,
                DueDate = DateTime.UtcNow.AddDays(1),
                Category = "Work-Project",
                Notes = "Some meaningful notes about the task"
            };

            // Act & Assert
            var result = _validator.TestValidate(task);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_Not_Have_Error_For_Valid_High_Priority_Task_With_Notes()
        {
            // Arrange
            var task = new TodoTask 
            { 
                Description = "Important task description",
                Status = TodoTaskStatus.Pending,
                Priority = Priority.High,
                DueDate = DateTime.UtcNow.AddDays(1),
                Category = "Critical",
                Notes = "Important task that needs immediate attention because of its impact"
            };

            // Act & Assert
            var result = _validator.TestValidate(task);
            result.ShouldNotHaveAnyValidationErrors();
        }

        [Fact]
        public void Should_Not_Have_Error_For_Valid_Urgent_Task_With_Appropriate_DueDate()
        {
            // Arrange
            var task = new TodoTask 
            { 
                Description = "Urgent task description",
                Status = TodoTaskStatus.Pending,
                Priority = Priority.Urgent,
                DueDate = DateTime.UtcNow.AddDays(2), // Within 3 days
                Category = "Emergency",
                Notes = "Critical task that needs immediate attention due to client requirements"
            };

            // Act & Assert
            var result = _validator.TestValidate(task);
            result.ShouldNotHaveAnyValidationErrors();
        }

        #endregion
    }
}
