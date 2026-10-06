using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Orders.Api.Resources;
using Orders.Application.Common;
using Orders.Domain.Common;

namespace Orders.Api.ErrorHandling;

/// <summary>
/// Turns every exception into a ProblemDetails response, in a single place, so controllers need no try/catch.
/// Known errors also carry their stable <c>code</c>.
/// </summary>
internal sealed class ApiExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    private const string CodeExtension = "code";

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException error => Problem(StatusCodes.Status400BadRequest, ApiErrors.InvalidRequestTitle, error.Message, error.Code),
            NotFoundException error => Problem(StatusCodes.Status404NotFound, ApiErrors.NotFoundTitle, error.Message, error.Code),
            ConcurrencyException error => Problem(StatusCodes.Status409Conflict, ApiErrors.ConflictTitle, error.Message, error.Code),
            DomainException error => Problem(StatusCodes.Status422UnprocessableEntity, ApiErrors.BusinessRuleViolatedTitle, error.Message, error.Code),
            // Request errors detected by the web server itself (e.g. body too large) already carry their status
            BadHttpRequestException error => Problem(error.StatusCode, ApiErrors.InvalidRequestTitle, error.Message),
            _ => Problem(StatusCodes.Status500InternalServerError, ApiErrors.UnexpectedErrorTitle)
        };

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            // Unexpected error: details go to the log only, never to the client
            logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Problem(int status, string title, string? detail = null, string? code = null)
    {
        var problem = new ProblemDetails { Status = status, Title = title, Detail = detail };

        if (code is not null)
            problem.Extensions[CodeExtension] = code;

        return problem;
    }
}
