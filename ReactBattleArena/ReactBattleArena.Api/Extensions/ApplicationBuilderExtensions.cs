using ReactBattleArena.Api.Middleware;
//Extension Program.cs temiz kalır

namespace ReactBattleArena.Api.Extensions;

public static class ApplicationBuilderExtensions
{
    public static IApplicationBuilder UseFluentValidationExceptionHandler(this IApplicationBuilder app)
    {
        return app.UseMiddleware<FluentValidationExceptionMiddleware>();
    }
}