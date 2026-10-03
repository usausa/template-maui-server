SELECT
    *
FROM
    PushMessage
WHERE
    DeviceId = /*@ deviceId */''
    AND DeliveredAt IS NULL
ORDER BY
    Id
