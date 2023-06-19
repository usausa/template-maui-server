namespace Template.MobileServer.Web.Shared.Progress;

public sealed class ProgressState
{
    public event EventHandler<EventArgs>? StateChanged;

    public bool IsBusy { get; set; }

    public string Message { get; set; } = string.Empty;

#pragma warning disable CA1030
    public void RaiseStateChanged()
    {
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
#pragma warning restore CA1030
}
