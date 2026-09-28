SELECT
    *
FROM
    Span
WHERE
    TraceId = /*@ traceId */''
ORDER BY
    StartTimeUnixNano,
    SpanId
