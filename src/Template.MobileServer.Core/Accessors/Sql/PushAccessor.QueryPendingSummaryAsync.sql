SELECT
    DeviceId, COUNT(*) AS Count
FROM
    PushMessage
WHERE
    DeliveredAt IS NULL
GROUP BY
    DeviceId
