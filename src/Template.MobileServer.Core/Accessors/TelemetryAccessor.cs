namespace Template.MobileServer.Accessors;

// 端末ごとのテレメトリの DB (端末ごとのファイルなので、接続とトランザクションは呼び出し側が持つ)
[DataAccessor]
[ExecuteConfig(typeof(DataProfile))]
public sealed partial class TelemetryAccessor
{
    //--------------------------------------------------------------------------------
    // DeviceInfo
    //--------------------------------------------------------------------------------

    [Execute]
    public partial ValueTask<int> UpsertDeviceInfoAsync(DbTransaction tx, TelemetryDeviceInfoEntity entity, CancellationToken cancellationToken);

    [QueryFirst]
    [SelectSingle(typeof(TelemetryDeviceInfoEntity))]
    public partial ValueTask<TelemetryDeviceInfoEntity?> QueryDeviceInfoAsync(DbConnection con, string deviceId, CancellationToken cancellationToken);

    //--------------------------------------------------------------------------------
    // Resource
    //--------------------------------------------------------------------------------

    // 保存済みの Id とハッシュ (最初の書き込みでメモリに読む)
    [Query]
    public partial ValueTask<List<TelemetryResourceEntity>> QueryResourceAllAsync(DbConnection con, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<TelemetryResourceEntity>> QueryResourceListAsync(DbTransaction tx, IEnumerable<long> ids, CancellationToken cancellationToken);

    [QueryFirst]
    [SelectSingle(typeof(TelemetryResourceEntity))]
    public partial ValueTask<TelemetryResourceEntity?> QueryResourceAsync(DbConnection con, long id, CancellationToken cancellationToken);

    [ExecuteScalar]
    public partial ValueTask<long> InsertResourceAsync(DbTransaction tx, TelemetryResourceEntity entity, CancellationToken cancellationToken);

    //--------------------------------------------------------------------------------
    // Metric
    //--------------------------------------------------------------------------------

    [Query]
    public partial ValueTask<List<TelemetryMetricSeriesEntity>> QueryMetricSeriesAllAsync(DbConnection con, CancellationToken cancellationToken);

    [ExecuteScalar]
    public partial ValueTask<long> InsertMetricSeriesAsync(DbTransaction tx, string name, string scopeName, string unit, TelemetryMetricKind kind, TelemetryTemporality temporality, bool isMonotonic, string attributesJson, CancellationToken cancellationToken);

    // 同じ系列・時刻の点 (送り直し) は入れない (0 件)
    [Execute]
    public partial ValueTask<int> InsertMetricPointAsync(DbTransaction tx, TelemetryMetricPointEntity entity, CancellationToken cancellationToken);

    // 計器の名前で絞った系列ごとの最後の点
    [Query]
    public partial ValueTask<List<TelemetryLatestValueView>> QueryLatestValueListAsync(DbConnection con, IEnumerable<string> names, CancellationToken cancellationToken);

    // 時刻以降の点を系列ごと・interval (ナノ秒。1 なら点ごと) ごとに束ねる
    [Query]
    public partial ValueTask<List<TelemetryMetricBucketView>> QueryMetricBucketListAsync(DbConnection con, long since, long interval, CancellationToken cancellationToken);

    //--------------------------------------------------------------------------------
    // Trace
    //--------------------------------------------------------------------------------

    // 同じ ID のスパン (送り直し) は入れない (0 件)
    [Execute]
    public partial ValueTask<int> InsertSpanAsync(DbTransaction tx, string traceId, string spanId, string parentSpanId, string name, TelemetrySpanKind kind, long startTimeUnixNano, long endTimeUnixNano, TelemetryStatusCode statusCode, string statusMessage, string scopeName, long resourceId, string attributesJson, string? eventsJson, string? linksJson, CancellationToken cancellationToken);

    // トレースの集計を、そのトレースのスパンから作り直す
    [Execute]
    public partial ValueTask<int> UpsertTraceAsync(DbTransaction tx, string traceId, CancellationToken cancellationToken);

    [QueryFirst]
    [SelectSingle(typeof(TelemetryTraceEntity))]
    public partial ValueTask<TelemetryTraceEntity?> QueryTraceAsync(DbTransaction tx, string traceId, CancellationToken cancellationToken);

    // 時刻以降に始まったトレース (新しい順)。エラーのあるものだけ、ルートスパンの名前 (LIKE のパターン) で絞る
    [Query]
    public partial ValueTask<List<TelemetryTraceEntity>> QueryTraceListAsync(DbConnection con, long since, bool errorOnly, string? name, int limit, CancellationToken cancellationToken);

    // トレースのスパン (開始の順)
    [Query]
    public partial ValueTask<List<TelemetrySpanEntity>> QuerySpanListByTraceAsync(DbTransaction tx, string traceId, CancellationToken cancellationToken);

    //--------------------------------------------------------------------------------
    // Log
    //--------------------------------------------------------------------------------

    // 同じ時刻・内容のログ (送り直し) は入れない (null)
    [ExecuteScalar]
    public partial ValueTask<long?> InsertLogAsync(DbTransaction tx, TelemetryLogEntity entity, CancellationToken cancellationToken);

    // 時刻以降のエラーとクラッシュを 1 時間ごとに数える
    [Query]
    public partial ValueTask<List<TelemetryLogSummaryView>> QueryLogSummaryAsync(DbConnection con, long since, CancellationToken cancellationToken);

    // 重大度以上のログを新しい順に
    [Query]
    public partial ValueTask<List<TelemetryLogEntity>> QueryLogListBySeverityAsync(DbConnection con, int severity, int limit, CancellationToken cancellationToken);

    // トレースのログ (時刻の順)
    [Query]
    public partial ValueTask<List<TelemetryLogEntity>> QueryLogListByTraceAsync(DbTransaction tx, string traceId, CancellationToken cancellationToken);

    // 時刻以降・重大度以上のログを新しい順に。本文 (LIKE のパターン)・トレースで絞り、続きは時刻と Id の前から
    [Query]
    public partial ValueTask<List<TelemetryLogEntity>> QueryLogListAsync(DbConnection con, long since, int severity, string? body, string? traceId, long? beforeTime, long beforeId, int limit, CancellationToken cancellationToken);

    //--------------------------------------------------------------------------------
    // Retention
    //--------------------------------------------------------------------------------

    [Execute]
    public partial ValueTask<int> DeleteLogsBeforeAsync(DbTransaction tx, long before, CancellationToken cancellationToken);

    // トレースの開始で判定し、トレースのスパンをまとめて削除する (トレースより先に消す)
    [Execute]
    public partial ValueTask<int> DeleteSpansBeforeAsync(DbTransaction tx, long before, CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> DeleteTracesBeforeAsync(DbTransaction tx, long before, CancellationToken cancellationToken);

    [Execute]
    public partial ValueTask<int> DeleteMetricPointsBeforeAsync(DbTransaction tx, long before, CancellationToken cancellationToken);
}
