DELETE FROM
    PushMessage
WHERE
    CreatedAt < /*@ before */''
