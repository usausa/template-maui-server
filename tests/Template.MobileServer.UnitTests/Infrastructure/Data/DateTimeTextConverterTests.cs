namespace Template.MobileServer.Infrastructure.Data;

public sealed class DateTimeTextConverterTests
{
    [Fact]
    public void WritesUtcWithSevenDigits()
    {
        // Act
        var text = DateTimeTextConverter.ToDb(new DateTime(2026, 9, 15, 16, 11, 19, DateTimeKind.Utc));

        // Assert
        Assert.Equal("2026-09-15 16:11:19.0000000", text);
    }

    [Theory]
    [InlineData("2026-09-15 16:11:19.4400000", 440)]
    [InlineData("2026-09-15 16:11:19.44", 440)]
    [InlineData("2026-09-15 16:11:19", 0)]
    [InlineData("2026-09-15T16:11:19", 0)]
    public void ReadsAsUtc(string text, int milliseconds)
    {
        // Act
        var value = DateTimeTextConverter.FromDb(text);

        // Assert
        Assert.Equal(DateTimeKind.Utc, value.Kind);
        Assert.Equal(new DateTime(2026, 9, 15, 16, 11, 19, milliseconds, DateTimeKind.Utc), value);
    }
}
