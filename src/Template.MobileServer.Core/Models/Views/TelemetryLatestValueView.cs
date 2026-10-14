namespace Template.MobileServer.Models.Views;

public sealed record TelemetryLatestValueView(
    string Name,
    string AttributesJson,
    long TimeUnixNano,
    double? Value);
