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
        var (status, title, code) = exception switch
        {
            ValidationException error => (StatusCodes.Status400BadRequest, ApiErrors.InvalidRequestTitle, error.Code),
            NotFoundException error => (StatusCodes.Status404NotFound, ApiErrors.NotFoundTitle, error.Code),
            DomainException error => (StatusCodes.Status422UnprocessableEntity, ApiErrors.BusinessRuleViolatedTitle, error.Code),
            _ => (StatusCodes.Status500InternalServerError, ApiErrors.UnexpectedErrorTitle, null)
        };

        var problem = new ProblemDetails { Status = status, Title = title };

        if (code is null)
        {
            // Unexpected error: details go to the log only, never to the client
            logger.LogError(exception, "Unhandled exception while processing {Path}", httpContext.Request.Path);
        }
        else
        {
            problem.Detail = exception.Message;
            problem.Extensions[CodeExtension] = code;
        }

        httpContext.Response.StatusCode = status;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }
}
