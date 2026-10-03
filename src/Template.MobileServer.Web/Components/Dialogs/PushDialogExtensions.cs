namespace Template.MobileServer.Web.Components.Dialogs;

using MudBlazor;

public static class PushDialogExtensions
{
    // 件名と本文 (取り消しは null)
    public static async ValueTask<(string Title, string Body)?> ShowPushSendDialog(this IDialogService dialog, string title)
    {
        var reference = await dialog.ShowAsync<PushSendDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(PushSendDialog.Title), title }
            });
        var result = await reference.Result;
        return (result is { Canceled: false }) ? ((string Title, string Body))result.Data! : null;
    }
}
