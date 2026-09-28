SELECT
    *
FROM
    Logs
WHERE
    SeverityNumber >= /*@ severity */17
ORDER BY
    TimeUnixNano DESC
LIMIT /*@ limit */20
