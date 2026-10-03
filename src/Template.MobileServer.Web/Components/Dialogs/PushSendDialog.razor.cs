namespace Template.MobileServer.Web.Components.Dialogs;

using FluentValidation;

using Microsoft.AspNetCore.Components;

using MudBlazor;

// 通知の件名と本文の入力。OK で前後の空白を除いた (件名, 本文) を返す
public sealed partial class PushSendDialog
{
    private static readonly PushFormValidator Validator = new();

    private readonly PushForm model = new();

#pragma warning disable CA2213
    private MudForm form = default!;
#pragma warning restore CA2213

    //--------------------------------------------------------------------------------
    // Parameter
    //--------------------------------------------------------------------------------

    [Parameter]
    public required string Title { get; set; }

    [CascadingParameter]
    public required IMudDialogInstance MudDialog { get; set; }

    //--------------------------------------------------------------------------------
    // Events
    //--------------------------------------------------------------------------------

    private async Task OnOkClick()
    {
        await form.ValidateAsync();
        if (form.IsValid)
        {
            MudDialog.Close(DialogResult.Ok((model.Title.Trim(), model.Body.Trim())));
        }
    }

    private void OnCancelClick() => MudDialog.Cancel();

    //--------------------------------------------------------------------------------
    // Form
    //--------------------------------------------------------------------------------

    internal sealed class PushForm
    {
        public string Title { get; set; } = string.Empty;

        public string Body { get; set; } = string.Empty;
    }

    internal sealed class PushFormValidator : AbstractValidator<PushForm>
    {
        public PushFormValidator()
        {
            RuleFor(static x => x.Title)
                .NotEmpty().WithMessage("件名を入力してください。")
                .MaximumLength(Length.Title).WithMessage($"件名は{Length.Title}文字以内で入力してください。");
            RuleFor(static x => x.Body)
                .MaximumLength(Length.Body).WithMessage($"本文は{Length.Body}文字以内で入力してください。");
        }

        public Func<object, string, Task<IEnumerable<string>>> ValidateValue => async (model, propertyName) =>
        {
            var result = await ValidateAsync(ValidationContext<PushForm>.CreateWithOptions((PushForm)model, x => x.IncludeProperties(propertyName)));
            return result.IsValid ? [] : result.Errors.Select(static e => e.ErrorMessage);
        };
    }
}
