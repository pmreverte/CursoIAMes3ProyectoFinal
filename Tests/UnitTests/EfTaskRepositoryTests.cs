using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Sprint2.Data;
using Sprint2.Models;
using Sprint2.Repositories;
using Xunit;

namespace Sprint2.Tests.UnitTests
{
/// <summary>
/// Unit tests for the EfTaskRepository, verifying data operations on tasks
/// using an in-memory database.
/// </summary>
public class EfTaskRepositoryTests
    {
/// <summary>
/// Creates a new in-memory database context for testing.
/// </summary>
/// <returns>A new instance of ApplicationDbContext.</returns>
private ApplicationDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            var context = new ApplicationDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

/// <summary>
/// Seeds the in-memory database with test data.
/// </summary>
/// <param name="context">The database context to seed.</param>
private void SeedDatabase(ApplicationDbContext context)
        {
            context.Tasks.AddRange(
                new TodoTask
                {
                    Description = "Task 1",
                    Status = TodoTaskStatus.Pending,
                    Priority = Priority.High,
                    Category = "Work",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    DueDate = DateTime.UtcNow.AddDays(1)
                },
                new TodoTask
                {
                    Description = "Task 2",
                    Status = TodoTaskStatus.InProgress,
                    Priority = Priority.Medium,
                    Category = "Personal",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    DueDate = DateTime.UtcNow.AddDays(2)
                },
                new TodoTask
                {
                    Description = "Task 3",
                    Status = TodoTaskStatus.Completed,
                    Priority = Priority.Low,
                    Category = "Work",
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    DueDate = DateTime.UtcNow.AddDays(-1)
                }
            );
            context.SaveChanges();
        }

/// <summary>
/// Tests that GetAll returns all tasks when no filter is applied.
/// </summary>
[Fact]
        public void GetAll_ReturnsAllTasks_WhenNoFilterApplied()
        {
            // Arrange
            using var context = CreateContext();
            SeedDatabase(context);
            var repository = new EfTaskRepository(context);
            var filter = new TaskFilter();

            // Act
            var (tasks, totalCount) = repository.GetAll(filter, 1, 10);

            // Assert
            Assert.Equal(3, totalCount);
            Assert.Equal(3, tasks.Count());
        }

/// <summary>
/// Tests that GetAll filters tasks by status.
/// </summary>
[Fact]
        public void GetAll_FiltersTasksByStatus()
        {
            // Arrange
            using var context = CreateContext();
            SeedDatabase(context);
            var repository = new EfTaskRepository(context);
            var filter = new TaskFilter { Status = TodoTaskStatus.InProgress };

            // Act
            var (tasks, totalCount) = repository.GetAll(filter, 1, 10);

            // Assert
            Assert.Equal(1, totalCount);
            Assert.Equal(TodoTaskStatus.InProgress, tasks.First().Status);
        }

/// <summary>
/// Tests that GetAll filters tasks by priority.
/// </summary>
[Fact]
        public void GetAll_FiltersTasksByPriority()
        {
            // Arrange
            using var context = CreateContext();
            SeedDatabase(context);
            var repository = new EfTaskRepository(context);
            var filter = new TaskFilter { Priority = Priority.High };

            // Act
            var (tasks, totalCount) = repository.GetAll(filter, 1, 10);

            // Assert
            Assert.Equal(1, totalCount);
            Assert.Equal(Priority.High, tasks.First().Priority);
        }

/// <summary>
/// Tests that GetAll filters tasks by category.
/// </summary>
[Fact]
        public void GetAll_FiltersTasksByCategory()
        {
            // Arrange
            using var context = CreateContext();
            SeedDatabase(context);
            var repository = new EfTaskRepository(context);
            var filter = new TaskFilter { Category = "Work" };

            // Act
            var (tasks, totalCount) = repository.GetAll(filter, 1, 10);

            // Assert
            Assert.Equal(2, totalCount);
            Assert.All(tasks, task => Assert.Equal("Work", task.Category));
        }

/// <summary>
/// Tests that GetAll filters tasks by search term.
/// </summary>
[Fact]
        public void GetAll_FiltersTasksBySearchTerm()
        {
            // Arrange
            using var context = CreateContext();
            SeedDatabase(context);
            var repository = new EfTaskRepository(context);
            var filter = new TaskFilter { SearchTerm = "Task 1" };

            // Act
            var (tasks, totalCount) = repository.GetAll(filter, 1, 10);

            // Assert
            Assert.Equal(1, totalCount);
            Assert.Contains(tasks, task => task.Description == "Task 1");
        }

/// <summary>
/// Tests that GetAll paginates results.
/// </summary>
[Fact]
        public void GetAll_PaginatesResults()
        {
            // Arrange
            using var context = CreateContext();
            SeedDatabase(context);
            var repository = new EfTaskRepository(context);
            var filter = new TaskFilter();

            // Act
            var (tasks, totalCount) = repository.GetAll(filter, 1, 2);

            // Assert
            Assert.Equal(3, totalCount); // Total count should still be 3
            Assert.Equal(2, tasks.Count()); // But only 2 tasks returned due to pagination
        }

/// <summary>
/// Tests that GetById returns the correct task.
/// </summary>
[Fact]
        public void GetById_ReturnsCorrectTask()
        {
            // Arrange
            using var context = CreateContext();
            SeedDatabase(context);
            var repository = new EfTaskRepository(context);
            var taskId = context.Tasks.First().Id;

            // Act
            var task = repository.GetById(taskId);

            // Assert
            Assert.NotNull(task);
            Assert.Equal(taskId, task.Id);
        }

/// <summary>
/// Tests that Add inserts a new task.
/// </summary>
[Fact]
        public void Add_InsertsNewTask()
        {
            // Arrange
            using var context = CreateContext();
            var repository = new EfTaskRepository(context);
            var newTask = new TodoTask { Description = "New Task", Status = TodoTaskStatus.Pending };

            // Act
            repository.Add(newTask);

            // Assert
            Assert.Equal(1, context.Tasks.Count());
            Assert.Equal("New Task", context.Tasks.First().Description);
            Assert.NotEqual(0, newTask.Id); // ID should be assigned
        }

/// <summary>
/// Tests that Update modifies an existing task.
/// </summary>
[Fact]
        public void Update_ModifiesExistingTask()
        {
            // Arrange
            using var context = CreateContext();
            SeedDatabase(context);
            var repository = new EfTaskRepository(context);
            var task = context.Tasks.First();
            task.Description = "Updated Description";
            task.Status = TodoTaskStatus.Completed;

            // Act
            repository.Update(task);

            // Assert
            var updatedTask = context.Tasks.Find(task.Id);
            Assert.Equal("Updated Description", updatedTask.Description);
            Assert.Equal(TodoTaskStatus.Completed, updatedTask.Status);
        }

/// <summary>
/// Tests that Delete removes a task.
/// </summary>
[Fact]
        public void Delete_RemovesTask()
        {
            // Arrange
            using var context = CreateContext();
            SeedDatabase(context);
            var repository = new EfTaskRepository(context);
            var taskId = context.Tasks.First().Id;
            var initialCount = context.Tasks.Count();

            // Act
            repository.Delete(taskId);

            // Assert
            Assert.Equal(initialCount - 1, context.Tasks.Count());
            Assert.Null(context.Tasks.Find(taskId));
        }

/// <summary>
/// Tests that GetCategories returns distinct categories.
/// </summary>
[Fact]
        public void GetCategories_ReturnsDistinctCategories()
        {
            // Arrange
            using var context = CreateContext();
            SeedDatabase(context);
            var repository = new EfTaskRepository(context);

            // Act
            var categories = repository.GetCategories().ToList();

            // Assert
            Assert.Equal(2, categories.Count);
            Assert.Contains("Work", categories);
            Assert.Contains("Personal", categories);
        }
    }
}
