DELETE FROM
    MetricPoint
WHERE
    TimeUnixNano < /*@ before */0
