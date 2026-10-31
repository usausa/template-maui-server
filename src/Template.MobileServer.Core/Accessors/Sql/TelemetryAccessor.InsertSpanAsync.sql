INSERT INTO Span (
    TraceId,
    SpanId,
    ParentSpanId,
    Name,
    Kind,
    StartTimeUnixNano,
    EndTimeUnixNano,
    StatusCode,
    StatusMessage,
    ScopeName,
    ResourceId,
    AttributesJson,
    EventsJson,
    LinksJson
) VALUES (
    /*@ entity.TraceId */'',
    /*@ entity.SpanId */'',
    /*@ entity.ParentSpanId */'',
    /*@ entity.Name */'',
    /*@ entity.Kind */'Internal',
    /*@ entity.StartTimeUnixNano */0,
    /*@ entity.EndTimeUnixNano */0,
    /*@ entity.StatusCode */'Unset',
    /*@ entity.StatusMessage */'',
    /*@ entity.ScopeName */'',
    /*@ entity.ResourceId */0,
    /*@ entity.AttributesJson */'',
    /*@ entity.EventsJson */NULL,
    /*@ entity.LinksJson */NULL
)
ON CONFLICT (TraceId, SpanId) DO NOTHING
