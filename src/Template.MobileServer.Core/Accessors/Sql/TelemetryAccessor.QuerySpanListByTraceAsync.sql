SELECT
    *
FROM
    Spans
WHERE
    TraceId = /*@ traceId */''
ORDER BY
    StartTimeUnixNano,
    SpanId
