namespace Template.MobileServer.Components.Dialogs;

using Template.MobileServer.Domain;
using Template.MobileServer.Web.Components.Dialogs;

public sealed class DeviceFormValidatorTests
{
    [Fact]
    public void ValidateValidFormReturnsValid()
    {
        // Arrange
        var validator = new DeviceEditDialog.DeviceFormValidator();
        var form = new DeviceEditDialog.DeviceForm { TerminalId = "4db55dc544d5fec3", Name = "Pixel 9a" };

        // Act
        var result = validator.Validate(form);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("device 1")]
    [InlineData("../data")]
    public void ValidateInvalidTerminalIdReturnsInvalid(string terminalId)
    {
        // Arrange
        var validator = new DeviceEditDialog.DeviceFormValidator();
        var form = new DeviceEditDialog.DeviceForm { TerminalId = terminalId, Name = "Pixel 9a" };

        // Act
        var result = validator.Validate(form);

        // Assert
        Assert.Equal(nameof(DeviceEditDialog.DeviceForm.TerminalId), Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public void ValidateTooLongTerminalIdReturnsInvalid()
    {
        // Arrange
        var validator = new DeviceEditDialog.DeviceFormValidator();
        var form = new DeviceEditDialog.DeviceForm { TerminalId = new string('a', Length.TerminalId + 1), Name = "Pixel 9a" };

        // Act
        var result = validator.Validate(form);

        // Assert
        Assert.Single(result.Errors);
    }

    [Fact]
    public void ValidateBlankNameReturnsInvalid()
    {
        // Arrange
        var validator = new DeviceEditDialog.DeviceFormValidator();
        var form = new DeviceEditDialog.DeviceForm { TerminalId = "device-1", Name = " " };

        // Act
        var result = validator.Validate(form);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateTooLongGroupAndNoteReturnsInvalid()
    {
        // Arrange
        var validator = new DeviceEditDialog.DeviceFormValidator();
        var form = new DeviceEditDialog.DeviceForm
        {
            TerminalId = "device-1",
            Name = "Pixel 9a",
            GroupName = new string('a', Length.Name + 1),
            Note = new string('a', Length.Note + 1)
        };

        // Act
        var result = validator.Validate(form);

        // Assert
        Assert.Equal(2, result.Errors.Count);
    }
}
