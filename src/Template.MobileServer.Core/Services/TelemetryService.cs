namespace Template.MobileServer.Services;

using Template.MobileServer.Accessors;
using Template.MobileServer.Infrastructure.Telemetry;

public sealed class TelemetrySaveResult
{
    public required string TerminalId { get; init; }

    public required TelemetryDeviceInfoEntity DeviceInfo { get; init; }

    public long ReceivedAt { get; init; }

    public IList<TelemetryResourceEntity> AddedResources { get; } = [];

    public IList<TelemetryMetricSeriesEntity> AddedSeries { get; } = [];

    public IList<TelemetryMetric> Metrics { get; } = [];

    public IList<TelemetrySpanEntity> Spans { get; } = [];

    public IList<TelemetryTraceEntity> Traces { get; } = [];

    public IList<TelemetryLogEntity> Logs { get; } = [];
}

public sealed record TelemetryDeleteResult(int Rows, bool FileDeleted);

public readonly record struct TelemetrySeriesKey(string Name, string ScopeName, string AttributesJson)
{
    public static TelemetrySeriesKey From(TelemetryMetricSeriesEntity series) => new(series.Name, series.ScopeName, series.AttributesJson);
}

public sealed class TelemetryService
{
    private const long NanosecondsPerMillisecond = 1_000_000;

    private IDialect Dialect { get; }

    private ITelemetryDbProvider Provider { get; }

    private TelemetryAccessor TelemetryAccessor { get; }

    private ServiceContextProvider ContextProvider { get; }

    public TelemetryService(
        IDialect dialect,
        ITelemetryDbProvider provider,
        TelemetryAccessor telemetryAccessor,
        ServiceContextProvider contextProvider)
    {
        Dialect = dialect;
        Provider = provider;
        TelemetryAccessor = telemetryAccessor;
        ContextProvider = contextProvider;
    }

    //--------------------------------------------------------------------------------
    // Save
    //--------------------------------------------------------------------------------

    public async ValueTask<TelemetryIdView> QueryIdsAsync(string terminalId, CancellationToken cancellationToken = default)
    {
        await using var con = await Provider.OpenExistingAsync(terminalId, cancellationToken);
        if (con is null)
        {
            return new TelemetryIdView([], []);
        }

        return new TelemetryIdView(
            await TelemetryAccessor.QueryResourceAllAsync(con, cancellationToken),
            await TelemetryAccessor.QueryMetricSeriesAllAsync(con, cancellationToken));
    }

    public async ValueTask<TelemetrySaveResult> SaveAsync(TelemetryBatch batch, IReadOnlyDictionary<string, long> resourceIds, IReadOnlyDictionary<TelemetrySeriesKey, long> seriesIds)
    {
        var context = ContextProvider.Current;
        var receivedAt = context.Now.ToUnixTimeMilliseconds() * NanosecondsPerMillisecond;
        batch.DeviceInfo.FirstReceivedAt = receivedAt;
        batch.DeviceInfo.LastReceivedAt = receivedAt;
        batch.Resource.FirstSeenAt = receivedAt;

        await using var con = await Provider.OpenAsync(batch.TerminalId);

        var result = new TelemetrySaveResult
        {
            TerminalId = batch.TerminalId,
            DeviceInfo = batch.DeviceInfo,
            ReceivedAt = receivedAt
        };
        await using var tx = await con.BeginTransactionAsync();

        await TelemetryAccessor.UpsertDeviceInfoAsync(tx, batch.DeviceInfo);

        // Resource
        if (resourceIds.TryGetValue(batch.Resource.Hash, out var resourceId))
        {
            batch.Resource.Id = resourceId;
        }
        else
        {
            batch.Resource.Id = await TelemetryAccessor.InsertResourceAsync(tx, batch.Resource);
            result.AddedResources.Add(batch.Resource);
        }

        // Metric
        foreach (var metric in batch.Metrics)
        {
            var entity = metric.Series;
            if (seriesIds.TryGetValue(TelemetrySeriesKey.From(entity), out var seriesId))
            {
                entity.Id = seriesId;
            }
            else
            {
                entity.Id = await TelemetryAccessor.InsertMetricSeriesAsync(tx, entity.Name, entity.ScopeName, entity.Unit, entity.Kind, entity.Temporality, entity.IsMonotonic, entity.AttributesJson);
                result.AddedSeries.Add(entity);
            }

            var saved = default(TelemetryMetric);
            foreach (var point in metric.Points)
            {
                point.SeriesId = entity.Id;
                if (await TelemetryAccessor.InsertMetricPointAsync(tx, point) > 0)
                {
                    saved ??= new TelemetryMetric { Series = entity };
                    saved.Points.Add(point);
                }
            }

            if (saved is not null)
            {
                result.Metrics.Add(saved);
            }
        }

        // Trace
        var traceIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var span in batch.Spans)
        {
            span.ResourceId = batch.Resource.Id;
            if (await TelemetryAccessor.InsertSpanAsync(tx, span.TraceId, span.SpanId, span.ParentSpanId, span.Name, span.Kind, span.StartTimeUnixNano, span.EndTimeUnixNano, span.StatusCode, span.StatusMessage, span.ScopeName, span.ResourceId, span.AttributesJson, span.EventsJson, span.LinksJson) > 0)
            {
                result.Spans.Add(span);
                traceIds.Add(span.TraceId);
            }
        }

        foreach (var traceId in traceIds)
        {
            await TelemetryAccessor.UpsertTraceAsync(tx, traceId);
            if (await TelemetryAccessor.QueryTraceAsync(tx, traceId) is { } trace)
            {
                result.Traces.Add(trace);
            }
        }

        // Log
        foreach (var log in batch.Logs)
        {
            log.ResourceId = batch.Resource.Id;
            if (await TelemetryAccessor.InsertLogAsync(tx, log) is { } id)
            {
                log.Id = id;
                result.Logs.Add(log);
            }
        }

        await tx.CommitAsync();

        return result;
    }

    //--------------------------------------------------------------------------------
    // Device
    //--------------------------------------------------------------------------------

    public async ValueTask<TelemetryDeviceSummaryView?> QuerySummaryAsync(string terminalId, IEnumerable<string> metricNames, long since, int errorLimit, CancellationToken cancellationToken = default)
    {
        await using var con = await Provider.OpenExistingAsync(terminalId, cancellationToken);
        if (con is null)
        {
            return null;
        }

        var info = await TelemetryAccessor.QueryDeviceInfoAsync(con, terminalId);
        if (info is null)
        {
            return null;
        }

        return new TelemetryDeviceSummaryView(
            info,
            await TelemetryAccessor.QueryLatestValueListAsync(con, metricNames, cancellationToken),
            await TelemetryAccessor.QueryLogSummaryAsync(con, since, cancellationToken),
            await TelemetryAccessor.QueryLogListBySeverityAsync(con, TelemetrySeverity.Error, errorLimit, cancellationToken));
    }

    //--------------------------------------------------------------------------------
    // Metric
    //--------------------------------------------------------------------------------

    public async ValueTask<TelemetryMetricHistoryView> QueryMetricHistoryAsync(string terminalId, long since, long interval, CancellationToken cancellationToken = default)
    {
        await using var con = await Provider.OpenExistingAsync(terminalId, cancellationToken);
        if (con is null)
        {
            return new TelemetryMetricHistoryView([], []);
        }

        return new TelemetryMetricHistoryView(
            await TelemetryAccessor.QueryMetricSeriesAllAsync(con, cancellationToken),
            await TelemetryAccessor.QueryMetricBucketListAsync(con, since, interval, cancellationToken));
    }

    //--------------------------------------------------------------------------------
    // Trace
    //--------------------------------------------------------------------------------

    public async ValueTask<List<TelemetryTraceEntity>> QueryTraceListAsync(string terminalId, long since, bool errorOnly, string? name, int limit, CancellationToken cancellationToken = default)
    {
        await using var con = await Provider.OpenExistingAsync(terminalId, cancellationToken);
        if (con is null)
        {
            return [];
        }

        return await TelemetryAccessor.QueryTraceListAsync(con, since, errorOnly, Dialect.Match(name), limit, cancellationToken);
    }

    public async ValueTask<TelemetryTraceDetailView?> QueryTraceDetailAsync(string terminalId, string traceId, CancellationToken cancellationToken = default)
    {
        await using var con = await Provider.OpenExistingAsync(terminalId, cancellationToken);
        if (con is null)
        {
            return null;
        }

        await using var tx = await con.BeginTransactionAsync(cancellationToken);
        var trace = await TelemetryAccessor.QueryTraceAsync(tx, traceId);
        if (trace is null)
        {
            return null;
        }

        var spans = await TelemetryAccessor.QuerySpanListByTraceAsync(tx, traceId, cancellationToken);
        return new TelemetryTraceDetailView(
            trace,
            spans,
            await TelemetryAccessor.QueryLogListByTraceAsync(tx, traceId, cancellationToken),
            await TelemetryAccessor.QueryResourceListAsync(tx, spans.Select(static x => x.ResourceId).Distinct(), cancellationToken));
    }

    //--------------------------------------------------------------------------------
    // Log
    //--------------------------------------------------------------------------------

    public async ValueTask<List<TelemetryLogEntity>> QueryLogListAsync(string terminalId, TelemetryLogQuery query, CancellationToken cancellationToken = default)
    {
        await using var con = await Provider.OpenExistingAsync(terminalId, cancellationToken);
        if (con is null)
        {
            return [];
        }

        return await TelemetryAccessor.QueryLogListAsync(con, query.Since, query.Severity, Dialect.Match(query.Body), query.TraceId, query.BeforeTime, query.BeforeId, query.Limit, cancellationToken);
    }

    public async ValueTask<TelemetryResourceEntity?> QueryResourceAsync(string terminalId, long id)
    {
        await using var con = await Provider.OpenExistingAsync(terminalId);
        return con is null ? null : await TelemetryAccessor.QueryResourceAsync(con, id);
    }

    //--------------------------------------------------------------------------------
    // Retention
    //--------------------------------------------------------------------------------

    public async ValueTask<TelemetryDeleteResult> DeleteExpiredAsync(string terminalId, long logBefore, long traceBefore, long metricBefore, long deviceBefore)
    {
        await using (var con = await Provider.OpenExistingAsync(terminalId))
        {
            if (con is null)
            {
                return new TelemetryDeleteResult(0, false);
            }

            var info = await TelemetryAccessor.QueryDeviceInfoAsync(con, terminalId);
            if ((info is not null) && (info.LastReceivedAt >= deviceBefore))
            {
                await using var tx = await con.BeginTransactionAsync();
                var rows = await TelemetryAccessor.DeleteLogsBeforeAsync(tx, logBefore);
                rows += await TelemetryAccessor.DeleteSpansBeforeAsync(tx, traceBefore);
                rows += await TelemetryAccessor.DeleteTracesBeforeAsync(tx, traceBefore);
                rows += await TelemetryAccessor.DeleteMetricPointsBeforeAsync(tx, metricBefore);
                await tx.CommitAsync();
                return new TelemetryDeleteResult(rows, false);
            }
        }

        Provider.Delete(terminalId);
        return new TelemetryDeleteResult(0, true);
    }
}
