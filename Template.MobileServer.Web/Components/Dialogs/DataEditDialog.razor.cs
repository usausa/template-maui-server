namespace Template.MobileServer.Web.Components.Dialogs;

using Microsoft.AspNetCore.Components;

using MudBlazor;

using Template.MobileServer.Web.Models.Forms;

public sealed partial class DataEditDialog
{
    private static readonly DataFormValidator Validator = new();

    private MudForm form = default!;

    [Parameter]
    public required string Title { get; set; }

    [Parameter]
    public required DataForm Form { get; set; }

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    private async Task OnOkClick()
    {
        await form.ValidateAsync();
        if (form.IsValid)
        {
            MudDialog.Close(DialogResult.Ok(Form));
        }
    }

    private void OnCancelClick() => MudDialog.Cancel();
}
