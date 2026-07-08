using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ReactBattleArena.Api.Middleware;

public sealed class FluentValidationExceptionMiddleware
{ //Middleware ValidationException yakalar
    //Hata olunca exception'ı HTTP 400'e çevirir
    private readonly RequestDelegate _next;

    public FluentValidationExceptionMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ValidationException ex)
        {
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            context.Response.ContentType = "application/problem+json";

            var errors = ex.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(
                    g => string.IsNullOrEmpty(g.Key) ? "_" : g.Key,
                    g => g.Select(e => e.ErrorMessage).ToArray());

            var problem = new ValidationProblemDetails// ValidationProblemDetails Standart 400 JSON formatı
            {
                Title = "One or more validation errors occurred.",
                Status = StatusCodes.Status400BadRequest,
                Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                Errors = errors
            };

            await context.Response.WriteAsJsonAsync(problem);
        }
    }
}