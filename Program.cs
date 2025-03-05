using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using FluentValidation.AspNetCore;
using Sprint2.Data;
using Sprint2.Interfaces;
using Sprint2.Middleware;
using Sprint2.Models;
using Sprint2.Repositories;
using Sprint2.Services;
using Sprint2.Validators;
using System.Net;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(); // Adds MVC services to the application
builder.Services.AddHttpContextAccessor(); // Adds IHttpContextAccessor for accessing HttpContext
builder.Services.AddRazorPages(); // Adds Razor Pages support for Identity UI
builder.Services.AddOpenApi(); // Adds OpenAPI/Swagger services for API documentation

// Register FluentValidation
builder.Services.AddFluentValidationAutoValidation()
    .AddFluentValidationClientsideAdapters()
    .AddValidatorsFromAssemblyContaining<TodoTaskValidator>();

// Configure Entity Framework Core with SQL Server
// Note: In production, the connection string should be moved to user secrets or environment variables
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configure Identity
builder.Services.AddIdentity<ApplicationUser, Microsoft.AspNetCore.Identity.IdentityRole>(options =>
{
    // Configuración de contraseñas
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Password.RequiredLength = 8;
    
    // Configuración de bloqueo de cuenta
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.AllowedForNewUsers = true;
    
    // Configuración de usuario
    options.User.RequireUniqueEmail = true;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
    
    // Configuración de inicio de sesión
    options.SignIn.RequireConfirmedEmail = false; // Cambiar a true si se implementa confirmación por email
    options.SignIn.RequireConfirmedPhoneNumber = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// Configurar opciones de cookies de Identity
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(1);
    options.LoginPath = "/Identity/Account/Login";
    options.AccessDeniedPath = "/Identity/Account/AccessDenied";
    options.SlidingExpiration = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

// Register dependencies
// Configurar caché
builder.Services.AddMemoryCache(); // Añadir caché en memoria como fallback

// Configurar Redis
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = builder.Configuration.GetConnectionString("Redis") ?? "localhost:6379";
    options.InstanceName = "Sprint2_";
});

// Registrar servicios
builder.Services.AddScoped<ITaskRepository, EfTaskRepository>(); // Registers the task repository
builder.Services.AddScoped<ITaskService, TaskService>(); // Registers the task service
builder.Services.AddScoped<ICacheService, RedisCacheService>(); // Registers the cache service
builder.Services.AddTransient<Microsoft.AspNetCore.Identity.UI.Services.IEmailSender, Sprint2.Services.EmailSender>(); // Registers the email sender service

// Configure Data Protection
builder.Services.AddDataProtection()
    .SetApplicationName("Sprint2") // Sets the application name for data protection
    .SetDefaultKeyLifetime(TimeSpan.FromDays(90)); // Sets the key lifetime

// Configure Cookie Policy
builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.CheckConsentNeeded = context => true; // Requires consent for non-essential cookies
    options.MinimumSameSitePolicy = SameSiteMode.Strict; // Sets the SameSite policy
    options.HttpOnly = Microsoft.AspNetCore.CookiePolicy.HttpOnlyPolicy.Always; // Ensures cookies are HttpOnly
    options.Secure = CookieSecurePolicy.Always; // Ensures cookies are only sent over HTTPS
});

// Configure CORS policy
builder.Services.AddCors(options =>
{
    options.AddPolicy("DefaultPolicy", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Security:Cors:AllowedOrigins").Get<string[]>()
                            ?? new[] { "https://localhost:5001" };
        
        policy.WithOrigins(allowedOrigins) // Sets allowed origins
              .AllowAnyMethod() // Allows any HTTP method
              .AllowAnyHeader() // Allows any header
              .AllowCredentials(); // Allows credentials
    });
});

// Configure Forwarded Headers
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto; // Configures forwarded headers
    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("::ffff:10.0.0.0"), 104));
    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("::ffff:192.168.0.0"), 112));
    options.KnownNetworks.Add(new Microsoft.AspNetCore.HttpOverrides.IPNetwork(IPAddress.Parse("::ffff:172.16.0.0"), 108));
});

var app = builder.Build();

// Ensure database is created and migrations are applied
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<ApplicationDbContext>();
        
        // Seed initial data if the database is empty
        if (!context.Tasks.Any())
        {
            context.Tasks.AddRange(
                new TodoTask
                {
                    Description = "Completar informe mensual",
                    Status = TodoTaskStatus.Pending,
                    Priority = Priority.High,
                    Category = "Trabajo",
                    DueDate = DateTime.UtcNow.AddDays(2),
                    Notes = "Incluir estadísticas de ventas y proyecciones"
                },
                new TodoTask
                {
                    Description = "Comprar víveres",
                    Status = TodoTaskStatus.Pending,
                    Priority = Priority.Medium,
                    Category = "Personal",
                    DueDate = DateTime.UtcNow.AddDays(1)
                },
                new TodoTask
                {
                    Description = "Preparar presentación",
                    Status = TodoTaskStatus.InProgress,
                    Priority = Priority.Urgent,
                    Category = "Trabajo",
                    DueDate = DateTime.UtcNow.AddDays(1),
                    Notes = "Presentación para el cliente sobre el nuevo producto"
                },
                new TodoTask
                {
                    Description = "Llamar al médico",
                    Status = TodoTaskStatus.Completed,
                    Priority = Priority.High,
                    Category = "Salud",
                    DueDate = DateTime.UtcNow.AddDays(-1)
                },
                new TodoTask
                {
                    Description = "Pagar facturas",
                    Status = TodoTaskStatus.Pending,
                    Priority = Priority.High,
                    Category = "Finanzas",
                    DueDate = DateTime.UtcNow.AddDays(3)
                }
            );
            context.SaveChanges(); // Saves changes to the database
        }
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocurrió un error al inicializar la base de datos."); // Logs any errors during database initialization
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi(); // Maps OpenAPI endpoints in development
}
else
{
    // Always use HTTPS in production
    app.UseHsts(); // Enables HTTP Strict Transport Security
}

// Use forwarded headers (important for proxy scenarios)
app.UseForwardedHeaders();

// HTTPS redirection
app.UseHttpsRedirection();

// Add rate limiting
app.UseRateLimiting();

// Add security headers
app.UseSecurityHeaders();

// Use CORS
app.UseCors("DefaultPolicy");

// Use cookie policy
app.UseCookiePolicy();

app.UseStaticFiles(); // Serves static files

// Añadir middleware de autenticación y autorización
app.UseAuthentication();
app.UseAuthorization();

// Añadir endpoints de Identity UI
app.MapRazorPages();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}"); // Sets Home/Index as default route

app.Run(); // Runs the application

// Make the implicit Program class public so test projects can access it
public partial class Program { }
