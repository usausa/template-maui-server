INSERT INTO
    Logs
    (TimeUnixNano, ObservedTimeUnixNano, SeverityNumber, SeverityText, EventName, Body, TraceId, SpanId, ScopeName, ResourceId, AttributesJson, Hash)
VALUES
    (/*@ entity.TimeUnixNano */0, /*@ entity.ObservedTimeUnixNano */0, /*@ entity.SeverityNumber */0, /*@ entity.SeverityText */'', /*@ entity.EventName */'', /*@ entity.Body */'', /*@ entity.TraceId */'', /*@ entity.SpanId */'', /*@ entity.ScopeName */'', /*@ entity.ResourceId */0, /*@ entity.AttributesJson */'', /*@ entity.Hash */0)
ON CONFLICT (TimeUnixNano, Hash) DO NOTHING
RETURNING
    Id
