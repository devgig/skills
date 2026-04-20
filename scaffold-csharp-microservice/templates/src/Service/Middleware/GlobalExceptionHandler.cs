using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace Service.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        _logger.LogError(exception, "Exception occurred: {Message}", exception.Message);

        var problemDetails = exception switch
        {
            ValidationException validationEx => CreateValidationProblemDetails(httpContext, validationEx),

            ArgumentNullException => CreateProblemDetails(
                httpContext,
                "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                "Bad Request",
                "One or more required parameters are missing",
                (int)HttpStatusCode.BadRequest,
                exception.Message),

            ArgumentException => CreateProblemDetails(
                httpContext,
                "https://tools.ietf.org/html/rfc7231#section-6.5.1",
                "Bad Request",
                "One or more parameters are invalid",
                (int)HttpStatusCode.BadRequest,
                exception.Message),

            KeyNotFoundException => CreateProblemDetails(
                httpContext,
                "https://tools.ietf.org/html/rfc7231#section-6.5.4",
                "Not Found",
                "The requested resource was not found",
                (int)HttpStatusCode.NotFound,
                exception.Message),

            InvalidOperationException => CreateProblemDetails(
                httpContext,
                "https://tools.ietf.org/html/rfc7231#section-6.5.8",
                "Conflict",
                "The operation could not be completed due to a conflict",
                (int)HttpStatusCode.Conflict,
                exception.Message),

            _ => CreateProblemDetails(
                httpContext,
                "https://tools.ietf.org/html/rfc7231#section-6.6.1",
                "Internal Server Error",
                "An unexpected error occurred while processing the request",
                (int)HttpStatusCode.InternalServerError,
                "An internal server error has occurred")
        };

        httpContext.Response.StatusCode = problemDetails.Status ?? (int)HttpStatusCode.InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblemDetails(
        HttpContext httpContext,
        string type,
        string title,
        string detail,
        int status,
        string? additionalDetail = null)
    {
        var problemDetails = new ProblemDetails
        {
            Type = type,
            Title = title,
            Detail = detail,
            Status = status,
            Instance = httpContext.Request.Path
        };

        if (httpContext.Items.TryGetValue("TraceId", out var traceId))
        {
            problemDetails.Extensions["traceId"] = traceId;
        }
        else
        {
            problemDetails.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier;
        }

        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        if (!string.IsNullOrEmpty(additionalDetail) && additionalDetail != detail)
        {
            problemDetails.Extensions["exception"] = additionalDetail;
        }

        return problemDetails;
    }

    private static ProblemDetails CreateValidationProblemDetails(
        HttpContext httpContext,
        ValidationException validationException)
    {
        var problemDetails = new ProblemDetails
        {
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1",
            Title = "Validation Error",
            Detail = "One or more validation errors occurred",
            Status = (int)HttpStatusCode.BadRequest,
            Instance = httpContext.Request.Path
        };

        var errors = validationException.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
        problemDetails.Extensions["errors"] = errors;

        if (httpContext.Items.TryGetValue("TraceId", out var traceId))
        {
            problemDetails.Extensions["traceId"] = traceId;
        }
        else
        {
            problemDetails.Extensions["traceId"] = System.Diagnostics.Activity.Current?.Id ?? httpContext.TraceIdentifier;
        }

        problemDetails.Extensions["timestamp"] = DateTimeOffset.UtcNow;

        return problemDetails;
    }
}
