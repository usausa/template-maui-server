DELETE FROM
    Span
WHERE
    TraceId IN (
        SELECT
            TraceId
        FROM
            Trace
        WHERE
            StartTimeUnixNano < /*@ before */0
    )
