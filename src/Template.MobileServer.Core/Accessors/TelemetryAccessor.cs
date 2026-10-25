namespace Template.MobileServer.Accessors;

[DataAccessor]
[ExecuteConfig(typeof(DataProfile))]
public sealed partial class TelemetryAccessor
{
    //--------------------------------------------------------------------------------
    // DeviceInfo
    //--------------------------------------------------------------------------------

    [Execute]
    public partial ValueTask<int> UpsertDeviceInfoAsync(DbTransaction tx, TelemetryDeviceInfoEntity entity);

    [QueryFirst]
    [SelectSingle(typeof(TelemetryDeviceInfoEntity))]
    public partial ValueTask<TelemetryDeviceInfoEntity?> QueryDeviceInfoAsync(DbConnection con, string terminalId);

    //--------------------------------------------------------------------------------
    // Resource
    //--------------------------------------------------------------------------------

    [Query]
    public partial ValueTask<List<TelemetryResourceEntity>> QueryResourceAllAsync(DbConnection con, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<TelemetryResourceEntity>> QueryResourceListAsync(DbTransaction tx, IEnumerable<long> ids, CancellationToken cancellationToken);

    [QueryFirst]
    [SelectSingle(typeof(TelemetryResourceEntity))]
    public partial ValueTask<TelemetryResourceEntity?> QueryResourceAsync(DbConnection con, long id);

    [ExecuteScalar]
    public partial ValueTask<long> InsertResourceAsync(DbTransaction tx, TelemetryResourceEntity entity);

    //--------------------------------------------------------------------------------
    // Metric
    //--------------------------------------------------------------------------------

    [Query]
    public partial ValueTask<List<TelemetryMetricSeriesEntity>> QueryMetricSeriesAllAsync(DbConnection con, CancellationToken cancellationToken);

    [ExecuteScalar]
    public partial ValueTask<long> InsertMetricSeriesAsync(DbTransaction tx, string name, string scopeName, string unit, string kind, string temporality, bool isMonotonic, string attributesJson);

    [Execute]
    public partial ValueTask<int> InsertMetricPointAsync(DbTransaction tx, TelemetryMetricPointEntity entity);

    [Query]
    public partial ValueTask<List<TelemetryLatestValueView>> QueryLatestValueListAsync(DbConnection con, IEnumerable<string> names, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<TelemetryMetricBucketView>> QueryMetricBucketListAsync(DbConnection con, long since, long interval, CancellationToken cancellationToken);

    //--------------------------------------------------------------------------------
    // Trace
    //--------------------------------------------------------------------------------

    [Execute]
    public partial ValueTask<int> InsertSpanAsync(DbTransaction tx, string traceId, string spanId, string parentSpanId, string name, string kind, long startTimeUnixNano, long endTimeUnixNano, string statusCode, string statusMessage, string scopeName, long resourceId, string attributesJson, string? eventsJson, string? linksJson);

    [Execute]
    public partial ValueTask<int> UpsertTraceAsync(DbTransaction tx, string traceId);

    [QueryFirst]
    [SelectSingle(typeof(TelemetryTraceEntity))]
    public partial ValueTask<TelemetryTraceEntity?> QueryTraceAsync(DbTransaction tx, string traceId);

    [Query]
    public partial ValueTask<List<TelemetryTraceEntity>> QueryTraceListAsync(DbConnection con, long since, bool errorOnly, string? name, int limit, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<TelemetrySpanEntity>> QuerySpanListByTraceAsync(DbTransaction tx, string traceId, CancellationToken cancellationToken);

    //--------------------------------------------------------------------------------
    // Log
    //--------------------------------------------------------------------------------

    [ExecuteScalar]
    public partial ValueTask<long?> InsertLogAsync(DbTransaction tx, TelemetryLogEntity entity);

    [Query]
    public partial ValueTask<List<TelemetryLogSummaryView>> QueryLogSummaryAsync(DbConnection con, long since, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<TelemetryLogEntity>> QueryLogListBySeverityAsync(DbConnection con, int severity, int limit, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<TelemetryLogEntity>> QueryLogListByTraceAsync(DbTransaction tx, string traceId, CancellationToken cancellationToken);

    [Query]
    public partial ValueTask<List<TelemetryLogEntity>> QueryLogListAsync(DbConnection con, long since, int severity, string? body, string? traceId, long? beforeTime, long beforeId, int limit, CancellationToken cancellationToken);

    //--------------------------------------------------------------------------------
    // Retention
    //--------------------------------------------------------------------------------

    [Execute]
    public partial ValueTask<int> DeleteLogsBeforeAsync(DbTransaction tx, long before);

    [Execute]
    public partial ValueTask<int> DeleteSpansBeforeAsync(DbTransaction tx, long before);

    [Execute]
    public partial ValueTask<int> DeleteTracesBeforeAsync(DbTransaction tx, long before);

    [Execute]
    public partial ValueTask<int> DeleteMetricPointsBeforeAsync(DbTransaction tx, long before);
}
