namespace Template.MobileServer.Web.Components.Dialogs;

using FluentValidation;

using Microsoft.AspNetCore.Components;

using MudBlazor;

using Smart.Mapper;

public sealed partial class DataEditDialog
{
    private static readonly DataFormValidator Validator = new();

#pragma warning disable CA2213
    private MudForm form = default!;
#pragma warning restore CA2213

    private DataForm model = default!;

    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [Parameter]
    public required string Title { get; set; }

    [Parameter]
    public DataEntity? Entity { get; set; }

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    //--------------------------------------------------------------------------------
    // Events
    //--------------------------------------------------------------------------------

    protected override void OnInitialized()
    {
        model = Entity is null ? new DataForm() : ToForm(Entity);
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
    private static partial DataForm ToForm(DataEntity entity);

    [Mapper]
    private static partial DataEntity ToEntity(DataForm form);

    //--------------------------------------------------------------------------------
    // Form
    //--------------------------------------------------------------------------------

    internal sealed class DataForm
    {
        public long Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public int Value { get; set; }
    }

    internal sealed class DataFormValidator : AbstractValidator<DataForm>
    {
        public DataFormValidator()
        {
            RuleFor(static x => x.Name)
                .NotEmpty().WithMessage("名前を入力してください。")
                .MaximumLength(Length.Name).WithMessage($"名前は{Length.Name}文字以内で入力してください。");
            RuleFor(static x => x.Value)
                .InclusiveBetween(0, 1000000).WithMessage("値は0から1000000の範囲で入力してください。");
        }

        public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
        {
            var result = await ValidateAsync(ValidationContext<DataForm>.CreateWithOptions((DataForm)model, x => x.IncludeProperties(propertyName)));
            return result.IsValid ? [] : result.Errors.Select(static e => e.ErrorMessage);
        };
    }
}
