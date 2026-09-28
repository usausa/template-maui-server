DELETE FROM
    Log
WHERE
    TimeUnixNano < /*@ before */0
