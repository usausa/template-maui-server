DELETE FROM
    Spans
WHERE
    TraceId IN (
        SELECT
            TraceId
        FROM
            Traces
        WHERE
            StartTimeUnixNano < /*@ before */0
    )
