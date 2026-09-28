SELECT
    *
FROM
    Log
WHERE
    TraceId = /*@ traceId */''
    AND TraceId <> ''
ORDER BY
    TimeUnixNano,
    Id
