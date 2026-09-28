namespace Template.MobileServer.Web.Telemetry;

using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;

using Google.Protobuf;

using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Logs.V1;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Resource.V1;
using OpenTelemetry.Proto.Trace.V1;

// OTLP → 保存用のまとまり。属性は型を残した JSON (系列と Resource はキー順)、ID は小文字の 16 進。Add~ は保存しない不正な項目の数を返す
public static class OtlpMapper
{
    private const string InstallationIdKey = "app.installation.id";
    private const string ServiceVersionKey = "service.version";
    private const string ServiceInstanceIdKey = "service.instance.id";
    private const string ManufacturerKey = "device.manufacturer";
    private const string ModelKey = "device.model.identifier";
    private const string OsNameKey = "os.name";
    private const string OsVersionKey = "os.version";

    private const int TraceIdLength = 16;
    private const int SpanIdLength = 8;

    // DataPointFlags.NO_RECORDED_VALUE
    private const uint NoRecordedValue = 1;

    private static readonly JsonWriterOptions WriterOptions = new() { Encoder = JavaScriptEncoder.Create(UnicodeRanges.All) };

    //--------------------------------------------------------------------------------
    // Resource
    //--------------------------------------------------------------------------------

    public static TelemetryBatch CreateBatch(string deviceId, Resource? resource)
    {
        var json = ToJson(resource?.Attributes.OrderBy(static x => x.Key, StringComparer.Ordinal) ?? Enumerable.Empty<KeyValue>());
        return new TelemetryBatch
        {
            DeviceId = deviceId,
            DeviceInfo = new TelemetryDeviceInfoEntity
            {
                DeviceId = deviceId,
                InstallationId = resource.GetString(InstallationIdKey),
                Manufacturer = resource.GetString(ManufacturerKey),
                Model = resource.GetString(ModelKey),
                OsName = resource.GetString(OsNameKey),
                OsVersion = resource.GetString(OsVersionKey),
                ServiceName = resource.GetServiceName(),
                ServiceVersion = resource.GetString(ServiceVersionKey)
            },
            Resource = new TelemetryResourceEntity
            {
                Hash = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(json))),
                ServiceInstanceId = resource.GetString(ServiceInstanceIdKey),
                ServiceVersion = resource.GetString(ServiceVersionKey),
                AttributesJson = json
            }
        };
    }

    //--------------------------------------------------------------------------------
    // Trace
    //--------------------------------------------------------------------------------

    // ID の長さが違うスパンは保存しない
    public static int AddSpans(TelemetryBatch batch, IEnumerable<ScopeSpans> scopeSpans)
    {
        var rejected = 0;
        foreach (var scope in scopeSpans)
        {
            var scopeName = scope.Scope?.Name ?? string.Empty;
            foreach (var span in scope.Spans)
            {
                if ((span.TraceId.Length != TraceIdLength) || (span.SpanId.Length != SpanIdLength))
                {
                    rejected++;
                    continue;
                }

                batch.Spans.Add(new TelemetrySpanEntity
                {
                    TraceId = span.TraceId.ToHex(),
                    SpanId = span.SpanId.ToHex(),
                    ParentSpanId = span.ParentSpanId.ToHex(),
                    Name = span.Name,
                    Kind = (TelemetrySpanKind)span.Kind,
                    StartTimeUnixNano = (long)span.StartTimeUnixNano,
                    EndTimeUnixNano = (long)span.EndTimeUnixNano,
                    StatusCode = (TelemetryStatusCode)(span.Status?.Code ?? default),
                    StatusMessage = span.Status?.Message ?? string.Empty,
                    ScopeName = scopeName,
                    AttributesJson = ToJson(span.Attributes),
                    EventsJson = span.Events.Count > 0 ? ToJson(writer => WriteEvents(writer, span.Events)) : null,
                    LinksJson = span.Links.Count > 0 ? ToJson(writer => WriteLinks(writer, span.Links)) : null
                });
            }
        }

        return rejected;
    }

    //--------------------------------------------------------------------------------
    // Metric
    //--------------------------------------------------------------------------------

    // 時刻の無い点と値の無い数値の点は保存しない。値を記録していない点 (NO_RECORDED_VALUE) は数えずに捨てる
    public static int AddMetrics(TelemetryBatch batch, IEnumerable<ScopeMetrics> scopeMetrics)
    {
        var rejected = 0;
        var metrics = new Dictionary<string, TelemetryMetric>(StringComparer.Ordinal);
        foreach (var scope in scopeMetrics)
        {
            var scopeName = scope.Scope?.Name ?? string.Empty;
            foreach (var metric in scope.Metrics)
            {
                switch (metric.DataCase)
                {
                    case Metric.DataOneofCase.Gauge:
                        foreach (var point in metric.Gauge.DataPoints)
                        {
                            rejected += AddNumber(batch, metrics, metric, scopeName, TelemetryTemporality.Unspecified, false, point);
                        }

                        break;
                    case Metric.DataOneofCase.Sum:
                        foreach (var point in metric.Sum.DataPoints)
                        {
                            rejected += AddNumber(batch, metrics, metric, scopeName, (TelemetryTemporality)metric.Sum.AggregationTemporality, metric.Sum.IsMonotonic, point);
                        }

                        break;
                    case Metric.DataOneofCase.Histogram:
                        foreach (var point in metric.Histogram.DataPoints)
                        {
                            rejected += AddPoint(batch, metrics, metric, scopeName, TelemetryMetricKind.Histogram, (TelemetryTemporality)metric.Histogram.AggregationTemporality, false, point.Attributes, point.Flags, point.TimeUnixNano, new TelemetryMetricPointEntity
                            {
                                StartTimeUnixNano = (long)point.StartTimeUnixNano,
                                Count = (long)point.Count,
                                Sum = point.HasSum ? point.Sum : null,
                                Min = point.HasMin ? point.Min : null,
                                Max = point.HasMax ? point.Max : null,
                                Detail = ToJson(writer => WriteHistogram(writer, point))
                            });
                        }

                        break;
                    case Metric.DataOneofCase.ExponentialHistogram:
                        foreach (var point in metric.ExponentialHistogram.DataPoints)
                        {
                            rejected += AddPoint(batch, metrics, metric, scopeName, TelemetryMetricKind.ExponentialHistogram, (TelemetryTemporality)metric.ExponentialHistogram.AggregationTemporality, false, point.Attributes, point.Flags, point.TimeUnixNano, new TelemetryMetricPointEntity
                            {
                                StartTimeUnixNano = (long)point.StartTimeUnixNano,
                                Count = (long)point.Count,
                                Sum = point.HasSum ? point.Sum : null,
                                Min = point.HasMin ? point.Min : null,
                                Max = point.HasMax ? point.Max : null,
                                Detail = ToJson(writer => WriteExponentialHistogram(writer, point))
                            });
                        }

                        break;
                    case Metric.DataOneofCase.Summary:
                        foreach (var point in metric.Summary.DataPoints)
                        {
                            rejected += AddPoint(batch, metrics, metric, scopeName, TelemetryMetricKind.Summary, TelemetryTemporality.Unspecified, false, point.Attributes, point.Flags, point.TimeUnixNano, new TelemetryMetricPointEntity
                            {
                                StartTimeUnixNano = (long)point.StartTimeUnixNano,
                                Count = (long)point.Count,
                                Sum = point.Sum,
                                Detail = ToJson(writer => WriteSummary(writer, point))
                            });
                        }

                        break;
                }
            }
        }

        return rejected;
    }

    private static int AddNumber(TelemetryBatch batch, Dictionary<string, TelemetryMetric> metrics, Metric metric, string scopeName, TelemetryTemporality temporality, bool isMonotonic, NumberDataPoint point)
    {
        var kind = metric.DataCase == Metric.DataOneofCase.Sum ? TelemetryMetricKind.Sum : TelemetryMetricKind.Gauge;
        if (point.ValueCase == NumberDataPoint.ValueOneofCase.None)
        {
            return (point.Flags & NoRecordedValue) != 0 ? 0 : 1;
        }

        return AddPoint(batch, metrics, metric, scopeName, kind, temporality, isMonotonic, point.Attributes, point.Flags, point.TimeUnixNano, new TelemetryMetricPointEntity
        {
            StartTimeUnixNano = (long)point.StartTimeUnixNano,
            Value = point.ValueCase == NumberDataPoint.ValueOneofCase.AsInt ? point.AsInt : point.AsDouble
        });
    }

    private static int AddPoint(TelemetryBatch batch, Dictionary<string, TelemetryMetric> metrics, Metric metric, string scopeName, TelemetryMetricKind kind, TelemetryTemporality temporality, bool isMonotonic, IEnumerable<KeyValue> attributes, uint flags, ulong time, TelemetryMetricPointEntity point)
    {
        if ((flags & NoRecordedValue) != 0)
        {
            return 0;
        }

        if (time == 0)
        {
            return 1;
        }

        var json = ToJson(attributes.OrderBy(static x => x.Key, StringComparer.Ordinal));
        var key = $"{metric.Name}\n{scopeName}\n{json}";
        if (!metrics.TryGetValue(key, out var entry))
        {
            entry = new TelemetryMetric
            {
                Series = new TelemetryMetricSeriesEntity
                {
                    Name = metric.Name,
                    ScopeName = scopeName,
                    Unit = metric.Unit,
                    Kind = kind,
                    Temporality = temporality,
                    IsMonotonic = isMonotonic,
                    AttributesJson = json
                }
            };
            metrics[key] = entry;
            batch.Metrics.Add(entry);
        }

        point.TimeUnixNano = (long)time;
        entry.Points.Add(point);
        return 0;
    }

    //--------------------------------------------------------------------------------
    // Log
    //--------------------------------------------------------------------------------

    // 時刻 (記録時刻も観測時刻も) の無いログは保存しない
    public static int AddLogs(TelemetryBatch batch, IEnumerable<ScopeLogs> scopeLogs)
    {
        var rejected = 0;
        foreach (var scope in scopeLogs)
        {
            var scopeName = scope.Scope?.Name ?? string.Empty;
            foreach (var record in scope.LogRecords)
            {
                var time = record.TimeUnixNano > 0 ? record.TimeUnixNano : record.ObservedTimeUnixNano;
                if (time == 0)
                {
                    rejected++;
                    continue;
                }

                batch.Logs.Add(new TelemetryLogEntity
                {
                    TimeUnixNano = (long)time,
                    ObservedTimeUnixNano = (long)record.ObservedTimeUnixNano,
                    SeverityNumber = (int)record.SeverityNumber,
                    SeverityText = record.SeverityText,
                    EventName = record.EventName,
                    Body = ToText(record.Body),
                    TraceId = record.TraceId.ToHex(),
                    SpanId = record.SpanId.ToHex(),
                    ScopeName = scopeName,
                    AttributesJson = ToJson(record.Attributes),
                    Hash = ToHash(record)
                });
            }
        }

        return rejected;
    }

    // 送り直しで同じバイト列になる (時刻と組で重複を見分ける)
    private static long ToHash(IMessage message)
    {
        Span<byte> hash = stackalloc byte[SHA256.HashSizeInBytes];
        SHA256.HashData(message.ToByteArray(), hash);
        return BinaryPrimitives.ReadInt64LittleEndian(hash);
    }

    // 文字列はそのまま、それ以外は JSON
    private static string ToText(AnyValue? value) =>
        value?.ValueCase switch
        {
            null or AnyValue.ValueOneofCase.None => string.Empty,
            AnyValue.ValueOneofCase.StringValue => value.StringValue,
            _ => ToJson(writer => WriteValue(writer, value))
        };

    //--------------------------------------------------------------------------------
    // Json
    //--------------------------------------------------------------------------------

    private static string ToJson(IEnumerable<KeyValue> attributes) =>
        ToJson(writer => WriteObject(writer, attributes));

    private static string ToJson(Action<Utf8JsonWriter> write)
    {
        var buffer = new ArrayBufferWriter<byte>();
        using (var writer = new Utf8JsonWriter(buffer, WriterOptions))
        {
            write(writer);
        }

        return Encoding.UTF8.GetString(buffer.WrittenSpan);
    }

    private static void WriteObject(Utf8JsonWriter writer, IEnumerable<KeyValue> attributes)
    {
        writer.WriteStartObject();
        foreach (var attribute in attributes)
        {
            writer.WritePropertyName(attribute.Key);
            WriteValue(writer, attribute.Value);
        }

        writer.WriteEndObject();
    }

    private static void WriteValue(Utf8JsonWriter writer, AnyValue? value)
    {
        switch (value?.ValueCase)
        {
            case AnyValue.ValueOneofCase.StringValue:
                writer.WriteStringValue(value.StringValue);
                break;
            case AnyValue.ValueOneofCase.BoolValue:
                writer.WriteBooleanValue(value.BoolValue);
                break;
            case AnyValue.ValueOneofCase.IntValue:
                writer.WriteNumberValue(value.IntValue);
                break;
            // JSON has no NaN / Infinity
            case AnyValue.ValueOneofCase.DoubleValue when Double.IsFinite(value.DoubleValue):
                writer.WriteNumberValue(value.DoubleValue);
                break;
            case AnyValue.ValueOneofCase.DoubleValue:
                writer.WriteStringValue(value.DoubleValue.ToString(CultureInfo.InvariantCulture));
                break;
            case AnyValue.ValueOneofCase.BytesValue:
                writer.WriteStringValue(value.BytesValue.ToHex());
                break;
            case AnyValue.ValueOneofCase.ArrayValue:
                writer.WriteStartArray();
                foreach (var item in value.ArrayValue.Values)
                {
                    WriteValue(writer, item);
                }

                writer.WriteEndArray();
                break;
            case AnyValue.ValueOneofCase.KvlistValue:
                WriteObject(writer, value.KvlistValue.Values);
                break;
            default:
                writer.WriteNullValue();
                break;
        }
    }

    private static void WriteEvents(Utf8JsonWriter writer, IEnumerable<Span.Types.Event> events)
    {
        writer.WriteStartArray();
        foreach (var e in events)
        {
            writer.WriteStartObject();
            writer.WriteNumber("time", e.TimeUnixNano);
            writer.WriteString("name", e.Name);
            writer.WritePropertyName("attributes");
            WriteObject(writer, e.Attributes);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteLinks(Utf8JsonWriter writer, IEnumerable<Span.Types.Link> links)
    {
        writer.WriteStartArray();
        foreach (var link in links)
        {
            writer.WriteStartObject();
            writer.WriteString("traceId", link.TraceId.ToHex());
            writer.WriteString("spanId", link.SpanId.ToHex());
            writer.WritePropertyName("attributes");
            WriteObject(writer, link.Attributes);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
    }

    private static void WriteHistogram(Utf8JsonWriter writer, HistogramDataPoint point)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("bounds");
        writer.WriteStartArray();
        foreach (var bound in point.ExplicitBounds)
        {
            writer.WriteNumberValue(bound);
        }

        writer.WriteEndArray();
        WriteCounts(writer, "counts", point.BucketCounts);
        writer.WriteEndObject();
    }

    private static void WriteExponentialHistogram(Utf8JsonWriter writer, ExponentialHistogramDataPoint point)
    {
        writer.WriteStartObject();
        writer.WriteNumber("scale", point.Scale);
        writer.WriteNumber("zeroCount", point.ZeroCount);
        WriteBuckets(writer, "positive", point.Positive);
        WriteBuckets(writer, "negative", point.Negative);
        writer.WriteEndObject();
    }

    private static void WriteBuckets(Utf8JsonWriter writer, string name, ExponentialHistogramDataPoint.Types.Buckets? buckets)
    {
        writer.WritePropertyName(name);
        writer.WriteStartObject();
        writer.WriteNumber("offset", buckets?.Offset ?? 0);
        WriteCounts(writer, "counts", buckets?.BucketCounts ?? Enumerable.Empty<ulong>());
        writer.WriteEndObject();
    }

    private static void WriteCounts(Utf8JsonWriter writer, string name, IEnumerable<ulong> counts)
    {
        writer.WritePropertyName(name);
        writer.WriteStartArray();
        foreach (var count in counts)
        {
            writer.WriteNumberValue(count);
        }

        writer.WriteEndArray();
    }

    private static void WriteSummary(Utf8JsonWriter writer, SummaryDataPoint point)
    {
        writer.WriteStartObject();
        writer.WritePropertyName("quantiles");
        writer.WriteStartArray();
        foreach (var quantile in point.QuantileValues)
        {
            writer.WriteStartObject();
            writer.WriteNumber("quantile", quantile.Quantile);
            writer.WriteNumber("value", quantile.Value);
            writer.WriteEndObject();
        }

        writer.WriteEndArray();
        writer.WriteEndObject();
    }
}
