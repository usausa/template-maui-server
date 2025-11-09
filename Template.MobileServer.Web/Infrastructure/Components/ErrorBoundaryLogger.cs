namespace Template.MobileServer.Web.Infrastructure.Components;

using Microsoft.AspNetCore.Components.Web;

public sealed class ErrorBoundaryLogger : IErrorBoundaryLogger
{
    private readonly ILogger<ErrorBoundaryLogger> log;

    public ErrorBoundaryLogger(ILogger<ErrorBoundaryLogger> log)
    {
        this.log = log;
    }

    public ValueTask LogErrorAsync(Exception exception)
    {
        log.ErrorUnknownException(exception);
        return ValueTask.CompletedTask;
    }
}
