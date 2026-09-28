SELECT
    *
FROM
    Log
WHERE
    SeverityNumber >= /*@ severity */17
ORDER BY
    TimeUnixNano DESC
LIMIT /*@ limit */20
