namespace Template.MobileServer.Models.Forms;

using Template.MobileServer.Domain;
using Template.MobileServer.Web.Models.Forms;

public sealed class DataFormValidatorTests
{
    [Fact]
    public void ValidateValidFormReturnsValid()
    {
        // Arrange
        var validator = new DataFormValidator();
        var form = new DataForm { Name = "Data-1", Value = 100 };

        // Act
        var result = validator.Validate(form);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidateEmptyNameReturnsInvalid()
    {
        // Arrange
        var validator = new DataFormValidator();
        var form = new DataForm { Name = string.Empty, Value = 100 };

        // Act
        var result = validator.Validate(form);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateTooLongNameReturnsInvalid()
    {
        // Arrange
        var validator = new DataFormValidator();
        var form = new DataForm { Name = new string('a', Length.Name + 1), Value = 100 };

        // Act
        var result = validator.Validate(form);

        // Assert
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ValidateOutOfRangeValueReturnsInvalid()
    {
        // Arrange
        var validator = new DataFormValidator();
        var form = new DataForm { Name = "Data-1", Value = -1 };

        // Act
        var result = validator.Validate(form);

        // Assert
        Assert.False(result.IsValid);
    }
}
