namespace Template.MobileServer.Web.Models.Forms;

using FluentValidation;

public sealed class DataFormValidator : AbstractValidator<DataForm>
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
