namespace Template.MobileServer.Infrastructure.Security;

public sealed class DefaultPasswordProviderTests
{
    [Fact]
    public void MatchGeneratedHashReturnsTrue()
    {
        // Arrange
        var provider = new DefaultPasswordProvider(new DefaultPasswordProviderOptions());
        var hash = provider.Generate("password");

        // Act
        var result = provider.Match("password", hash);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void MatchWrongPasswordReturnsFalse()
    {
        // Arrange
        var provider = new DefaultPasswordProvider(new DefaultPasswordProviderOptions());
        var hash = provider.Generate("password");

        // Act
        var result = provider.Match("wrong", hash);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void MatchInvalidLengthHashReturnsFalse()
    {
        // Arrange
        var provider = new DefaultPasswordProvider(new DefaultPasswordProviderOptions());

        // Act
        var result = provider.Match("password", [0x00, 0x01]);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GenerateSamePasswordReturnsDifferentHash()
    {
        // Arrange
        var provider = new DefaultPasswordProvider(new DefaultPasswordProviderOptions());

        // Act
        var hash1 = provider.Generate("password");
        var hash2 = provider.Generate("password");

        // Assert
        Assert.NotEqual(hash1, hash2);
    }
}
