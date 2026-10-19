namespace Template.MobileServer.Web.Telemetry;

public enum TelemetryDeviceState
{
    NoData,
    Receiving,
    Stopped
}

public readonly record struct TelemetryValue(long TimeUnixNano, double Value);

public sealed record TelemetryDeviceSummary(
    DeviceEntity Device,
    TelemetryDeviceInfoEntity? Info,
    TelemetryValue? Battery,
    TelemetryValue? WiFi,
    TelemetryValue? Cpu,
    TelemetryValue? Memory,
    TelemetryValue? CustomValue1,
    TelemetryValue? CustomValue2,
    int ErrorCount,
    int CrashCount);

public sealed record TelemetryErrorEntry(
    string DeviceId,
    long TimeUnixNano,
    int SeverityNumber,
    string Body);
