namespace Template.MobileServer.Web.Components;

using Microsoft.AspNetCore.Components.Web;

public sealed class ErrorBoundaryLogger : IErrorBoundaryLogger
{
    private ILogger<ErrorBoundaryLogger> Log { get; }

    public ErrorBoundaryLogger(ILogger<ErrorBoundaryLogger> log)
    {
        Log = log;
    }

    public ValueTask LogErrorAsync(Exception exception)
    {
        Log.ErrorComponentException(exception);
        return ValueTask.CompletedTask;
    }
}
