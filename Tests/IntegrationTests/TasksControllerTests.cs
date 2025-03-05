using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Sprint2.Data;
using Sprint2.Models;
using Xunit;

namespace Sprint2.Tests.IntegrationTests
{
/// <summary>
/// Integration tests for the TasksController, ensuring the controller's endpoints
/// return the expected HTTP status codes and content types.
/// </summary>
public class TasksControllerTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

/// <summary>
/// Initializes a new instance of the TasksControllerTests class.
/// Sets up the test server and in-memory database for testing.
/// </summary>
/// <param name="factory">The WebApplicationFactory for creating the test server.</param>
public TasksControllerTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    // Remove the app's ApplicationDbContext registration
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    // Add ApplicationDbContext using an in-memory database for testing
                    services.AddDbContext<ApplicationDbContext>(options =>
                    {
                        options.UseInMemoryDatabase("InMemoryDbForTesting");
                    });

                    // Build the service provider
                    var sp = services.BuildServiceProvider();

                    // Create a scope to obtain a reference to the database context
                    using (var scope = sp.CreateScope())
                    {
                        var scopedServices = scope.ServiceProvider;
                        var db = scopedServices.GetRequiredService<ApplicationDbContext>();

                        // Ensure the database is created
                        db.Database.EnsureCreated();

                        // Seed the database with test data
                        SeedDatabase(db);
                    }
                });
            });
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
                    Description = "Integration Test Task 1",
                    Status = Sprint2.Models.TodoTaskStatus.Pending,
                    Priority = Sprint2.Models.Priority.High,
                    Category = "Work",
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    DueDate = DateTime.UtcNow.AddDays(1)
                },
                new TodoTask
                {
                    Description = "Integration Test Task 2",
                    Status = Sprint2.Models.TodoTaskStatus.InProgress,
                    Priority = Sprint2.Models.Priority.Medium,
                    Category = "Personal",
                    CreatedAt = DateTime.UtcNow.AddDays(-2),
                    DueDate = DateTime.UtcNow.AddDays(2)
                }
            );
            context.SaveChanges();
        }

/// <summary>
/// Tests that the Index action returns a success status code and the correct content type.
/// </summary>
[Fact]
        public async Task Index_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Tasks");

            // Assert
            response.EnsureSuccessStatusCode(); // Status code 200-299
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }

        /// <summary>
        /// Tests that the Create action returns a success status code and the correct content type.
        /// </summary>
        [Fact]
        public async Task Create_Get_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/Tasks/Create");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }

        /// <summary>
        /// Tests that the Edit action returns a success status code and the correct content type.
        /// </summary>
        [Fact]
        public async Task Edit_Get_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateClient();
            var context = GetDbContext();
            var taskId = context.Tasks.First().Id;

            // Act
            var response = await client.GetAsync($"/Tasks/Edit/{taskId}");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }

        /// <summary>
        /// Tests that the Delete action returns a success status code and the correct content type.
        /// </summary>
        [Fact]
        public async Task Delete_Get_ReturnsSuccessStatusCode()
        {
            // Arrange
            var client = _factory.CreateClient();
            var context = GetDbContext();
            var taskId = context.Tasks.First().Id;

            // Act
            var response = await client.GetAsync($"/Tasks/Delete/{taskId}");

            // Assert
            response.EnsureSuccessStatusCode();
            Assert.Equal("text/html; charset=utf-8", response.Content.Headers.ContentType.ToString());
        }

        /// <summary>
        /// Tests that the Edit action returns a NotFound status code for a non-existent task ID.
        /// </summary>
        [Fact]
        public async Task Edit_NonExistentId_ReturnsNotFound()
        {
            // Arrange
            var client = _factory.CreateClient();
            int nonExistentId = 9999;

            // Act
            var response = await client.GetAsync($"/Tasks/Edit/{nonExistentId}");

            // Assert
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        private ApplicationDbContext GetDbContext()
        {
            var scope = _factory.Services.CreateScope();
            return scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        }
    }
}
