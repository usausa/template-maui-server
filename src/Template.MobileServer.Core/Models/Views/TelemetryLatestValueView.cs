namespace Template.MobileServer.Models.Views;

// 系列の最後の点
public sealed record TelemetryLatestValueView(
    string Name,
    string AttributesJson,
    long TimeUnixNano,
    double? Value);
