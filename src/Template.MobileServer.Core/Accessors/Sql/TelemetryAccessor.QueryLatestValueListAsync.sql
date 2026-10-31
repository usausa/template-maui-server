SELECT
    T.Name,
    T.AttributesJson,
    T1.TimeUnixNano,
    T1.Value
FROM
    MetricSeries T
    JOIN MetricPoint T1 ON T1.SeriesId = T.Id
WHERE
    T.Name IN /*@ names */('')
    AND T1.TimeUnixNano = (
        SELECT
            MAX(TimeUnixNano)
        FROM
            MetricPoint
        WHERE
            SeriesId = T.Id
    )
