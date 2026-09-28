namespace Template.MobileServer.Domain;

using System.Text.RegularExpressions;

public sealed partial class DeviceIdFormatTests
{
    // 端末 ID の形式: 英数字・'-'・'_' の 64 文字以内。API の検証の正規表現 (Pattern) と同じ判定
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

    // 長さ: 64 文字まで
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
