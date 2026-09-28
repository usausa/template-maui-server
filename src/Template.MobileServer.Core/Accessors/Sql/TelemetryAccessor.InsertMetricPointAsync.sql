INSERT INTO
    MetricPoints (SeriesId, TimeUnixNano, StartTimeUnixNano, Value, Count, Sum, Min, Max, Detail)
VALUES
    (/*@ entity.SeriesId */0, /*@ entity.TimeUnixNano */0, /*@ entity.StartTimeUnixNano */0, /*@ entity.Value */NULL, /*@ entity.Count */NULL, /*@ entity.Sum */NULL, /*@ entity.Min */NULL, /*@ entity.Max */NULL, /*@ entity.Detail */NULL)
ON CONFLICT (SeriesId, TimeUnixNano) DO NOTHING
