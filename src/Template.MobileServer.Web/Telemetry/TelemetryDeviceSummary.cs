namespace Template.MobileServer.Web.Telemetry;

// 端末の状態 (受信なし = テレメトリを受けていない、途絶 = 最後の受信から TelemetryDeviceRegistry.ReceivingThreshold を過ぎた)
public enum TelemetryDeviceState
{
    NoData,
    Receiving,
    Stopped
}

// 最新値 (時刻は UTC の Unix ナノ秒)
public readonly record struct TelemetryValue(long TimeUnixNano, double Value);

// ダッシュボードの端末の行 (キャッシュのスナップショット)。Info はテレメトリを受けていなければ null、固有値はアプリケーション固有値 (0〜100)、件数は直近 24 時間
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

// 直近のエラー (全端末)。本文は 1 行目
public sealed record TelemetryErrorEntry(
    string DeviceId,
    long TimeUnixNano,
    int SeverityNumber,
    string Body);
