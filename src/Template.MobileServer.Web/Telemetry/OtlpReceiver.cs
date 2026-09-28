namespace Template.MobileServer.Web.Telemetry;

using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Resource.V1;

// OTLP の受信 (gRPC と HTTP で共通)。リソースごとに端末を決め、登録を確かめて保存し、キャッシュの更新とバスへの通知を行う。保存しない項目は partial_success で返し、保存の失敗は例外のまま返す (呼び出し側が 503 / UNAVAILABLE にする)
public sealed class OtlpReceiver
{
    private const string InvalidDeviceMessage = "The resource has no valid device.id or app.installation.id.";

    private const string DisabledDeviceMessage = "The device is disabled.";

    private const string InvalidItemMessage = "Some items are invalid.";

    private readonly ILogger<OtlpReceiver> log;

    private readonly TelemetryService telemetryService;

    private readonly TelemetryDeviceRegistry registry;

    private readonly TelemetryBus bus;

    public OtlpReceiver(
        ILogger<OtlpReceiver> log,
        TelemetryService telemetryService,
        TelemetryDeviceRegistry registry,
        TelemetryBus bus)
    {
        this.log = log;
        this.telemetryService = telemetryService;
        this.registry = registry;
        this.bus = bus;
    }

    // Retryable failures of the storage
    public static bool IsStorageError(Exception ex) => ex is DbException or IOException;

    //--------------------------------------------------------------------------------
    // Trace
    //--------------------------------------------------------------------------------

    public async ValueTask<ExportTraceServiceResponse> ReceiveAsync(ExportTraceServiceRequest request, CancellationToken cancellationToken)
    {
        var rejected = new Rejected();
        foreach (var resourceSpans in request.ResourceSpans)
        {
            var count = resourceSpans.ScopeSpans.Sum(static x => x.Spans.Count);
            if (!TryGetDeviceId(resourceSpans.Resource, count, rejected, out var deviceId))
            {
                continue;
            }

            var batch = OtlpMapper.CreateBatch(deviceId, resourceSpans.Resource);
            var invalid = OtlpMapper.AddSpans(batch, resourceSpans.ScopeSpans);

            var result = await SaveAsync(batch, cancellationToken);
            if (result is null)
            {
                log.DebugTelemetryDisabled(batch.DeviceInfo.ServiceName, deviceId, count);
                rejected.Add(count, DisabledDeviceMessage);
                continue;
            }

            rejected.AddInvalid(invalid);
            log.DebugTracesReceived(batch.DeviceInfo.ServiceName, deviceId, count, result.Spans.Count);
        }

        return new ExportTraceServiceResponse
        {
            PartialSuccess = rejected.Count > 0 ? new ExportTracePartialSuccess { RejectedSpans = rejected.Count, ErrorMessage = rejected.Message } : null
        };
    }

    //--------------------------------------------------------------------------------
    // Metrics
    //--------------------------------------------------------------------------------

    public async ValueTask<ExportMetricsServiceResponse> ReceiveAsync(ExportMetricsServiceRequest request, CancellationToken cancellationToken)
    {
        var rejected = new Rejected();
        foreach (var resourceMetrics in request.ResourceMetrics)
        {
            var count = resourceMetrics.ScopeMetrics.Sum(static x => x.Metrics.Sum(static y => y.GetPointCount()));
            if (!TryGetDeviceId(resourceMetrics.Resource, count, rejected, out var deviceId))
            {
                continue;
            }

            var batch = OtlpMapper.CreateBatch(deviceId, resourceMetrics.Resource);
            var invalid = OtlpMapper.AddMetrics(batch, resourceMetrics.ScopeMetrics);

            var result = await SaveAsync(batch, cancellationToken);
            if (result is null)
            {
                log.DebugTelemetryDisabled(batch.DeviceInfo.ServiceName, deviceId, count);
                rejected.Add(count, DisabledDeviceMessage);
                continue;
            }

            rejected.AddInvalid(invalid);
            if (log.IsEnabled(LogLevel.Debug))
            {
                log.DebugMetricsReceived(batch.DeviceInfo.ServiceName, deviceId, count, result.Metrics.Sum(static x => x.Points.Count));
            }
        }

        return new ExportMetricsServiceResponse
        {
            PartialSuccess = rejected.Count > 0 ? new ExportMetricsPartialSuccess { RejectedDataPoints = rejected.Count, ErrorMessage = rejected.Message } : null
        };
    }

    //--------------------------------------------------------------------------------
    // Logs
    //--------------------------------------------------------------------------------

    public async ValueTask<ExportLogsServiceResponse> ReceiveAsync(ExportLogsServiceRequest request, CancellationToken cancellationToken)
    {
        var rejected = new Rejected();
        foreach (var resourceLogs in request.ResourceLogs)
        {
            var count = resourceLogs.ScopeLogs.Sum(static x => x.LogRecords.Count);
            if (!TryGetDeviceId(resourceLogs.Resource, count, rejected, out var deviceId))
            {
                continue;
            }

            var batch = OtlpMapper.CreateBatch(deviceId, resourceLogs.Resource);
            var invalid = OtlpMapper.AddLogs(batch, resourceLogs.ScopeLogs);

            var result = await SaveAsync(batch, cancellationToken);
            if (result is null)
            {
                log.DebugTelemetryDisabled(batch.DeviceInfo.ServiceName, deviceId, count);
                rejected.Add(count, DisabledDeviceMessage);
                continue;
            }

            rejected.AddInvalid(invalid);
            log.DebugLogsReceived(batch.DeviceInfo.ServiceName, deviceId, count, result.Logs.Count);
        }

        return new ExportLogsServiceResponse
        {
            PartialSuccess = rejected.Count > 0 ? new ExportLogsPartialSuccess { RejectedLogRecords = rejected.Count, ErrorMessage = rejected.Message } : null
        };
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private bool TryGetDeviceId(Resource? resource, int count, Rejected rejected, out string deviceId)
    {
        deviceId = resource.GetDeviceId();
        if (DeviceIdFormat.IsValid(deviceId))
        {
            return true;
        }

        log.WarnTelemetryRejected(resource.GetServiceName(), deviceId, count);
        rejected.Add(count, InvalidDeviceMessage);
        return false;
    }

    // 登録を確かめて保存し、キャッシュを更新して通知する。無効の端末は null
    private async ValueTask<TelemetrySaveResult?> SaveAsync(TelemetryBatch batch, CancellationToken cancellationToken)
    {
        try
        {
            if (!await registry.EnsureRegisteredAsync(batch.DeviceInfo, cancellationToken))
            {
                return null;
            }

            var result = await telemetryService.SaveAsync(batch, cancellationToken);
            registry.Apply(result);
            bus.PublishReceived(result);
            return result;
        }
        catch (Exception ex) when (IsStorageError(ex))
        {
            log.ErrorTelemetrySaveFailed(batch.DeviceId, ex);
            throw;
        }
    }

    // 保存しない項目の数と理由 (理由は最初のもの)
    private sealed class Rejected
    {
        public long Count { get; private set; }

        public string? Message { get; private set; }

        public void Add(int count, string message)
        {
            Count += count;
            Message ??= message;
        }

        public void AddInvalid(int count)
        {
            if (count > 0)
            {
                Add(count, InvalidItemMessage);
            }
        }
    }
}
