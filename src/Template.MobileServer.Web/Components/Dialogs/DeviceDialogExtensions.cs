namespace Template.MobileServer.Web.Components.Dialogs;

using MudBlazor;

public static class DeviceDialogExtensions
{
    // 追加は entity = null (端末 ID を入力する)
    public static async ValueTask<DeviceEntity?> ShowDeviceEditDialog(this IDialogService dialog, string title, DeviceEntity? entity)
    {
        var reference = await dialog.ShowAsync<DeviceEditDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(DeviceEditDialog.Title), title },
                { nameof(DeviceEditDialog.Entity), entity }
            });
        var result = await reference.Result;
        return (result is { Canceled: false }) ? (DeviceEntity)result.Data! : null;
    }
}
