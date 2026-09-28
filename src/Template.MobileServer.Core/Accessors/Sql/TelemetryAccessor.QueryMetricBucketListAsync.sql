SELECT
    SeriesId,
    TimeUnixNano / /*@ interval */1 * /*@ interval */1 AS BucketUnixNano,
    COUNT(*) AS Points,
    COUNT(Value) AS ValueCount,
    SUM(Value) AS TotalValue,
    SUM(TimeUnixNano - StartTimeUnixNano) AS Duration,
    SUM(Count) AS Count,
    SUM(Sum) AS Sum,
    MAX(Max) AS Max,
    MAX(TimeUnixNano) AS LastUnixNano
FROM
    MetricPoint
WHERE
    TimeUnixNano >= /*@ since */0
GROUP BY
    SeriesId,
    BucketUnixNano
ORDER BY
    SeriesId,
    BucketUnixNano
