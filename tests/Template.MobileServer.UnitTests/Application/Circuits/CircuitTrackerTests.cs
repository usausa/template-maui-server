namespace Template.MobileServer.Application.Circuits;

using Template.MobileServer.Web.Application.Circuits;

public sealed class CircuitTrackerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 20, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void AddAndRemoveChangeCount()
    {
        // Arrange
        var tracker = new CircuitTracker();
        var changed = 0;
        tracker.Changed += (_, _) => changed++;

        // Act
        tracker.Add(new CircuitInfo("a", Now, true));
        tracker.Add(new CircuitInfo("b", Now.AddSeconds(1), true));
        tracker.Remove("a");
        tracker.Remove("x");

        // Assert
        Assert.Equal(1, tracker.Count);
        Assert.Equal("b", Assert.Single(tracker.List()).Id);
        Assert.Equal(3, changed);
    }

    [Fact]
    public void SetConnectedUpdatesOnlyWhenChanged()
    {
        // Arrange
        var tracker = new CircuitTracker();
        tracker.Add(new CircuitInfo("a", Now, true));
        var changed = 0;
        tracker.Changed += (_, _) => changed++;

        // Act
        tracker.SetConnected("a", true);
        tracker.SetConnected("a", false);
        tracker.SetConnected("x", false);

        // Assert
        Assert.False(tracker.List()[0].Connected);
        Assert.Equal(1, changed);
    }
}
