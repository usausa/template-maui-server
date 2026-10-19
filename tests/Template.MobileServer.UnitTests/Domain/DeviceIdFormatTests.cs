namespace Template.MobileServer.Domain;

using System.Text.RegularExpressions;

public sealed partial class DeviceIdFormatTests
{
    [Theory]
    [InlineData("4db55dc544d5fec3", true)]
    [InlineData("9e97fa77-df72-458e-a8e0-5e9e359d0794", true)]
    [InlineData("device_1", true)]
    [InlineData("", false)]
    [InlineData("../data", false)]
    [InlineData("device 1", false)]
    [InlineData("端末", false)]
    public void IsValidMatchesPattern(string value, bool expected)
    {
        // Act / Assert
        Assert.Equal(expected, DeviceIdFormat.IsValid(value));
        Assert.Equal(expected, PatternRegex().IsMatch(value));
    }

    [Fact]
    public void IsValidChecksLength()
    {
        // Act / Assert
        Assert.True(DeviceIdFormat.IsValid(new string('a', 64)));
        Assert.False(DeviceIdFormat.IsValid(new string('a', 65)));
        Assert.DoesNotMatch(PatternRegex(), new string('a', 65));
    }

    [GeneratedRegex(DeviceIdFormat.Pattern)]
    private static partial Regex PatternRegex();
}
