namespace Template.MobileServer.Web.Components.Pages;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using MudBlazor;

using Template.MobileServer.Web.Components.Dialogs;
using Template.MobileServer.Web.Infrastructure.Components;
using Template.MobileServer.Web.Mappers;
using Template.MobileServer.Web.Models.Forms;

public sealed partial class DataPage
{
    private MudDataGrid<DataEntity> grid = default!;

    private string? searchName;

    [Inject]
    public required DataService DataService { get; set; }

    [Inject]
    public required DataUsecase DataUsecase { get; set; }

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
        var result = await DataUsecase.QueryPageAsync(searchName, state.Page, state.PageSize, cancellationToken);
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
        var form = await ShowEditDialog("データ追加", new DataForm());
        if (form is null)
        {
            return;
        }

        var id = await DataService.InsertAsync(form.Name, form.Value);
        if (id.HasValue)
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
        var form = await ShowEditDialog("データ編集", DataMapper.ToForm(entity));
        if (form is null)
        {
            return;
        }

        var result = await DataService.UpdateAsync(form.Id, form.Name, form.Value);
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

        if (await DataService.DeleteAsync(entity.Id))
        {
            Snackbar.AddSuccess("削除しました。");
        }
        else
        {
            Snackbar.AddError("対象が存在しません。");
        }

        await grid.ReloadServerData();
    }

    private async Task<DataForm?> ShowEditDialog(string title, DataForm form)
    {
        var reference = await DialogService.ShowAsync<DataEditDialog>(
            string.Empty,
            new DialogParameters
            {
                { nameof(DataEditDialog.Title), title },
                { nameof(DataEditDialog.Form), form }
            });
        var result = await reference.Result;
        return (result is { Canceled: false }) ? (DataForm)result.Data! : null;
    }
}
