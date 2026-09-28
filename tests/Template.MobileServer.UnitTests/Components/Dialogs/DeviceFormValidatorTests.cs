namespace Template.MobileServer.Components.Dialogs;

using Template.MobileServer.Domain;
using Template.MobileServer.Web.Components.Dialogs;

public sealed class DeviceFormValidatorTests
{
    // 正しい入力: グループとメモは省略できる
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

    // 端末 ID: 空と受信と違う形式 (空白・記号) は不可。理由は 1 つだけ返す
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

    // 端末 ID: 長すぎるものは不可
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

    // 名前: 空白だけは不可
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

    // 長さ: グループは名前と同じ、メモは Note まで
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
