DELETE FROM
    Logs
WHERE
    TimeUnixNano < /*@ before */0
