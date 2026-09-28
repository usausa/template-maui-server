namespace Template.MobileServer.Web.Components.Dialogs;

using FluentValidation;

using Microsoft.AspNetCore.Components;

using MudBlazor;

using Smart.Mapper;

public sealed partial class DeviceEditDialog
{
    private static readonly DeviceFormValidator Validator = new();

#pragma warning disable CA2213
    private MudForm form = default!;
#pragma warning restore CA2213

    private DeviceForm model = default!;

    // 端末 ID は追加のときだけ入力する
    private bool IsEdit => Entity is not null;

    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [Parameter]
    public required string Title { get; set; }

    [Parameter]
    public DeviceEntity? Entity { get; set; }

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    //--------------------------------------------------------------------------------
    // Events
    //--------------------------------------------------------------------------------

    protected override void OnInitialized()
    {
        model = Entity is null ? new DeviceForm() : ToForm(Entity);
    }

    private async Task OnOkClick()
    {
        await form.ValidateAsync();
        if (form.IsValid)
        {
            MudDialog.Close(DialogResult.Ok(ToEntity(model)));
        }
    }

    private void OnCancelClick() => MudDialog.Cancel();

    //--------------------------------------------------------------------------------
    // Mapping
    //--------------------------------------------------------------------------------

    [Mapper]
    private static partial DeviceForm ToForm(DeviceEntity entity);

    // 名前の前後の空白を除き、空のグループとメモは null
    private static DeviceEntity ToEntity(DeviceForm form) =>
        new()
        {
            DeviceId = form.DeviceId,
            Name = form.Name.Trim(),
            GroupName = String.IsNullOrWhiteSpace(form.GroupName) ? null : form.GroupName.Trim(),
            Note = String.IsNullOrWhiteSpace(form.Note) ? null : form.Note.Trim(),
            IsEnabled = form.IsEnabled
        };

    //--------------------------------------------------------------------------------
    // Form
    //--------------------------------------------------------------------------------

    internal sealed class DeviceForm
    {
        public string DeviceId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? GroupName { get; set; }

        public string? Note { get; set; }

        public bool IsEnabled { get; set; } = true;
    }

    internal sealed class DeviceFormValidator : AbstractValidator<DeviceForm>
    {
        public DeviceFormValidator()
        {
            // 受信と同じ形式 (ファイル名に使う)
            RuleFor(static x => x.DeviceId)
                .Cascade(CascadeMode.Stop)
                .NotEmpty().WithMessage("端末 ID を入力してください。")
                .MaximumLength(Length.DeviceId).WithMessage($"端末 ID は{Length.DeviceId}文字以内で入力してください。")
                .Must(DeviceIdFormat.IsValid).WithMessage("端末 ID は英数字・'-'・'_' で入力してください。");
            RuleFor(static x => x.Name)
                .NotEmpty().WithMessage("名前を入力してください。")
                .MaximumLength(Length.Name).WithMessage($"名前は{Length.Name}文字以内で入力してください。");
            RuleFor(static x => x.GroupName)
                .MaximumLength(Length.Name).WithMessage($"グループは{Length.Name}文字以内で入力してください。");
            RuleFor(static x => x.Note)
                .MaximumLength(Length.Note).WithMessage($"メモは{Length.Note}文字以内で入力してください。");
        }

        public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
        {
            var result = await ValidateAsync(ValidationContext<DeviceForm>.CreateWithOptions((DeviceForm)model, x => x.IncludeProperties(propertyName)));
            return result.IsValid ? [] : result.Errors.Select(static e => e.ErrorMessage);
        };
    }
}
