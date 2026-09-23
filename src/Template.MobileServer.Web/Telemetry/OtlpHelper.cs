namespace Template.MobileServer.Web.Telemetry;

using Google.Protobuf;
using Google.Protobuf.Collections;

using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Resource.V1;

public static class OtlpHelper
{
    private const string ServiceNameKey = "service.name";
    private const string DeviceIdKey = "device.id";
    private const string InstallationIdKey = "app.installation.id";

    //--------------------------------------------------------------------------------
    // Resource
    //--------------------------------------------------------------------------------

    public static string GetServiceName(this Resource? resource) =>
        FindString(resource, ServiceNameKey) ?? string.Empty;

    // Devices without device.id are identified by app.installation.id
    public static string GetDeviceId(this Resource? resource) =>
        FindString(resource, DeviceIdKey) ?? FindString(resource, InstallationIdKey) ?? string.Empty;

    private static string? FindString(Resource? resource, string key)
    {
        var value = resource?.Attributes.FirstOrDefault(x => x.Key == key)?.Value;
        return value is { ValueCase: AnyValue.ValueOneofCase.StringValue, StringValue.Length: > 0 } ? value.StringValue : null;
    }

    //--------------------------------------------------------------------------------
    // Value
    //--------------------------------------------------------------------------------

    public static string ToHex(this ByteString id) => Convert.ToHexStringLower(id.Span);

    public static DateTimeOffset ToDateTimeOffset(ulong unixNano) =>
        DateTimeOffset.UnixEpoch.AddTicks((long)(unixNano / 100));

    public static TimeSpan ToDuration(ulong startUnixNano, ulong endUnixNano) =>
        endUnixNano > startUnixNano ? TimeSpan.FromTicks((long)((endUnixNano - startUnixNano) / 100)) : TimeSpan.Zero;

    //--------------------------------------------------------------------------------
    // Metric
    //--------------------------------------------------------------------------------

    public static int GetPointCount(this Metric metric) =>
        metric.DataCase switch
        {
            Metric.DataOneofCase.Gauge => metric.Gauge.DataPoints.Count,
            Metric.DataOneofCase.Sum => metric.Sum.DataPoints.Count,
            Metric.DataOneofCase.Histogram => metric.Histogram.DataPoints.Count,
            Metric.DataOneofCase.ExponentialHistogram => metric.ExponentialHistogram.DataPoints.Count,
            Metric.DataOneofCase.Summary => metric.Summary.DataPoints.Count,
            _ => 0
        };

    public static AggregationTemporality GetTemporality(this Metric metric) =>
        metric.DataCase switch
        {
            Metric.DataOneofCase.Sum => metric.Sum.AggregationTemporality,
            Metric.DataOneofCase.Histogram => metric.Histogram.AggregationTemporality,
            Metric.DataOneofCase.ExponentialHistogram => metric.ExponentialHistogram.AggregationTemporality,
            _ => AggregationTemporality.Unspecified
        };

    //--------------------------------------------------------------------------------
    // Format
    //--------------------------------------------------------------------------------

    public static string FormatAttributes(this IEnumerable<KeyValue> attributes) =>
        String.Join(", ", attributes.Select(static x => $"{x.Key}={x.Value.FormatValue()}"));

    public static string FormatValue(this AnyValue? value) =>
        value?.ValueCase switch
        {
            AnyValue.ValueOneofCase.StringValue => value.StringValue,
            AnyValue.ValueOneofCase.BoolValue => value.BoolValue ? "true" : "false",
            AnyValue.ValueOneofCase.IntValue => value.IntValue.ToString(CultureInfo.InvariantCulture),
            AnyValue.ValueOneofCase.DoubleValue => value.DoubleValue.ToString(CultureInfo.InvariantCulture),
            AnyValue.ValueOneofCase.ArrayValue => $"[{String.Join(", ", value.ArrayValue.Values.Select(static x => x.FormatValue()))}]",
            AnyValue.ValueOneofCase.KvlistValue => $"{{{value.KvlistValue.Values.FormatAttributes()}}}",
            AnyValue.ValueOneofCase.BytesValue => value.BytesValue.ToHex(),
            _ => string.Empty
        };

    // Number: value, histogram and summary: count and sum. Attributes precede the value
    public static string FormatPoints(this Metric metric) =>
        metric.DataCase switch
        {
            Metric.DataOneofCase.Gauge => String.Join(", ", metric.Gauge.DataPoints.Select(static x => FormatPoint(x.Attributes, FormatNumber(x)))),
            Metric.DataOneofCase.Sum => String.Join(", ", metric.Sum.DataPoints.Select(static x => FormatPoint(x.Attributes, FormatNumber(x)))),
            Metric.DataOneofCase.Histogram => String.Join(", ", metric.Histogram.DataPoints.Select(static x => FormatPoint(x.Attributes, FormatDistribution(x.Count, x.Sum)))),
            Metric.DataOneofCase.ExponentialHistogram => String.Join(", ", metric.ExponentialHistogram.DataPoints.Select(static x => FormatPoint(x.Attributes, FormatDistribution(x.Count, x.Sum)))),
            Metric.DataOneofCase.Summary => String.Join(", ", metric.Summary.DataPoints.Select(static x => FormatPoint(x.Attributes, FormatDistribution(x.Count, x.Sum)))),
            _ => string.Empty
        };

    private static string FormatPoint(RepeatedField<KeyValue> attributes, string value) =>
        attributes.Count > 0 ? $"{{{attributes.FormatAttributes()}}}={value}" : value;

    private static string FormatNumber(NumberDataPoint point) =>
        point.ValueCase == NumberDataPoint.ValueOneofCase.AsInt ? point.AsInt.ToString(CultureInfo.InvariantCulture) : point.AsDouble.ToString(CultureInfo.InvariantCulture);

    private static string FormatDistribution(ulong count, double sum) =>
        String.Create(CultureInfo.InvariantCulture, $"count={count} sum={sum}");
}
