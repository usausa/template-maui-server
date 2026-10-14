namespace Template.MobileServer.Web.Application.ExceptionHandling;

using Microsoft.AspNetCore.Diagnostics;

using Template.MobileServer.Web.Endpoints;

public sealed class GlobalExceptionHandler : IExceptionHandler
{
    private ILogger<GlobalExceptionHandler> Logger { get; }

    private IProblemDetailsService ProblemDetailsService { get; }

    public GlobalExceptionHandler(
        ILogger<GlobalExceptionHandler> logger,
        IProblemDetailsService problemDetailsService)
    {
        Logger = logger;
        ProblemDetailsService = problemDetailsService;
    }

    public ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (!httpContext.Request.Path.StartsWithSegments(ApiRoutes.Prefix, StringComparison.OrdinalIgnoreCase))
        {
            return ValueTask.FromResult(false);
        }

        if (exception is BadHttpRequestException badRequest)
        {
            httpContext.Response.StatusCode = badRequest.StatusCode;

            return ProblemDetailsService.TryWriteAsync(new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = badRequest.StatusCode
                }
            });
        }

        Logger.ErrorUnhandledException(exception);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return ProblemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred."
            }
        });
    }
}
