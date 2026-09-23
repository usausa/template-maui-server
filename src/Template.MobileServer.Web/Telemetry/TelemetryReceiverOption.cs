namespace Template.MobileServer.Web.Telemetry;

public sealed class TelemetryReceiverOption
{
    [Range(1_048_576, 16_777_216)]
    public int MaxReceiveMessageSize { get; set; } = 16_777_216;
}
