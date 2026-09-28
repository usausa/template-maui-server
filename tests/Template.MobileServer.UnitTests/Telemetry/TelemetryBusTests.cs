namespace Template.MobileServer.Telemetry;

using Microsoft.Extensions.Logging.Abstractions;

using Template.MobileServer.Web.Telemetry;

public sealed class TelemetryBusTests
{
    // 受け手の例外: 次の受け手へ進み、発行した側には返さない
    [Fact]
    public void PublishContinuesAfterHandlerException()
    {
        // Arrange
        var bus = new TelemetryBus(NullLogger<TelemetryBus>.Instance);
        var received = new List<string>();
        bus.DeviceChanged += static (_, _) => throw new InvalidOperationException("failed");
        bus.DeviceChanged += (_, e) => received.Add(e.DeviceId);

        // Act
        bus.PublishDeviceChanged("device-1");

        // Assert
        Assert.Equal(["device-1"], received);
    }
}
