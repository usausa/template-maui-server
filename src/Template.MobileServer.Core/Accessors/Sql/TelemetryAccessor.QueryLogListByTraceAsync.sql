SELECT
    *
FROM
    Logs
WHERE
    TraceId = /*@ traceId */''
    AND TraceId <> ''
ORDER BY
    TimeUnixNano,
    Id
