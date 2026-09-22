namespace Template.MobileServer.Web.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using MudBlazor;

using Template.MobileServer.Web.Components.Dialogs;

public sealed partial class DataPage
{
    private MudDataGrid<DataEntity> grid = default!;

    private string? searchName;

    [Inject]
    public required DataService DataService { get; set; }

    [Inject]
    public required IDialogService DialogService { get; set; }

    [Inject]
    public required ISnackbar Snackbar { get; set; }

    [Inject]
    public required NavigationManager Navigation { get; set; }

    [SupplyParameterFromQuery(Name = "name")]
    public string? Name { get; set; }

    protected override void OnInitialized()
    {
        searchName = Name;
    }

    //--------------------------------------------------------------------------------
    // Grid
    //--------------------------------------------------------------------------------

    private async Task<GridData<DataEntity>> LoadServerData(GridState<DataEntity> state, CancellationToken cancellationToken)
    {
        // 並べ替えはサーバー側で行う。グリッドが選んだ列 (プロパティ名 = 列挙名) と昇降を渡す
        var sort = state.SortDefinitions.FirstOrDefault();
        var result = await DataService.QueryPageAsync(searchName, RequestHelper.Parse(sort?.SortBy, DataSort.Id), sort?.Descending ?? false, state.Page, state.PageSize, cancellationToken);
        return new GridData<DataEntity>
        {
            TotalItems = result.Total,
            Items = result.Items
        };
    }

    private Task SearchAsync()
    {
        // Sync search condition to URL
        Navigation.NavigateTo(Navigation.GetUriWithQueryParameter("name", String.IsNullOrEmpty(searchName) ? null : searchName));
        return grid.ReloadServerData();
    }

    private Task OnSearchKeyDown(KeyboardEventArgs args) =>
        args.Key == "Enter" ? SearchAsync() : Task.CompletedTask;

    //--------------------------------------------------------------------------------
    // Operation
    //--------------------------------------------------------------------------------

    private async Task AddAsync()
    {
        var entity = await DialogService.ShowEditDialog("データ追加", null);
        if (entity is null)
        {
            return;
        }

        if (await DataService.InsertAsync(entity) == DataWriteStatus.Success)
        {
            Snackbar.AddSuccess("追加しました。");
            await grid.ReloadServerData();
        }
        else
        {
            Snackbar.AddError("名前が重複しています。");
        }
    }

    private async Task EditAsync(DataEntity entity)
    {
        var edited = await DialogService.ShowEditDialog("データ編集", entity);
        if (edited is null)
        {
            return;
        }

        var result = await DataService.UpdateAsync(edited.Id, edited.Name, edited.Value);
        switch (result)
        {
            case DataWriteStatus.Success:
                Snackbar.AddSuccess("更新しました。");
                await grid.ReloadServerData();
                break;
            case DataWriteStatus.NotFound:
                Snackbar.AddError("対象が存在しません。");
                await grid.ReloadServerData();
                break;
            default:
                Snackbar.AddError("名前が重複しています。");
                break;
        }
    }

    private async Task DeleteAsync(DataEntity entity)
    {
        if (!await DialogService.ShowConfirm("データ削除", $"{entity.Name} を削除してよろしいですか？"))
        {
            return;
        }

        if (await DataService.DeleteAsync(entity.Id) == DataWriteStatus.Success)
        {
            Snackbar.AddSuccess("削除しました。");
        }
        else
        {
            Snackbar.AddError("対象が存在しません。");
        }

        await grid.ReloadServerData();
    }
}
