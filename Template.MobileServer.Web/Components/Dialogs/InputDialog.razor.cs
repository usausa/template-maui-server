namespace Template.MobileServer.Web.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// テキスト入力ダイアログ(フォルダ作成等)
public sealed partial class InputDialog
{
    private string value = string.Empty;

    [Parameter]
    public required string Title { get; set; }

    [Parameter]
    public required string Label { get; set; }

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    private void OnOkClick()
    {
        var input = value.Trim();
        if (input.Length > 0)
        {
            MudDialog.Close(DialogResult.Ok(input));
        }
    }

    private void OnCancelClick() => MudDialog.Cancel();
}
