namespace Template.MobileServer.Telemetry;

using Google.Protobuf;

using OpenTelemetry.Proto.Common.V1;
using OpenTelemetry.Proto.Metrics.V1;
using OpenTelemetry.Proto.Resource.V1;

using Template.MobileServer.Web.Telemetry;

public sealed class OtlpHelperTests
{
    // 端末: device.id を優先する
    [Fact]
    public void GetDeviceIdPrefersDeviceId()
    {
        // Arrange
        var resource = CreateResource(("device.id", "device-1"), ("app.installation.id", "installation-1"));

        // Act
        var result = resource.GetDeviceId();

        // Assert
        Assert.Equal("device-1", result);
    }

    // 端末: device.id が無い (空) ときは app.installation.id
    [Fact]
    public void GetDeviceIdWithoutDeviceIdReturnsInstallationId()
    {
        // Arrange
        var resource = CreateResource(("device.id", string.Empty), ("app.installation.id", "installation-1"));

        // Act
        var result = resource.GetDeviceId();

        // Assert
        Assert.Equal("installation-1", result);
    }

    [Fact]
    public void GetServiceNameWithoutResourceReturnsEmpty()
    {
        // Act
        var result = ((Resource?)null).GetServiceName();

        // Assert
        Assert.Equal(string.Empty, result);
    }

    // 属性: 配列 / キー値リスト / バイト列を展開する。値の無い属性は空
    [Fact]
    public void FormatAttributesFormatsNestedValues()
    {
        // Arrange
        KeyValue[] attributes =
        [
            CreateAttribute("text", new AnyValue { StringValue = "a" }),
            CreateAttribute("list", new AnyValue { ArrayValue = new ArrayValue { Values = { new AnyValue { IntValue = 1 }, new AnyValue { DoubleValue = 1.5 } } } }),
            CreateAttribute("map", new AnyValue { KvlistValue = new KeyValueList { Values = { CreateAttribute("flag", new AnyValue { BoolValue = true }) } } }),
            CreateAttribute("bytes", new AnyValue { BytesValue = ByteString.CopyFrom(0x0a, 0xff) }),
            new() { Key = "empty" }
        ];

        // Act
        var result = attributes.FormatAttributes();

        // Assert
        Assert.Equal("text=a, list=[1, 1.5], map={flag=true}, bytes=0aff, empty=", result);
    }

    // 数値の点: 属性があれば値の前に付ける
    [Fact]
    public void FormatPointsFormatsNumberPoints()
    {
        // Arrange
        var metric = new Metric
        {
            Sum = new Sum
            {
                AggregationTemporality = AggregationTemporality.Delta,
                DataPoints =
                {
                    new NumberDataPoint { AsInt = 12, Attributes = { CreateAttribute("element.type", new AnyValue { StringValue = "Label" }) } },
                    new NumberDataPoint { AsDouble = 0.5 }
                }
            }
        };

        // Act
        var result = metric.FormatPoints();

        // Assert
        Assert.Equal("{element.type=Label}=12, 0.5", result);
        Assert.Equal(2, metric.GetPointCount());
        Assert.Equal(AggregationTemporality.Delta, metric.GetTemporality());
    }

    // 分布の点: 件数と合計
    [Fact]
    public void FormatPointsFormatsHistogramPoints()
    {
        // Arrange
        var metric = new Metric
        {
            Histogram = new Histogram
            {
                AggregationTemporality = AggregationTemporality.Delta,
                DataPoints = { new HistogramDataPoint { Count = 3, Sum = 0.25 } }
            }
        };

        // Act
        var result = metric.FormatPoints();

        // Assert
        Assert.Equal("count=3 sum=0.25", result);
    }

    // 期間: 終了が開始より前なら 0
    [Theory]
    [InlineData(1_000_000UL, 3_500_000UL, 25_000L)]
    [InlineData(2_000_000UL, 1_000_000UL, 0L)]
    public void ToDurationReturnsElapsedTime(ulong startUnixNano, ulong endUnixNano, long ticks)
    {
        // Act
        var result = OtlpHelper.ToDuration(startUnixNano, endUnixNano);

        // Assert
        Assert.Equal(TimeSpan.FromTicks(ticks), result);
    }

    private static Resource CreateResource(params (string Key, string Value)[] attributes)
    {
        var resource = new Resource();
        resource.Attributes.Add(attributes.Select(static x => CreateAttribute(x.Key, new AnyValue { StringValue = x.Value })));
        return resource;
    }

    private static KeyValue CreateAttribute(string key, AnyValue value) => new() { Key = key, Value = value };
}
