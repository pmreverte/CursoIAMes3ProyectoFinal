using System;
using System.Collections.Concurrent;
using System.Net;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Sprint2.Middleware
{
/// <summary>
/// Middleware for limiting the rate of requests from clients based on IP address.
/// </summary>
public class RateLimitingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RateLimitingMiddleware> _logger;
        private static readonly ConcurrentDictionary<string, RateLimitInfo> _rateLimits = new();
        
        // Configuración de límites por tipo de endpoint
        private readonly int _maxRequestsApi;
        private readonly int _maxRequestsStatic;
        private readonly int _maxRequestsGeneral;
        private readonly TimeSpan _interval;
        
        // Lista de IPs bloqueadas permanentemente (podría cargarse desde configuración)
        private static readonly HashSet<string> _permanentlyBlockedIps = new();
        
        // Regex para validar formato de IP
        private static readonly Regex _ipRegex = new(@"^(([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])\.){3}([0-9]|[1-9][0-9]|1[0-9]{2}|2[0-4][0-9]|25[0-5])$");
        
        public RateLimitingMiddleware(RequestDelegate next, ILogger<RateLimitingMiddleware> logger, IConfiguration configuration)
        {
            _next = next;
            _logger = logger;
            
            // Cargar configuración desde appsettings.json
            _maxRequestsApi = configuration.GetValue<int>("Security:RateLimiting:MaxRequestsApi", 100);
            _maxRequestsStatic = configuration.GetValue<int>("Security:RateLimiting:MaxRequestsStatic", 200);
            _maxRequestsGeneral = configuration.GetValue<int>("Security:RateLimiting:MaxRequestsGeneral", 60);
            int intervalInMinutes = configuration.GetValue<int>("Security:RateLimiting:IntervalInMinutes", 1);
            _interval = TimeSpan.FromMinutes(intervalInMinutes);
        }
        
    /// <summary>
    /// Processes an HTTP request and applies rate limiting based on client IP address.
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    public async Task InvokeAsync(HttpContext context)
        {
            // Obtener la dirección IP del cliente
            var ipAddress = GetClientIpAddress(context);
            
            // Validar formato de IP
            if (!IsValidIpAddress(ipAddress))
            {
                _logger.LogWarning("Intento de acceso con IP inválida: {IpAddress}", ipAddress);
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync("Acceso denegado.");
                return;
            }
            
            // Comprobar si la IP está en la lista negra permanente
            if (_permanentlyBlockedIps.Contains(ipAddress))
            {
                _logger.LogWarning("Solicitud bloqueada para IP en lista negra: {IpAddress}", ipAddress);
                context.Response.StatusCode = (int)HttpStatusCode.Forbidden;
                await context.Response.WriteAsync("Acceso denegado.");
                return;
            }
            
            // Determinar el límite de solicitudes según el tipo de endpoint
            int maxRequests = GetMaxRequestsForEndpoint(context);
            
            // Comprobar si la IP está temporalmente bloqueada por exceder límites
            if (IsIpTemporarilyBlocked(ipAddress, maxRequests))
            {
                _logger.LogWarning("Solicitud bloqueada por límite de tasa para IP: {IpAddress}", ipAddress);
                
                // Añadir encabezados de límite de tasa
                AddRateLimitHeaders(context, ipAddress, maxRequests);
                
                context.Response.StatusCode = (int)HttpStatusCode.TooManyRequests;
                await context.Response.WriteAsync("Demasiadas solicitudes. Por favor, inténtelo de nuevo más tarde.");
                return;
            }
            
            // Actualizar el contador de solicitudes
            UpdateRateLimit(ipAddress);
            
            // Añadir encabezados de límite de tasa
            AddRateLimitHeaders(context, ipAddress, maxRequests);
            
            // Continuar con la solicitud
            await _next(context);
        }
        
    /// <summary>
    /// Retrieves the client's IP address from the HTTP context.
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    /// <returns>The client's IP address as a string.</returns>
    private string GetClientIpAddress(HttpContext context)
        {
            // Intentar obtener la IP real del cliente a través de encabezados de proxy
            var forwardedFor = context.Request.Headers["X-Forwarded-For"].ToString();
            if (!string.IsNullOrEmpty(forwardedFor))
            {
                // El primer valor es la IP original del cliente
                var ips = forwardedFor.Split(',');
                return ips[0].Trim();
            }
            
            // Si no hay encabezado X-Forwarded-For, usar la IP de conexión
            return context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        }
        
    /// <summary>
    /// Validates the format of an IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address to validate.</param>
    /// <returns>True if the IP address is valid; otherwise, false.</returns>
    private bool IsValidIpAddress(string ipAddress)
        {
            // Permitir localhost IPv6
            if (ipAddress == "::1")
            {
                return true;
            }
            
            return !string.IsNullOrEmpty(ipAddress) && 
                   ipAddress != "unknown" && 
                   (_ipRegex.IsMatch(ipAddress) || IPAddress.TryParse(ipAddress, out _));
        }
        
    /// <summary>
    /// Determines the maximum number of requests allowed for a specific endpoint.
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    /// <returns>The maximum number of requests allowed.</returns>
    private int GetMaxRequestsForEndpoint(HttpContext context)
        {
            var path = context.Request.Path.ToString().ToLower();
            
            // Determinar el tipo de endpoint
            if (path.StartsWith("/api/"))
            {
                return _maxRequestsApi;
            }
            else if (path.StartsWith("/css/") || 
                     path.StartsWith("/js/") || 
                     path.StartsWith("/lib/") || 
                     path.StartsWith("/images/"))
            {
                return _maxRequestsStatic;
            }
            
            return _maxRequestsGeneral;
        }
        
    /// <summary>
    /// Checks if an IP address is temporarily blocked due to exceeding the rate limit.
    /// </summary>
    /// <param name="ipAddress">The IP address to check.</param>
    /// <param name="maxRequests">The maximum number of requests allowed.</param>
    /// <returns>True if the IP address is blocked; otherwise, false.</returns>
    private bool IsIpTemporarilyBlocked(string ipAddress, int maxRequests)
        {
            if (_rateLimits.TryGetValue(ipAddress, out var info))
            {
                // Limpiar solicitudes antiguas
                info.Requests.RemoveAll(r => r < DateTime.UtcNow - _interval);
                
                // Comprobar si se ha superado el límite
                return info.Requests.Count >= maxRequests;
            }
            
            return false;
        }
        
    /// <summary>
    /// Updates the rate limit information for a specific IP address.
    /// </summary>
    /// <param name="ipAddress">The IP address to update.</param>
    private void UpdateRateLimit(string ipAddress)
        {
            var info = _rateLimits.GetOrAdd(ipAddress, _ => new RateLimitInfo());
            
            // Limpiar solicitudes antiguas
            info.Requests.RemoveAll(r => r < DateTime.UtcNow - _interval);
            
            // Añadir la solicitud actual
            info.Requests.Add(DateTime.UtcNow);
            
            // Detectar posibles ataques (muchas solicitudes en poco tiempo)
            if (info.Requests.Count > 300)
            {
                _logger.LogWarning("Posible ataque detectado desde IP: {IpAddress}", ipAddress);
            }
        }
        
    /// <summary>
    /// Adds rate limit headers to the HTTP response.
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    /// <param name="ipAddress">The client's IP address.</param>
    /// <param name="maxRequests">The maximum number of requests allowed.</param>
    private void AddRateLimitHeaders(HttpContext context, string ipAddress, int maxRequests)
        {
            if (_rateLimits.TryGetValue(ipAddress, out var info))
            {
                int remaining = Math.Max(0, maxRequests - info.Requests.Count);
                
                context.Response.Headers["X-RateLimit-Limit"] = maxRequests.ToString();
                context.Response.Headers["X-RateLimit-Remaining"] = remaining.ToString();
                context.Response.Headers["X-RateLimit-Reset"] = DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeSeconds().ToString();
            }
        }
        
        private class RateLimitInfo
        {
            public List<DateTime> Requests { get; } = new List<DateTime>();
        }
    }
    
    // Extension method to make it easier to add the middleware to the pipeline
/// <summary>
/// Extension methods for adding rate limiting middleware to the application pipeline.
/// </summary>
public static class RateLimitingMiddlewareExtensions
    {
    /// <summary>
    /// Adds the rate limiting middleware to the application pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The updated application builder.</returns>
    public static IApplicationBuilder UseRateLimiting(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<RateLimitingMiddleware>();
        }
    }
}
