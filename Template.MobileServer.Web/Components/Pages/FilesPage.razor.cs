namespace Template.MobileServer.Web.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using MudBlazor;

using Template.MobileServer.Infrastructure.Storage;
using Template.MobileServer.Web.Infrastructure.Components;
using Template.MobileServer.Web.Infrastructure.IO;

public sealed partial class FilesPage
{
    private const long MaxFileSize = 100L * 1024 * 1024;

    private string[] entries = [];

    private bool uploading;

    private int progress;

    [Inject]
    public required IStorage Storage { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    protected override Task OnInitializedAsync() =>
        LoadAsync();

    private async Task LoadAsync()
    {
        entries = await Storage.ListAsync(string.Empty);
    }

    private async Task UploadAsync(IBrowserFile? file)
    {
        if (file is null)
        {
            return;
        }

        uploading = true;
        progress = 0;
        try
        {
            var fileName = Path.GetFileName(file.Name);
            await using var browser = file.OpenReadStream(MaxFileSize);
            await using var progressStream = new ReadProgressStream(browser, file.Size, OnProgress);
            await Storage.WriteAsync(fileName, progressStream);

            Snackbar.AddSuccess($"{fileName} をアップロードしました。");
        }
        catch (IOException)
        {
            Snackbar.AddError("アップロードに失敗しました。");
        }
        finally
        {
            uploading = false;
        }

        await LoadAsync();
    }

    private void OnProgress(int percent)
    {
        _ = InvokeAsync(() =>
        {
            progress = percent;
            StateHasChanged();
        });
    }

    private async Task DeleteAsync(string entry)
    {
        if (!await DialogService.ShowConfirm("ファイル削除", $"{entry} を削除してよろしいですか？"))
        {
            return;
        }

        await Storage.DeleteAsync(entry);
        Snackbar.AddSuccess("削除しました。");
        await LoadAsync();
    }
}
