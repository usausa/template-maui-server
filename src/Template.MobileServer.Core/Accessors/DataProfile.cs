namespace Template.MobileServer.Accessors;

using Template.MobileServer.Infrastructure.Data;

[AccessorProfile]
[TypeHandler(typeof(EnumTextConverter<TelemetryMetricKind>))]
[TypeHandler(typeof(EnumTextConverter<TelemetryTemporality>))]
[TypeHandler(typeof(EnumTextConverter<TelemetrySpanKind>))]
[TypeHandler(typeof(EnumTextConverter<TelemetryStatusCode>))]
[TypeHandler(typeof(DateTimeTextConverter))]
public static class DataProfile;
