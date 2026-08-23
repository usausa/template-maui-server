namespace Template.MobileServer.Web.Components.Dialogs;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using MudBlazor;

public sealed partial class AppMessageBox
{
    [Parameter]
    public required MessageBoxType Type { get; set; }

    [Parameter]
    public required string Title { get; set; }

    [Parameter]
    public required string Message { get; set; }

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

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
