namespace Template.MobileServer.Backend.Web.Infrastructure;

using System.Reactive.Disposables;

using Microsoft.AspNetCore.Components;

public abstract class AppComponentBase : ComponentBase, IDisposable
{
    private CompositeDisposable? disposables;

    protected ICollection<IDisposable> Disposables => disposables ??= [];

    ~AppComponentBase()
    {
        Dispose(false);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            disposables?.Dispose();
        }
    }
}
