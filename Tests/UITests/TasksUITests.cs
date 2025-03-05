using System;
using System.IO;
using System.Reflection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Hosting;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using WebDriverManager;
using WebDriverManager.DriverConfigs.Impl;
using Xunit;

namespace Sprint2.Tests.UITests
{
/// <summary>
/// UI tests for the Tasks application, verifying the user interface and interactions
/// using Selenium WebDriver.
/// </summary>
public class TasksUITests : IClassFixture<WebApplicationFactory<Program>>, IDisposable
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly IWebDriver _driver;
        private readonly string _baseUrl;

/// <summary>
/// Initializes a new instance of the TasksUITests class.
/// Sets up the test server and Chrome WebDriver for UI testing.
/// </summary>
/// <param name="factory">The WebApplicationFactory for creating the test server.</param>
public TasksUITests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            
            // Setup Chrome WebDriver
            new DriverManager().SetUpDriver(new ChromeConfig());
            var options = new ChromeOptions();
            options.AddArgument("--headless"); // Run in headless mode
            options.AddArgument("--no-sandbox");
            options.AddArgument("--disable-dev-shm-usage");
            
            _driver = new ChromeDriver(options);
            
            // Start the web server
            _baseUrl = "https://localhost:5001";
        }

/// <summary>
/// Tests that the home page displays the list of tasks.
/// </summary>
[Fact]
        public void HomePage_DisplaysTasksList()
        {
            // Arrange & Act
            _driver.Navigate().GoToUrl($"{_baseUrl}/Tasks");
            
            // Wait for the page to load
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.Title.Contains("TaskApp"));
            
            // Assert
            Assert.Contains("Tareas", _driver.PageSource);
            Assert.Contains("Nueva Tarea", _driver.PageSource);
        }

/// <summary>
/// Tests that the create page displays the task creation form.
/// </summary>
[Fact]
        public void CreatePage_DisplaysForm()
        {
            // Arrange & Act
            _driver.Navigate().GoToUrl($"{_baseUrl}/Tasks/Create");
            
            // Wait for the page to load
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElement(By.Id("Description")));
            
            // Assert
            Assert.Contains("Descripción", _driver.PageSource);
            Assert.Contains("Estado", _driver.PageSource);
            Assert.Contains("Prioridad", _driver.PageSource);
            Assert.Contains("Categoría", _driver.PageSource);
        }

/// <summary>
/// Tests that creating a task adds it to the task list.
/// </summary>
[Fact]
        public void CreateTask_AddsNewTask()
        {
            // Arrange
            _driver.Navigate().GoToUrl($"{_baseUrl}/Tasks/Create");
            
            // Wait for the page to load
            var wait = new WebDriverWait(_driver, TimeSpan.FromSeconds(10));
            wait.Until(d => d.FindElement(By.Id("Description")));
            
            // Act - Fill out the form
            _driver.FindElement(By.Id("Description")).SendKeys("UI Test Task");
            
            var statusSelect = new SelectElement(_driver.FindElement(By.Id("Status")));
            statusSelect.SelectByText("Pendiente");
            
            var prioritySelect = new SelectElement(_driver.FindElement(By.Id("Priority")));
            prioritySelect.SelectByText("Alta");
            
            _driver.FindElement(By.Id("Category")).SendKeys("UI Testing");
            
            // Submit the form
            _driver.FindElement(By.CssSelector("button[type='submit']")).Click();
            
            // Wait for redirect to index page
            wait.Until(d => d.Url.EndsWith("/Tasks"));
            
            // Assert
            Assert.Contains("UI Test Task", _driver.PageSource);
            Assert.Contains("UI Testing", _driver.PageSource);
        }

/// <summary>
/// Disposes the WebDriver instance after tests are completed.
/// </summary>
public void Dispose()
        {
            _driver.Quit();
            _driver.Dispose();
        }
    }
}
