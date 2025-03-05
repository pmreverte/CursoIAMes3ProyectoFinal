using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Moq;
using Sprint2.Data;
using Sprint2.Interfaces;
using Sprint2.Models;
using Sprint2.Services;
using Xunit;

namespace Sprint2.Tests.UnitTests
{
    /// <summary>
    /// Unit tests for the TaskService, verifying service operations on tasks.
    /// </summary>
    public class TaskServiceTests
    {
        private readonly Mock<ITaskRepository> _mockRepository;
        private readonly Mock<ApplicationDbContext> _mockContext;
        private readonly Mock<IHttpContextAccessor> _mockHttpContextAccessor;
        private readonly Mock<ICacheService> _mockCacheService;
        private readonly TaskService _service;

        /// <summary>
        /// Initializes a new instance of the TaskServiceTests class.
        /// Sets up the mock repository and task service for testing.
        /// </summary>
        public TaskServiceTests()
        {
            _mockRepository = new Mock<ITaskRepository>();
            
            // Setup mock DbContext and DbSet
            var mockSet = new Mock<DbSet<AuditLog>>();
            _mockContext = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
            _mockContext.Setup(c => c.AuditLogs).Returns(mockSet.Object);
            
            _mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            // Setup HttpContext with a mock user
            var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, "test-user-id"),
                new Claim(ClaimTypes.Name, "test-user@example.com")
            }));

            var mockHttpContext = new Mock<HttpContext>();
            mockHttpContext.Setup(x => x.User).Returns(user);
            _mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(mockHttpContext.Object);

            // Setup mock cache service
            _mockCacheService = new Mock<ICacheService>();
            _mockCacheService.Setup(c => c.GetAsync<TodoTask>(It.IsAny<string>()))
                .Returns(Task.FromResult<TodoTask>(null));
            _mockCacheService.Setup(c => c.GetAsync<IEnumerable<string>>(It.IsAny<string>()))
                .Returns(Task.FromResult<IEnumerable<string>>(null));
            _mockCacheService.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<object>(), It.IsAny<TimeSpan?>()))
                .Returns(Task.CompletedTask);
            _mockCacheService.Setup(c => c.RemoveAsync(It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _service = new TaskService(
                _mockRepository.Object,
                _mockContext.Object,
                _mockHttpContextAccessor.Object,
                _mockCacheService.Object
            );
        }

        /// <summary>
        /// Tests that GetTaskById handles exceptions from the repository.
        /// </summary>
        [Fact]
        public void GetTaskById_HandlesRepositoryException()
        {
            // Arrange
            int taskId = 1;
            _mockRepository.Setup(repo => repo.GetById(taskId))
                .Throws(new Exception("Repository exception"));

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => _service.GetTaskById(taskId));
            Assert.Equal("Repository exception", exception.Message);
        }

        /// <summary>
        /// Tests that CreateTask handles exceptions from the repository.
        /// </summary>
        [Fact]
        public void CreateTask_HandlesRepositoryException()
        {
            // Arrange
            var task = new TodoTask { Description = "New Task" };
            _mockRepository.Setup(repo => repo.Add(task))
                .Throws(new Exception("Repository exception"));

            // Act & Assert
            var exception = Assert.Throws<Exception>(() => _service.CreateTask(task));
            Assert.Equal("Repository exception", exception.Message);
        }
        /// <summary>
        /// Tests that CreateTask throws an exception for invalid task data.
        /// </summary>
        [Fact]
        public void CreateTask_ThrowsException_ForInvalidData()
        {
            // Arrange
            var invalidTask = new TodoTask { Description = "" }; // Invalid because description is empty

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.CreateTask(invalidTask));
        }

        /// <summary>
        /// Tests that UpdateTask throws an exception for invalid task data.
        /// </summary>
        [Fact]
        public void UpdateTask_ThrowsException_ForInvalidData()
        {
            // Arrange
            var invalidTask = new TodoTask { Id = 1, Description = "" }; // Invalid because description is empty

            // Act & Assert
            Assert.Throws<ArgumentException>(() => _service.UpdateTask(invalidTask));
        }

        /// <summary>
        /// Tests that GetTaskList returns the correct view model with tasks and pagination.
        /// </summary>
        [Fact]
        public void GetTaskList_ReturnsCorrectViewModel()
        {
            // Arrange
            var filter = new TaskFilter();
            int page = 1;
            int pageSize = 10;
            var tasks = new List<TodoTask>
            {
                new TodoTask { Id = 1, Description = "Test Task 1" },
                new TodoTask { Id = 2, Description = "Test Task 2" }
            };
            int totalCount = 2;

            _mockRepository.Setup(repo => repo.GetAll(filter, page, pageSize))
                .Returns((tasks, totalCount));

            // Act
            var result = _service.GetTaskList(filter, page, pageSize);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(filter, result.Filter);
            Assert.Equal(page, result.Pagination.CurrentPage);
            Assert.Equal(pageSize, result.Pagination.PageSize);
            Assert.Equal(totalCount, result.Pagination.TotalItems);
            Assert.Equal(tasks.Count, result.Tasks.Count());
        }

        /// <summary>
        /// Tests that GetTaskById returns the correct task based on the provided ID.
        /// </summary>
        [Fact]
        public void GetTaskById_ReturnsCorrectTask()
        {
            // Arrange
            int taskId = 1;
            var expectedTask = new TodoTask { Id = taskId, Description = "Test Task" };
            
            _mockRepository.Setup(repo => repo.GetById(taskId))
                .Returns(expectedTask);

            // Act
            var result = _service.GetTaskById(taskId);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedTask.Id, result.Id);
            Assert.Equal(expectedTask.Description, result.Description);
            
            // Verify cache was checked and then set
            _mockCacheService.Verify(c => c.GetAsync<TodoTask>($"task_{taskId}"), Times.Once);
            _mockCacheService.Verify(c => c.SetAsync($"task_{taskId}", expectedTask, It.IsAny<TimeSpan>()), Times.Once);
        }

        /// <summary>
        /// Tests that CreateTask calls the repository's Add method.
        /// </summary>
        [Fact]
        public void CreateTask_CallsRepositoryAdd()
        {
            // Arrange
            var task = new TodoTask { Description = "New Task" };

            // Act
            _service.CreateTask(task);

            // Assert
            _mockRepository.Verify(repo => repo.Add(task), Times.Once);
            _mockCacheService.Verify(c => c.RemoveAsync("categories"), Times.Once);
        }

        /// <summary>
        /// Tests that UpdateTask calls the repository's Update method.
        /// </summary>
        [Fact]
        public void UpdateTask_CallsRepositoryUpdate()
        {
            // Arrange
            var task = new TodoTask { Id = 1, Description = "Updated Task", Category = "New Category" };
            var oldTask = new TodoTask { Id = 1, Description = "Old Task", Category = "Old Category" };
            
            _mockRepository.Setup(repo => repo.GetById(task.Id)).Returns(oldTask);

            // Act
            _service.UpdateTask(task);

            // Assert
            _mockRepository.Verify(repo => repo.Update(task), Times.Once);
            _mockCacheService.Verify(c => c.RemoveAsync($"task_{task.Id}"), Times.Once);
            _mockCacheService.Verify(c => c.RemoveAsync("categories"), Times.Once);
        }

        /// <summary>
        /// Tests that DeleteTask calls the repository's Delete method.
        /// </summary>
        [Fact]
        public void DeleteTask_CallsRepositoryDelete()
        {
            // Arrange
            int taskId = 1;
            var task = new TodoTask { Id = taskId, Description = "Task to Delete" };
            
            _mockRepository.Setup(repo => repo.GetById(taskId)).Returns(task);

            // Act
            _service.DeleteTask(taskId);

            // Assert
            _mockRepository.Verify(repo => repo.Delete(taskId), Times.Once);
            _mockCacheService.Verify(c => c.RemoveAsync($"task_{taskId}"), Times.Once);
            _mockCacheService.Verify(c => c.RemoveAsync("categories"), Times.Once);
        }

        /// <summary>
        /// Tests that GetCategories returns the categories from the repository.
        /// </summary>
        [Fact]
        public void GetCategories_ReturnsRepositoryCategories()
        {
            // Arrange
            var categories = new List<string> { "Work", "Personal", "Health" };
            
            _mockRepository.Setup(repo => repo.GetCategories())
                .Returns(categories);

            // Act
            var result = _service.GetCategories();

            // Assert
            Assert.Equal(categories, result);
            
            // Verify cache was checked and then set
            _mockCacheService.Verify(c => c.GetAsync<IEnumerable<string>>("categories"), Times.Once);
            _mockCacheService.Verify(c => c.SetAsync("categories", categories, It.IsAny<TimeSpan>()), Times.Once);
        }

        /// <summary>
        /// Tests that CreateTask creates an audit log entry.
        /// </summary>
        [Fact]
        public void CreateTask_CreatesAuditLog()
        {
            // Arrange
            var task = new TodoTask { Id = 1, Description = "New Task" };
            var addedAuditLog = null as AuditLog;

            _mockContext.Setup(c => c.AuditLogs.Add(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => addedAuditLog = log);

            // Act
            _service.CreateTask(task);

            // Assert
            _mockContext.Verify(c => c.AuditLogs.Add(It.IsAny<AuditLog>()), Times.Once);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
            
            Assert.NotNull(addedAuditLog);
            Assert.Equal("Create", addedAuditLog.Action);
            Assert.Equal("TodoTask", addedAuditLog.EntityName);
            Assert.Equal(task.Id, addedAuditLog.EntityId);
            Assert.Equal("test-user-id", addedAuditLog.UserId);
            Assert.Equal("test-user@example.com", addedAuditLog.UserName);
        }

        /// <summary>
        /// Tests that UpdateTask creates an audit log entry with changes.
        /// </summary>
        [Fact]
        public void UpdateTask_CreatesAuditLogWithChanges()
        {
            // Arrange
            var oldTask = new TodoTask 
            { 
                Id = 1, 
                Description = "Old Description",
                Category = "Old Category",
                Status = TodoTaskStatus.Pending
            };

            var updatedTask = new TodoTask 
            { 
                Id = 1, 
                Description = "New Description",
                Category = "New Category",
                Status = TodoTaskStatus.Completed
            };

            var addedAuditLog = null as AuditLog;

            _mockRepository.Setup(r => r.GetById(1)).Returns(oldTask);
            _mockContext.Setup(c => c.AuditLogs.Add(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => addedAuditLog = log);

            // Act
            _service.UpdateTask(updatedTask);

            // Assert
            _mockContext.Verify(c => c.AuditLogs.Add(It.IsAny<AuditLog>()), Times.Once);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
            
            Assert.NotNull(addedAuditLog);
            Assert.Equal("Update", addedAuditLog.Action);
            Assert.Equal("TodoTask", addedAuditLog.EntityName);
            Assert.Equal(updatedTask.Id, addedAuditLog.EntityId);
            Assert.Contains("Description", addedAuditLog.Changes);
            Assert.Contains("Category", addedAuditLog.Changes);
            Assert.Contains("Status", addedAuditLog.Changes);
        }

        /// <summary>
        /// Tests that DeleteTask creates an audit log entry.
        /// </summary>
        [Fact]
        public void DeleteTask_CreatesAuditLog()
        {
            // Arrange
            var task = new TodoTask { Id = 1, Description = "Task to Delete" };
            var addedAuditLog = null as AuditLog;

            _mockRepository.Setup(r => r.GetById(1)).Returns(task);
            _mockContext.Setup(c => c.AuditLogs.Add(It.IsAny<AuditLog>()))
                .Callback<AuditLog>(log => addedAuditLog = log);

            // Act
            _service.DeleteTask(1);

            // Assert
            _mockContext.Verify(c => c.AuditLogs.Add(It.IsAny<AuditLog>()), Times.Once);
            _mockContext.Verify(c => c.SaveChanges(), Times.Once);
            
            Assert.NotNull(addedAuditLog);
            Assert.Equal("Delete", addedAuditLog.Action);
            Assert.Equal("TodoTask", addedAuditLog.EntityName);
            Assert.Equal(task.Id, addedAuditLog.EntityId);
        }
    }
}
