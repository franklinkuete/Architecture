using Microsoft.AspNetCore.Builder;

namespace Core.Middlewares;

public static class MiddlewareExtensions
{
    public static IApplicationBuilder UseGlobalErrorMiddleware(this IApplicationBuilder app)
    {
        return app.UseMiddleware<GlobalErrorMiddleware>();
    }
}

