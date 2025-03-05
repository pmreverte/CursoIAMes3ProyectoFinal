using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace Sprint2.Middleware
{
/// <summary>
/// Middleware for adding security headers to HTTP responses.
/// </summary>
public class SecurityHeadersMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly IConfiguration _configuration;

        public SecurityHeadersMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _configuration = configuration;
        }

    /// <summary>
    /// Processes an HTTP request and adds security headers to the response.
    /// </summary>
    /// <param name="context">The HTTP context of the request.</param>
    public async Task InvokeAsync(HttpContext context)
        {
            // Add security headers
            
            // Prevents the browser from interpreting files as a different MIME type
            context.Response.Headers["X-Content-Type-Options"] = "nosniff";
            
            // Prevents the page from being framed (clickjacking protection)
            context.Response.Headers["X-Frame-Options"] = "DENY";
            
            // Enables XSS filtering in the browser
            context.Response.Headers["X-XSS-Protection"] = "1; mode=block";
            
            // Strict Content Security Policy to prevent XSS attacks
            context.Response.Headers["Content-Security-Policy"] = 
                "default-src 'self'; " +
                "script-src 'self' https://cdn.jsdelivr.net; " + // Removed unsafe-inline
                "style-src 'self' https://cdn.jsdelivr.net; " + // Removed unsafe-inline
                "img-src 'self' data:; " +
                "font-src 'self' https://cdn.jsdelivr.net; " +
                "connect-src 'self'; " +
                "frame-ancestors 'none'; " +
                "form-action 'self'; " +
                "upgrade-insecure-requests; " +
                "block-all-mixed-content;";
            
            // Prevents the browser from sending the Referer header when navigating away from the page
            context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            
            // Permissions policy to limit browser features
            context.Response.Headers["Permissions-Policy"] = 
                "camera=(), " +
                "microphone=(), " +
                "geolocation=(), " +
                "payment=()";
            
            // Strict Transport Security to enforce HTTPS
            var hstsMaxAgeInDays = _configuration.GetValue<int>("Security:Hsts:MaxAgeInDays", 365);
            var hstsIncludeSubdomains = _configuration.GetValue<bool>("Security:Hsts:IncludeSubdomains", true);
            var hstsPreload = _configuration.GetValue<bool>("Security:Hsts:Preload", true);
            
            var hstsValue = $"max-age={hstsMaxAgeInDays * 86400}";
            if (hstsIncludeSubdomains)
                hstsValue += "; includeSubDomains";
            if (hstsPreload)
                hstsValue += "; preload";
                
            context.Response.Headers["Strict-Transport-Security"] = hstsValue;
            
            // Cache control to prevent sensitive information caching
            if (!context.Response.Headers.ContainsKey("Cache-Control"))
            {
                context.Response.Headers["Cache-Control"] = "no-store, max-age=0";
            }

            await _next(context);
        }
    }

    // Extension method to make it easier to add the middleware to the pipeline
/// <summary>
/// Extension methods for adding security headers middleware to the application pipeline.
/// </summary>
public static class SecurityHeadersMiddlewareExtensions
    {
    /// <summary>
    /// Adds the security headers middleware to the application pipeline.
    /// </summary>
    /// <param name="builder">The application builder.</param>
    /// <returns>The updated application builder.</returns>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<SecurityHeadersMiddleware>();
        }
    }
}
