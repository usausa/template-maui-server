namespace Template.MobileServer.Components.Dialogs;

using Template.MobileServer.Domain;
using Template.MobileServer.Web.Components.Dialogs;

public sealed class PushFormValidatorTests
{
    // 正しい入力: 本文は省略できる
    [Fact]
    public void ValidateValidFormReturnsValid()
    {
        // Arrange
        var validator = new PushSendDialog.PushFormValidator();
        var form = new PushSendDialog.PushForm { Title = "お知らせ" };

        // Act
        var result = validator.Validate(form);

        // Assert
        Assert.True(result.IsValid);
    }

    // 件名: 空と空白だけは不可
    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public void ValidateBlankTitleReturnsInvalid(string title)
    {
        // Arrange
        var validator = new PushSendDialog.PushFormValidator();
        var form = new PushSendDialog.PushForm { Title = title };

        // Act
        var result = validator.Validate(form);

        // Assert
        Assert.Equal(nameof(PushSendDialog.PushForm.Title), Assert.Single(result.Errors).PropertyName);
    }

    // 件名と本文: 長すぎるものは不可
    [Fact]
    public void ValidateTooLongReturnsInvalid()
    {
        // Arrange
        var validator = new PushSendDialog.PushFormValidator();
        var form = new PushSendDialog.PushForm { Title = new string('a', Length.Title + 1), Body = new string('a', Length.Body + 1) };

        // Act
        var result = validator.Validate(form);

        // Assert
        Assert.Equal(
            [nameof(PushSendDialog.PushForm.Title), nameof(PushSendDialog.PushForm.Body)],
            result.Errors.Select(static x => x.PropertyName));
    }
}
