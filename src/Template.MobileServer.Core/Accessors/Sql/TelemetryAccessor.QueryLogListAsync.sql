SELECT
    *
FROM
    Log
WHERE
    TimeUnixNano >= /*@ since */0
    AND SeverityNumber >= /*@ severity */0
/*% if (body != null) { */
    AND Body LIKE /*@ body */'' ESCAPE '\'
/*% } */
/*% if (traceId != null) { */
    AND TraceId = /*@ traceId */''
/*% } */
/*% if (beforeTime != null) { */
    AND (TimeUnixNano < /*@ beforeTime */0 OR (TimeUnixNano = /*@ beforeTime */0 AND Id < /*@ beforeId */0))
/*% } */
ORDER BY
    TimeUnixNano DESC,
    Id DESC
LIMIT /*@ limit */100
