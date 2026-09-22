namespace Template.MobileServer.Services;

using Template.MobileServer.Web.Hubs;
using Template.MobileServer.Web.Services;

public sealed class DeviceRegistryTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 15, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void RegisterAddsEntryAndRaisesChanged()
    {
        // Arrange
        var registry = new DeviceRegistry();
        var changed = 0;
        registry.Changed += (_, _) => changed++;

        // Act
        registry.Register("c1", Now);

        // Assert
        Assert.Equal(1, registry.Count);
        Assert.Equal(1, changed);
        var entry = Assert.Single(registry.Entries);
        Assert.Equal("c1", entry.ConnectionId);
        Assert.Equal(Now, entry.ConnectedAt);
        Assert.Null(entry.DeviceId);
    }

    [Fact]
    public void UpdateAppliesStatusToRegisteredEntry()
    {
        // Arrange
        var registry = new DeviceRegistry();
        registry.Register("c1", Now);
        var status = new DeviceStatusMessage
        {
            DeviceId = "device-1",
            Model = "Pixel 9a",
            Platform = "Android 16",
            Battery = 0.75,
            BatteryState = "Discharging",
            Network = "WiFi"
        };

        // Act
        var updated = registry.Update("c1", status, Now.AddSeconds(10));

        // Assert
        Assert.True(updated);
        var entry = Assert.Single(registry.Entries);
        Assert.Equal("device-1", entry.DeviceId);
        Assert.Equal("Pixel 9a", entry.Model);
        Assert.Equal(0.75, entry.Battery);
        Assert.Equal(Now.AddSeconds(10), entry.LastReceivedAt);
        Assert.Equal(Now, entry.ConnectedAt);
    }

    [Fact]
    public void UpdateUnknownConnectionIsIgnored()
    {
        // Arrange
        var registry = new DeviceRegistry();

        // Act
        var updated = registry.Update("unknown", new DeviceStatusMessage(), Now);

        // Assert
        Assert.False(updated);
        Assert.Equal(0, registry.Count);
    }

    [Fact]
    public void RemoveDeletesEntryAndRaisesChanged()
    {
        // Arrange
        var registry = new DeviceRegistry();
        registry.Register("c1", Now);
        var changed = 0;
        registry.Changed += (_, _) => changed++;

        // Act
        var removed = registry.Remove("c1");
        var removedAgain = registry.Remove("c1");

        // Assert
        Assert.True(removed);
        Assert.False(removedAgain);
        Assert.Equal(0, registry.Count);
        Assert.Equal(1, changed);
    }

    [Fact]
    public void DisconnectInvokesAbortOfRegisteredConnection()
    {
        // Arrange
        var registry = new DeviceRegistry();
        var aborted = 0;
        registry.Register("c1", Now, () => aborted++);
        registry.Register("c2", Now);

        // Act
        var disconnected = registry.Disconnect("c1");
        var withoutAbort = registry.Disconnect("c2");
        var unknown = registry.Disconnect("unknown");

        // Assert
        Assert.True(disconnected);
        Assert.False(withoutAbort);
        Assert.False(unknown);
        Assert.Equal(1, aborted);
        Assert.Equal(2, registry.Count);
    }

    [Fact]
    public void EntriesAreOrderedByConnectedAt()
    {
        // Arrange
        var registry = new DeviceRegistry();
        registry.Register("c2", Now.AddSeconds(5));
        registry.Register("c1", Now);

        // Act
        var entries = registry.Entries;

        // Assert
        Assert.Equal(["c1", "c2"], entries.Select(static x => x.ConnectionId).ToArray());
    }
}
