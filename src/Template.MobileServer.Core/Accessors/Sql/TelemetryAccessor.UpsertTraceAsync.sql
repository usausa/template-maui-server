INSERT INTO
    Traces (TraceId, RootName, StartTimeUnixNano, EndTimeUnixNano, SpanCount, ErrorCount)
SELECT
    TraceId,
    COALESCE(MAX(CASE WHEN ParentSpanId = '' THEN Name END), (
        SELECT
            Name
        FROM
            Spans
        WHERE
            TraceId = /*@ traceId */''
        ORDER BY
            StartTimeUnixNano
        LIMIT 1
    )),
    MIN(StartTimeUnixNano),
    MAX(EndTimeUnixNano),
    COUNT(*),
    SUM(CASE WHEN StatusCode = 'Error' THEN 1 ELSE 0 END)
FROM
    Spans
WHERE
    TraceId = /*@ traceId */''
GROUP BY
    TraceId
ON CONFLICT (TraceId) DO UPDATE SET
    RootName = excluded.RootName,
    StartTimeUnixNano = excluded.StartTimeUnixNano,
    EndTimeUnixNano = excluded.EndTimeUnixNano,
    SpanCount = excluded.SpanCount,
    ErrorCount = excluded.ErrorCount
