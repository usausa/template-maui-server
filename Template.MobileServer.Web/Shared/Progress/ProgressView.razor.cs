namespace Template.MobileServer.Web.Shared.Progress;

public sealed partial class ProgressView : IDisposable
{
    private readonly ProgressState progress = new();

    [Inject]
    public IJSRuntime Script { get; set; } = default!;

    [Parameter]
    public RenderFragment? ChildContent { get; set; }

    private string visibility = "hidden";

    protected override void OnInitialized()
    {
        progress.StateChanged += OnStateChanged;
    }

    public void Dispose()
    {
        progress.StateChanged -= OnStateChanged;
    }

    private async void OnStateChanged(object? sender, EventArgs e)
    {
        StateHasChanged();
        visibility = progress.IsBusy ? "visible" : "hidden";
        await Script.InvokeVoidAsync(progress.IsBusy ? "Progress.showProgress" : "Progress.hideProgress", "progress-modal");
        StateHasChanged();
    }
}
