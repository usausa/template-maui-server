namespace Template.MobileServer.Services;

using Template.MobileServer.Accessors;
using Template.MobileServer.Infrastructure.Telemetry;

// 保存で新しく入った分 (送り直しで重複した分は含まない)。Traces は集計し直したトレース、AddedResources / AddedSeries は新しく足した Resource と系列 (Id は保存したもの)
public sealed class TelemetrySaveResult
{
    public required string DeviceId { get; init; }

    public required TelemetryDeviceInfoEntity DeviceInfo { get; init; }

    public long ReceivedAt { get; init; }

    public IList<TelemetryResourceEntity> AddedResources { get; } = [];

    public IList<TelemetryMetricSeriesEntity> AddedSeries { get; } = [];

    public IList<TelemetryMetric> Metrics { get; } = [];

    public IList<TelemetrySpanEntity> Spans { get; } = [];

    public IList<TelemetryTraceEntity> Traces { get; } = [];

    public IList<TelemetryLogEntity> Logs { get; } = [];
}

// 保持期間の削除の結果 (削除した行の数、受信が途絶えてファイルごと削除したか)
public sealed record TelemetryDeleteResult(int Rows, bool FileDeleted);

public readonly record struct TelemetrySeriesKey(string Name, string ScopeName, string AttributesJson)
{
    public static TelemetrySeriesKey From(TelemetryMetricSeriesEntity series) => new(series.Name, series.ScopeName, series.AttributesJson);
}

// 端末ごとのテレメトリの保存と照会 (状態を持たない。書き込みを端末ごとに 1 つずつにするのは呼び出し側)
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

    // 保存済みの Resource と系列 (Id とキーだけ)。ファイルが無ければ空
    public async ValueTask<TelemetryIdView> QueryIdsAsync(string deviceId, CancellationToken cancellationToken = default)
    {
        await using var con = await Provider.OpenExistingAsync(deviceId, cancellationToken);
        if (con is null)
        {
            return new TelemetryIdView([], []);
        }

        return new TelemetryIdView(
            await TelemetryAccessor.QueryResourceAllAsync(con, cancellationToken),
            await TelemetryAccessor.QueryMetricSeriesAllAsync(con, cancellationToken));
    }

    // 1 トランザクションで保存し、新しく入った分を返す (同じ端末の書き込みと並べて呼ばない)。resourceIds / seriesIds は保存済みの Id (無いものは足す)
    public async ValueTask<TelemetrySaveResult> SaveAsync(TelemetryBatch batch, IReadOnlyDictionary<string, long> resourceIds, IReadOnlyDictionary<TelemetrySeriesKey, long> seriesIds)
    {
        var context = ContextProvider.Current;
        var receivedAt = context.Now.ToUnixTimeMilliseconds() * NanosecondsPerMillisecond;
        batch.DeviceInfo.FirstReceivedAt = receivedAt;
        batch.DeviceInfo.LastReceivedAt = receivedAt;
        batch.Resource.FirstSeenAt = receivedAt;

        await using var con = await Provider.OpenAsync(batch.DeviceId);

        var result = new TelemetrySaveResult
        {
            DeviceId = batch.DeviceId,
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

    // 起動時のキャッシュ用 (端末の情報、系列ごとの最後の点、時刻以降のエラーの件数、直近のエラー)。ファイルが無ければ null
    public async ValueTask<TelemetryDeviceSummaryView?> QuerySummaryAsync(string deviceId, IEnumerable<string> metricNames, long since, int errorLimit, CancellationToken cancellationToken = default)
    {
        await using var con = await Provider.OpenExistingAsync(deviceId, cancellationToken);
        if (con is null)
        {
            return null;
        }

        var info = await TelemetryAccessor.QueryDeviceInfoAsync(con, deviceId);
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

    // 端末の全系列と、時刻以降の点を interval (ナノ秒。1 なら点ごと) ごとに束ねた集計。ファイルが無ければ空
    public async ValueTask<TelemetryMetricHistoryView> QueryMetricHistoryAsync(string deviceId, long since, long interval, CancellationToken cancellationToken = default)
    {
        await using var con = await Provider.OpenExistingAsync(deviceId, cancellationToken);
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

    // 時刻以降に始まったトレースを新しい順に limit 件。エラーのあるものだけ、ルートスパンの名前の部分一致で絞る。ファイルが無ければ空
    public async ValueTask<List<TelemetryTraceEntity>> QueryTraceListAsync(string deviceId, long since, bool errorOnly, string? name, int limit, CancellationToken cancellationToken = default)
    {
        await using var con = await Provider.OpenExistingAsync(deviceId, cancellationToken);
        if (con is null)
        {
            return [];
        }

        return await TelemetryAccessor.QueryTraceListAsync(con, since, errorOnly, Dialect.Match(name), limit, cancellationToken);
    }

    // トレースと、そのスパン・ログ・Resource (読み取りのトランザクションで同じ時点のもの)。無ければ null
    public async ValueTask<TelemetryTraceDetailView?> QueryTraceDetailAsync(string deviceId, string traceId, CancellationToken cancellationToken = default)
    {
        await using var con = await Provider.OpenExistingAsync(deviceId, cancellationToken);
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

    // 条件に合うログを新しい順に。本文は部分一致。ファイルが無ければ空
    public async ValueTask<List<TelemetryLogEntity>> QueryLogListAsync(string deviceId, TelemetryLogQuery query, CancellationToken cancellationToken = default)
    {
        await using var con = await Provider.OpenExistingAsync(deviceId, cancellationToken);
        if (con is null)
        {
            return [];
        }

        return await TelemetryAccessor.QueryLogListAsync(con, query.Since, query.Severity, Dialect.Match(query.Body), query.TraceId, query.BeforeTime, query.BeforeId, query.Limit, cancellationToken);
    }

    // ログ・スパンの Resource。無ければ null
    public async ValueTask<TelemetryResourceEntity?> QueryResourceAsync(string deviceId, long id)
    {
        await using var con = await Provider.OpenExistingAsync(deviceId);
        return con is null ? null : await TelemetryAccessor.QueryResourceAsync(con, id);
    }

    //--------------------------------------------------------------------------------
    // Retention
    //--------------------------------------------------------------------------------

    // 時刻 (UTC の Unix ナノ秒) より前の行を削除する (同じ端末の書き込みと並べて呼ばない)。最後の受信が deviceBefore より前なら、ファイルごと削除する
    public async ValueTask<TelemetryDeleteResult> DeleteExpiredAsync(string deviceId, long logBefore, long traceBefore, long metricBefore, long deviceBefore)
    {
        await using (var con = await Provider.OpenExistingAsync(deviceId))
        {
            if (con is null)
            {
                return new TelemetryDeleteResult(0, false);
            }

            var info = await TelemetryAccessor.QueryDeviceInfoAsync(con, deviceId);
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

        // The connection is closed before the file is deleted
        Provider.Delete(deviceId);
        return new TelemetryDeleteResult(0, true);
    }
}
