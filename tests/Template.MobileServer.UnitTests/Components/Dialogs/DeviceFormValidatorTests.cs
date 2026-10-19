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
        var form = new DeviceEditDialog.DeviceForm { DeviceId = "4db55dc544d5fec3", Name = "Pixel 9a" };

        // Act
        var result = validator.Validate(form);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("device 1")]
    [InlineData("../data")]
    public void ValidateInvalidDeviceIdReturnsInvalid(string deviceId)
    {
        // Arrange
        var validator = new DeviceEditDialog.DeviceFormValidator();
        var form = new DeviceEditDialog.DeviceForm { DeviceId = deviceId, Name = "Pixel 9a" };

        // Act
        var result = validator.Validate(form);

        // Assert
        Assert.Equal(nameof(DeviceEditDialog.DeviceForm.DeviceId), Assert.Single(result.Errors).PropertyName);
    }

    [Fact]
    public void ValidateTooLongDeviceIdReturnsInvalid()
    {
        // Arrange
        var validator = new DeviceEditDialog.DeviceFormValidator();
        var form = new DeviceEditDialog.DeviceForm { DeviceId = new string('a', Length.DeviceId + 1), Name = "Pixel 9a" };

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
        var form = new DeviceEditDialog.DeviceForm { DeviceId = "device-1", Name = " " };

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
            DeviceId = "device-1",
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
