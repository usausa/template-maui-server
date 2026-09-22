namespace Template.MobileServer.Web.Components.Shared;

using MudBlazor;

public static class DialogServiceExtensions
{
    public static async ValueTask ShowInformation(this IDialogService dialog, string title, string message)
    {
        var reference = await dialog.ShowAsync<MessageBox>(
            string.Empty,
            new DialogParameters
            {
                { nameof(MessageBox.Type), MessageBoxType.Information },
                { nameof(MessageBox.Title), title },
                { nameof(MessageBox.Message), message }
            },
            null);
        await reference.Result;
    }

    public static async ValueTask<bool> ShowConfirm(this IDialogService dialog, string title, string message)
    {
        var reference = await dialog.ShowAsync<MessageBox>(
            string.Empty,
            new DialogParameters
            {
                { nameof(MessageBox.Type), MessageBoxType.Confirm },
                { nameof(MessageBox.Title), title },
                { nameof(MessageBox.Message), message }
            },
            null);
        var result = await reference.Result;
        return (bool?)result!.Data == true;
    }

    public static async ValueTask<string?> ShowInputDialog(this IDialogService dialog, string title, string label)
    {
        var reference = await dialog.ShowAsync<InputDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(InputDialog.Title), title },
                { nameof(InputDialog.Label), label }
            });
        var result = await reference.Result;
        return (result is { Canceled: false }) ? (string)result.Data! : null;
    }
}
