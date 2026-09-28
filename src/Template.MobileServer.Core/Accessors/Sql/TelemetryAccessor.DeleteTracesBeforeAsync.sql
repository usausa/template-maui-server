DELETE FROM
    Traces
WHERE
    StartTimeUnixNano < /*@ before */0
