using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Serilog;
using Serilog.Context;

namespace OrderService.Api.Middleware
{
    public sealed class CorrelationIdMiddleware
    {
        public const string HeaderName = "X-Correlation-ID"; // normalized
        private readonly RequestDelegate _next;

        public CorrelationIdMiddleware(RequestDelegate next) => _next = next;

        public async Task Invoke(HttpContext context)
        {
            // 1) Correlation Id: read from common header variants, else generate
            var correlationId =
                   FirstNonEmpty(
                       context.Request.Headers[HeaderName].FirstOrDefault(),
                       context.Request.Headers["X-Correlation-Id"].FirstOrDefault(), // legacy casing
                       context.Request.Headers["X-CorrelationID"].FirstOrDefault()
                   )
                ?? Guid.NewGuid().ToString("n");

            // expose for downstream
            context.Items[HeaderName] = correlationId;

            // ensure response header
            context.Response.OnStarting(() =>
            {
                context.Response.Headers[HeaderName] = correlationId;
                return Task.CompletedTask;
            });

            // 2) User enrichment (works with JWT)
            var userId =
                   context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value
                ?? context.User.FindFirst("id")?.Value
                ?? "anonymous";

            var username =
                   context.User.Identity?.Name
                ?? context.User.FindFirst(ClaimTypes.Name)?.Value
                ?? context.User.FindFirst("unique_name")?.Value
                ?? context.User.FindFirst("preferred_username")?.Value
                ?? context.User.FindFirst("username")?.Value
                ?? "anonymous";

            // 3) Push properties to Serilog for ALL logs in this request
            using (LogContext.PushProperty("CorrelationId", correlationId))
            using (LogContext.PushProperty("UserId", userId))
            using (LogContext.PushProperty("Username", username))
            {
                try
                {
                    await _next(context);
                }
                catch (Exception ex)
                {
                    // Single, readable error line with who/what/where
                    Log.Error(ex, "Unhandled exception | {Method} {Path}", context.Request?.Method, context.Request?.Path.Value);
                    throw; // let the pipeline/exception handler return the proper response
                }
            }
        }

        private static string? FirstNonEmpty(params string?[] values)
            => values.FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));
    }
}
