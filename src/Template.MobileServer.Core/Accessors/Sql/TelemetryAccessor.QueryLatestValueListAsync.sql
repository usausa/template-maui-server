SELECT
    s.Name,
    s.AttributesJson,
    p.TimeUnixNano,
    p.Value
FROM
    MetricSeries s
    JOIN MetricPoint p ON p.SeriesId = s.Id
WHERE
    s.Name IN /*@ names */('')
    AND p.TimeUnixNano = (
        SELECT
            MAX(TimeUnixNano)
        FROM
            MetricPoint
        WHERE
            SeriesId = s.Id
    )
