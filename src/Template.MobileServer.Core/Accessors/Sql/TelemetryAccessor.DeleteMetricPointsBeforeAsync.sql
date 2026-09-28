DELETE FROM
    MetricPoints
WHERE
    TimeUnixNano < /*@ before */0
