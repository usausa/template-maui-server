namespace Template.MobileServer.Services;

using System.Collections.Concurrent;

using Template.MobileServer.Accessors;
using Template.MobileServer.Infrastructure.Telemetry;

// 保存で新しく入った分 (送り直しで重複した分は含まない)。Traces は集計し直したトレース
public sealed class TelemetrySaveResult
{
    public required string DeviceId { get; init; }

    public required TelemetryDeviceInfoEntity DeviceInfo { get; init; }

    public long ReceivedAt { get; init; }

    public IList<TelemetryMetric> Metrics { get; } = [];

    public IList<TelemetrySpanEntity> Spans { get; } = [];

    public IList<TelemetryTraceEntity> Traces { get; } = [];

    public IList<TelemetryLogEntity> Logs { get; } = [];
}

// 保持期間の削除の結果 (削除した行の数、受信が途絶えてファイルごと削除したか)
public sealed record TelemetryDeleteResult(int Rows, bool FileDeleted);

// 端末ごとのテレメトリの保存と照会。書き込みは端末ごとに 1 つずつ行う
public sealed class TelemetryService : IDisposable
{
    private const long NanosecondsPerMillisecond = 1_000_000;

    private readonly ConcurrentDictionary<string, DeviceEntry> entries = new(StringComparer.Ordinal);

    private readonly IDialect dialect;

    private readonly ITelemetryDbProvider provider;

    private readonly TelemetryAccessor telemetryAccessor;

    private readonly ServiceContextProvider contextProvider;

    public TelemetryService(
        IDialect dialect,
        ITelemetryDbProvider provider,
        TelemetryAccessor telemetryAccessor,
        ServiceContextProvider contextProvider)
    {
        this.dialect = dialect;
        this.provider = provider;
        this.telemetryAccessor = telemetryAccessor;
        this.contextProvider = contextProvider;
    }

    public void Dispose()
    {
        foreach (var entry in entries.Values)
        {
            entry.Dispose();
        }

        entries.Clear();
    }

    //--------------------------------------------------------------------------------
    // Save
    //--------------------------------------------------------------------------------

    // 1 トランザクションで保存し、新しく入った分を返す
    public async ValueTask<TelemetrySaveResult> SaveAsync(TelemetryBatch batch, CancellationToken cancellationToken = default)
    {
        var context = contextProvider.Current;
        var receivedAt = context.Now.ToUnixTimeMilliseconds() * NanosecondsPerMillisecond;
        batch.DeviceInfo.FirstReceivedAt = receivedAt;
        batch.DeviceInfo.LastReceivedAt = receivedAt;
        batch.Resource.FirstSeenAt = receivedAt;

        var entry = entries.GetOrAdd(batch.DeviceId, static _ => new DeviceEntry());
        await entry.Lock.WaitAsync(cancellationToken);
        try
        {
            await using var con = await provider.OpenAsync(batch.DeviceId, cancellationToken);
            var resources = entry.Resources ??= (await telemetryAccessor.QueryResourceAllAsync(con, cancellationToken))
                .ToDictionary(static x => x.Hash, static x => x.Id, StringComparer.Ordinal);
            var series = entry.Series ??= (await telemetryAccessor.QueryMetricSeriesAllAsync(con, cancellationToken))
                .ToDictionary(MakeSeriesKey, static x => x.Id, StringComparer.Ordinal);

            var result = new TelemetrySaveResult
            {
                DeviceId = batch.DeviceId,
                DeviceInfo = batch.DeviceInfo,
                ReceivedAt = receivedAt
            };
            var addedResources = new List<TelemetryResourceEntity>();
            var addedSeries = new List<TelemetryMetricSeriesEntity>();

            await using var tx = await con.BeginTransactionAsync(cancellationToken);

            await telemetryAccessor.UpsertDeviceInfoAsync(tx, batch.DeviceInfo, cancellationToken);

            // Resource
            if (resources.TryGetValue(batch.Resource.Hash, out var resourceId))
            {
                batch.Resource.Id = resourceId;
            }
            else
            {
                batch.Resource.Id = await telemetryAccessor.InsertResourceAsync(tx, batch.Resource, cancellationToken);
                addedResources.Add(batch.Resource);
            }

            // Metric
            foreach (var metric in batch.Metrics)
            {
                var entity = metric.Series;
                if (series.TryGetValue(MakeSeriesKey(entity), out var seriesId))
                {
                    entity.Id = seriesId;
                }
                else
                {
                    entity.Id = await telemetryAccessor.InsertMetricSeriesAsync(tx, entity.Name, entity.ScopeName, entity.Unit, entity.Kind, entity.Temporality, entity.IsMonotonic, entity.AttributesJson, cancellationToken);
                    addedSeries.Add(entity);
                }

                var saved = default(TelemetryMetric);
                foreach (var point in metric.Points)
                {
                    point.SeriesId = entity.Id;
                    if (await telemetryAccessor.InsertMetricPointAsync(tx, point, cancellationToken) > 0)
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
                if (await telemetryAccessor.InsertSpanAsync(tx, span.TraceId, span.SpanId, span.ParentSpanId, span.Name, span.Kind, span.StartTimeUnixNano, span.EndTimeUnixNano, span.StatusCode, span.StatusMessage, span.ScopeName, span.ResourceId, span.AttributesJson, span.EventsJson, span.LinksJson, cancellationToken) > 0)
                {
                    result.Spans.Add(span);
                    traceIds.Add(span.TraceId);
                }
            }

            foreach (var traceId in traceIds)
            {
                await telemetryAccessor.UpsertTraceAsync(tx, traceId, cancellationToken);
                if (await telemetryAccessor.QueryTraceAsync(tx, traceId, cancellationToken) is { } trace)
                {
                    result.Traces.Add(trace);
                }
            }

            // Log
            foreach (var log in batch.Logs)
            {
                log.ResourceId = batch.Resource.Id;
                if (await telemetryAccessor.InsertLogAsync(tx, log, cancellationToken) is { } id)
                {
                    log.Id = id;
                    result.Logs.Add(log);
                }
            }

            await tx.CommitAsync(cancellationToken);

            // The ids are kept after the commit only (a rolled back insert would leave an unknown id)
            foreach (var resource in addedResources)
            {
                resources[resource.Hash] = resource.Id;
            }

            foreach (var added in addedSeries)
            {
                series[MakeSeriesKey(added)] = added.Id;
            }

            return result;
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    //--------------------------------------------------------------------------------
    // Device
    //--------------------------------------------------------------------------------

    public IReadOnlyList<string> EnumerateDevices() => provider.EnumerateDevices();

    // 起動時のキャッシュ用 (端末の情報、系列ごとの最後の点、時刻以降のエラーの件数、直近のエラー)。ファイルが無ければ null
    public async ValueTask<TelemetryDeviceSummaryView?> QuerySummaryAsync(string deviceId, IEnumerable<string> metricNames, long since, int errorLimit, CancellationToken cancellationToken = default)
    {
        await using var con = await provider.OpenExistingAsync(deviceId, cancellationToken);
        if (con is null)
        {
            return null;
        }

        var info = await telemetryAccessor.QueryDeviceInfoAsync(con, deviceId, cancellationToken);
        if (info is null)
        {
            return null;
        }

        return new TelemetryDeviceSummaryView(
            info,
            await telemetryAccessor.QueryLatestValueListAsync(con, metricNames, cancellationToken),
            await telemetryAccessor.QueryLogSummaryAsync(con, since, cancellationToken),
            await telemetryAccessor.QueryLogListBySeverityAsync(con, TelemetrySeverity.Error, errorLimit, cancellationToken));
    }

    //--------------------------------------------------------------------------------
    // Metric
    //--------------------------------------------------------------------------------

    // 端末の全系列と、時刻以降の点を interval (ナノ秒。1 なら点ごと) ごとに束ねた集計。ファイルが無ければ空
    public async ValueTask<TelemetryMetricHistoryView> QueryMetricHistoryAsync(string deviceId, long since, long interval, CancellationToken cancellationToken = default)
    {
        await using var con = await provider.OpenExistingAsync(deviceId, cancellationToken);
        if (con is null)
        {
            return new TelemetryMetricHistoryView([], []);
        }

        return new TelemetryMetricHistoryView(
            await telemetryAccessor.QueryMetricSeriesAllAsync(con, cancellationToken),
            await telemetryAccessor.QueryMetricBucketListAsync(con, since, interval, cancellationToken));
    }

    //--------------------------------------------------------------------------------
    // Trace
    //--------------------------------------------------------------------------------

    // 時刻以降に始まったトレースを新しい順に limit 件。エラーのあるものだけ、ルートスパンの名前の部分一致で絞る。ファイルが無ければ空
    public async ValueTask<List<TelemetryTraceEntity>> QueryTraceListAsync(string deviceId, long since, bool errorOnly, string? name, int limit, CancellationToken cancellationToken = default)
    {
        await using var con = await provider.OpenExistingAsync(deviceId, cancellationToken);
        if (con is null)
        {
            return [];
        }

        return await telemetryAccessor.QueryTraceListAsync(con, since, errorOnly, dialect.Match(name), limit, cancellationToken);
    }

    // トレースと、そのスパン・ログ・Resource (読み取りのトランザクションで同じ時点のもの)。無ければ null
    public async ValueTask<TelemetryTraceDetailView?> QueryTraceDetailAsync(string deviceId, string traceId, CancellationToken cancellationToken = default)
    {
        await using var con = await provider.OpenExistingAsync(deviceId, cancellationToken);
        if (con is null)
        {
            return null;
        }

        await using var tx = await con.BeginTransactionAsync(cancellationToken);
        var trace = await telemetryAccessor.QueryTraceAsync(tx, traceId, cancellationToken);
        if (trace is null)
        {
            return null;
        }

        var spans = await telemetryAccessor.QuerySpanListByTraceAsync(tx, traceId, cancellationToken);
        return new TelemetryTraceDetailView(
            trace,
            spans,
            await telemetryAccessor.QueryLogListByTraceAsync(tx, traceId, cancellationToken),
            await telemetryAccessor.QueryResourceListAsync(tx, spans.Select(static x => x.ResourceId).Distinct(), cancellationToken));
    }

    //--------------------------------------------------------------------------------
    // Log
    //--------------------------------------------------------------------------------

    // 条件に合うログを新しい順に。本文は部分一致。ファイルが無ければ空
    public async ValueTask<List<TelemetryLogEntity>> QueryLogListAsync(string deviceId, TelemetryLogQuery query, CancellationToken cancellationToken = default)
    {
        await using var con = await provider.OpenExistingAsync(deviceId, cancellationToken);
        if (con is null)
        {
            return [];
        }

        return await telemetryAccessor.QueryLogListAsync(con, query.Since, query.Severity, dialect.Match(query.Body), query.TraceId, query.BeforeTime, query.BeforeId, query.Limit, cancellationToken);
    }

    // ログ・スパンの Resource。無ければ null
    public async ValueTask<TelemetryResourceEntity?> QueryResourceAsync(string deviceId, long id, CancellationToken cancellationToken = default)
    {
        await using var con = await provider.OpenExistingAsync(deviceId, cancellationToken);
        return con is null ? null : await telemetryAccessor.QueryResourceAsync(con, id, cancellationToken);
    }

    //--------------------------------------------------------------------------------
    // Delete
    //--------------------------------------------------------------------------------

    // 端末のファイルを削除する (書き込みと同じロックの中。ファイルが無ければ false)
    public async ValueTask<bool> DeleteDeviceAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        var entry = entries.GetOrAdd(deviceId, static _ => new DeviceEntry());
        await entry.Lock.WaitAsync(cancellationToken);
        try
        {
            entry.Resources = null;
            entry.Series = null;
            return provider.Delete(deviceId);
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    //--------------------------------------------------------------------------------
    // Retention
    //--------------------------------------------------------------------------------

    // 時刻 (UTC の Unix ナノ秒) より前の行を削除する (書き込みと同じロックの中)。最後の受信が deviceBefore より前なら、ファイルごと削除する
    public async ValueTask<TelemetryDeleteResult> DeleteExpiredAsync(string deviceId, long logBefore, long traceBefore, long metricBefore, long deviceBefore, CancellationToken cancellationToken = default)
    {
        var entry = entries.GetOrAdd(deviceId, static _ => new DeviceEntry());
        await entry.Lock.WaitAsync(cancellationToken);
        try
        {
            await using (var con = await provider.OpenExistingAsync(deviceId, cancellationToken))
            {
                if (con is null)
                {
                    return new TelemetryDeleteResult(0, false);
                }

                var info = await telemetryAccessor.QueryDeviceInfoAsync(con, deviceId, cancellationToken);
                if ((info is not null) && (info.LastReceivedAt >= deviceBefore))
                {
                    await using var tx = await con.BeginTransactionAsync(cancellationToken);
                    var rows = await telemetryAccessor.DeleteLogsBeforeAsync(tx, logBefore, cancellationToken);
                    rows += await telemetryAccessor.DeleteSpansBeforeAsync(tx, traceBefore, cancellationToken);
                    rows += await telemetryAccessor.DeleteTracesBeforeAsync(tx, traceBefore, cancellationToken);
                    rows += await telemetryAccessor.DeleteMetricPointsBeforeAsync(tx, metricBefore, cancellationToken);
                    await tx.CommitAsync(cancellationToken);
                    return new TelemetryDeleteResult(rows, false);
                }
            }

            // The connection is closed before the file is deleted
            entry.Resources = null;
            entry.Series = null;
            provider.Delete(deviceId);
            return new TelemetryDeleteResult(0, true);
        }
        finally
        {
            entry.Lock.Release();
        }
    }

    //--------------------------------------------------------------------------------
    // Helper
    //--------------------------------------------------------------------------------

    private static string MakeSeriesKey(TelemetryMetricSeriesEntity series) =>
        $"{series.Name}\n{series.ScopeName}\n{series.AttributesJson}";

    // 端末ごとの書き込みのロックと、保存済みの Resource と系列の Id (最初の書き込みで DB から読む)
    private sealed class DeviceEntry : IDisposable
    {
        public SemaphoreSlim Lock { get; } = new(1, 1);

        public Dictionary<string, long>? Resources { get; set; }

        public Dictionary<string, long>? Series { get; set; }

        public void Dispose() => Lock.Dispose();
    }
}
