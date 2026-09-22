namespace Template.MobileServer.Web.Components.Dialogs;

using MudBlazor;

public static class DialogExtensions
{
    public static async ValueTask ShowInformation(this IDialogService dialog, string title, string message)
    {
        var reference = await dialog.ShowAsync<AppMessageBox>(
            string.Empty,
            new DialogParameters
            {
                { nameof(AppMessageBox.Type), MessageBoxType.Information },
                { nameof(AppMessageBox.Title), title },
                { nameof(AppMessageBox.Message), message }
            },
            null);
        await reference.Result;
    }

    public static async ValueTask<bool> ShowConfirm(this IDialogService dialog, string title, string message)
    {
        var reference = await dialog.ShowAsync<AppMessageBox>(
            string.Empty,
            new DialogParameters
            {
                { nameof(AppMessageBox.Type), MessageBoxType.Confirm },
                { nameof(AppMessageBox.Title), title },
                { nameof(AppMessageBox.Message), message }
            },
            null);
        var result = await reference.Result;
        return (bool?)result!.Data == true;
    }

    public static async ValueTask<DataEntity?> ShowEditDialog(this IDialogService dialog, string title, DataEntity? entity)
    {
        var reference = await dialog.ShowAsync<DataEditDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(DataEditDialog.Title), title },
                { nameof(DataEditDialog.Entity), entity }
            });
        var result = await reference.Result;
        return (result is { Canceled: false }) ? (DataEntity)result.Data! : null;
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
