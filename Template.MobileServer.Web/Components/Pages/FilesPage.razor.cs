namespace Template.MobileServer.Web.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

using MudBlazor;

using Template.MobileServer.Infrastructure.Storage;
using Template.MobileServer.Web.Components.Dialogs;
using Template.MobileServer.Web.Infrastructure.Components;
using Template.MobileServer.Web.Infrastructure.IO;

// ストレージブラウザ(ディレクトリ階層のブラウズ/アップロード/ダウンロード/削除/フォルダ作成)
public sealed partial class FilesPage
{
    private const long MaxFileSize = 100L * 1024 * 1024;

    private List<StorageEntry> entries = [];

    private List<BreadcrumbItem> breadcrumbs = [];

    private string currentPath = string.Empty;

    private bool loading;

    private bool uploading;

    private int progress;

    [Inject]
    public required IStorage Storage { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Inject]
    public required NavigationManager Navigation { get; set; }

    [Parameter]
    public string? Path { get; set; }

    protected override Task OnParametersSetAsync()
    {
        currentPath = (Path ?? string.Empty).Trim('/');
        BuildBreadcrumbs();
        return LoadAsync();
    }

    //--------------------------------------------------------------------------------
    // Navigation
    //--------------------------------------------------------------------------------

    private void BuildBreadcrumbs()
    {
        breadcrumbs = [new BreadcrumbItem("ホーム", "files", disabled: currentPath.Length == 0)];

        if (currentPath.Length > 0)
        {
            var segments = currentPath.Split('/');
            var path = string.Empty;
            for (var i = 0; i < segments.Length; i++)
            {
                path = path.Length == 0 ? segments[i] : path + "/" + segments[i];
                breadcrumbs.Add(new BreadcrumbItem(segments[i], "files/" + EscapePath(path), disabled: i == segments.Length - 1));
            }
        }
    }

    private void MoveTo(string name) =>
        Navigation.NavigateTo("files/" + EscapePath(MakeItemPath(name)));

    private string MakeItemPath(string name) =>
        currentPath.Length == 0 ? name : currentPath + "/" + name;

    private string MakeDownloadUrl(string name) =>
        "api/storage/" + EscapePath(MakeItemPath(name));

    private static string EscapePath(string path) =>
        String.Join('/', path.Split('/').Select(Uri.EscapeDataString));

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private async Task LoadAsync()
    {
        loading = true;
        try
        {
            if (await Storage.DirectoryExistsAsync(currentPath))
            {
                var list = await Storage.ListEntriesAsync(currentPath);
                // ディレクトリ優先ソート
                entries = list
                    .OrderByDescending(static x => x.IsDirectory)
                    .ThenBy(static x => x.Name, StringComparer.OrdinalIgnoreCase)
                    .ToList();
            }
            else
            {
                entries = [];
                Snackbar.AddError("ディレクトリが存在しません。");
            }
        }
        catch (StorageException)
        {
            entries = [];
            Snackbar.AddError("不正なパスです。");
        }
        finally
        {
            loading = false;
        }
    }

    private async Task CreateDirectoryAsync()
    {
        var reference = await DialogService.ShowAsync<InputDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(InputDialog.Title), "フォルダ作成" },
                { nameof(InputDialog.Label), "フォルダ名" }
            });
        var result = await reference.Result;
        if (result is not { Canceled: false })
        {
            return;
        }

        var name = (string)result.Data!;
        try
        {
            await Storage.CreateDirectoryAsync(MakeItemPath(name));
            Snackbar.AddSuccess($"{name} を作成しました。");
        }
        catch (StorageException)
        {
            Snackbar.AddError("不正なフォルダ名です。");
        }
        catch (IOException)
        {
            Snackbar.AddError("フォルダの作成に失敗しました。");
        }

        await LoadAsync();
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
            var fileName = System.IO.Path.GetFileName(file.Name);
            await using var browser = file.OpenReadStream(MaxFileSize);
            await using var progressStream = new ReadProgressStream(browser, file.Size, OnProgress);
            await Storage.WriteAsync(MakeItemPath(fileName), progressStream);

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

    private async Task DeleteAsync(StorageEntry entry)
    {
        var caption = entry.IsDirectory ? "フォルダ削除" : "ファイル削除";
        var message = entry.IsDirectory
            ? $"{entry.Name} を配下も含めて削除してよろしいですか？"
            : $"{entry.Name} を削除してよろしいですか？";
        if (!await DialogService.ShowConfirm(caption, message))
        {
            return;
        }

        await Storage.DeleteAsync(MakeItemPath(entry.Name));
        Snackbar.AddSuccess("削除しました。");
        await LoadAsync();
    }
}
