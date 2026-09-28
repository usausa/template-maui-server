DELETE FROM
    Trace
WHERE
    StartTimeUnixNano < /*@ before */0
