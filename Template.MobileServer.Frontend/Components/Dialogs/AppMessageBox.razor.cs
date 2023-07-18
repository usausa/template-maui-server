namespace Template.MobileServer.Frontend.Components.Dialogs;

public sealed partial class AppMessageBox
{
    [Parameter]
    public MessageBoxType Type { get; set; }

    [Parameter]
    public string Title { get; set; } = default!;

    [Parameter]
    public string Message { get; set; } = default!;

    [CascadingParameter]
    public MudDialogInstance MudDialog { get; set; } = default!;

    private void OnCloseClick() => MudDialog.Close();

    private void OnOkClick() => MudDialog.Close(true);

    private void OnCancelClick() => MudDialog.Close(false);

    private void HandleKeyDown(KeyboardEventArgs args)
    {
        if (args.Key == "Escape")
        {
            MudDialog.Close();
        }
    }
}
