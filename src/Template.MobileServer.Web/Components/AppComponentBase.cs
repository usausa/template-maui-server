namespace Template.MobileServer.Web.Components;

using Microsoft.AspNetCore.Components;

using Template.MobileServer.Web.Application.Context;

public abstract class AppComponentBase : ComponentBase, IHandleEvent, IDisposable
{
    private List<IDisposable>? disposables;

    protected ICollection<IDisposable> Disposables => disposables ??= [];

    [Inject]
    private BlazorServiceScope ServiceScope { get; set; } = default!;

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing && (disposables is not null))
        {
            foreach (var disposable in disposables)
            {
                disposable.Dispose();
            }

            disposables = null;
        }
    }

    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg) => HandleEventAsync(callback, arg);

    protected async Task HandleEventAsync(EventCallbackWorkItem callback, object? arg)
    {
        using (ServiceScope.Begin())
        {
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
    }

    public override async Task SetParametersAsync(ParameterView parameters)
    {
        using (ServiceScope.Begin())
        {
            await base.SetParametersAsync(parameters);
        }
    }
}
