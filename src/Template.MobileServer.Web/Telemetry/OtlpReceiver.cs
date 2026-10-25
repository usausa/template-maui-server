namespace Template.MobileServer.Web.Telemetry;

using OpenTelemetry.Proto.Collector.Logs.V1;
using OpenTelemetry.Proto.Collector.Metrics.V1;
using OpenTelemetry.Proto.Collector.Trace.V1;
using OpenTelemetry.Proto.Resource.V1;

public sealed class OtlpReceiver
{
    private const string InvalidDeviceMessage = "The resource has no valid app.installation.id.";

    private const string DisabledDeviceMessage = "The device is disabled.";

    private const string InvalidItemMessage = "Some items are invalid.";

    private ILogger<OtlpReceiver> Log { get; }

    private TelemetryStore Store { get; }

    private TelemetryDeviceRegistry Registry { get; }

    private TelemetryBus Bus { get; }

    public OtlpReceiver(
        ILogger<OtlpReceiver> log,
        TelemetryStore store,
        TelemetryDeviceRegistry registry,
        TelemetryBus bus)
    {
        Log = log;
        Store = store;
        Registry = registry;
        Bus = bus;
    }

    public static bool IsStorageError(Exception ex) => ex is DbException or IOException;

    //--------------------------------------------------------------------------------
    // Trace
    //--------------------------------------------------------------------------------

    public async ValueTask<ExportTraceServiceResponse> ReceiveAsync(ExportTraceServiceRequest request)
    {
        var rejected = new Rejected();
        foreach (var resourceSpans in request.ResourceSpans)
        {
            var count = resourceSpans.ScopeSpans.Sum(static x => x.Spans.Count);
            if (!TryGetTerminalId(resourceSpans.Resource, count, rejected, out var terminalId))
            {
                continue;
            }

            var batch = OtlpMapper.CreateBatch(terminalId, resourceSpans.Resource);
            var invalid = OtlpMapper.AddSpans(batch, resourceSpans.ScopeSpans);

            var result = await SaveAsync(batch);
            if (result is null)
            {
                Log.DebugTelemetryDisabled(batch.DeviceInfo.ServiceName, terminalId, count);
                rejected.Add(count, DisabledDeviceMessage);
                continue;
            }

            rejected.AddInvalid(invalid);
            Log.DebugTracesReceived(batch.DeviceInfo.ServiceName, terminalId, count, result.Spans.Count);
        }

        return new ExportTraceServiceResponse
        {
            PartialSuccess = rejected.Count > 0 ? new ExportTracePartialSuccess { RejectedSpans = rejected.Count, ErrorMessage = rejected.Message } : null
        };
    }

    //--------------------------------------------------------------------------------
    // Metrics
    //--------------------------------------------------------------------------------

    public async ValueTask<ExportMetricsServiceResponse> ReceiveAsync(ExportMetricsServiceRequest request)
    {
        var rejected = new Rejected();
        foreach (var resourceMetrics in request.ResourceMetrics)
        {
            var count = resourceMetrics.ScopeMetrics.Sum(static x => x.Metrics.Sum(static y => y.GetPointCount()));
            if (!TryGetTerminalId(resourceMetrics.Resource, count, rejected, out var terminalId))
            {
                continue;
            }

            var batch = OtlpMapper.CreateBatch(terminalId, resourceMetrics.Resource);
            var invalid = OtlpMapper.AddMetrics(batch, resourceMetrics.ScopeMetrics);

            var result = await SaveAsync(batch);
            if (result is null)
            {
                Log.DebugTelemetryDisabled(batch.DeviceInfo.ServiceName, terminalId, count);
                rejected.Add(count, DisabledDeviceMessage);
                continue;
            }

            rejected.AddInvalid(invalid);
            if (Log.IsEnabled(LogLevel.Debug))
            {
                Log.DebugMetricsReceived(batch.DeviceInfo.ServiceName, terminalId, count, result.Metrics.Sum(static x => x.Points.Count));
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

    public async ValueTask<ExportLogsServiceResponse> ReceiveAsync(ExportLogsServiceRequest request)
    {
        var rejected = new Rejected();
        foreach (var resourceLogs in request.ResourceLogs)
        {
            var count = resourceLogs.ScopeLogs.Sum(static x => x.LogRecords.Count);
            if (!TryGetTerminalId(resourceLogs.Resource, count, rejected, out var terminalId))
            {
                continue;
            }

            var batch = OtlpMapper.CreateBatch(terminalId, resourceLogs.Resource);
            var invalid = OtlpMapper.AddLogs(batch, resourceLogs.ScopeLogs);

            var result = await SaveAsync(batch);
            if (result is null)
            {
                Log.DebugTelemetryDisabled(batch.DeviceInfo.ServiceName, terminalId, count);
                rejected.Add(count, DisabledDeviceMessage);
                continue;
            }

            rejected.AddInvalid(invalid);
            Log.DebugLogsReceived(batch.DeviceInfo.ServiceName, terminalId, count, result.Logs.Count);
        }

        return new ExportLogsServiceResponse
        {
            PartialSuccess = rejected.Count > 0 ? new ExportLogsPartialSuccess { RejectedLogRecords = rejected.Count, ErrorMessage = rejected.Message } : null
        };
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private bool TryGetTerminalId(Resource? resource, int count, Rejected rejected, out string terminalId)
    {
        terminalId = resource.GetTerminalId();
        if (TerminalIdFormat.IsValid(terminalId))
        {
            return true;
        }

        Log.WarnTelemetryRejected(resource.GetServiceName(), terminalId, count);
        rejected.Add(count, InvalidDeviceMessage);
        return false;
    }

    private async ValueTask<TelemetrySaveResult?> SaveAsync(TelemetryBatch batch)
    {
        try
        {
            if (!await Registry.EnsureRegisteredAsync(batch.DeviceInfo))
            {
                return null;
            }

            var result = await Store.SaveAsync(batch);
            Registry.Apply(result);
            Bus.PublishReceived(result);
            return result;
        }
        catch (Exception ex) when (IsStorageError(ex))
        {
            Log.ErrorTelemetrySaveFailed(batch.TerminalId, ex);
            throw;
        }
    }

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
