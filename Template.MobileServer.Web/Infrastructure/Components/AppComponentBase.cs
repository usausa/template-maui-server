namespace Template.MobileServer.Web.Infrastructure.Components;

using Microsoft.AspNetCore.Components;

public abstract class AppComponentBase : ComponentBase, IDisposable
{
    private List<IDisposable>? disposables;

    protected ICollection<IDisposable> Disposables => disposables ??= [];

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
}
