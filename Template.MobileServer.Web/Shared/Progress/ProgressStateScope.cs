namespace Template.MobileServer.Web.Shared.Progress;

#pragma warning disable CA1815
public readonly struct ProgressStateScope : IDisposable
{
    private readonly ProgressState state;

    public ProgressStateScope(ProgressState state, string message = "")
    {
        this.state = state;
        state.IsBusy = true;
        state.Message = message;
        state.RaiseStateChanged();
    }

    public void Dispose()
    {
        state.IsBusy = false;
        state.Message = string.Empty;
        state.RaiseStateChanged();
    }
}
#pragma warning restore CA1815
