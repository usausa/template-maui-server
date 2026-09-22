namespace Template.MobileServer.Web.Components.Dialogs;

using MudBlazor;

public static class DataDialogExtensions
{
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
}
