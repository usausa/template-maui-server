namespace Template.MobileServer.Web.Components.Validation;

using FluentValidation;
using FluentValidation.Internal;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

public sealed class FluentValidationValidator : ComponentBase, IDisposable
{
    private ValidationMessageStore? messageStore;

    [CascadingParameter]
    private EditContext? EditContext { get; set; }

    [Parameter]
    public IValidator? Validator { get; set; }

    [Parameter]
    public IReadOnlyDictionary<Type, IValidator>? Validators { get; set; }

    protected override void OnInitialized()
    {
        if (EditContext is null)
        {
            throw new InvalidOperationException($"{nameof(EditContext)} is required.");
        }

        messageStore = new(EditContext);

        EditContext.OnValidationRequested += EditContextOnOnValidationRequested;
        EditContext.OnFieldChanged += EditContextOnOnFieldChanged;
    }

    public void Dispose()
    {
        if (EditContext is not null)
        {
            EditContext.OnValidationRequested -= EditContextOnOnValidationRequested;
            EditContext.OnFieldChanged -= EditContextOnOnFieldChanged;
        }
    }

    private async void EditContextOnOnValidationRequested(object? sender, ValidationRequestedEventArgs e)
    {
        await ValidateModel();
    }

    private async void EditContextOnOnFieldChanged(object? sender, FieldChangedEventArgs e)
    {
        await ValidateField(e.FieldIdentifier);
    }

    private async Task ValidateModel()
    {
        var validator = FindValidator(EditContext!.Model.GetType());
        if (validator is null)
        {
            return;
        }

        var context = new ValidationContext<object>(EditContext!.Model);
        var result = await validator.ValidateAsync(context);

        messageStore!.Clear();
        foreach (var failure in result.Errors)
        {
            var fieldIdentifier = ToFieldIdentifier(EditContext, failure.PropertyName);
            messageStore.Add(fieldIdentifier, failure.ErrorMessage);
        }

        EditContext?.NotifyValidationStateChanged();
    }

    private async Task ValidateField(FieldIdentifier fieldIdentifier)
    {
        var validator = FindValidator(fieldIdentifier.Model.GetType());
        if (validator is null)
        {
            return;
        }

        var context = new ValidationContext<object>(fieldIdentifier.Model, new PropertyChain(), new MemberNameValidatorSelector(new[] { fieldIdentifier.FieldName }));
        var result = await validator.ValidateAsync(context);
        messageStore!.Clear(fieldIdentifier);
        messageStore.Add(fieldIdentifier, result.Errors.Select(static error => error.ErrorMessage));

        EditContext?.NotifyValidationStateChanged();
    }

    private IValidator? FindValidator(Type type)
    {
        return Validators?.TryGetValue(type, out var validator) ?? false ? validator : Validator;
    }

    public void ClearErrors()
    {
        messageStore?.Clear();
        EditContext?.NotifyValidationStateChanged();
    }

    private static readonly char[] Separators = { '.', '[' };

    private static FieldIdentifier ToFieldIdentifier(EditContext editContext, string propertyPath)
    {
        // https://blog.stevensanderson.com/2019/09/04/blazor-fluentvalidation/
        // This method parses property paths like 'SomeProp.MyCollection[123].ChildProp'
        // and returns a FieldIdentifier which is an (instance, propName) pair. For example,
        // it would return the pair (SomeProp.MyCollection[123], "ChildProp"). It traverses
        // as far into the propertyPath as it can go until it finds any null instance.

        var obj = editContext.Model;
        var nextTokenEnd = propertyPath.IndexOfAny(Separators);

        // Optimize for a scenario when parsing isn't needed.
        if (nextTokenEnd < 0)
        {
            return new FieldIdentifier(obj, propertyPath);
        }

        ReadOnlySpan<char> propertyPathSpan = propertyPath;
        while (true)
        {
            var nextToken = propertyPathSpan[..nextTokenEnd];
            propertyPathSpan = propertyPathSpan[(nextTokenEnd + 1)..];

            object? newObj;
            if (nextToken.EndsWith("]"))
            {
                // It's an indexer
                // This code assumes C# conventions (one indexer named Item with one param)
                nextToken = nextToken[..^1];
                var prop = obj.GetType().GetProperty("Item");

                if (prop is not null)
                {
                    // we've got an Item property
                    var indexerType = prop.GetIndexParameters()[0].ParameterType;
                    var indexerValue = Convert.ChangeType(nextToken.ToString(), indexerType, CultureInfo.InvariantCulture);

                    newObj = prop.GetValue(obj, new[] { indexerValue });
                }
                else
                {
                    // If there is no Item property
                    // Try to cast the object to array
                    if (obj is object[] array)
                    {
                        var indexerValue = Int32.Parse(nextToken, CultureInfo.InvariantCulture);
                        newObj = array[indexerValue];
                    }
                    else
                    {
                        throw new InvalidOperationException($"Could not find indexer on object of type {obj.GetType().FullName}.");
                    }
                }
            }
            else
            {
                // It's a regular property
                var prop = obj.GetType().GetProperty(nextToken.ToString());
                if (prop == null)
                {
                    throw new InvalidOperationException($"Could not find property named {nextToken.ToString()} on object of type {obj.GetType().FullName}.");
                }
                newObj = prop.GetValue(obj);
            }

            if (newObj == null)
            {
                // This is as far as we can go
                return new FieldIdentifier(obj, nextToken.ToString());
            }

            obj = newObj;

            nextTokenEnd = propertyPathSpan.IndexOfAny(Separators);
            if (nextTokenEnd < 0)
            {
                return new FieldIdentifier(obj, propertyPathSpan.ToString());
            }
        }
    }
}
