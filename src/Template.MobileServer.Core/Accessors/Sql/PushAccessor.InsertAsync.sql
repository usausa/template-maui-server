INSERT INTO
    PushMessage (DeviceId, Title, Body, CreatedAt)
SELECT
    DeviceId, /*@ title */'', /*@ body */'', /*@ createdAt */''
FROM
    Device
WHERE
    IsEnabled = 1
/*% if (deviceId != null) { */
    AND DeviceId = /*@ deviceId */''
/*% } */
ORDER BY
    DeviceId
RETURNING
    *
