INSERT INTO
    Spans
    (TraceId, SpanId, ParentSpanId, Name, Kind, StartTimeUnixNano, EndTimeUnixNano, StatusCode, StatusMessage, ScopeName, ResourceId, AttributesJson, EventsJson, LinksJson)
VALUES
    (/*@ traceId */'', /*@ spanId */'', /*@ parentSpanId */'', /*@ name */'', /*@ kind */'Internal', /*@ startTimeUnixNano */0, /*@ endTimeUnixNano */0, /*@ statusCode */'Unset', /*@ statusMessage */'', /*@ scopeName */'', /*@ resourceId */0, /*@ attributesJson */'', /*@ eventsJson */NULL, /*@ linksJson */NULL)
ON CONFLICT (TraceId, SpanId) DO NOTHING
