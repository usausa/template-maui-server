SELECT
    TimeUnixNano / 3600000000000 AS Hour,
    COUNT(*) AS ErrorCount,
    SUM(CASE WHEN SeverityNumber >= 21 THEN 1 ELSE 0 END) AS CrashCount
FROM
    Log
WHERE
    TimeUnixNano >= /*@ since */0
    AND SeverityNumber >= 17
GROUP BY
    TimeUnixNano / 3600000000000
