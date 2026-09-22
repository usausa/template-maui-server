namespace Template.MobileServer.Web.Components;

using Microsoft.AspNetCore.Components;

using Template.MobileServer.Web.Application.Context;

public abstract class AppPageBase : AppComponentBase, IHandleEvent
{
    [Inject]
    private BlazorServiceScope ServiceScope { get; set; } = default!;

    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => HandleEventAsync(callback, arg);

    protected async Task HandleEventAsync(EventCallbackWorkItem callback, object? arg)
    {
        using var scope = ServiceScope.Begin();

        var task = callback.InvokeAsync(arg);
        var shouldAwait = task.Status != TaskStatus.RanToCompletion && task.Status != TaskStatus.Canceled;
        StateHasChanged();
        if (shouldAwait)
        {
            try
            {
                await task;
            }
            catch
            {
                if (task.IsCanceled)
                {
                    return;
                }

                throw;
            }

            StateHasChanged();
        }
    }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        using var scope = ServiceScope.Begin();
        await base.SetParametersAsync(parameters);
    }
}
